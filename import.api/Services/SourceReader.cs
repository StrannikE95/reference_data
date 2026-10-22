using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using import.api.Contracts;

namespace import.api.Services;

/// <summary>
/// Читает файл источника и собирает JSON для management.api.
/// Имена таблиц и колонок берутся из постановки, а не из текста файла.
/// </summary>
public sealed class SourceReader
{
    static readonly JsonSerializerOptions Json = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    static readonly string[] LevelAttributes = ["LEVEL", "NAME", "SHORTNAME", "ISACTIVE"];
    static readonly string[] AddressAttributes = ["ID", "OBJECTID", "OBJECTGUID", "NAME", "TYPENAME", "LEVEL", "ISACTUAL", "ISACTIVE", "STARTDATE", "ENDDATE"];

    readonly IConfiguration configuration;

    static SourceReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>Берёт каталог ImportRoot, внутри которого разрешены файлы.</summary>
    public SourceReader(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    /// <summary>
    /// Собирает одно сообщение метаданных. Строк в нём нет.
    /// Файл при этом читается: чужой XSD и пустой набор до JSON не доходят.
    /// </summary>
    public SourceLoad Metadata(string filePath, string sourceCode)
    {
        Console.WriteLine($"SourceReader.Metadata {sourceCode}");
        if (Locate(filePath, out var full) is { } error)
            return SourceLoad.Fail(error);
        var sourceFile = Path.GetFileName(full);
        var loadId = Guid.NewGuid().ToString("D");
        try
        {
            if (Check(full, sourceCode, metadata: true) is { } fail)
                return fail;
            var tables = Tables(sourceCode);
            var message = new Dictionary<string, object?>
            {
                ["loadId"] = loadId,
                ["sourceCode"] = sourceCode,
                ["sourceFile"] = sourceFile,
                ["tables"] = tables
            };
            return SourceLoad.Ok(JsonSerializer.Serialize(message, Json));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return SourceLoad.Fail("invalid_request", ex.Message);
        }
    }

    /// <summary>
    /// Собирает массив пачек строк. part идёт с 1, last стоит только на последней.
    /// Повтор ключа и пустой файл возвращаются до вызова management.api.
    /// </summary>
    public SourceLoad Data(string filePath, string sourceCode)
    {
        Console.WriteLine($"SourceReader.Data {sourceCode}");
        if (Locate(filePath, out var full) is { } error)
            return SourceLoad.Fail(error);
        var sourceFile = Path.GetFileName(full);
        var loadId = Guid.NewGuid().ToString("D");
        try
        {
            var read = ReadRows(full, sourceCode);
            if (read.Error is not null)
                return read.Error;
            var messages = (List<Dictionary<string, object?>>)read.Rows!;
            if (messages.Count == 0)
                return SourceLoad.Fail("empty");
            for (var i = 0; i < messages.Count; i++)
            {
                messages[i]["loadId"] = loadId;
                messages[i]["sourceCode"] = sourceCode;
                messages[i]["sourceFile"] = sourceFile;
                messages[i]["part"] = i + 1;
                messages[i]["last"] = i == messages.Count - 1;
            }
            if (read.SkippedLevel is not null)
                messages[^1]["skippedLevel"] = read.SkippedLevel;
            return SourceLoad.Ok(JsonSerializer.Serialize(messages, Json));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return SourceLoad.Fail("invalid_request", ex.Message);
        }
    }

    string? Locate(string filePath, out string full)
    {
        full = "";
        var root = configuration["ImportRoot"];
        if (string.IsNullOrWhiteSpace(root))
            return "invalid_request";
        full = Path.GetFullPath(filePath);
        var rootFull = Path.GetFullPath(root);
        var prefix = rootFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return "path_not_allowed";
        if (!File.Exists(full))
            return "invalid_request";
        return null;
    }

    SourceLoad? Check(string full, string sourceCode, bool metadata)
    {
        if (sourceCode is "ass-building" or "wall-material")
        {
            var parsed = Enumerations(full, sourceCode == "ass-building" ? "dAssBuilding" : "dWall");
            if (parsed.Error is not null)
                return parsed.Error;
            return parsed.Pairs!.Count == 0 ? SourceLoad.Fail("empty") : null;
        }
        if (sourceCode == "oktmo")
        {
            var parsed = Oktmo(full);
            if (parsed.Error is not null)
                return parsed.Error;
            return parsed.Rows!.Count == 0 ? SourceLoad.Fail("empty") : null;
        }
        var gar = Gar(full, metadata);
        return gar.Error;
    }

    RowRead ReadRows(string full, string sourceCode)
    {
        if (sourceCode == "ass-building")
            return EnumerationRows(full, "dAssBuilding", "ass_building", parent: false);
        if (sourceCode == "wall-material")
            return EnumerationRows(full, "dWall", "wall_material", parent: true);
        if (sourceCode == "oktmo")
            return OktmoRows(full);
        return GarRows(full);
    }

    RowRead EnumerationRows(string full, string typeName, string table, bool parent)
    {
        var parsed = Enumerations(full, typeName);
        if (parsed.Error is not null)
            return new RowRead(null, parsed.Error, null);
        var pairs = parsed.Pairs!;
        if (pairs.Count == 0)
            return new RowRead(null, SourceLoad.Fail("empty"), null);
        List<(string Code, string Name, string? Parent)> ordered;
        if (!parent)
        {
            ordered = pairs.Select(pair => (pair.Code, pair.Name, (string?)null)).ToList();
        }
        else
        {
            var codes = pairs.Select(pair => pair.Code).ToHashSet(StringComparer.Ordinal);
            var withParent = new List<(string Code, string Name, string? Parent)>();
            foreach (var pair in pairs)
            {
                var owner = WallParent(pair.Code, codes);
                if (owner == pair.Code)
                    return new RowRead(null, SourceLoad.Fail("parent_cycle", pair.Code), null);
                withParent.Add((pair.Code, pair.Name, owner));
            }
            var sorted = Order(withParent, row => row.Code, row => row.Parent);
            if (sorted is null)
                return new RowRead(null, SourceLoad.Fail("parent_cycle"), null);
            ordered = sorted;
        }
        var rows = ordered.Select(row =>
        {
            var item = new Dictionary<string, object?>
            {
                ["value_code"] = row.Code,
                ["name"] = row.Name
            };
            if (parent)
                item["parent_code"] = row.Parent;
            return item;
        }).ToList();
        return new RowRead(Batches(table, rows), null, null);
    }

    (List<(string Code, string Name)>? Pairs, SourceLoad? Error) Enumerations(string full, string typeName)
    {
        var document = XDocument.Load(full);
        var types = document.Descendants().Where(element => element.Name.LocalName == "simpleType" && (string?)element.Attribute("name") == typeName).ToList();
        if (types.Count != 1)
            return (null, SourceLoad.Fail("type_not_found", typeName));
        var restriction = types[0].Elements().FirstOrDefault(element => element.Name.LocalName == "restriction");
        var enums = restriction?.Elements().Where(element => element.Name.LocalName == "enumeration").ToList() ?? [];
        var pairs = new List<(string Code, string Name)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var missing = new List<string>();
        foreach (var item in enums)
        {
            var code = ((string?)item.Attribute("value") ?? "").Trim();
            if (code.Length != 12 || code.Any(symbol => symbol is < '0' or > '9'))
                return (null, SourceLoad.Fail("invalid_request", code));
            if (!seen.Add(code))
                return (null, SourceLoad.Fail("duplicate_code", code));
            var name = item.Descendants().FirstOrDefault(element => element.Name.LocalName == "documentation")?.Value?.Trim() ?? "";
            if (name.Length == 0)
            {
                missing.Add(code);
                continue;
            }
            if (name.Length > 500)
                return (null, SourceLoad.Fail("invalid_request", code));
            pairs.Add((code, name));
        }
        if (missing.Count > 0)
            return (null, SourceLoad.Fail("name_not_in_xsd", string.Join(",", missing)));
        return (pairs, null);
    }

    static string? WallParent(string code, HashSet<string> codes)
    {
        var chars = code.ToCharArray();
        for (var group = 3; group >= 0; group--)
        {
            var start = group * 3;
            if (chars[start] == '0' && chars[start + 1] == '0' && chars[start + 2] == '0')
                continue;
            chars[start] = chars[start + 1] = chars[start + 2] = '0';
            var candidate = new string(chars);
            if (codes.Contains(candidate))
                return candidate;
        }
        return null;
    }

    RowRead OktmoRows(string full)
    {
        var parsed = Oktmo(full);
        if (parsed.Error is not null)
            return new RowRead(null, parsed.Error, null);
        var rows = parsed.Rows!;
        if (rows.Count == 0)
            return new RowRead(null, SourceLoad.Fail("empty"), null);
        var sorted = Order(rows, row => row.Code + "|" + row.Section, row => row.Parent is null ? null : row.Parent + "|" + row.Section);
        if (sorted is null)
            return new RowRead(null, SourceLoad.Fail("parent_cycle"), null);
        var payload = sorted.Select(row => new Dictionary<string, object?>
        {
            ["value_code"] = row.Code,
            ["section"] = row.Section,
            ["name"] = row.Name,
            ["parent_code"] = row.Parent,
            ["change_number"] = row.Change,
            ["introduced_on"] = row.Introduced
        }).ToList();
        return new RowRead(Batches("oktmo", payload), null, null);
    }

    (List<(string Code, string Section, string Name, string? Parent, string Change, string Introduced)>? Rows, SourceLoad? Error) Oktmo(string full)
    {
        var lines = SplitLines(File.ReadAllText(full, Encoding.UTF8));
        if (lines.Take(20).Any(line => line.Contains('\uFFFD')))
            lines = SplitLines(File.ReadAllText(full, Encoding.GetEncoding(1251)));
        var rows = new List<(string Code, string Section, string Name, string? Parent, string Change, string Introduced)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var lineNo = 0;
        foreach (var line in lines)
        {
            lineNo++;
            if (line.Trim().Length == 0)
                continue;
            var fields = Fields(line);
            if (fields is null || fields.Count != 13)
                return (null, SourceLoad.Fail("bad_csv_row", lineNo.ToString(CultureInfo.InvariantCulture)));
            if (!Part(fields[0], 2) || !Part(fields[1], 3) || !Part(fields[2], 3) || !Part(fields[3], 3))
                return (null, SourceLoad.Fail("bad_code", lineNo.ToString(CultureInfo.InvariantCulture)));
            var section = fields[5].Trim();
            if (section is not ("1" or "2"))
                return (null, SourceLoad.Fail("bad_csv_row", lineNo.ToString(CultureInfo.InvariantCulture)));
            var name = fields[6].Trim();
            if (name.Length == 0)
                return (null, SourceLoad.Fail("empty_name", lineNo.ToString(CultureInfo.InvariantCulture)));
            if (name.Length > 500 || fields[9].Length > 10)
                return (null, SourceLoad.Fail("bad_csv_row", lineNo.ToString(CultureInfo.InvariantCulture)));
            if (!DateTime.TryParseExact(fields[12].Trim(), "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var introduced))
                return (null, SourceLoad.Fail("bad_date", lineNo.ToString(CultureInfo.InvariantCulture)));
            var code = fields[0] + fields[1] + fields[2] + fields[3];
            if (!seen.Add(code + "|" + section))
                return (null, SourceLoad.Fail("duplicate_code", code));
            rows.Add((code, section, name, null, fields[9], introduced.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        }
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < rows.Count; i++)
            index[rows[i].Code + "|" + rows[i].Section] = i;
        for (var i = 0; i < rows.Count; i++)
        {
            var parent = OktmoParent(rows[i].Code);
            if (parent is not null && index.ContainsKey(parent + "|" + rows[i].Section))
                rows[i] = (rows[i].Code, rows[i].Section, rows[i].Name, parent, rows[i].Change, rows[i].Introduced);
        }
        return (rows, null);
    }

    static string? OktmoParent(string code)
    {
        if (code.Substring(8, 3) != "000")
            return code.Substring(0, 8) + "000";
        if (code.Substring(5, 3) != "000")
            return code.Substring(0, 5) + "000000";
        if (code.Substring(2, 3) != "000")
            return code.Substring(0, 2) + "000000000";
        return null;
    }

    static List<string> SplitLines(string text) =>
        text.Split(["\r\n", "\n", "\r"], StringSplitOptions.None).ToList();

    static List<string>? Fields(string line)
    {
        var fields = new List<string>();
        var i = 0;
        while (i < line.Length)
        {
            if (line[i] == '"')
            {
                var value = new StringBuilder();
                i++;
                var closed = false;
                while (i < line.Length)
                {
                    if (line[i] == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            value.Append('"');
                            i += 2;
                            continue;
                        }
                        i++;
                        closed = true;
                        break;
                    }
                    value.Append(line[i]);
                    i++;
                }
                if (!closed)
                    return null;
                fields.Add(value.ToString());
                if (i >= line.Length)
                    break;
                if (line[i] != ';')
                    return null;
                i++;
                if (i == line.Length)
                    fields.Add("");
            }
            else
            {
                var end = line.IndexOf(';', i);
                if (end < 0)
                {
                    fields.Add(line[i..]);
                    break;
                }
                fields.Add(line[i..end]);
                i = end + 1;
                if (i == line.Length)
                    fields.Add("");
            }
        }
        return fields;
    }

    static bool Part(string value, int length) =>
        value.Length == length && value.All(symbol => symbol is >= '0' and <= '9');

    (SourceLoad? Error, int Skipped) Gar(string full, bool metadata)
    {
        using var zip = ZipFile.OpenRead(full);
        if (metadata)
        {
            var levels = Schema(zip, "AS_OBJECT_LEVELS");
            var address = Schema(zip, "AS_ADDR_OBJ");
            if (levels is null || address is null)
                return (SourceLoad.Fail("xsd_not_found"), 0);
            var missing = Missing(levels, LevelAttributes) ?? Missing(address, AddressAttributes);
            if (missing is not null)
                return (SourceLoad.Fail("xsd_attribute_missing", missing), 0);
            return (null, 0);
        }
        return (null, 0);
    }

    RowRead GarRows(string full)
    {
        using var zip = ZipFile.OpenRead(full);
        var levelRows = new List<Dictionary<string, object?>>();
        var levelCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in Entries(zip, "AS_OBJECT_LEVELS"))
        {
            var read = LevelFile(entry, levelRows, levelCodes);
            if (read is not null)
                return new RowRead(null, read, null);
        }
        var addressRows = new List<Dictionary<string, object?>>();
        var objectIds = new HashSet<long>();
        var skipped = 0;
        foreach (var entry in Entries(zip, "AS_ADDR_OBJ"))
        {
            var read = AddressFile(entry, addressRows, objectIds, levelCodes, ref skipped);
            if (read is not null)
                return new RowRead(null, read, null);
        }
        var messages = Batches("object_level", levelRows);
        messages.AddRange(Batches("addr_obj", addressRows));
        return new RowRead(messages, null, skipped);
    }

    static SourceLoad? LevelFile(ZipArchiveEntry entry, List<Dictionary<string, object?>> rows, HashSet<string> codes)
    {
        foreach (var attributes in Objects(entry))
        {
            var active = attributes.GetValueOrDefault("ISACTIVE");
            if (active is null)
                return SourceLoad.Fail("bad_gar_row", Path.GetFileName(entry.FullName));
            if (active != "1")
                continue;
            var code = Clean(attributes.GetValueOrDefault("LEVEL"));
            var name = Clean(attributes.GetValueOrDefault("NAME"));
            var shortName = Clean(attributes.GetValueOrDefault("SHORTNAME"));
            if (code.Length is 0 or > 10 || name.Length is 0 or > 250 || shortName.Length is 0 or > 50)
                return SourceLoad.Fail("bad_gar_row", Path.GetFileName(entry.FullName) + " " + code);
            if (!codes.Add(code))
                return SourceLoad.Fail("duplicate_code", code);
            rows.Add(new Dictionary<string, object?>
            {
                ["level_code"] = code,
                ["name"] = name,
                ["short_name"] = shortName
            });
        }
        return null;
    }

    static SourceLoad? AddressFile(ZipArchiveEntry entry, List<Dictionary<string, object?>> rows, HashSet<long> objectIds, HashSet<string> levels, ref int skipped)
    {
        var file = Path.GetFileName(entry.FullName);
        foreach (var attributes in Objects(entry))
        {
            var objectIdText = attributes.GetValueOrDefault("OBJECTID") ?? "";
            var actual = attributes.GetValueOrDefault("ISACTUAL");
            var active = attributes.GetValueOrDefault("ISACTIVE");
            if (actual is null || active is null)
                return SourceLoad.Fail("bad_gar_row", file + " " + objectIdText);
            if (actual != "1" || active != "1")
                continue;
            if (!long.TryParse(objectIdText, NumberStyles.None, CultureInfo.InvariantCulture, out var objectId))
                return SourceLoad.Fail("bad_gar_row", file + " " + objectIdText);
            var guidText = attributes.GetValueOrDefault("OBJECTGUID") ?? "";
            if (!Guid.TryParse(guidText, out var guid))
                return SourceLoad.Fail("bad_gar_row", file + " " + objectIdText);
            var name = Clean(attributes.GetValueOrDefault("NAME"));
            var typeName = Clean(attributes.GetValueOrDefault("TYPENAME"));
            var level = Clean(attributes.GetValueOrDefault("LEVEL"));
            var start = attributes.GetValueOrDefault("STARTDATE") ?? "";
            var end = attributes.GetValueOrDefault("ENDDATE") ?? "";
            if (name.Length is 0 or > 250 || typeName.Length is 0 or > 50 || level.Length is 0 or > 10)
                return SourceLoad.Fail("bad_gar_row", file + " " + objectIdText);
            if (!DateTime.TryParseExact(start, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                return SourceLoad.Fail("bad_gar_row", file + " " + objectIdText);
            if (!DateTime.TryParseExact(end, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                return SourceLoad.Fail("bad_gar_row", file + " " + objectIdText);
            if (!levels.Contains(level))
            {
                skipped++;
                continue;
            }
            if (!objectIds.Add(objectId))
                return SourceLoad.Fail("duplicate_code", objectIdText);
            rows.Add(new Dictionary<string, object?>
            {
                ["object_id"] = objectId,
                ["object_guid"] = guid.ToString("D"),
                ["name"] = name,
                ["type_name"] = typeName,
                ["level_code"] = level,
                ["start_date"] = start,
                ["end_date"] = end
            });
        }
        return null;
    }

    static IEnumerable<Dictionary<string, string>> Objects(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            IgnoreWhitespace = true,
            IgnoreComments = true,
            DtdProcessing = DtdProcessing.Prohibit
        });
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "OBJECT")
                continue;
            var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                    attributes[reader.Name] = reader.Value;
                reader.MoveToElement();
            }
            yield return attributes;
        }
    }

    static List<ZipArchiveEntry> Entries(ZipArchive zip, string prefix) =>
        zip.Entries
            .Where(entry =>
            {
                var name = Path.GetFileName(entry.FullName);
                return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase)
            .ToList();

    static XDocument? Schema(ZipArchive zip, string prefix)
    {
        var entry = zip.Entries
            .Where(item =>
            {
                var name = Path.GetFileName(item.FullName);
                return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && name.EndsWith(".xsd", StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(item => item.FullName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (entry is null)
            return null;
        using var stream = entry.Open();
        return XDocument.Load(stream);
    }

    static string? Missing(XDocument document, string[] names)
    {
        foreach (var name in names)
        {
            var found = document.Descendants().Any(element =>
                element.Name.LocalName == "attribute" && string.Equals((string?)element.Attribute("name"), name, StringComparison.Ordinal));
            if (!found)
                return name;
        }
        return null;
    }

    static string Clean(string? value) =>
        (value ?? "").Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ').Trim();

    static List<Dictionary<string, object?>> Tables(string sourceCode)
    {
        if (sourceCode == "ass-building")
            return [Table("ass_building",
                [Column("value_code", "string", 12, false), Column("name", "string", 500, false)],
                ["value_code"], [])];
        if (sourceCode == "wall-material")
            return [Table("wall_material",
                [Column("value_code", "string", 12, false), Column("name", "string", 500, false), Column("parent_code", "string", 12, true)],
                ["value_code"],
                [ForeignKey(["parent_code"], "wall_material", ["value_code"])])];
        if (sourceCode == "oktmo")
            return [Table("oktmo",
                [
                    Column("value_code", "string", 11, false),
                    Column("section", "string", 1, false),
                    Column("name", "string", 500, false),
                    Column("parent_code", "string", 11, true),
                    Column("change_number", "string", 10, false),
                    Column("introduced_on", "date", null, false)
                ],
                ["value_code", "section"],
                [ForeignKey(["parent_code", "section"], "oktmo", ["value_code", "section"])])];
        return
        [
            Table("object_level",
                [Column("level_code", "string", 10, false), Column("name", "string", 250, false), Column("short_name", "string", 50, false)],
                ["level_code"], []),
            Table("addr_obj",
                [
                    Column("object_id", "bigint", null, false),
                    Column("object_guid", "uuid", null, false),
                    Column("name", "string", 250, false),
                    Column("type_name", "string", 50, false),
                    Column("level_code", "string", 10, false),
                    Column("start_date", "date", null, false),
                    Column("end_date", "date", null, false)
                ],
                ["object_id"],
                [ForeignKey(["level_code"], "object_level", ["level_code"])])
        ];
    }

    static Dictionary<string, object?> Table(string name, List<Dictionary<string, object?>> columns, List<string> primaryKey, List<Dictionary<string, object?>> foreignKeys) =>
        new()
        {
            ["name"] = name,
            ["columns"] = columns,
            ["primaryKey"] = primaryKey,
            ["foreignKeys"] = foreignKeys
        };

    static Dictionary<string, object?> Column(string name, string type, int? length, bool nullable)
    {
        var column = new Dictionary<string, object?>
        {
            ["name"] = name,
            ["type"] = type,
            ["nullable"] = nullable
        };
        if (length is not null)
            column["length"] = length;
        return column;
    }

    static Dictionary<string, object?> ForeignKey(List<string> columns, string table, List<string> target) =>
        new()
        {
            ["columns"] = columns,
            ["references"] = new Dictionary<string, object?>
            {
                ["table"] = table,
                ["columns"] = target
            }
        };

    static List<Dictionary<string, object?>> Batches(string table, List<Dictionary<string, object?>> rows)
    {
        var messages = new List<Dictionary<string, object?>>();
        for (var offset = 0; offset < rows.Count; offset += 1000)
        {
            var count = Math.Min(1000, rows.Count - offset);
            messages.Add(new Dictionary<string, object?>
            {
                ["tables"] = new List<Dictionary<string, object?>>
                {
                    new()
                    {
                        ["name"] = table,
                        ["rows"] = rows.GetRange(offset, count)
                    }
                }
            });
        }
        return messages;
    }

    static List<T>? Order<T>(List<T> rows, Func<T, string> key, Func<T, string?> parent)
    {
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < rows.Count; i++)
            index[key(rows[i])] = i;
        var degree = new int[rows.Count];
        var children = new List<int>[rows.Count];
        for (var i = 0; i < rows.Count; i++)
            children[i] = [];
        for (var i = 0; i < rows.Count; i++)
        {
            var owner = parent(rows[i]);
            if (owner is null || !index.TryGetValue(owner, out var ownerIndex))
                continue;
            degree[i]++;
            children[ownerIndex].Add(i);
        }
        var queue = new Queue<int>();
        for (var i = 0; i < rows.Count; i++)
            if (degree[i] == 0)
                queue.Enqueue(i);
        var ordered = new List<T>(rows.Count);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            ordered.Add(rows[current]);
            foreach (var child in children[current])
            {
                degree[child]--;
                if (degree[child] == 0)
                    queue.Enqueue(child);
            }
        }
        return ordered.Count == rows.Count ? ordered : null;
    }

    sealed record RowRead(object? Rows, SourceLoad? Error, int? SkippedLevel);
}

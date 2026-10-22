using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using management.api.Contracts;
using db;

namespace management.api.Services;

/// <summary>
/// Собирает SQL из JSON прогона, записывает файл миграции и только потом выполняет его.
/// </summary>
public sealed class ImportService
{
    static readonly ConcurrentDictionary<string, string> MetadataBySource = new();

    readonly IConfiguration configuration;
    readonly string contentRoot;

    /// <summary>
    /// Берёт строку подключения и каталог проекта, чтобы найти db и JSON-схемы.
    /// </summary>
    public ImportService(IConfiguration configuration, string contentRoot)
    {
        this.configuration = configuration;
        this.contentRoot = contentRoot;
    }

    /// <summary>
    /// По JSON метаданных пишет родительские таблицы в db/sql-schema и выполняет файл.
    /// </summary>
    public ActionResult<ImportRunResponse> ApplyMetadata(string? json)
    {
        Console.WriteLine("Import.ApplyMetadata");
        if (string.IsNullOrWhiteSpace(json))
            return Bad("invalid_request");
        try
        {
            return CreateTables(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new ObjectResult(new AdminErrorResponse { Error = "script_failed", Detail = ex.Message }) { StatusCode = 500 };
        }
    }

    /// <summary>
    /// По JSON данных (одно сообщение или массив пачек) пишет секцию в db/sql-data и выполняет файл.
    /// versionId назначает модуль справочников, в JSON источника его нет.
    /// </summary>
    public ActionResult<ImportRunResponse> ApplyData(string? json, int? versionId)
    {
        Console.WriteLine($"Import.ApplyData versionId={versionId}");
        if (string.IsNullOrWhiteSpace(json) || versionId is null)
            return Bad("invalid_request");
        try
        {
            return FillSection(Messages(json), versionId.Value);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new ObjectResult(new AdminErrorResponse { Error = "script_failed", Detail = ex.Message }) { StatusCode = 500 };
        }
    }

    static List<string> Messages(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
            return doc.RootElement.EnumerateArray().Select(item => item.GetRawText()).ToList();
        return [json];
    }

    ActionResult<ImportRunResponse> CreateTables(string message)
    {
        var schemaPath = Path.Combine(contentRoot, "Contracts", "json", "schema", "dictionary.integration.metadata.schema.json");
        if (SchemaGuard.Metadata(message, schemaPath) is not null)
            return Bad("schema_invalid");

        using var doc = JsonDocument.Parse(message);
        var root = doc.RootElement;
        var source = root.GetProperty("sourceCode").GetString()!;
        var sql = new StringBuilder();
        sql.AppendLine("BEGIN;");
        if (source == "gar")
            sql.AppendLine("CREATE SCHEMA \"gar\";");
        foreach (var table in root.GetProperty("tables").EnumerateArray())
            sql.AppendLine(CreateTable(source, table));
        sql.AppendLine("COMMIT;");

        var path = NewFile(Path.Combine(RepoDb(), "sql-schema"), source, out var exists);
        if (exists)
            return Conflict(path);
        Write(path, sql.ToString());
        MetadataBySource[source] = message;
        Execute(path);
        return new OkObjectResult(new ImportRunResponse { Path = path });
    }

    ActionResult<ImportRunResponse> FillSection(List<string> messages, int versionId)
    {
        if (messages.Count == 0)
            return Bad("invalid_request");
        var schemaPath = Path.Combine(contentRoot, "Contracts", "json", "schema", "dictionary.integration.data.schema.json");
        var parsed = new List<JsonDocument>();
        try
        {
            foreach (var message in messages)
            {
                if (SchemaGuard.Data(message, schemaPath) is not null)
                    return Bad("schema_invalid");
                parsed.Add(JsonDocument.Parse(message));
            }
            parsed.Sort((left, right) => left.RootElement.GetProperty("part").GetInt32().CompareTo(right.RootElement.GetProperty("part").GetInt32()));
            for (var i = 0; i < parsed.Count; i++)
            {
                var part = parsed[i].RootElement.GetProperty("part").GetInt32();
                var last = parsed[i].RootElement.GetProperty("last").GetBoolean();
                if (part != i + 1 || last != (i == parsed.Count - 1))
                    return Bad("invalid_request");
            }

            var source = parsed[0].RootElement.GetProperty("sourceCode").GetString()!;
            if (!MetadataBySource.TryGetValue(source, out var metadataJson))
                return Bad("invalid_request");
            var columns = Columns(metadataJson);
            var keys = PrimaryKeys(metadataJson);
            if (Duplicate(parsed, keys))
                return Bad("duplicate_code");
            if (!AnyRow(parsed))
                return Bad("empty");

            var sql = new StringBuilder();
            sql.AppendLine("BEGIN;");
            var created = new HashSet<string>(StringComparer.Ordinal);
            foreach (var document in parsed)
            {
                foreach (var table in document.RootElement.GetProperty("tables").EnumerateArray())
                {
                    var name = table.GetProperty("name").GetString()!;
                    if (created.Add(name))
                        sql.AppendLine($"CREATE TABLE {Qualify(source, name + "_" + versionId.ToString(CultureInfo.InvariantCulture))} PARTITION OF {Qualify(source, name)} FOR VALUES IN ({versionId.ToString(CultureInfo.InvariantCulture)});");
                    var rows = table.GetProperty("rows");
                    if (rows.GetArrayLength() == 0)
                        continue;
                    if (!columns.TryGetValue(name, out var tableColumns))
                        return Bad("invalid_request");
                    sql.Append(Insert(source, name, versionId, tableColumns, rows));
                }
            }
            sql.AppendLine("COMMIT;");

            var path = NewFile(Path.Combine(RepoDb(), "sql-data", source), source, out var exists);
            if (exists)
                return Conflict(path);
            Write(path, sql.ToString());
            Execute(path);
            return new OkObjectResult(new ImportRunResponse { Path = path });
        }
        finally
        {
            foreach (var document in parsed)
                document.Dispose();
        }
    }

    static string CreateTable(string source, JsonElement table)
    {
        var name = table.GetProperty("name").GetString()!;
        var lines = new List<string> { "    \"version_id\" integer NOT NULL" };
        if (source != "gar")
            lines.Add("    \"simple_dictionary_table_id\" uuid NOT NULL");
        foreach (var column in table.GetProperty("columns").EnumerateArray())
        {
            var columnName = column.GetProperty("name").GetString()!;
            var sqlType = SqlType(column);
            var nullable = column.GetProperty("nullable").GetBoolean() ? "NULL" : "NOT NULL";
            lines.Add($"    {Quote(columnName)} {sqlType} {nullable}");
        }
        if (source == "gar")
        {
            var key = string.Join(", ", table.GetProperty("primaryKey").EnumerateArray().Select(item => Quote(item.GetString()!)).Prepend(Quote("version_id")));
            lines.Add($"    PRIMARY KEY ({key})");
        }
        else
        {
            lines.Add("    PRIMARY KEY (\"version_id\", \"simple_dictionary_table_id\")");
            var unique = string.Join(", ", table.GetProperty("primaryKey").EnumerateArray().Select(item => Quote(item.GetString()!)).Prepend(Quote("version_id")));
            lines.Add($"    UNIQUE ({unique})");
        }
        foreach (var foreignKey in table.GetProperty("foreignKeys").EnumerateArray())
        {
            var columns = string.Join(", ", foreignKey.GetProperty("columns").EnumerateArray().Select(item => Quote(item.GetString()!)).Prepend(Quote("version_id")));
            var references = foreignKey.GetProperty("references");
            var target = Qualify(source, references.GetProperty("table").GetString()!);
            var targetColumns = string.Join(", ", references.GetProperty("columns").EnumerateArray().Select(item => Quote(item.GetString()!)).Prepend(Quote("version_id")));
            lines.Add($"    FOREIGN KEY ({columns}) REFERENCES {target} ({targetColumns})");
        }
        return $"CREATE TABLE {Qualify(source, name)} (\n{string.Join(",\n", lines)}\n) PARTITION BY LIST (\"version_id\");";
    }

    static string Insert(string source, string table, int versionId, List<Column> columns, JsonElement rows)
    {
        var names = new List<string> { Quote("version_id") };
        if (source != "gar")
            names.Add(Quote("simple_dictionary_table_id"));
        names.AddRange(columns.Select(column => Quote(column.Name)));
        var sql = new StringBuilder();
        sql.Append("INSERT INTO ").Append(Qualify(source, table)).Append(" (").Append(string.Join(", ", names)).AppendLine(") VALUES");
        var index = 0;
        var count = rows.GetArrayLength();
        foreach (var row in rows.EnumerateArray())
        {
            var values = new List<string> { versionId.ToString(CultureInfo.InvariantCulture) };
            if (source != "gar")
                values.Add("gen_random_uuid()");
            foreach (var column in columns)
                values.Add(Literal(column, row));
            sql.Append("    (").Append(string.Join(", ", values)).Append(')');
            sql.AppendLine(++index == count ? ";" : ",");
        }
        return sql.ToString();
    }

    static string Literal(Column column, JsonElement row)
    {
        if (!row.TryGetProperty(column.Name, out var value) || value.ValueKind == JsonValueKind.Null)
            return "NULL";
        if (column.Type is "integer" or "bigint")
            return value.GetRawText();
        if (column.Type == "date")
            return "DATE '" + value.GetString() + "'";
        var text = value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText();
        return "'" + text.Replace('\r', ' ').Replace('\n', ' ').Replace("'", "''") + "'";
    }

    static Dictionary<string, List<Column>> Columns(string metadataJson)
    {
        using var doc = JsonDocument.Parse(metadataJson);
        var map = new Dictionary<string, List<Column>>(StringComparer.Ordinal);
        foreach (var table in doc.RootElement.GetProperty("tables").EnumerateArray())
        {
            var columns = table.GetProperty("columns").EnumerateArray().Select(column => new Column(
                column.GetProperty("name").GetString()!,
                column.GetProperty("type").GetString()!,
                column.GetProperty("nullable").GetBoolean())).ToList();
            map[table.GetProperty("name").GetString()!] = columns;
        }
        return map;
    }

    static Dictionary<string, List<string>> PrimaryKeys(string metadataJson)
    {
        using var doc = JsonDocument.Parse(metadataJson);
        var map = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var table in doc.RootElement.GetProperty("tables").EnumerateArray())
            map[table.GetProperty("name").GetString()!] = table.GetProperty("primaryKey").EnumerateArray().Select(item => item.GetString()!).ToList();
        return map;
    }

    static bool Duplicate(List<JsonDocument> messages, Dictionary<string, List<string>> keys)
    {
        var seen = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var document in messages)
        {
            foreach (var table in document.RootElement.GetProperty("tables").EnumerateArray())
            {
                var name = table.GetProperty("name").GetString()!;
                if (!keys.TryGetValue(name, out var key))
                    continue;
                if (!seen.TryGetValue(name, out var set))
                    seen[name] = set = new HashSet<string>(StringComparer.Ordinal);
                foreach (var row in table.GetProperty("rows").EnumerateArray())
                {
                    var token = string.Join("|", key.Select(column => row.TryGetProperty(column, out var value) ? value.ToString() : ""));
                    if (!set.Add(token))
                        return true;
                }
            }
        }
        return false;
    }

    static bool AnyRow(List<JsonDocument> messages) =>
        messages.Any(document => document.RootElement.GetProperty("tables").EnumerateArray().Any(table => table.GetProperty("rows").GetArrayLength() > 0));

    static string SqlType(JsonElement column)
    {
        var type = column.GetProperty("type").GetString();
        if (type == "string")
            return "varchar(" + column.GetProperty("length").GetInt32().ToString(CultureInfo.InvariantCulture) + ")";
        return type!;
    }

    static string Qualify(string source, string table) =>
        source == "gar" ? $"{Quote("gar")}.{Quote(table)}" : Quote(table);

    static string Quote(string name) => "\"" + name.Replace("\"", "\"\"") + "\"";

    string NewFile(string directory, string source, out bool exists)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, DateTime.Now.ToString("yyyy.MM.dd HH.mm", CultureInfo.InvariantCulture) + " - " + source + ".sql");
        exists = File.Exists(path);
        return path;
    }

    static void Write(string path, string sql)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(sql);
    }

    void Execute(string path)
    {
        Console.WriteLine($"Import.Execute {path}");
        using var connection = Db.Open(configuration);
        using var command = connection.CreateCommand();
        command.CommandTimeout = 0;
        command.CommandText = File.ReadAllText(path);
        command.ExecuteNonQuery();
    }

    string RepoDb() => Path.GetFullPath(Path.Combine(contentRoot, "..", "db"));

    static ActionResult<ImportRunResponse> Bad(string code, string? detail = null) =>
        new BadRequestObjectResult(new AdminErrorResponse { Error = code, Detail = detail });

    static ActionResult<ImportRunResponse> Conflict(string path) =>
        new ConflictObjectResult(new AdminErrorResponse { Error = "script_exists", Detail = path });

    sealed record Column(string Name, string Type, bool Nullable);
}

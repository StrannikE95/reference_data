using System.Text;
using System.Text.Json;
using Json.Schema;

public static class Load
{
    public static void CheckJson(string json, string schemaJson)
    {
        Console.WriteLine("Load.CheckJson");
        var schema = JsonSchema.FromText(schemaJson);
        var options = new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical };
        var result = schema.Evaluate(JsonDocument.Parse(json).RootElement, options);
        if (result.IsValid)
            return;

        var errors = new List<string>();
        Collect(result, errors);
        throw new Exception(errors.Count == 0 ? "json не прошёл схему" : string.Join("; ", errors));
    }

    public static string CsvToJson(string csv, string metadataJson, string schemaJson, string tableName, string sourceFile, int part, bool last, int? skippedLevel)
    {
        Console.WriteLine("Load.CsvToJson " + tableName);
        var meta = ReadMeta(metadataJson);
        var table = meta.Tables.First(t => t.Name == tableName);
        var rows = ReadCsv(csv);
        if (rows.Count == 0)
            throw new Exception("empty");
        var header = rows[0];
        if (header.Count != table.Columns.Count || header.Where((name, i) => name != table.Columns[i].Name).Any())
            throw new Exception("колонки csv не совпадают с metadata");

        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append("  \"loadId\": " + Json(meta.LoadId) + ",\n");
        sb.Append("  \"sourceCode\": " + Json(meta.SourceCode) + ",\n");
        sb.Append("  \"sourceFile\": " + Json(sourceFile) + ",\n");
        sb.Append("  \"part\": " + part + ",\n");
        sb.Append("  \"last\": " + (last ? "true" : "false"));
        if (skippedLevel != null)
            sb.Append(",\n  \"skippedLevel\": " + skippedLevel.Value);
        sb.Append(",\n  \"tables\": [\n    {\n      \"name\": " + Json(tableName) + ",\n      \"rows\": [");
        for (var r = 1; r < rows.Count; r++)
        {
            if (r > 1)
                sb.Append(",");
            sb.Append("\n        {");
            for (var c = 0; c < table.Columns.Count; c++)
            {
                if (c > 0)
                    sb.Append(", ");
                var col = table.Columns[c];
                var raw = c < rows[r].Count ? rows[r][c] : "";
                sb.Append(Json(col.Name) + ": " + JsonValue(col, raw));
            }
            sb.Append("}");
        }
        sb.Append("\n      ]\n    }\n  ]\n}\n");
        var json = sb.ToString();
        CheckJson(json, schemaJson);
        return json;
    }

    public static string DataToCsv(string dataJson, string metadataJson)
    {
        Console.WriteLine("Load.DataToCsv");
        var meta = ReadMeta(metadataJson);
        using var doc = JsonDocument.Parse(dataJson);
        var tableEl = doc.RootElement.GetProperty("tables")[0];
        var tableName = tableEl.GetProperty("name").GetString();
        var table = meta.Tables.First(t => t.Name == tableName);
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(";", table.Columns.Select(c => c.Name)));
        foreach (var row in tableEl.GetProperty("rows").EnumerateArray())
        {
            var cells = new List<string>();
            foreach (var col in table.Columns)
            {
                if (!row.TryGetProperty(col.Name, out var value) || value.ValueKind == JsonValueKind.Null)
                    cells.Add("");
                else if (value.ValueKind == JsonValueKind.Number)
                    cells.Add(value.GetRawText());
                else
                    cells.Add(EscapeCsv(value.GetString() ?? ""));
            }
            sb.AppendLine(string.Join(";", cells));
        }
        return sb.ToString();
    }

    public static string MetadataToCreateSql(string metadataJson)
    {
        Console.WriteLine("Load.MetadataToCreateSql");
        var meta = ReadMeta(metadataJson);
        var set = meta.Tables.Count > 1;
        var sb = new StringBuilder();
        if (set)
            sb.Append("CREATE SCHEMA " + meta.SourceCode + ";\n\n");
        foreach (var table in meta.Tables)
        {
            var q = set ? meta.SourceCode + "." + table.Name : table.Name;
            sb.Append("CREATE TABLE " + q + " (\n");
            sb.Append("    version_id integer NOT NULL");
            if (!set)
                sb.Append(",\n    simple_dictionary_table_id uuid NOT NULL");
            foreach (var col in table.Columns)
                sb.Append(",\n    " + col.Name + " " + SqlType(col) + " " + (col.Nullable ? "NULL" : "NOT NULL"));
            if (set)
                sb.Append(",\n    PRIMARY KEY (version_id, " + string.Join(", ", table.PrimaryKey) + ")");
            else
            {
                sb.Append(",\n    PRIMARY KEY (version_id, simple_dictionary_table_id)");
                sb.Append(",\n    UNIQUE (version_id, " + string.Join(", ", table.PrimaryKey) + ")");
            }
            foreach (var fk in table.ForeignKeys)
            {
                var refName = set ? meta.SourceCode + "." + fk.Table : fk.Table;
                sb.Append(",\n    FOREIGN KEY (version_id, " + string.Join(", ", fk.Columns) + ")");
                sb.Append("\n        REFERENCES " + refName + " (version_id, " + string.Join(", ", fk.RefColumns) + ")");
            }
            sb.Append("\n) PARTITION BY LIST (version_id);\n\n");
        }
        return sb.ToString();
    }

    public static string DataToInsertSql(string dataJson, string metadataJson, int versionId)
    {
        Console.WriteLine("Load.DataToInsertSql " + versionId);
        var meta = ReadMeta(metadataJson);
        var set = meta.Tables.Count > 1;
        using var doc = JsonDocument.Parse(dataJson);
        var sb = new StringBuilder();
        foreach (var tableEl in doc.RootElement.GetProperty("tables").EnumerateArray())
        {
            var tableName = tableEl.GetProperty("name").GetString();
            var table = meta.Tables.First(t => t.Name == tableName);
            var q = set ? meta.SourceCode + "." + table.Name : table.Name;
            sb.Append("CREATE TABLE " + q + "_" + versionId + "\n");
            sb.Append("    PARTITION OF " + q + " FOR VALUES IN (" + versionId + ");\n\n");
            var cols = new List<string> { "version_id" };
            if (!set)
                cols.Add("simple_dictionary_table_id");
            cols.AddRange(table.Columns.Select(c => c.Name));
            sb.Append("INSERT INTO " + q + " (" + string.Join(", ", cols) + ") VALUES\n");
            var rowList = tableEl.GetProperty("rows").EnumerateArray().ToList();
            for (var i = 0; i < rowList.Count; i++)
            {
                var row = rowList[i];
                sb.Append("    (" + versionId);
                if (!set)
                    sb.Append(", gen_random_uuid()");
                foreach (var col in table.Columns)
                {
                    sb.Append(", ");
                    if (!row.TryGetProperty(col.Name, out var value) || value.ValueKind == JsonValueKind.Null)
                        sb.Append("NULL");
                    else if (col.Type == "integer" || col.Type == "bigint")
                        sb.Append(value.GetRawText());
                    else if (col.Type == "date")
                        sb.Append("DATE " + SqlString(value.GetString()));
                    else
                        sb.Append(SqlString(value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText()));
                }
                sb.Append(i + 1 < rowList.Count ? "),\n" : ");\n\n");
            }
        }
        return sb.ToString();
    }

    static void Collect(EvaluationResults result, List<string> errors)
    {
        if (result.Errors != null)
            foreach (var error in result.Errors)
                errors.Add(result.InstanceLocation + " " + error.Value);
        if (result.Details == null)
            return;
        foreach (var detail in result.Details)
            Collect(detail, errors);
    }

    static string JsonValue(MetaColumn col, string raw)
    {
        if (raw == "")
            return "null";
        if (col.Type == "integer" || col.Type == "bigint")
            return raw;
        return Json(raw);
    }

    static string Json(string text)
    {
        return JsonSerializer.Serialize(text);
    }

    static string SqlType(MetaColumn col)
    {
        if (col.Type == "string")
            return "varchar(" + col.Length + ")";
        return col.Type;
    }

    static string SqlString(string text)
    {
        if (text == null)
            return "NULL";
        return "'" + text.Replace("'", "''").Replace("\r", " ").Replace("\n", " ") + "'";
    }

    static string EscapeCsv(string text)
    {
        if (text.Contains(';') || text.Contains('"') || text.Contains('\n') || text.Contains('\r'))
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        return text;
    }

    static List<List<string>> ReadCsv(string csv)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < csv.Length; i++)
        {
            var ch = csv[i];
            if (quoted)
            {
                if (ch == '"')
                {
                    if (i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                        quoted = false;
                }
                else
                    cell.Append(ch);
                continue;
            }
            if (ch == '"')
            {
                quoted = true;
                continue;
            }
            if (ch == ';')
            {
                row.Add(cell.ToString());
                cell.Clear();
                continue;
            }
            if (ch == '\n')
            {
                row.Add(cell.ToString());
                cell.Clear();
                if (row.Count > 1 || row[0] != "")
                    rows.Add(row);
                row = new List<string>();
                continue;
            }
            if (ch != '\r')
                cell.Append(ch);
        }
        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            rows.Add(row);
        }
        return rows;
    }

    static Meta ReadMeta(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var meta = new Meta
        {
            LoadId = root.GetProperty("loadId").GetString(),
            SourceCode = root.GetProperty("sourceCode").GetString(),
            Tables = new List<MetaTable>()
        };
        foreach (var tableEl in root.GetProperty("tables").EnumerateArray())
        {
            var table = new MetaTable
            {
                Name = tableEl.GetProperty("name").GetString(),
                Columns = new List<MetaColumn>(),
                PrimaryKey = tableEl.GetProperty("primaryKey").EnumerateArray().Select(x => x.GetString()).ToList(),
                ForeignKeys = new List<MetaFk>()
            };
            foreach (var colEl in tableEl.GetProperty("columns").EnumerateArray())
            {
                table.Columns.Add(new MetaColumn
                {
                    Name = colEl.GetProperty("name").GetString(),
                    Type = colEl.GetProperty("type").GetString(),
                    Length = colEl.TryGetProperty("length", out var length) ? length.GetInt32() : 0,
                    Nullable = colEl.GetProperty("nullable").GetBoolean()
                });
            }
            foreach (var fkEl in tableEl.GetProperty("foreignKeys").EnumerateArray())
            {
                var refs = fkEl.GetProperty("references");
                table.ForeignKeys.Add(new MetaFk
                {
                    Columns = fkEl.GetProperty("columns").EnumerateArray().Select(x => x.GetString()).ToList(),
                    Table = refs.GetProperty("table").GetString(),
                    RefColumns = refs.GetProperty("columns").EnumerateArray().Select(x => x.GetString()).ToList()
                });
            }
            meta.Tables.Add(table);
        }
        return meta;
    }

    class Meta
    {
        public string LoadId;
        public string SourceCode;
        public List<MetaTable> Tables;
    }

    class MetaTable
    {
        public string Name;
        public List<MetaColumn> Columns;
        public List<string> PrimaryKey;
        public List<MetaFk> ForeignKeys;
    }

    class MetaColumn
    {
        public string Name;
        public string Type;
        public int Length;
        public bool Nullable;
    }

    class MetaFk
    {
        public List<string> Columns;
        public string Table;
        public List<string> RefColumns;
    }
}

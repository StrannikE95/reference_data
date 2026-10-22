using System.Text.Json;

namespace management.api.Services;

/// <summary>
/// Сверяет сообщение с файлами schema до записи SQL.
/// Коды источников и типы колонок читает из самих схем, не из зашитого списка.
/// </summary>
public static class SchemaGuard
{
    static readonly string[] MetadataRoot = ["loadId", "sourceCode", "sourceFile", "tables"];
    static readonly string[] DataRoot = ["loadId", "sourceCode", "sourceFile", "part", "last", "tables", "skippedLevel"];
    static readonly string[] TableFields = ["name", "columns", "primaryKey", "foreignKeys"];
    static readonly string[] ColumnFields = ["name", "type", "nullable", "length"];
    static readonly string[] ForeignKeyFields = ["columns", "references"];
    static readonly string[] ReferenceFields = ["table", "columns"];
    static readonly string[] DataTableFields = ["name", "rows"];

    /// <summary>
    /// Проверяет сообщение метаданных. Пустая строка — сообщение годится.
    /// </summary>
    public static string? Metadata(string json, string schemaPath)
    {
        var allowed = ReadEnum(schemaPath, "sourceCode");
        var types = ReadDefEnum(schemaPath, "column", "type");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            return "schema_invalid";
        if (Extra(root, MetadataRoot) is not null)
            return "schema_invalid";
        if (!Uuid(root, "loadId") || !Text(root, "sourceFile") || !Allowed(root, "sourceCode", allowed))
            return "schema_invalid";
        if (!root.TryGetProperty("tables", out var tables) || tables.ValueKind != JsonValueKind.Array || tables.GetArrayLength() < 1)
            return "schema_invalid";
        foreach (var table in tables.EnumerateArray())
        {
            if (table.ValueKind != JsonValueKind.Object || Extra(table, TableFields) is not null || !Text(table, "name"))
                return "schema_invalid";
            if (!table.TryGetProperty("columns", out var columns) || columns.ValueKind != JsonValueKind.Array || columns.GetArrayLength() < 1)
                return "schema_invalid";
            foreach (var column in columns.EnumerateArray())
            {
                if (column.ValueKind != JsonValueKind.Object || Extra(column, ColumnFields) is not null || !Text(column, "name"))
                    return "schema_invalid";
                if (!column.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String || !types.Contains(type.GetString()!))
                    return "schema_invalid";
                if (!column.TryGetProperty("nullable", out var nullable) || nullable.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return "schema_invalid";
                var isString = type.GetString() == "string";
                var hasLength = column.TryGetProperty("length", out var length) && length.ValueKind == JsonValueKind.Number && length.TryGetInt32(out var size) && size >= 1;
                if (isString && !hasLength)
                    return "schema_invalid";
            }
            if (!StringArray(table, "primaryKey", requiredMin: 1))
                return "schema_invalid";
            if (!table.TryGetProperty("foreignKeys", out var keys) || keys.ValueKind != JsonValueKind.Array)
                return "schema_invalid";
            foreach (var key in keys.EnumerateArray())
            {
                if (key.ValueKind != JsonValueKind.Object || Extra(key, ForeignKeyFields) is not null || !StringArray(key, "columns", 1))
                    return "schema_invalid";
                if (!key.TryGetProperty("references", out var references) || references.ValueKind != JsonValueKind.Object || Extra(references, ReferenceFields) is not null)
                    return "schema_invalid";
                if (!Text(references, "table") || !StringArray(references, "columns", 1))
                    return "schema_invalid";
            }
        }
        return null;
    }

    /// <summary>
    /// Проверяет пачку строк. Пустая строка — сообщение годится.
    /// </summary>
    public static string? Data(string json, string schemaPath)
    {
        var allowed = ReadEnum(schemaPath, "sourceCode");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object || Extra(root, DataRoot) is not null)
            return "schema_invalid";
        if (!Uuid(root, "loadId") || !Text(root, "sourceFile") || !Allowed(root, "sourceCode", allowed))
            return "schema_invalid";
        if (!root.TryGetProperty("part", out var part) || !part.TryGetInt32(out var partNo) || partNo < 1)
            return "schema_invalid";
        if (!root.TryGetProperty("last", out var last) || last.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            return "schema_invalid";
        if (root.TryGetProperty("skippedLevel", out var skipped) && (!skipped.TryGetInt32(out var skippedNo) || skippedNo < 0))
            return "schema_invalid";
        if (!root.TryGetProperty("tables", out var tables) || tables.ValueKind != JsonValueKind.Array || tables.GetArrayLength() < 1)
            return "schema_invalid";
        foreach (var table in tables.EnumerateArray())
        {
            if (table.ValueKind != JsonValueKind.Object || Extra(table, DataTableFields) is not null || !Text(table, "name"))
                return "schema_invalid";
            if (!table.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > 1000)
                return "schema_invalid";
            foreach (var row in rows.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object)
                    return "schema_invalid";
            }
        }
        return null;
    }

    static HashSet<string> ReadEnum(string schemaPath, string property)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(schemaPath));
        return doc.RootElement.GetProperty("properties").GetProperty(property).GetProperty("enum")
            .EnumerateArray().Select(item => item.GetString()!).ToHashSet(StringComparer.Ordinal);
    }

    static HashSet<string> ReadDefEnum(string schemaPath, string definition, string property)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(schemaPath));
        return doc.RootElement.GetProperty("$defs").GetProperty(definition).GetProperty("properties").GetProperty(property).GetProperty("enum")
            .EnumerateArray().Select(item => item.GetString()!).ToHashSet(StringComparer.Ordinal);
    }

    static string? Extra(JsonElement obj, string[] known)
    {
        foreach (var property in obj.EnumerateObject())
        {
            if (!known.Contains(property.Name))
                return "schema_invalid";
        }
        return null;
    }

    static bool Uuid(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out _);

    static bool Text(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && value.GetString()!.Length > 0;

    static bool Allowed(JsonElement obj, string name, HashSet<string> allowed) =>
        obj.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && allowed.Contains(value.GetString()!);

    static bool StringArray(JsonElement obj, string name, int requiredMin)
    {
        if (!obj.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array || value.GetArrayLength() < requiredMin)
            return false;
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || item.GetString()!.Length < 1)
                return false;
        }
        return true;
    }
}

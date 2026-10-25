using System.Text.Json;

var v3 = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var schemaDir = Path.Combine(v3, "settings", "json-schema");
var metaSchema = File.ReadAllText(Path.Combine(schemaDir, "dictionary.integration.metadata.schema.json"));
var dataSchema = File.ReadAllText(Path.Combine(schemaDir, "dictionary.integration.data.schema.json"));
var testData = Path.Combine(v3, "test-data");

foreach (var dir in Directory.GetDirectories(testData))
{
    if (Path.GetFileName(dir) == "example")
        continue;
    var metaPath = Directory.GetFiles(dir, "*.json").FirstOrDefault(f => Path.GetFileName(f).Contains("metadata."));
    if (metaPath == null)
        continue;
    var metadata = File.ReadAllText(metaPath);
    Load.CheckJson(metadata, metaSchema);
    File.WriteAllText(Path.Combine(dir, "1.2. create.sql"), Load.MetadataToCreateSql(metadata));

    var dataFiles = Directory.GetFiles(dir, "*.json")
        .Where(f => Path.GetFileName(f).Contains("data."))
        .Select(f => (path: f, part: JsonDocument.Parse(File.ReadAllText(f)).RootElement.GetProperty("part").GetInt32()))
        .OrderBy(f => f.part)
        .ToList();
    var group = 2;
    foreach (var dataFile in dataFiles)
    {
        var data = File.ReadAllText(dataFile.path);
        Load.CheckJson(data, dataSchema);
        using var doc = JsonDocument.Parse(data);
        var root = doc.RootElement;
        var tableName = root.GetProperty("tables")[0].GetProperty("name").GetString();
        int? skipped = root.TryGetProperty("skippedLevel", out var skippedEl) ? skippedEl.GetInt32() : null;
        var csv = Load.DataToCsv(data, metadata);
        var stem = StripNumber(Path.GetFileNameWithoutExtension(dataFile.path));
        File.WriteAllText(Path.Combine(dir, group + ".1. " + stem + ".csv"), csv);
        var json = Load.CsvToJson(
            csv,
            metadata,
            dataSchema,
            tableName,
            root.GetProperty("sourceFile").GetString(),
            root.GetProperty("part").GetInt32(),
            root.GetProperty("last").GetBoolean(),
            skipped);
        using var back = JsonDocument.Parse(json);
        if (!Same(back.RootElement, root))
            throw new Exception("csv не совпал с " + stem);
        File.WriteAllText(Path.Combine(dir, group + ".3. " + stem + ".sql"), Load.DataToInsertSql(data, metadata, 1));
        group++;
    }
}

Console.WriteLine("ok " + testData);

static string StripNumber(string name)
{
    if (name.Length == 0 || !char.IsDigit(name[0]))
        return name;
    var space = name.IndexOf(' ');
    if (space < 0)
        return name;
    return name.Substring(space + 1);
}

static bool Same(JsonElement left, JsonElement right)
{
    if (left.ValueKind != right.ValueKind)
        return false;
    if (left.ValueKind == JsonValueKind.Object)
    {
        var names = left.EnumerateObject().Select(p => p.Name).ToList();
        if (names.Count != right.EnumerateObject().Count())
            return false;
        foreach (var name in names)
        {
            if (!right.TryGetProperty(name, out var other) || !Same(left.GetProperty(name), other))
                return false;
        }
        return true;
    }
    if (left.ValueKind == JsonValueKind.Array)
    {
        var a = left.EnumerateArray().ToList();
        var b = right.EnumerateArray().ToList();
        if (a.Count != b.Count)
            return false;
        for (var i = 0; i < a.Count; i++)
            if (!Same(a[i], b[i]))
                return false;
        return true;
    }
    if (left.ValueKind == JsonValueKind.Number)
        return left.GetRawText() == right.GetRawText();
    if (left.ValueKind == JsonValueKind.String)
        return left.GetString() == right.GetString();
    return left.GetRawText() == right.GetRawText();
}

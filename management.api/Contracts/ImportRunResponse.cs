using System.Text.Json.Serialization;

namespace management.api.Contracts;

/// <summary>
/// Путь к уже записанному SQL-файлу. Скрипт к этому моменту выполнен.
/// </summary>
public sealed class ImportRunResponse
{
    [JsonPropertyName("path")]
    public required string Path { get; init; }
}

using System.Text.Json.Serialization;

namespace management.api.Contracts;

/// <summary>
/// Ошибка команды администратора. Это не пустой список и не файл в состоянии invalid.
/// </summary>
public sealed class AdminErrorResponse
{
    public required string Error { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DictionaryCode { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? MissingTables { get; init; }
}

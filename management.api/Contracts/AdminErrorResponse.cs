using System.Text.Json.Serialization;

namespace management.api.Contracts;

/// <summary>
/// Ошибка команды администратора.
/// </summary>
public sealed class AdminErrorResponse
{
    public required string Error { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DictionaryCode { get; init; }

    /// <summary>Уточнение к коду: номера строк, коды без названия, текст сбоя скрипта.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; init; }
}

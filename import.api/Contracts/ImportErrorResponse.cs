using System.Text.Json.Serialization;

namespace import.api.Contracts;

/// <summary>
/// Ошибка разбора файла. До этого вызова management.api JSON не уходит.
/// </summary>
public sealed class ImportErrorResponse
{
    /// <summary>Код: invalid_request, path_not_allowed, duplicate_code, empty и коды разбора источника.</summary>
    public required string Error { get; init; }

    /// <summary>Номер строки, коды без названия или текст сбоя вызова management.api.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; init; }
}

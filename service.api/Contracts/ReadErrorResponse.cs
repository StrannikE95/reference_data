using System.Text.Json.Serialization;

namespace service.api.Contracts;

/// <summary>
/// Ошибка чтения, когда редакции нет или форма запроса неверна.
/// Это не пустая страница: вызывающий не должен принять её за справочник без строк.
/// </summary>
public sealed class ReadErrorResponse
{
    public required string Error { get; init; }

    public required string DictionaryCode { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateOnly? OnDate { get; init; }
}

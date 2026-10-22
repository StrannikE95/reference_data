namespace import.api.Contracts;

/// <summary>
/// JSON, который уходит в management.api, либо код ошибки разбора.
/// </summary>
public sealed class SourceLoad
{
    /// <summary>Тело dictionary.integration.metadata или массив dictionary.integration.data.</summary>
    public string? Json { get; init; }

    /// <summary>Код ошибки. Пока он задан, JSON в management.api не передаётся.</summary>
    public string? Error { get; init; }

    /// <summary>Уточнение к коду: строка CSV, код без названия, имя файла ГАР.</summary>
    public string? Detail { get; init; }

    /// <summary>Разбор закончился JSON, который можно отдать management.api.</summary>
    public static SourceLoad Ok(string json) => new() { Json = json };

    /// <summary>Разбор остановился. Файл SQL из-за этого не создаётся.</summary>
    public static SourceLoad Fail(string error, string? detail = null) => new() { Error = error, Detail = detail };
}

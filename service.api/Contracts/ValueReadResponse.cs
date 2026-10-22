namespace service.api.Contracts;

/// <summary>
/// Ответ чтения одного кода. Подпись есть только при result = present, чтобы отсутствующий код не получал чужое имя.
/// version_id — номер редакции, которую модуль фактически прочитал.
/// </summary>
public sealed class ValueReadResponse
{
    public required string DictionaryCode { get; init; }

    public int VersionId { get; init; }

    public required string VersionStatus { get; init; }

    public DateOnly ValidFrom { get; init; }

    public DateOnly? ValidTo { get; init; }

    public bool Archived { get; init; }

    public required string Result { get; init; }

    public required string ValueCode { get; init; }

    public string? Name { get; init; }
}

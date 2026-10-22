namespace service.api.Contracts;

/// <summary>
/// Страница редакции, упорядоченная по value_code.
/// Пустой next_cursor означает конец списка, а не отсутствие действующей версии.
/// </summary>
public sealed class ValuePageResponse
{
    public required string DictionaryCode { get; init; }

    public int VersionId { get; init; }

    public required string VersionStatus { get; init; }

    public DateOnly ValidFrom { get; init; }

    public DateOnly? ValidTo { get; init; }

    public bool Archived { get; init; }

    public required IReadOnlyList<ValueItem> Items { get; init; }

    public string? NextCursor { get; init; }
}

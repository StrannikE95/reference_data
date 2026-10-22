namespace management.api.Contracts;

/// <summary>
/// Страница паспортов по dictionary_code. Пустой next_cursor — конец списка.
/// </summary>
public sealed class DictionaryListResponse
{
    public required IReadOnlyList<DictionaryPassport> Items { get; init; }

    public string? NextCursor { get; init; }
}

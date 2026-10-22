namespace service.api.Contracts;

/// <summary>
/// Ответ проверки ссылок. Чужой код не отменяет остальные: у каждой строки свой result.
/// </summary>
public sealed class CheckValuesResponse
{
    public required string DictionaryCode { get; init; }

    public int VersionId { get; init; }

    public required string VersionStatus { get; init; }

    public DateOnly ValidFrom { get; init; }

    public DateOnly? ValidTo { get; init; }

    public bool Archived { get; init; }

    public required IReadOnlyList<CheckValueItem> Items { get; init; }
}

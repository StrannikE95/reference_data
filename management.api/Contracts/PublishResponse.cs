namespace management.api.Contracts;

/// <summary>
/// Результат публикации. У первой версии closed_version_id и closed_valid_to пусты; здесь пример публикации поверх уже действующей.
/// </summary>
public sealed class PublishResponse
{
    public required string DictionaryCode { get; init; }

    public int VersionId { get; init; }

    public required string VersionStatus { get; init; }

    public DateOnly ValidFrom { get; init; }

    public DateOnly? ValidTo { get; init; }

    public int? ClosedVersionId { get; init; }

    public DateOnly? ClosedValidTo { get; init; }
}

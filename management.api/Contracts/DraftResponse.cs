namespace management.api.Contracts;

/// <summary>
/// Черновик версии и его файлы. Сервис чтения эти строки не отдаёт, пока версия не опубликована.
/// </summary>
public sealed class DraftResponse
{
    public required string DictionaryCode { get; init; }

    public int VersionId { get; init; }

    public required string VersionStatus { get; init; }

    public required string Delivery { get; init; }

    public required IReadOnlyList<FileDescription> Files { get; init; }
}

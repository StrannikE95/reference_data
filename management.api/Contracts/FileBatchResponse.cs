namespace management.api.Contracts;

/// <summary>
/// Описания файлов пачки со ссылкой на запись, если запись ещё не подтверждена.
/// </summary>
public sealed class FileBatchResponse
{
    public required IReadOnlyList<FileDescription> Files { get; init; }
}

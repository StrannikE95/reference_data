namespace management.api.Contracts;

/// <summary>
/// Пачка от 1 до 100 файлов. Следующий запрос дополняет тот же набор.
/// </summary>
public sealed class FileBatchRequest
{
    public List<FileUpload>? Files { get; init; }
}

namespace management.api.Contracts;

/// <summary>
/// Описание файла в пачке до выдачи ссылки: имя в наборе и размер.
/// </summary>
public sealed class FileUpload
{
    public string? Name { get; init; }

    public long Size { get; init; }
}

namespace service.api.Contracts;

/// <summary>
/// Проверка ссылок по уже сохранённому version_id. Даты нет: смотрит секцию этой версии и ничего не записывает.
/// </summary>
public sealed class CheckValuesRequest
{
    public int VersionId { get; init; }

    public List<string>? ValueCodes { get; init; }
}

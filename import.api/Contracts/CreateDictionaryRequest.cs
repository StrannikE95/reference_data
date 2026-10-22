namespace import.api.Contracts;

/// <summary>
/// Создание нового справочника: путь к уже скачанному файлу источника.
/// </summary>
public sealed class CreateDictionaryRequest
{
    /// <summary>Полный путь к XSD, CSV или ZIP. Файл должен лежать внутри ImportRoot.</summary>
    public string? FilePath { get; init; }

    /// <summary>Источник: ass-building, wall-material, oktmo или gar.</summary>
    public string? SourceCode { get; init; }
}

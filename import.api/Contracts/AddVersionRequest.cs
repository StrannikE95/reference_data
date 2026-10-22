namespace import.api.Contracts;

/// <summary>
/// Новая версия уже созданного справочника. Номер версии в файл источника не входит.
/// </summary>
public sealed class AddVersionRequest
{
    /// <summary>Полный путь к XSD, CSV или ZIP. Файл должен лежать внутри ImportRoot.</summary>
    public string? FilePath { get; init; }

    /// <summary>Источник: ass-building, wall-material, oktmo или gar.</summary>
    public string? SourceCode { get; init; }

    /// <summary>Номер секции. Его назначает модуль справочников и передаёт в management.api.</summary>
    public int? VersionId { get; init; }
}

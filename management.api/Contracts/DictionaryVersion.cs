namespace management.api.Contracts;

/// <summary>
/// Строка dictionary_version в ответе паспорта. Строки значений и секции сюда не входят.
/// </summary>
public sealed class DictionaryVersion
{
    public Guid DictionaryVersionId { get; init; }

    public int VersionId { get; init; }

    public required string Status { get; init; }

    public DateOnly? ValidFrom { get; init; }

    public DateOnly? ValidTo { get; init; }

    public string? TableName { get; init; }

    public string? SchemaName { get; init; }
}

using System.Text.Json.Serialization;

namespace management.api.Contracts;

/// <summary>
/// Паспорт справочника. В списке каталога versions нет: там только регистрация, уже записанная в каталог.
/// </summary>
public sealed class DictionaryPassport
{
    public Guid DictionaryId { get; init; }

    public required string DictionaryCode { get; init; }

    public required string DictionaryName { get; init; }

    public required string Owner { get; init; }

    public required string Source { get; init; }

    public required string Status { get; init; }

    public string? TableName { get; init; }

    public string? SchemaName { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<DictionaryVersion>? Versions { get; init; }
}

namespace management.api.Contracts;

/// <summary>
/// Поля паспорта для открытия регистрации. В каталог этим запросом ещё не пишутся.
/// </summary>
public sealed class OpenRegistrationRequest
{
    public string? DictionaryCode { get; init; }

    public string? DictionaryName { get; init; }

    public string? Owner { get; init; }

    public string? Source { get; init; }
}

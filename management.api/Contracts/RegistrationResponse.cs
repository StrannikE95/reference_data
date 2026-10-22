namespace management.api.Contracts;

/// <summary>
/// Открытая регистрация профиля: поля паспорта и текущий список файлов. Паспорт в каталоге ещё не создан.
/// </summary>
public sealed class RegistrationResponse
{
    public Guid RegistrationId { get; init; }

    public required string DictionaryCode { get; init; }

    public required string DictionaryName { get; init; }

    public required string Owner { get; init; }

    public required string Source { get; init; }

    public required IReadOnlyList<FileDescription> Files { get; init; }
}

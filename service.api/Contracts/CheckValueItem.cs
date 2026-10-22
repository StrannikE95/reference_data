namespace service.api.Contracts;

/// <summary>
/// Результат одного кода в проверке ссылок. Повтор кода в запросе сюда не попадает.
/// </summary>
public sealed class CheckValueItem
{
    public required string ValueCode { get; init; }

    public required string Result { get; init; }

    public string? Name { get; init; }
}

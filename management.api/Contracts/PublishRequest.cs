namespace management.api.Contracts;

/// <summary>
/// Дата начала новой действующей редакции. Предыдущая действующая версия закрывается днём раньше.
/// </summary>
public sealed class PublishRequest
{
    public DateOnly? ValidFrom { get; init; }
}

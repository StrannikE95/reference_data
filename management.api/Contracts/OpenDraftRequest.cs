namespace management.api.Contracts;

/// <summary>
/// Вид поставки черновика. full заполняет секции заново, increment применяется только когда профиль описывает дельту.
/// </summary>
public sealed class OpenDraftRequest
{
    public string? Delivery { get; init; }
}

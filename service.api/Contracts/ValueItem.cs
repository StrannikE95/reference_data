namespace service.api.Contracts;

/// <summary>
/// Строка простого списка в ответе страницы: код и подпись одной опубликованной версии.
/// Колонки сверх этого каркаса в контракт чтения не входят.
/// </summary>
public sealed class ValueItem
{
    public required string ValueCode { get; init; }

    public required string Name { get; init; }
}

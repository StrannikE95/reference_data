using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using service.api.Contracts;

namespace service.api.Services;

/// <summary>
/// Чтение опубликованных значений простого списка для DataController.
/// Черновик не отдаёт. Пока каталог и секции не подключены, здесь проверяется форма запроса,
/// а строки ответа — пример контракта: BRICK есть в редакции, любого другого кода нет.
/// </summary>
public sealed class DataService
{
    /// <summary>
    /// Возвращает одно значение выбранной редакции.
    /// on_date и version_id вместе не принимаются: редакцию выбирает один признак.
    /// </summary>
    public ActionResult<ValueReadResponse> GetValue(
        string dictionaryCode,
        string valueCode,
        DateOnly? onDate,
        int? versionId,
        ModelStateDictionary modelState)
    {
        var rejected = RejectSelector(dictionaryCode, onDate, versionId, modelState);
        if (rejected is not null)
            return rejected;

        var known = valueCode == "BRICK";
        return new OkObjectResult(new ValueReadResponse
        {
            DictionaryCode = dictionaryCode,
            VersionId = versionId ?? 2,
            VersionStatus = "active",
            ValidFrom = new DateOnly(2026, 10, 20),
            ValidTo = null,
            Archived = false,
            Result = known ? "present" : "absent",
            ValueCode = valueCode,
            Name = known ? "Кирпич" : null
        });
    }

    /// <summary>
    /// Возвращает страницу той же редакции. limit ограничивает размер страницы, cursor продолжает уже выбранную редакцию.
    /// </summary>
    public ActionResult<ValuePageResponse> GetPage(
        string dictionaryCode,
        DateOnly? onDate,
        int? versionId,
        int? limit,
        string? cursor,
        ModelStateDictionary modelState)
    {
        var rejected = RejectSelector(dictionaryCode, onDate, versionId, modelState);
        if (rejected is not null)
            return rejected;

        if (limit is < 1 or > 1000 || cursor is not null && string.IsNullOrWhiteSpace(cursor))
            return new BadRequestObjectResult(Error("invalid_request", dictionaryCode, onDate));

        var items = string.IsNullOrEmpty(cursor)
            ? new List<ValueItem> { new() { ValueCode = "BRICK", Name = "Кирпич" } }
            : [];

        return new OkObjectResult(new ValuePageResponse
        {
            DictionaryCode = dictionaryCode,
            VersionId = versionId ?? 2,
            VersionStatus = "active",
            ValidFrom = new DateOnly(2026, 10, 20),
            ValidTo = null,
            Archived = false,
            Items = items,
            NextCursor = null
        });
    }

    /// <summary>
    /// Проверяет список кодов в секции переданного version_id и ничего не записывает.
    /// Повтор одного кода возвращается один раз, чужой код не отменяет остальные.
    /// </summary>
    public ActionResult<CheckValuesResponse> Check(
        string dictionaryCode,
        CheckValuesRequest? request,
        ModelStateDictionary modelState)
    {
        if (!modelState.IsValid || request is null || request.VersionId < 1 || request.ValueCodes is null || request.ValueCodes.Count is < 1 or > 1000)
            return new BadRequestObjectResult(Error("invalid_request", dictionaryCode, null));

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var items = new List<CheckValueItem>();
        foreach (var code in request.ValueCodes)
        {
            if (string.IsNullOrWhiteSpace(code))
                return new BadRequestObjectResult(Error("invalid_request", dictionaryCode, null));

            if (!seen.Add(code))
                continue;

            var known = code == "BRICK";
            items.Add(new CheckValueItem
            {
                ValueCode = code,
                Result = known ? "present" : "absent",
                Name = known ? "Кирпич" : null
            });
        }

        return new OkObjectResult(new CheckValuesResponse
        {
            DictionaryCode = dictionaryCode,
            VersionId = request.VersionId,
            VersionStatus = "active",
            ValidFrom = new DateOnly(2026, 10, 20),
            ValidTo = null,
            Archived = false,
            Items = items
        });
    }

    private static ActionResult? RejectSelector(
        string dictionaryCode,
        DateOnly? onDate,
        int? versionId,
        ModelStateDictionary modelState)
    {
        if (!modelState.IsValid)
            return new BadRequestObjectResult(Error("invalid_request", dictionaryCode, onDate));

        if (onDate is not null && versionId is not null)
            return new BadRequestObjectResult(Error("version_selector_conflict", dictionaryCode, onDate));

        return null;
    }

    private static ReadErrorResponse Error(string error, string dictionaryCode, DateOnly? onDate) => new()
    {
        Error = error,
        DictionaryCode = dictionaryCode,
        OnDate = onDate
    };
}

using Microsoft.AspNetCore.Mvc;
using service.api.Contracts;

namespace service.api.Controllers;

/// <summary>
/// Чтение опубликованных значений простого списка.
/// Черновик не отдаёт. Пока каталог и секции не подключены, форма запроса проверяется здесь,
/// а строки ответа — пример контракта: BRICK есть в редакции, любого другого кода нет.
/// </summary>
[ApiController]
[Route("dictionaries/{dictionary_code}")]
[Produces("application/json")]
public sealed class DataController : ControllerBase
{
    /// <summary>
    /// Возвращает одно значение выбранной редакции.
    /// on_date и version_id вместе не принимаются: редакцию выбирает один признак.
    /// </summary>
    /// <remarks>
    /// GET /dictionaries/WallMaterial/values/BRICK
    /// GET /dictionaries/WallMaterial/values/BRICK?on_date=2024-06-01
    /// GET /dictionaries/WallMaterial/values/BRICK?version_id=2
    /// </remarks>
    [HttpGet("values/{value_code}")]
    [ProducesResponseType(typeof(ValueReadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ReadErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<ValueReadResponse> GetValue(
        [FromRoute(Name = "dictionary_code")] string dictionaryCode,
        [FromRoute(Name = "value_code")] string valueCode,
        [FromQuery(Name = "on_date")] DateOnly? onDate,
        [FromQuery(Name = "version_id")] int? versionId)
    {
        var rejected = RejectSelector(dictionaryCode, onDate, versionId);
        if (rejected is not null)
            return rejected;

        var known = valueCode == "BRICK";
        return Ok(new ValueReadResponse
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
    /// <remarks>
    /// GET /dictionaries/WallMaterial/values?limit=100
    /// GET /dictionaries/WallMaterial/values?on_date=2024-06-01
    /// </remarks>
    [HttpGet("values")]
    [ProducesResponseType(typeof(ValuePageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ReadErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<ValuePageResponse> GetPage(
        [FromRoute(Name = "dictionary_code")] string dictionaryCode,
        [FromQuery(Name = "on_date")] DateOnly? onDate,
        [FromQuery(Name = "version_id")] int? versionId,
        [FromQuery] int? limit,
        [FromQuery] string? cursor)
    {
        var rejected = RejectSelector(dictionaryCode, onDate, versionId);
        if (rejected is not null)
            return rejected;

        if (limit is < 1 or > 1000 || cursor is not null && string.IsNullOrWhiteSpace(cursor))
            return BadRequest(Error("invalid_request", dictionaryCode, onDate));

        var items = string.IsNullOrEmpty(cursor)
            ? new List<ValueItem> { new() { ValueCode = "BRICK", Name = "Кирпич" } }
            : [];

        return Ok(new ValuePageResponse
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
    /// <remarks>
    /// POST /dictionaries/WallMaterial/values/check
    /// { "version_id": 2, "value_codes": ["BRICK", "PAPER"] }
    /// </remarks>
    [HttpPost("values/check")]
    [ProducesResponseType(typeof(CheckValuesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ReadErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<CheckValuesResponse> Check(
        [FromRoute(Name = "dictionary_code")] string dictionaryCode,
        [FromBody] CheckValuesRequest? request)
    {
        if (!ModelState.IsValid || request is null || request.VersionId < 1 || request.ValueCodes is null || request.ValueCodes.Count is < 1 or > 1000)
            return BadRequest(Error("invalid_request", dictionaryCode, null));

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var items = new List<CheckValueItem>();
        foreach (var code in request.ValueCodes)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(Error("invalid_request", dictionaryCode, null));

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

        return Ok(new CheckValuesResponse
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

    private ActionResult? RejectSelector(string dictionaryCode, DateOnly? onDate, int? versionId)
    {
        if (!ModelState.IsValid)
            return BadRequest(Error("invalid_request", dictionaryCode, onDate));

        if (onDate is not null && versionId is not null)
            return BadRequest(Error("version_selector_conflict", dictionaryCode, onDate));

        return null;
    }

    private static ReadErrorResponse Error(string error, string dictionaryCode, DateOnly? onDate) => new()
    {
        Error = error,
        DictionaryCode = dictionaryCode,
        OnDate = onDate
    };
}

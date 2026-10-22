using Microsoft.AspNetCore.Mvc;
using service.api.Contracts;
using service.api.Services;

namespace service.api.Controllers;

/// <summary>
/// Чтение опубликованных значений простого списка.
/// Черновик не отдаёт. Пока каталог и секции не подключены, форма запроса проверяется в DataService,
/// а строки ответа — пример контракта: BRICK есть в редакции, любого другого кода нет.
/// </summary>
[ApiController]
[Route("dictionaries/{dictionary_code}")]
[Produces("application/json")]
public sealed class DataController : ControllerBase
{
    private readonly DataService service = new();

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
        return service.GetValue(dictionaryCode, valueCode, onDate, versionId, ModelState);
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
        return service.GetPage(dictionaryCode, onDate, versionId, limit, cursor, ModelState);
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
        return service.Check(dictionaryCode, request, ModelState);
    }
}

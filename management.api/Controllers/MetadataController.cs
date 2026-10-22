using Microsoft.AspNetCore.Mvc;
using management.api.Contracts;
using management.api.Services;

namespace management.api.Controllers;

/// <summary>
/// Каталог справочников и JSON метаданных.
/// По сообщению схемы dictionary.integration.metadata пишет SQL и выполняет его.
/// </summary>
[ApiController]
[Route("admin")]
[Produces("application/json")]
public sealed class MetadataController : ControllerBase
{
    private readonly MetadataService catalog;
    private readonly ImportService import;

    /// <summary>
    /// Передаёт сервисам строку подключения и каталог схем JSON.
    /// </summary>
    public MetadataController(IConfiguration configuration, IWebHostEnvironment environment)
    {
        catalog = new MetadataService(configuration);
        import = new ImportService(configuration, environment.ContentRootPath);
    }

    /// <summary>
    /// Возвращает страницу паспортов. Незавершённая регистрация в список не входит.
    /// </summary>
    /// <remarks>GET /admin/dictionaries?limit=100</remarks>
    [HttpGet("dictionaries")]
    [ProducesResponseType(typeof(DictionaryListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<DictionaryListResponse> List([FromQuery] int? limit, [FromQuery] string? cursor)
    {
        return catalog.List(limit, cursor, ModelState);
    }

    /// <summary>
    /// Возвращает паспорт и все версии справочника по возрастанию version_id.
    /// Строк значений в ответе нет.
    /// </summary>
    /// <remarks>GET /admin/dictionaries/WallMaterial</remarks>
    [HttpGet("dictionaries/{dictionary_code}")]
    [ProducesResponseType(typeof(DictionaryPassport), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<DictionaryPassport> Get([FromRoute(Name = "dictionary_code")] string dictionaryCode)
    {
        return catalog.Get(dictionaryCode);
    }

    /// <summary>
    /// Принимает JSON метаданных, сверяет его со схемой, пишет скрипт в db/sql-schema и выполняет.
    /// Сообщение не по схеме файл не создаёт. Ошибка выполнения уже записанный файл не удаляет.
    /// </summary>
    /// <remarks>
    /// POST /admin/metadata
    /// Тело — сообщение dictionary.integration.metadata, как в Contracts/json/example.
    /// </remarks>
    [HttpPost("metadata")]
    [ProducesResponseType(typeof(ImportRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AdminErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ImportRunResponse>> Apply()
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();
        return import.ApplyMetadata(json);
    }
}

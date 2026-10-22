using Microsoft.AspNetCore.Mvc;
using management.api.Contracts;
using management.api.Services;

namespace management.api.Controllers;

/// <summary>
/// JSON данных справочника.
/// По сообщению схемы dictionary.integration.data пишет SQL секции и выполняет его.
/// </summary>
[ApiController]
[Route("admin")]
[Produces("application/json")]
public sealed class DataController : ControllerBase
{
    private readonly ImportService import;

    /// <summary>
    /// Передаёт сервису строку подключения и каталог проекта.
    /// </summary>
    public DataController(IConfiguration configuration, IWebHostEnvironment environment)
    {
        import = new ImportService(configuration, environment.ContentRootPath);
    }

    /// <summary>
    /// Принимает JSON данных, сверяет его со схемой, пишет скрипт в db/sql-data и выполняет.
    /// Тело — одно сообщение или массив пачек одного прогона. versionId в запрос передаёт модуль справочников.
    /// Сообщение не по схеме файл не создаёт. Ошибка выполнения уже записанный файл не удаляет.
    /// </summary>
    /// <remarks>
    /// POST /admin/data?versionId=1
    /// Тело — сообщение dictionary.integration.data, как в Contracts/json/example.
    /// </remarks>
    [HttpPost("data")]
    [ProducesResponseType(typeof(ImportRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AdminErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ImportRunResponse>> Apply([FromQuery] int? versionId)
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();
        return import.ApplyData(json, versionId);
    }
}

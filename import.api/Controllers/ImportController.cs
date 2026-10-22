using Microsoft.AspNetCore.Mvc;
using import.api.Contracts;
using import.api.Services;

namespace import.api.Controllers;

/// <summary>
/// Разбор файла справочника в JSON и передача его в management.api.
/// Создание справочника и новая версия — разные методы.
/// </summary>
[ApiController]
[Route("dictionaries")]
[Produces("application/json")]
public sealed class ImportController : ControllerBase
{
    static readonly HashSet<string> Sources = new(StringComparer.Ordinal)
    {
        "ass-building", "wall-material", "oktmo", "gar"
    };

    readonly SourceReader reader;
    readonly ManagementClient management;

    /// <summary>Берёт каталог файлов и адрес management.api из конфигурации.</summary>
    public ImportController(IConfiguration configuration)
    {
        reader = new SourceReader(configuration);
        management = new ManagementClient(configuration);
    }

    /// <summary>
    /// Создаёт новый справочник: читает файл, собирает JSON метаданных и передаёт его в management.api.
    /// Строки в этот метод не входят. Ошибка разбора до management.api не доходит.
    /// </summary>
    /// <remarks>
    /// POST /dictionaries
    /// { "filePath": "C:\\inbox\\dAssBuilding_v04.xsd", "sourceCode": "ass-building" }
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ImportErrorResponse), StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Create([FromBody] CreateDictionaryRequest? request)
    {
        Console.WriteLine("ImportController.Create");
        if (request is null || string.IsNullOrWhiteSpace(request.FilePath) || !Sources.Contains(request.SourceCode ?? ""))
            return Task.FromResult<IActionResult>(BadRequest(new ImportErrorResponse { Error = "invalid_request" }));
        var load = reader.Metadata(request.FilePath, request.SourceCode!);
        if (load.Error is not null)
        {
            Console.WriteLine(load.Error + " " + load.Detail);
            return Task.FromResult<IActionResult>(BadRequest(new ImportErrorResponse { Error = load.Error, Detail = load.Detail }));
        }
        return management.Metadata(load.Json!);
    }

    /// <summary>
    /// Добавляет версию справочника: читает файл, собирает JSON строк и передаёт его в management.api вместе с versionId.
    /// Таблицы этим методом не создаются.
    /// </summary>
    /// <remarks>
    /// POST /dictionaries/versions
    /// { "filePath": "C:\\inbox\\dAssBuilding_v05.xsd", "sourceCode": "ass-building", "versionId": 1 }
    /// </remarks>
    [HttpPost("versions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ImportErrorResponse), StatusCodes.Status400BadRequest)]
    public Task<IActionResult> AddVersion([FromBody] AddVersionRequest? request)
    {
        Console.WriteLine("ImportController.AddVersion");
        if (request is null || string.IsNullOrWhiteSpace(request.FilePath) || !Sources.Contains(request.SourceCode ?? "") || request.VersionId is null or < 1)
            return Task.FromResult<IActionResult>(BadRequest(new ImportErrorResponse { Error = "invalid_request" }));
        var load = reader.Data(request.FilePath, request.SourceCode!);
        if (load.Error is not null)
        {
            Console.WriteLine(load.Error + " " + load.Detail);
            return Task.FromResult<IActionResult>(BadRequest(new ImportErrorResponse { Error = load.Error, Detail = load.Detail }));
        }
        return management.Data(load.Json!, request.VersionId.Value);
    }
}

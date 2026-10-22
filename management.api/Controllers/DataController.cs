using Microsoft.AspNetCore.Mvc;
using management.api.Contracts;
using management.api.Services;

namespace management.api.Controllers;

/// <summary>
/// Файлы заполнения черновика и заливка строк в секции.
/// Паспорт и статус версии не меняет. Пока хранилища нет, успешный ответ — пример контракта.
/// </summary>
[ApiController]
[Route("admin")]
[Produces("application/json")]
public sealed class DataController : ControllerBase
{
    private readonly DataService service = new();

    /// <summary>
    /// Начинает запись пачки файлов черновика и возвращает ссылки на запись.
    /// </summary>
    /// <remarks>
    /// POST /admin/dictionaries/WallMaterial/draft/files
    /// { "files": [ { "name": "wall_material.sql", "size": 240 } ] }
    /// </remarks>
    [HttpPost("dictionaries/{dictionary_code}/draft/files")]
    [ProducesResponseType(typeof(FileBatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<FileBatchResponse> StartDraftFiles(
        [FromRoute(Name = "dictionary_code")] string dictionaryCode,
        [FromBody] FileBatchRequest? request)
    {
        return service.StartDraftFiles(dictionaryCode, request, ModelState);
    }

    /// <summary>
    /// Подтверждает запись файла черновика. Файл версии должен заполнять исходную таблицу профиля.
    /// </summary>
    /// <remarks>POST /admin/dictionaries/WallMaterial/draft/files/91c0e2b4-6a18-4f0d-9b77-3e5a1c8d4420/complete</remarks>
    [HttpPost("dictionaries/{dictionary_code}/draft/files/{file_id}/complete")]
    [ProducesResponseType(typeof(FileDescription), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<FileDescription> CompleteDraftFile(
        [FromRoute(Name = "dictionary_code")] string dictionaryCode,
        [FromRoute(Name = "file_id")] Guid fileId)
    {
        return service.CompleteDraftFile(dictionaryCode, fileId, ModelState);
    }

    /// <summary>
    /// Снимает файл с черновика. Файл в состоянии loaded дополнительно очищает секцию своей таблицы.
    /// </summary>
    /// <remarks>DELETE /admin/dictionaries/WallMaterial/draft/files/91c0e2b4-6a18-4f0d-9b77-3e5a1c8d4420</remarks>
    [HttpDelete("dictionaries/{dictionary_code}/draft/files/{file_id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(AdminErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult DeleteDraftFile(
        [FromRoute(Name = "dictionary_code")] string dictionaryCode,
        [FromRoute(Name = "file_id")] Guid fileId)
    {
        return service.DeleteDraftFile(dictionaryCode, fileId, ModelState);
    }

    /// <summary>
    /// Завершает заливку файлов в состоянии valid. Ошибка одной таблицы не откатывает таблицы, которые уже loaded.
    /// </summary>
    /// <remarks>POST /admin/dictionaries/WallMaterial/draft/apply</remarks>
    [HttpPost("dictionaries/{dictionary_code}/draft/apply")]
    [ProducesResponseType(typeof(DraftResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<DraftResponse> Apply([FromRoute(Name = "dictionary_code")] string dictionaryCode)
    {
        return service.Apply(dictionaryCode, ModelState);
    }
}

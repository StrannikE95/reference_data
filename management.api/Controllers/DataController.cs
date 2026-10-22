using Microsoft.AspNetCore.Mvc;
using management.api.Contracts;

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
        var rejected = RejectFiles(request);
        if (rejected is not null)
            return rejected;

        var files = new List<FileDescription>();
        foreach (var file in request!.Files!)
        {
            var fileId = Guid.NewGuid();
            files.Add(new FileDescription
            {
                FileId = fileId,
                Name = file.Name!,
                Size = file.Size,
                State = "uploading",
                Error = null,
                UploadUrl = $"https://storage.example/upload/{fileId}"
            });
        }

        return Ok(new FileBatchResponse { Files = files });
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
        if (!ModelState.IsValid)
            return BadRequest(new AdminErrorResponse { Error = "invalid_request", DictionaryCode = dictionaryCode });

        return Ok(new FileDescription
        {
            FileId = fileId,
            Name = "wall_material.sql",
            Size = 240,
            State = "valid",
            Error = null
        });
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
        if (!ModelState.IsValid)
            return BadRequest(new AdminErrorResponse { Error = "invalid_request", DictionaryCode = dictionaryCode });

        return NoContent();
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
        if (!ModelState.IsValid)
            return BadRequest(new AdminErrorResponse { Error = "invalid_request", DictionaryCode = dictionaryCode });

        return Ok(new DraftResponse
        {
            DictionaryCode = dictionaryCode,
            VersionId = 3,
            VersionStatus = "draft",
            Delivery = "full",
            Files =
            [
                new FileDescription
                {
                    FileId = Guid.Parse("91c0e2b4-6a18-4f0d-9b77-3e5a1c8d4420"),
                    Name = "wall_material.sql",
                    Size = 240,
                    State = "loaded",
                    Error = null
                }
            ]
        });
    }

    private ActionResult? RejectFiles(FileBatchRequest? request)
    {
        if (!ModelState.IsValid || request?.Files is null || request.Files.Count is < 1 or > 100)
            return BadRequest(new AdminErrorResponse { Error = "invalid_request" });

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in request.Files)
        {
            if (file is null || string.IsNullOrWhiteSpace(file.Name) || file.Size < 1 || !names.Add(file.Name))
                return BadRequest(new AdminErrorResponse { Error = "invalid_request" });
        }

        return null;
    }
}

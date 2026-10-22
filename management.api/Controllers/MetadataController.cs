using Microsoft.AspNetCore.Mvc;
using management.api.Contracts;
using management.api.Services;

namespace management.api.Controllers;

/// <summary>
/// Каталог справочников: паспорта, регистрация профиля, черновик версии и публикация.
/// Список и паспорт читаются из PostgreSQL. Регистрация, черновик и публикация пока отвечают примером контракта.
/// </summary>
[ApiController]
[Route("admin")]
[Produces("application/json")]
public sealed class MetadataController : ControllerBase
{
    private readonly MetadataService service;

    /// <summary>
    /// Передаёт сервису конфигурацию со строкой подключения.
    /// </summary>
    public MetadataController(IConfiguration configuration)
    {
        service = new MetadataService(configuration);
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
        return service.List(limit, cursor, ModelState);
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
        return service.Get(dictionaryCode);
    }

    /// <summary>
    /// Открывает регистрацию профиля и ещё не пишет паспорт в каталог.
    /// Пока регистрация открыта, тот же dictionary_code нельзя занять второй раз.
    /// </summary>
    /// <remarks>
    /// POST /admin/registrations
    /// { "dictionary_code": "WallMaterial", "dictionary_name": "Материал стен", "owner": "Имущество", "source": "Ведомственный перечень" }
    /// </remarks>
    [HttpPost("registrations")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<RegistrationResponse> OpenRegistration([FromBody] OpenRegistrationRequest? request)
    {
        return service.OpenRegistration(request, ModelState);
    }

    /// <summary>
    /// Возвращает открытую регистрацию и текущий список её файлов.
    /// </summary>
    /// <remarks>GET /admin/registrations/a17d5e90-4c22-4b8e-8f01-6d9a2b3c4401</remarks>
    [HttpGet("registrations/{registration_id}")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<RegistrationResponse> GetRegistration([FromRoute(Name = "registration_id")] Guid registrationId)
    {
        return service.GetRegistration(registrationId, ModelState);
    }

    /// <summary>
    /// Удаляет регистрацию и её файлы. Паспорт не создаётся.
    /// </summary>
    /// <remarks>DELETE /admin/registrations/a17d5e90-4c22-4b8e-8f01-6d9a2b3c4401</remarks>
    [HttpDelete("registrations/{registration_id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(AdminErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult CancelRegistration([FromRoute(Name = "registration_id")] Guid registrationId)
    {
        return service.CancelRegistration(registrationId, ModelState);
    }

    /// <summary>
    /// Завершает регистрацию: профиль сохраняется, родительские таблицы создаются, паспорт пишется со статусом active.
    /// Строка версии этим шагом не создаётся.
    /// </summary>
    /// <remarks>POST /admin/registrations/a17d5e90-4c22-4b8e-8f01-6d9a2b3c4401/commit</remarks>
    [HttpPost("registrations/{registration_id}/commit")]
    [ProducesResponseType(typeof(DictionaryPassport), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<DictionaryPassport> CommitRegistration([FromRoute(Name = "registration_id")] Guid registrationId)
    {
        return service.CommitRegistration(registrationId, ModelState);
    }

    /// <summary>
    /// Начинает запись пачки файлов профиля и возвращает ссылки. Повтор имени заменяет файл, который ещё не loaded.
    /// </summary>
    /// <remarks>
    /// POST /admin/registrations/a17d5e90-4c22-4b8e-8f01-6d9a2b3c4401/files
    /// { "files": [ { "name": "wall_material.sql", "size": 240 } ] }
    /// </remarks>
    [HttpPost("registrations/{registration_id}/files")]
    [ProducesResponseType(typeof(FileBatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<FileBatchResponse> StartRegistrationFiles(
        [FromRoute(Name = "registration_id")] Guid registrationId,
        [FromBody] FileBatchRequest? request)
    {
        return service.StartRegistrationFiles(registrationId, request, ModelState);
    }

    /// <summary>
    /// Подтверждает запись файла профиля. Для профиля файл должен быть созданием таблиц без version_id.
    /// </summary>
    /// <remarks>POST /admin/registrations/a17d5e90-4c22-4b8e-8f01-6d9a2b3c4401/files/91c0e2b4-6a18-4f0d-9b77-3e5a1c8d4420/complete</remarks>
    [HttpPost("registrations/{registration_id}/files/{file_id}/complete")]
    [ProducesResponseType(typeof(FileDescription), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<FileDescription> CompleteRegistrationFile(
        [FromRoute(Name = "registration_id")] Guid registrationId,
        [FromRoute(Name = "file_id")] Guid fileId)
    {
        return service.CompleteRegistrationFile(registrationId, fileId, ModelState);
    }

    /// <summary>
    /// Снимает файл с набора регистрации.
    /// </summary>
    /// <remarks>DELETE /admin/registrations/a17d5e90-4c22-4b8e-8f01-6d9a2b3c4401/files/91c0e2b4-6a18-4f0d-9b77-3e5a1c8d4420</remarks>
    [HttpDelete("registrations/{registration_id}/files/{file_id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(AdminErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult DeleteRegistrationFile(
        [FromRoute(Name = "registration_id")] Guid registrationId,
        [FromRoute(Name = "file_id")] Guid fileId)
    {
        return service.DeleteRegistrationFile(registrationId, fileId, ModelState);
    }

    /// <summary>
    /// Открывает черновик версии. Второй черновик не создаётся. increment без описания дельты черновик не создаёт.
    /// </summary>
    /// <remarks>
    /// POST /admin/dictionaries/WallMaterial/draft
    /// { "delivery": "full" }
    /// </remarks>
    [HttpPost("dictionaries/{dictionary_code}/draft")]
    [ProducesResponseType(typeof(DraftResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<DraftResponse> OpenDraft(
        [FromRoute(Name = "dictionary_code")] string dictionaryCode,
        [FromBody] OpenDraftRequest? request)
    {
        return service.OpenDraft(dictionaryCode, request, ModelState);
    }

    /// <summary>
    /// Возвращает черновик и текущие состояния файлов, в том числе переход valid → loading → loaded.
    /// </summary>
    /// <remarks>GET /admin/dictionaries/WallMaterial/draft</remarks>
    [HttpGet("dictionaries/{dictionary_code}/draft")]
    [ProducesResponseType(typeof(DraftResponse), StatusCodes.Status200OK)]
    public ActionResult<DraftResponse> GetDraft([FromRoute(Name = "dictionary_code")] string dictionaryCode)
    {
        return service.GetDraft(dictionaryCode);
    }

    /// <summary>
    /// Удаляет секции черновика, его файлы и строку dictionary_version. События публикации нет.
    /// </summary>
    /// <remarks>DELETE /admin/dictionaries/WallMaterial/draft</remarks>
    [HttpDelete("dictionaries/{dictionary_code}/draft")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult CancelDraft([FromRoute(Name = "dictionary_code")] string dictionaryCode)
    {
        return service.CancelDraft(dictionaryCode);
    }

    /// <summary>
    /// Публикует черновик и закрывает прежнюю действующую версию одной командой.
    /// valid_from обязателен и не позже дня публикации.
    /// </summary>
    /// <remarks>
    /// POST /admin/dictionaries/WallMaterial/draft/publish
    /// { "valid_from": "2026-10-22" }
    /// </remarks>
    [HttpPost("dictionaries/{dictionary_code}/draft/publish")]
    [ProducesResponseType(typeof(PublishResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<PublishResponse> Publish(
        [FromRoute(Name = "dictionary_code")] string dictionaryCode,
        [FromBody] PublishRequest? request)
    {
        return service.Publish(dictionaryCode, request, ModelState);
    }
}

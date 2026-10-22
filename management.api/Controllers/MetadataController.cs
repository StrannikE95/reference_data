using Microsoft.AspNetCore.Mvc;
using management.api.Contracts;

namespace management.api.Controllers;

/// <summary>
/// Каталог справочников: паспорта, регистрация профиля, черновик версии и публикация.
/// Строки значений не читает и не пишет. Пока каталог не подключён, успешный ответ — пример паспорта WallMaterial.
/// </summary>
[ApiController]
[Route("admin")]
[Produces("application/json")]
public sealed class MetadataController : ControllerBase
{
    private static readonly Guid ExampleDictionaryId = Guid.Parse("6b1f3c2a-1d4e-4a7b-9c31-0a5e8f2b7d10");

    /// <summary>
    /// Возвращает страницу паспортов. Незавершённая регистрация в список не входит.
    /// </summary>
    /// <remarks>GET /admin/dictionaries?limit=100</remarks>
    [HttpGet("dictionaries")]
    [ProducesResponseType(typeof(DictionaryListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<DictionaryListResponse> List([FromQuery] int? limit, [FromQuery] string? cursor)
    {
        if (!ModelState.IsValid || limit is < 1 or > 1000 || cursor is not null && string.IsNullOrWhiteSpace(cursor))
            return BadRequest(new AdminErrorResponse { Error = "invalid_request" });

        IReadOnlyList<DictionaryPassport> items = string.IsNullOrEmpty(cursor)
            ? [Passport("WallMaterial", includeVersions: false)]
            : [];

        return Ok(new DictionaryListResponse { Items = items, NextCursor = null });
    }

    /// <summary>
    /// Возвращает паспорт и все версии справочника по возрастанию version_id.
    /// Строк значений в ответе нет.
    /// </summary>
    /// <remarks>GET /admin/dictionaries/WallMaterial</remarks>
    [HttpGet("dictionaries/{dictionary_code}")]
    [ProducesResponseType(typeof(DictionaryPassport), StatusCodes.Status200OK)]
    public ActionResult<DictionaryPassport> Get([FromRoute(Name = "dictionary_code")] string dictionaryCode)
    {
        return Ok(Passport(dictionaryCode, includeVersions: true));
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
        if (!ModelState.IsValid || request is null || Blank(request.DictionaryCode) || Blank(request.DictionaryName) || Blank(request.Owner) || Blank(request.Source))
            return BadRequest(new AdminErrorResponse { Error = "invalid_request" });

        return Ok(new RegistrationResponse
        {
            RegistrationId = Guid.NewGuid(),
            DictionaryCode = request.DictionaryCode!,
            DictionaryName = request.DictionaryName!,
            Owner = request.Owner!,
            Source = request.Source!,
            Files = []
        });
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
        if (!ModelState.IsValid)
            return BadRequest(new AdminErrorResponse { Error = "invalid_request" });

        return Ok(new RegistrationResponse
        {
            RegistrationId = registrationId,
            DictionaryCode = "WallMaterial",
            DictionaryName = "Материал стен",
            Owner = "Имущество",
            Source = "Ведомственный перечень",
            Files = []
        });
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
        if (!ModelState.IsValid)
            return BadRequest(new AdminErrorResponse { Error = "invalid_request" });

        return NoContent();
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
        if (!ModelState.IsValid)
            return BadRequest(new AdminErrorResponse { Error = "invalid_request" });

        var passport = Passport("WallMaterial", includeVersions: false);
        return Ok(new DictionaryPassport
        {
            DictionaryId = passport.DictionaryId,
            DictionaryCode = passport.DictionaryCode,
            DictionaryName = passport.DictionaryName,
            Owner = passport.Owner,
            Source = passport.Source,
            Status = passport.Status,
            TableName = passport.TableName,
            SchemaName = passport.SchemaName,
            Versions = []
        });
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
        var rejected = RejectFiles(request);
        if (rejected is not null)
            return rejected;

        return Ok(Uploading(request!));
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
        if (!ModelState.IsValid)
            return BadRequest(new AdminErrorResponse { Error = "invalid_request" });

        return Ok(Completed(fileId));
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
        if (!ModelState.IsValid)
            return BadRequest(new AdminErrorResponse { Error = "invalid_request" });

        return NoContent();
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
        if (!ModelState.IsValid || request is null || request.Delivery is not ("full" or "increment"))
            return BadRequest(new AdminErrorResponse { Error = "invalid_request", DictionaryCode = dictionaryCode });

        if (request.Delivery == "increment")
            return BadRequest(new AdminErrorResponse { Error = "increment_not_supported", DictionaryCode = dictionaryCode });

        return Ok(Draft(dictionaryCode, request.Delivery, []));
    }

    /// <summary>
    /// Возвращает черновик и текущие состояния файлов, в том числе переход valid → loading → loaded.
    /// </summary>
    /// <remarks>GET /admin/dictionaries/WallMaterial/draft</remarks>
    [HttpGet("dictionaries/{dictionary_code}/draft")]
    [ProducesResponseType(typeof(DraftResponse), StatusCodes.Status200OK)]
    public ActionResult<DraftResponse> GetDraft([FromRoute(Name = "dictionary_code")] string dictionaryCode)
    {
        return Ok(Draft(dictionaryCode, "full", []));
    }

    /// <summary>
    /// Удаляет секции черновика, его файлы и строку dictionary_version. События публикации нет.
    /// </summary>
    /// <remarks>DELETE /admin/dictionaries/WallMaterial/draft</remarks>
    [HttpDelete("dictionaries/{dictionary_code}/draft")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult CancelDraft([FromRoute(Name = "dictionary_code")] string dictionaryCode)
    {
        return NoContent();
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
        if (!ModelState.IsValid || request?.ValidFrom is null || request.ValidFrom.Value > DateOnly.FromDateTime(DateTime.Today))
        {
            return BadRequest(new AdminErrorResponse
            {
                Error = "invalid_valid_from",
                DictionaryCode = dictionaryCode
            });
        }

        var validFrom = request.ValidFrom.Value;
        return Ok(new PublishResponse
        {
            DictionaryCode = dictionaryCode,
            VersionId = 3,
            VersionStatus = "active",
            ValidFrom = validFrom,
            ValidTo = null,
            ClosedVersionId = 2,
            ClosedValidTo = validFrom.AddDays(-1)
        });
    }

    private static DictionaryPassport Passport(string dictionaryCode, bool includeVersions) => new()
    {
        DictionaryId = ExampleDictionaryId,
        DictionaryCode = dictionaryCode,
        DictionaryName = "Материал стен",
        Owner = "Имущество",
        Source = "Ведомственный перечень",
        Status = "active",
        TableName = "wall_material",
        SchemaName = null,
        Versions = includeVersions ? ExampleVersions() : null
    };

    private static IReadOnlyList<DictionaryVersion> ExampleVersions() =>
    [
        new()
        {
            DictionaryVersionId = Guid.Parse("c2a91e40-7b55-4d1a-8e66-15f0c9a4b812"),
            VersionId = 1,
            Status = "closed",
            ValidFrom = new DateOnly(2024, 1, 1),
            ValidTo = new DateOnly(2026, 10, 19),
            TableName = "wall_material",
            SchemaName = null
        },
        new()
        {
            DictionaryVersionId = Guid.Parse("e84b0d17-2f6c-4c9a-b331-90ad6e5f4412"),
            VersionId = 2,
            Status = "active",
            ValidFrom = new DateOnly(2026, 10, 20),
            ValidTo = null,
            TableName = "wall_material",
            SchemaName = null
        }
    ];

    private static DraftResponse Draft(string dictionaryCode, string delivery, IReadOnlyList<FileDescription> files) => new()
    {
        DictionaryCode = dictionaryCode,
        VersionId = 3,
        VersionStatus = "draft",
        Delivery = delivery,
        Files = files
    };

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

    private static FileBatchResponse Uploading(FileBatchRequest request)
    {
        var files = new List<FileDescription>();
        foreach (var file in request.Files!)
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

        return new FileBatchResponse { Files = files };
    }

    private static FileDescription Completed(Guid fileId) => new()
    {
        FileId = fileId,
        Name = "wall_material.sql",
        Size = 240,
        State = "valid",
        Error = null
    };

    private static bool Blank(string? value) => string.IsNullOrWhiteSpace(value);
}

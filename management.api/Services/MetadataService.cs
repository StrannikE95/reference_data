using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using management.api.Contracts;

namespace management.api.Services;

/// <summary>
/// Каталог, регистрация профиля и черновик версии для MetadataController.
/// Строки значений не читает из базы. Пока каталог не подключён, здесь проверяется форма запроса,
/// а успешный ответ — пример паспорта WallMaterial.
/// </summary>
public sealed class MetadataService
{
    private static readonly Guid ExampleDictionaryId = Guid.Parse("6b1f3c2a-1d4e-4a7b-9c31-0a5e8f2b7d10");

    /// <summary>
    /// Возвращает страницу паспортов. Незавершённая регистрация в список не входит.
    /// </summary>
    public ActionResult<DictionaryListResponse> List(int? limit, string? cursor, ModelStateDictionary modelState)
    {
        if (!modelState.IsValid || limit is < 1 or > 1000 || cursor is not null && string.IsNullOrWhiteSpace(cursor))
            return new BadRequestObjectResult(new AdminErrorResponse { Error = "invalid_request" });

        IReadOnlyList<DictionaryPassport> items = string.IsNullOrEmpty(cursor)
            ? [Passport("WallMaterial", includeVersions: false)]
            : [];

        return new OkObjectResult(new DictionaryListResponse { Items = items, NextCursor = null });
    }

    /// <summary>
    /// Возвращает паспорт и все версии справочника по возрастанию version_id.
    /// Строк значений в ответе нет.
    /// </summary>
    public ActionResult<DictionaryPassport> Get(string dictionaryCode)
    {
        return new OkObjectResult(Passport(dictionaryCode, includeVersions: true));
    }

    /// <summary>
    /// Открывает регистрацию профиля и ещё не пишет паспорт в каталог.
    /// Пока регистрация открыта, тот же dictionary_code нельзя занять второй раз.
    /// </summary>
    public ActionResult<RegistrationResponse> OpenRegistration(OpenRegistrationRequest? request, ModelStateDictionary modelState)
    {
        if (!modelState.IsValid || request is null || Blank(request.DictionaryCode) || Blank(request.DictionaryName) || Blank(request.Owner) || Blank(request.Source))
            return new BadRequestObjectResult(new AdminErrorResponse { Error = "invalid_request" });

        return new OkObjectResult(new RegistrationResponse
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
    public ActionResult<RegistrationResponse> GetRegistration(Guid registrationId, ModelStateDictionary modelState)
    {
        if (!modelState.IsValid)
            return new BadRequestObjectResult(new AdminErrorResponse { Error = "invalid_request" });

        return new OkObjectResult(new RegistrationResponse
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
    public ActionResult CancelRegistration(Guid registrationId, ModelStateDictionary modelState)
    {
        if (!modelState.IsValid)
            return new BadRequestObjectResult(new AdminErrorResponse { Error = "invalid_request" });

        return new NoContentResult();
    }

    /// <summary>
    /// Завершает регистрацию: профиль сохраняется, родительские таблицы создаются, паспорт пишется со статусом active.
    /// Строка версии этим шагом не создаётся.
    /// </summary>
    public ActionResult<DictionaryPassport> CommitRegistration(Guid registrationId, ModelStateDictionary modelState)
    {
        if (!modelState.IsValid)
            return new BadRequestObjectResult(new AdminErrorResponse { Error = "invalid_request" });

        var passport = Passport("WallMaterial", includeVersions: false);
        return new OkObjectResult(new DictionaryPassport
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
    public ActionResult<FileBatchResponse> StartRegistrationFiles(
        Guid registrationId,
        FileBatchRequest? request,
        ModelStateDictionary modelState)
    {
        var rejected = RejectFiles(request, modelState);
        if (rejected is not null)
            return rejected;

        return new OkObjectResult(Uploading(request!));
    }

    /// <summary>
    /// Подтверждает запись файла профиля. Для профиля файл должен быть созданием таблиц без version_id.
    /// </summary>
    public ActionResult<FileDescription> CompleteRegistrationFile(
        Guid registrationId,
        Guid fileId,
        ModelStateDictionary modelState)
    {
        if (!modelState.IsValid)
            return new BadRequestObjectResult(new AdminErrorResponse { Error = "invalid_request" });

        return new OkObjectResult(Completed(fileId));
    }

    /// <summary>
    /// Снимает файл с набора регистрации.
    /// </summary>
    public ActionResult DeleteRegistrationFile(Guid registrationId, Guid fileId, ModelStateDictionary modelState)
    {
        if (!modelState.IsValid)
            return new BadRequestObjectResult(new AdminErrorResponse { Error = "invalid_request" });

        return new NoContentResult();
    }

    /// <summary>
    /// Открывает черновик версии. Второй черновик не создаётся. increment без описания дельты черновик не создаёт.
    /// </summary>
    public ActionResult<DraftResponse> OpenDraft(
        string dictionaryCode,
        OpenDraftRequest? request,
        ModelStateDictionary modelState)
    {
        if (!modelState.IsValid || request is null || request.Delivery is not ("full" or "increment"))
            return new BadRequestObjectResult(new AdminErrorResponse { Error = "invalid_request", DictionaryCode = dictionaryCode });

        if (request.Delivery == "increment")
            return new BadRequestObjectResult(new AdminErrorResponse { Error = "increment_not_supported", DictionaryCode = dictionaryCode });

        return new OkObjectResult(Draft(dictionaryCode, request.Delivery, []));
    }

    /// <summary>
    /// Возвращает черновик и текущие состояния файлов, в том числе переход valid → loading → loaded.
    /// </summary>
    public ActionResult<DraftResponse> GetDraft(string dictionaryCode)
    {
        return new OkObjectResult(Draft(dictionaryCode, "full", []));
    }

    /// <summary>
    /// Удаляет секции черновика, его файлы и строку dictionary_version. События публикации нет.
    /// </summary>
    public ActionResult CancelDraft(string dictionaryCode)
    {
        return new NoContentResult();
    }

    /// <summary>
    /// Публикует черновик и закрывает прежнюю действующую версию одной командой.
    /// valid_from обязателен и не позже дня публикации.
    /// </summary>
    public ActionResult<PublishResponse> Publish(
        string dictionaryCode,
        PublishRequest? request,
        ModelStateDictionary modelState)
    {
        if (!modelState.IsValid || request?.ValidFrom is null || request.ValidFrom.Value > DateOnly.FromDateTime(DateTime.Today))
        {
            return new BadRequestObjectResult(new AdminErrorResponse
            {
                Error = "invalid_valid_from",
                DictionaryCode = dictionaryCode
            });
        }

        var validFrom = request.ValidFrom.Value;
        return new OkObjectResult(new PublishResponse
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

    private static ActionResult? RejectFiles(FileBatchRequest? request, ModelStateDictionary modelState)
    {
        if (!modelState.IsValid || request?.Files is null || request.Files.Count is < 1 or > 100)
            return new BadRequestObjectResult(new AdminErrorResponse { Error = "invalid_request" });

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in request.Files)
        {
            if (file is null || string.IsNullOrWhiteSpace(file.Name) || file.Size < 1 || !names.Add(file.Name))
                return new BadRequestObjectResult(new AdminErrorResponse { Error = "invalid_request" });
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

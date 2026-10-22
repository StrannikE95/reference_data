using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using management.api.Contracts;
using db;

namespace management.api.Services;

/// <summary>
/// Каталог, регистрация профиля и черновик версии для MetadataController.
/// Список и паспорт читает из PostgreSQL. Регистрация, черновик и публикация пока отвечают примером контракта.
/// </summary>
public sealed class MetadataService
{
    private static readonly Guid ExampleDictionaryId = Guid.Parse("6b1f3c2a-1d4e-4a7b-9c31-0a5e8f2b7d10");
    private readonly IConfiguration configuration;

    /// <summary>
    /// Берёт строку подключения для чтения каталога.
    /// </summary>
    public MetadataService(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    /// <summary>
    /// Возвращает страницу паспортов. Незавершённая регистрация в список не входит.
    /// </summary>
    public ActionResult<DictionaryListResponse> List(int? limit, string? cursor, ModelStateDictionary modelState)
    {
        Console.WriteLine($"List limit={limit} cursor={cursor}");
        if (!modelState.IsValid || limit is < 1 or > 1000 || cursor is not null && string.IsNullOrWhiteSpace(cursor))
            return new BadRequestObjectResult(new AdminErrorResponse { Error = "invalid_request" });

        var take = limit ?? 1000;
        var where = string.IsNullOrEmpty(cursor) ? "" : $"WHERE dictionary_code > '{cursor}'";
        using var connection = Db.Open(configuration);
        var rows = connection.Query<DictionaryPassport>(
            $"SELECT dictionary_id, dictionary_code, dictionary_name, owner, source, status FROM dictionary {where} ORDER BY dictionary_code LIMIT {take + 1}").ToList();

        string? next = null;
        if (rows.Count > take)
        {
            next = rows[take - 1].DictionaryCode;
            rows = rows.Take(take).ToList();
        }

        var placementByDictionary = PlacementByDictionary(connection, rows.Select(row => row.DictionaryId));
        var items = rows.Select(row =>
        {
            placementByDictionary.TryGetValue(row.DictionaryId, out var placement);
            return WithPlacement(row, placement);
        }).ToList();

        return new OkObjectResult(new DictionaryListResponse { Items = items, NextCursor = next });
    }

    /// <summary>
    /// Возвращает паспорт и все версии справочника по возрастанию version_id.
    /// Строк значений в ответе нет.
    /// </summary>
    public ActionResult<DictionaryPassport> Get(string dictionaryCode)
    {
        Console.WriteLine($"Get {dictionaryCode}");
        using var connection = Db.Open(configuration);
        var passport = connection.QueryFirstOrDefault<DictionaryPassport>(
            $"SELECT dictionary_id, dictionary_code, dictionary_name, owner, source, status FROM dictionary WHERE dictionary_code = '{dictionaryCode}'");
        if (passport is null)
            return new NotFoundObjectResult(new AdminErrorResponse { Error = "dictionary_not_found", DictionaryCode = dictionaryCode });

        var versions = connection.Query<DictionaryVersion>(
            $"SELECT dictionary_version_id, version_id, status, valid_from, valid_to, table_name, schema_name FROM dictionary_version WHERE dictionary_id = '{passport.DictionaryId}' ORDER BY version_id").ToList();
        var placement = versions.FirstOrDefault();

        return new OkObjectResult(new DictionaryPassport
        {
            DictionaryId = passport.DictionaryId,
            DictionaryCode = passport.DictionaryCode,
            DictionaryName = passport.DictionaryName,
            Owner = passport.Owner,
            Source = passport.Source,
            Status = passport.Status,
            TableName = placement?.TableName,
            SchemaName = placement?.SchemaName,
            Versions = versions
        });
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

    /// <summary>
    /// Размещение лежит на версии и одно на все версии справочника. В паспорте его нет.
    /// </summary>
    private static Dictionary<Guid, PlacementRow> PlacementByDictionary(Npgsql.NpgsqlConnection connection, IEnumerable<Guid> dictionaryIds)
    {
        var ids = dictionaryIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var inList = string.Join(", ", ids.Select(id => $"'{id}'"));
        return connection.Query<PlacementRow>(
                $"SELECT dictionary_id, table_name, schema_name FROM dictionary_version WHERE dictionary_id IN ({inList})")
            .GroupBy(row => row.DictionaryId)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private static DictionaryPassport WithPlacement(DictionaryPassport passport, PlacementRow? placement) => new()
    {
        DictionaryId = passport.DictionaryId,
        DictionaryCode = passport.DictionaryCode,
        DictionaryName = passport.DictionaryName,
        Owner = passport.Owner,
        Source = passport.Source,
        Status = passport.Status,
        TableName = placement?.TableName,
        SchemaName = placement?.SchemaName
    };

    private sealed class PlacementRow
    {
        public Guid DictionaryId { get; set; }
        public string? TableName { get; set; }
        public string? SchemaName { get; set; }
    }
}

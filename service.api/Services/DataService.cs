using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using service.api.Contracts;
using db;

namespace service.api.Services;

/// <summary>
/// Чтение опубликованных значений простого списка для DataController.
/// Редакцию выбирает по каталогу, строки берёт из секции table_name. Черновик не отдаёт.
/// Набор таблиц (schema_name) этим контрактом не читается.
/// </summary>
public sealed class DataService
{
    private readonly IConfiguration configuration;

    /// <summary>
    /// Берёт строку подключения для чтения каталога и секции.
    /// </summary>
    public DataService(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    /// <summary>
    /// Возвращает одно значение выбранной редакции.
    /// on_date и version_id вместе не принимаются: редакцию выбирает один признак.
    /// </summary>
    public ActionResult<ValueReadResponse> GetValue(
        string dictionaryCode,
        string valueCode,
        DateOnly? onDate,
        int? versionId,
        ModelStateDictionary modelState)
    {
        Console.WriteLine($"GetValue {dictionaryCode} {valueCode}");
        var rejected = RejectSelector(dictionaryCode, onDate, versionId, modelState);
        if (rejected is not null)
            return rejected;

        using var connection = Db.Open(configuration);
        var error = FindVersion(connection, dictionaryCode, onDate, versionId, out var version);
        if (error is not null)
            return error;

        var row = connection.QueryFirstOrDefault<ValueRow>(
            $"SELECT value_code, name FROM {version!.TableName} WHERE version_id = {version.VersionId} AND value_code = '{valueCode}'");

        return new OkObjectResult(Read(dictionaryCode, version, valueCode, row?.Name));
    }

    /// <summary>
    /// Возвращает страницу той же редакции. limit ограничивает размер страницы, cursor продолжает уже выбранную редакцию.
    /// </summary>
    public ActionResult<ValuePageResponse> GetPage(
        string dictionaryCode,
        DateOnly? onDate,
        int? versionId,
        int? limit,
        string? cursor,
        ModelStateDictionary modelState)
    {
        Console.WriteLine($"GetPage {dictionaryCode}");
        var rejected = RejectSelector(dictionaryCode, onDate, versionId, modelState);
        if (rejected is not null)
            return rejected;

        if (limit is < 1 or > 1000 || cursor is not null && string.IsNullOrWhiteSpace(cursor))
            return new BadRequestObjectResult(Error("invalid_request", dictionaryCode, onDate));

        using var connection = Db.Open(configuration);
        var error = FindVersion(connection, dictionaryCode, onDate, versionId, out var version);
        if (error is not null)
            return error;

        string? after = null;
        if (!string.IsNullOrEmpty(cursor))
        {
            var parts = cursor.Split('|', 2);
            if (parts.Length != 2 || !int.TryParse(parts[0], out var cursorVersion) || cursorVersion != version!.VersionId)
                return new BadRequestObjectResult(Error("invalid_request", dictionaryCode, onDate));
            after = parts[1];
        }

        var take = limit ?? 1000;
        var afterSql = after is null ? "" : $"AND value_code > '{after}'";
        var rows = connection.Query<ValueRow>(
            $"SELECT value_code, name FROM {version!.TableName} WHERE version_id = {version.VersionId} {afterSql} ORDER BY value_code LIMIT {take + 1}").ToList();

        string? next = null;
        if (rows.Count > take)
        {
            next = $"{version.VersionId}|{rows[take - 1].ValueCode}";
            rows = rows.Take(take).ToList();
        }

        return new OkObjectResult(new ValuePageResponse
        {
            DictionaryCode = dictionaryCode,
            VersionId = version.VersionId,
            VersionStatus = version.Status,
            ValidFrom = version.ValidFrom!.Value,
            ValidTo = version.ValidTo,
            Archived = false,
            Items = rows.Select(row => new ValueItem { ValueCode = row.ValueCode, Name = row.Name }).ToList(),
            NextCursor = next
        });
    }

    /// <summary>
    /// Проверяет список кодов в секции переданного version_id и ничего не записывает.
    /// Повтор одного кода возвращается один раз, чужой код не отменяет остальные.
    /// </summary>
    public ActionResult<CheckValuesResponse> Check(
        string dictionaryCode,
        CheckValuesRequest? request,
        ModelStateDictionary modelState)
    {
        Console.WriteLine($"Check {dictionaryCode}");
        if (!modelState.IsValid || request is null || request.VersionId < 1 || request.ValueCodes is null || request.ValueCodes.Count is < 1 or > 1000)
            return new BadRequestObjectResult(Error("invalid_request", dictionaryCode, null));

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var unique = new List<string>();
        foreach (var code in request.ValueCodes)
        {
            if (string.IsNullOrWhiteSpace(code))
                return new BadRequestObjectResult(Error("invalid_request", dictionaryCode, null));

            if (seen.Add(code))
                unique.Add(code);
        }

        using var connection = Db.Open(configuration);
        var error = FindVersion(connection, dictionaryCode, null, request.VersionId, out var version);
        if (error is not null)
            return error;

        var inList = string.Join(", ", unique.Select(code => $"'{code}'"));
        var rows = connection.Query<ValueRow>(
            $"SELECT value_code, name FROM {version!.TableName} WHERE version_id = {version.VersionId} AND value_code IN ({inList})");
        var byCode = rows.ToDictionary(row => row.ValueCode, row => row.Name);

        return new OkObjectResult(new CheckValuesResponse
        {
            DictionaryCode = dictionaryCode,
            VersionId = version.VersionId,
            VersionStatus = version.Status,
            ValidFrom = version.ValidFrom!.Value,
            ValidTo = version.ValidTo,
            Archived = false,
            Items = unique.Select(code => byCode.TryGetValue(code, out var name)
                ? new CheckValueItem { ValueCode = code, Result = "present", Name = name }
                : new CheckValueItem { ValueCode = code, Result = "absent", Name = null }).ToList()
        });
    }

    private static ActionResult? FindVersion(
        Npgsql.NpgsqlConnection connection,
        string dictionaryCode,
        DateOnly? onDate,
        int? versionId,
        out VersionRow? version)
    {
        version = null;
        var passport = connection.QueryFirstOrDefault<PassportRow>(
            $"SELECT dictionary_id, table_name FROM dictionary WHERE dictionary_code = '{dictionaryCode}'");
        if (passport is null)
            return new NotFoundObjectResult(Error("dictionary_not_found", dictionaryCode, onDate));

        if (string.IsNullOrEmpty(passport.TableName))
            return new BadRequestObjectResult(Error("invalid_request", dictionaryCode, onDate));

        var versions = connection.Query<VersionRow>(
            $"SELECT version_id, status, valid_from, valid_to FROM dictionary_version WHERE dictionary_id = '{passport.DictionaryId}'").ToList();

        VersionRow? selected;
        if (versionId is not null)
        {
            selected = versions.FirstOrDefault(row => row.VersionId == versionId);
            if (selected is null)
                return new NotFoundObjectResult(Error("version_not_found", dictionaryCode, onDate));
            if (selected.Status == "draft")
                return new NotFoundObjectResult(Error("version_not_published", dictionaryCode, onDate));
        }
        else if (onDate is not null)
        {
            var day = onDate.Value;
            var matches = versions.Where(row => row.Status is "active" or "closed"
                && row.ValidFrom is not null
                && row.ValidFrom <= day
                && (row.ValidTo is null || day <= row.ValidTo)).ToList();
            if (matches.Count == 0)
                return new NotFoundObjectResult(Error("date_out_of_range", dictionaryCode, onDate));
            if (matches.Count > 1)
                throw new InvalidOperationException($"date {day} matches {matches.Count} versions of {dictionaryCode}");
            selected = matches[0];
        }
        else
        {
            selected = versions.FirstOrDefault(row => row.Status == "active");
            if (selected is null)
                return new NotFoundObjectResult(Error("no_active_version", dictionaryCode, onDate));
        }

        selected.TableName = passport.TableName;
        version = selected;
        return null;
    }

    private static ValueReadResponse Read(string dictionaryCode, VersionRow version, string valueCode, string? name) => new()
    {
        DictionaryCode = dictionaryCode,
        VersionId = version.VersionId,
        VersionStatus = version.Status,
        ValidFrom = version.ValidFrom!.Value,
        ValidTo = version.ValidTo,
        Archived = false,
        Result = name is null ? "absent" : "present",
        ValueCode = valueCode,
        Name = name
    };

    private static ActionResult? RejectSelector(
        string dictionaryCode,
        DateOnly? onDate,
        int? versionId,
        ModelStateDictionary modelState)
    {
        if (!modelState.IsValid)
            return new BadRequestObjectResult(Error("invalid_request", dictionaryCode, onDate));

        if (onDate is not null && versionId is not null)
            return new BadRequestObjectResult(Error("version_selector_conflict", dictionaryCode, onDate));

        return null;
    }

    private static ReadErrorResponse Error(string error, string dictionaryCode, DateOnly? onDate) => new()
    {
        Error = error,
        DictionaryCode = dictionaryCode,
        OnDate = onDate
    };

    private sealed class PassportRow
    {
        public Guid DictionaryId { get; set; }
        public string? TableName { get; set; }
    }

    private sealed class VersionRow
    {
        public int VersionId { get; set; }
        public string Status { get; set; } = "";
        public DateOnly? ValidFrom { get; set; }
        public DateOnly? ValidTo { get; set; }
        public string TableName { get; set; } = "";
    }

    private sealed class ValueRow
    {
        public string ValueCode { get; set; } = "";
        public string Name { get; set; } = "";
    }
}

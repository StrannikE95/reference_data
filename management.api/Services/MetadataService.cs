using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using management.api.Contracts;
using db;

namespace management.api.Services;

/// <summary>
/// Чтение каталога для MetadataController.
/// </summary>
public sealed class MetadataService
{
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

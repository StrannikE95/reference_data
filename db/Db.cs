using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace db;

/// <summary>
/// Соединение с PostgreSQL для management.api и service.api.
/// Запрос остаётся в сервисе, который его вызывает.
/// </summary>
public static class Db
{
    static Db()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new DateOnlyHandler());
    }

    /// <summary>
    /// Открывает соединение по ключу ConnectionString.
    /// </summary>
    public static NpgsqlConnection Open(IConfiguration configuration)
    {
        Console.WriteLine("Db.Open");
        var connection = new NpgsqlConnection(configuration["ConnectionString"]);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Npgsql через ADO отдаёт date как DateTime. Контракт хранит календарный день в DateOnly.
    /// </summary>
    private sealed class DateOnlyHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly value) => parameter.Value = value;

        public override DateOnly Parse(object value) =>
            value is DateTime dateTime ? DateOnly.FromDateTime(dateTime) : (DateOnly)value;
    }
}

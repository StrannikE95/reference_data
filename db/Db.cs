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
    /// Снимает таблицы и заново выполняет 001_schema.sql и 002_seed.sql.
    /// Стартует вместе с API, поэтому F5 каждый раз возвращает базу к начальному наполнению.
    /// </summary>
    public static void Reset(IConfiguration configuration)
    {
        Console.WriteLine("Db.Reset");
        var directory = Path.Combine(AppContext.BaseDirectory, "sql");
        var schema = File.ReadAllText(Path.Combine(directory, "001_schema.sql"));
        var seed = File.ReadAllText(Path.Combine(directory, "002_seed.sql"));

        using var connection = Open(configuration);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT pg_advisory_lock(861341)";
        command.ExecuteNonQuery();
        try
        {
            command.CommandText = schema;
            command.ExecuteNonQuery();
            command.CommandText = seed;
            command.ExecuteNonQuery();
        }
        finally
        {
            command.CommandText = "SELECT pg_advisory_unlock(861341)";
            command.ExecuteNonQuery();
        }
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

using System.Data;
using System.Data.Common;

namespace DeveloperPortal.Api.Data;

/// <summary>
/// SQL con parámetros posicionales (?) al estilo Snowflake.Data.
/// Toda entrada del usuario viaja como parámetro; nunca se concatena en el SQL.
/// </summary>
public sealed class SqlQuery
{
    private readonly List<object?> _parameters = [];

    public SqlQuery(string sql) => Sql = sql;

    public string Sql { get; private set; }
    public IReadOnlyList<object?> Parameters => _parameters;

    public SqlQuery Append(string sql, params object?[] parameters)
    {
        Sql += sql;
        _parameters.AddRange(parameters);
        return this;
    }

    public DbCommand CreateCommand(DbConnection connection, int timeoutSeconds, DbTransaction? tx = null)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = Sql;
        cmd.CommandTimeout = timeoutSeconds;
        cmd.Transaction = tx;
        for (var i = 0; i < _parameters.Count; i++)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = (i + 1).ToString();
            var value = _parameters[i];
            p.DbType = value switch
            {
                int => DbType.Int32,
                long => DbType.Int64,
                DateTimeOffset => DbType.DateTimeOffset,
                _ => DbType.String,
            };
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
        return cmd;
    }
}

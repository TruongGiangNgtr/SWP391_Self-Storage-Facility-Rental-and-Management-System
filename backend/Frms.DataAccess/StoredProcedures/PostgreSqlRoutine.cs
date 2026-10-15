using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using Npgsql;
using NpgsqlTypes;

namespace Frms.DataAccess.StoredProcedures;

internal static partial class PostgreSqlRoutine
{
    public static Task<DbDataReader> ExecuteReaderAsync(DbCommand command, CancellationToken token)
    {
        Prepare(command);
        return command.ExecuteReaderAsync(token);
    }
    public static Task<object?> ExecuteScalarAsync(DbCommand command, CancellationToken token)
    {
        Prepare(command);
        return command.ExecuteScalarAsync(token);
    }
    public static Task<int> ExecuteNonQueryAsync(DbCommand command, CancellationToken token)
    {
        Prepare(command);
        return command.ExecuteNonQueryAsync(token);
    }

    private static void Prepare(DbCommand command)
    {
        if (command.CommandType != CommandType.StoredProcedure) return;
        var name = command.CommandText;
        if (!RoutineName().IsMatch(name))
            throw new InvalidOperationException("Unknown database routine name.");

        var arguments = new List<string>();
        foreach (DbParameter parameter in command.Parameters)
        {
            if (parameter.Direction != ParameterDirection.Input)
                throw new InvalidOperationException("PostgreSQL routines return result rows; output parameters are not supported here.");
            if (parameter is NpgsqlParameter npgsql)
            {
                // DbType.DateTime2 otherwise maps to timestamp without zone.
                if (parameter.ParameterName == "@ActualReturnDate")
                    npgsql.NpgsqlDbType = NpgsqlDbType.Date;
                else if (parameter.DbType == DbType.DateTime2)
                    npgsql.NpgsqlDbType = NpgsqlDbType.TimestampTz;
                else if (parameter.DbType == DbType.Binary)
                    npgsql.NpgsqlDbType = NpgsqlDbType.Bytea;
            }
            var parameterName = parameter.ParameterName.TrimStart('@');
            if (!ParameterName().IsMatch(parameterName))
                throw new InvalidOperationException("Invalid database routine parameter name.");
            var snakeName = CamelBoundary().Replace(parameterName, "_$1").ToLowerInvariant();
            arguments.Add("p_" + snakeName + " => @" + parameterName);
        }
        command.CommandType = CommandType.Text;
        command.CommandText = "SELECT * FROM public.\"" + name + "\"(" + string.Join(", ", arguments) + ")";
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex ParameterName();
    [GeneratedRegex("(?<=[a-z0-9])([A-Z])", RegexOptions.CultureInvariant)]
    private static partial Regex CamelBoundary();

    [GeneratedRegex("^usp_[A-Za-z0-9_]+$", RegexOptions.CultureInvariant)]
    private static partial Regex RoutineName();
}

using Npgsql;

namespace Frms.DataAccess.StoredProcedures;

// Preserve existing repository business-error translations while replacing
// provider diagnostics. No human-readable server message is parsed.
public static class PostgreSqlErrors
{
    public static int Number(PostgresException error) => error.SqlState switch
    {
        "23505" => 2627,
        "23503" or "23514" => 547,
        "23502" => 515,
        "22001" => 2628,
        _ when error.SqlState.Length == 5 && error.SqlState[0] == 'P'
            && int.TryParse(error.SqlState.AsSpan(1), out var number) => 50000 + number,
        _ => 0
    };
}

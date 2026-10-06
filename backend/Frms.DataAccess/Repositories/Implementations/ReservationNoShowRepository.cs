using System.Data;
using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class ReservationNoShowRepository(
    FrmsDbContext dbContext)
    : IReservationNoShowRepository
{
    public async Task ProcessAsync(
        CancellationToken cancellationToken)
    {
        var connection =
            dbContext.Database.GetDbConnection();

        var shouldCloseConnection =
            connection.State != ConnectionState.Open;

        try
        {
            if (shouldCloseConnection)
            {
                await connection.OpenAsync(
                    cancellationToken);
            }

            await using var command =
                connection.CreateCommand();

            command.CommandText =
                "dbo.usp_ProcessReservationNoShow";

            command.CommandType =
                CommandType.StoredProcedure;

            await command.ExecuteNonQueryAsync(
                cancellationToken);
        }
        finally
        {
            if (shouldCloseConnection &&
                connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }
}
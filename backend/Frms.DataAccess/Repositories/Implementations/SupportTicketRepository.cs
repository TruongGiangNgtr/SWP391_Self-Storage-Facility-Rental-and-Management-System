using System.Data;
using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Frms.DataAccess.StoredProcedures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;
internal sealed class SupportTicketRepository(FrmsDbContext db) : ISupportTicketRepository
{
    public Task<Guid?> GetCustomerIdAsync(Guid userAccountId, CancellationToken ct) =>
        db.Customers.AsNoTracking().Where(x => x.UserAccountId == userAccountId)
            .Select(x => (Guid?)x.CustomerId).SingleOrDefaultAsync(ct);

    public Task<Employee?> GetEmployeeAsync(Guid userAccountId, CancellationToken ct) =>
        db.Employees.AsNoTracking().SingleOrDefaultAsync(x => x.UserAccountId == userAccountId, ct);

    private IQueryable<SupportTicket> Scoped(Guid? customerId, Guid? facilityId, Guid? assignedEmployeeId)
    {
        var query = db.SupportTickets.AsNoTracking().AsQueryable();
        if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
        if (assignedEmployeeId.HasValue) query = query.Where(x => x.AssignedEmployeeId == assignedEmployeeId.Value);
        if (facilityId.HasValue) query = query.Where(x =>
            db.Contracts.Any(c => c.ContractId == x.ContractId && c.FacilityId == facilityId.Value));
        return query;
    }

    public Task<int> CountAsync(Guid? customerId, Guid? facilityId, Guid? assignedEmployeeId, CancellationToken ct) =>
        Scoped(customerId, facilityId, assignedEmployeeId).CountAsync(ct);

    public async Task<IReadOnlyList<SupportTicket>> ListAsync(Guid? customerId, Guid? facilityId,
        Guid? assignedEmployeeId, int skip, int take, CancellationToken ct) =>
        await Scoped(customerId, facilityId, assignedEmployeeId)
            .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.SupportTicketId)
            .Skip(skip).Take(take).ToListAsync(ct);

    public Task<SupportTicketRecord?> GetAsync(Guid ticketId, CancellationToken ct) =>
        (from ticket in db.SupportTickets.AsNoTracking()
         join contract in db.Contracts.AsNoTracking() on ticket.ContractId equals contract.ContractId
         where ticket.SupportTicketId == ticketId
         select new SupportTicketRecord(ticket, contract.FacilityId))
        .SingleOrDefaultAsync(ct);

    public async Task<Guid> CreateAsync(Guid contractId, Guid customerId, string category,
        string description, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        try
        {
            if (shouldClose) await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.usp_CreateSupportTicket";
            command.CommandType = CommandType.StoredProcedure;
            AddGuid(command, "@ContractId", contractId);
            AddGuid(command, "@CustomerId", customerId);
            AddString(command, "@Category", category, DbType.AnsiString, 50);
            AddString(command, "@Description", description, DbType.String, 1000);
            var result = await command.ExecuteScalarAsync(ct);
            return result is Guid ticketId ? ticketId
                : throw new InvalidOperationException("SupportTicket creation returned no identifier.");
        }
        catch (SqlException ex) when (ex.Number == 51152)
        {
            throw new StoredProcedureBusinessException("SUPPORT_CONTRACT_NOT_OWNED", "SUPPORT_CONTRACT_NOT_OWNED");
        }
        finally
        {
            if (shouldClose && connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }
    }

    public async Task CompleteAsync(Guid ticketId, Guid staffEmployeeId, string resultNote, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        try
        {
            if (shouldClose) await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.usp_CompleteSupportTicket";
            command.CommandType = CommandType.StoredProcedure;
            AddGuid(command, "@SupportTicketId", ticketId);
            AddGuid(command, "@StaffEmployeeId", staffEmployeeId);
            AddString(command, "@ResultNote", resultNote, DbType.String, 1000);
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (SqlException ex) when (ex.Number == 51146)
        {
            throw new StoredProcedureBusinessException(
                "SUPPORT_TICKET_INVALID_STATUS_OR_ACTOR", "SUPPORT_TICKET_INVALID_STATUS_OR_ACTOR");
        }
        finally
        {
            if (shouldClose && connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }
    }

    public async Task AssignAsync(Guid ticketId, Guid managerEmployeeId, Guid staffEmployeeId, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        try
        {
            if (shouldClose) await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.usp_AssignSupportTicket";
            command.CommandType = CommandType.StoredProcedure;
            AddGuid(command, "@SupportTicketId", ticketId);
            AddGuid(command, "@ManagerEmployeeId", managerEmployeeId);
            AddGuid(command, "@StaffEmployeeId", staffEmployeeId);
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (SqlException ex) when (ex.Number is 51146 or 51147 or 51114)
        {
            var code = ex.Number switch
            {
                51146 => "SUPPORT_TICKET_INVALID_STATUS",
                51147 => "FACILITY_MANAGER_SCOPE_INVALID",
                _ => "STAFF_FACILITY_OR_ROLE_INVALID"
            };
            throw new StoredProcedureBusinessException(code, code);
        }
        finally
        {
            if (shouldClose && connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }
    }

    public async Task CancelAsync(Guid ticketId, Guid customerId, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        try
        {
            if (shouldClose) await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.usp_CancelSupportTicket";
            command.CommandType = CommandType.StoredProcedure;
            AddGuid(command, "@SupportTicketId", ticketId);
            AddGuid(command, "@CustomerId", customerId);
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (SqlException ex) when (ex.Number == 51146)
        {
            throw new StoredProcedureBusinessException(
                "SUPPORT_TICKET_INVALID_STATUS_OR_OWNER", "SUPPORT_TICKET_INVALID_STATUS_OR_OWNER");
        }
        finally
        {
            if (shouldClose && connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }
    }

    private static void AddString(System.Data.Common.DbCommand command, string name,
        string value, DbType type, int size)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = type;
        parameter.Size = size;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static void AddGuid(System.Data.Common.DbCommand command, string name, Guid value)
    {
        var p = command.CreateParameter();
        p.ParameterName = name;
        p.DbType = DbType.Guid;
        p.Value = value;
        command.Parameters.Add(p);
    }
}

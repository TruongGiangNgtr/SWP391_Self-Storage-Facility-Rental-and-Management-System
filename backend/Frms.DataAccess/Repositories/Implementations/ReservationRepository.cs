using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Frms.DataAccess.StoredProcedures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class ReservationRepository(
    FrmsDbContext dbContext) : IReservationRepository {
    public Task<Guid?> GetCustomerIdByUserAccountIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken) {
        return dbContext.Customers
            .AsNoTracking()
            .Where(x => x.UserAccountId == userAccountId)
            .Select(x => (Guid?)x.CustomerId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<Guid?> GetEmployeeFacilityIdByUserAccountIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken) {
        return dbContext.Employees
            .AsNoTracking()
            .Where(x => x.UserAccountId == userAccountId)
            .Select(x => x.FacilityId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CreatedReservationRecord> CreateAsync(
        Guid customerId,
        Guid facilityId,
        Guid unitTypeId,
        DateOnly startMonth,
        DateOnly endMonth,
        CancellationToken cancellationToken) {
        Guid reservationId;
        Guid depositInvoiceId;

        var connection =
            dbContext.Database.GetDbConnection();

        var shouldCloseConnection =
            connection.State != ConnectionState.Open;

        try {
            if (shouldCloseConnection) {
                await connection.OpenAsync(
                    cancellationToken);
            }

            await using var command =
                connection.CreateCommand();

            command.CommandText =
                "dbo.usp_CreateReservation";

            command.CommandType =
                CommandType.StoredProcedure;

            var customerParameter =
                command.CreateParameter();

            customerParameter.ParameterName =
                "@CustomerId";

            customerParameter.DbType =
                DbType.Guid;

            customerParameter.Value =
                customerId;

            command.Parameters.Add(
                customerParameter);

            var facilityParameter =
                command.CreateParameter();

            facilityParameter.ParameterName =
                "@FacilityId";

            facilityParameter.DbType =
                DbType.Guid;

            facilityParameter.Value =
                facilityId;

            command.Parameters.Add(
                facilityParameter);

            var unitTypeParameter =
                command.CreateParameter();

            unitTypeParameter.ParameterName =
                "@UnitTypeId";

            unitTypeParameter.DbType =
                DbType.Guid;

            unitTypeParameter.Value =
                unitTypeId;

            command.Parameters.Add(
                unitTypeParameter);

            var startMonthParameter =
                command.CreateParameter();

            startMonthParameter.ParameterName =
                "@StartMonth";

            startMonthParameter.DbType =
                DbType.Date;

            startMonthParameter.Value =
                startMonth.ToDateTime(
                    TimeOnly.MinValue);

            command.Parameters.Add(
                startMonthParameter);

            var endMonthParameter =
                command.CreateParameter();

            endMonthParameter.ParameterName =
                "@EndMonth";

            endMonthParameter.DbType =
                DbType.Date;

            endMonthParameter.Value =
                endMonth.ToDateTime(
                    TimeOnly.MinValue);

            command.Parameters.Add(
                endMonthParameter);

            await using var reader =
                await command.ExecuteReaderAsync(
                    cancellationToken);

            if (!await reader.ReadAsync(
                    cancellationToken)) {
                throw new InvalidOperationException(
                    "usp_CreateReservation returned no result.");
            }

            reservationId =
                reader.GetGuid(
                    reader.GetOrdinal(
                        "ReservationId"));

            depositInvoiceId =
                reader.GetGuid(
                    reader.GetOrdinal(
                        "DepositInvoiceId"));
        }
        catch (SqlException ex)
            when (ex.Number is
                51101 or
                51102 or
                51103 or
                51104 or
                51106) {
            var code = ex.Number switch {
                51101 => "INVALID_MONTH_RANGE",
                51102 => "ACCOUNT_INACTIVE",
                51103 => "FACILITY_INACTIVE",
                51104 => "RESOURCE_NOT_FOUND",
                51106 => "CAPACITY_NOT_AVAILABLE",
                _ => throw new InvalidOperationException()
            };

            throw new StoredProcedureBusinessException(
                code,
                code);
        }
        finally {
            if (shouldCloseConnection &&
                connection.State ==
                ConnectionState.Open) {
                await connection.CloseAsync();
            }
        }

        var reservation =
            await dbContext.Reservations
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.ReservationId ==
                        reservationId,
                    cancellationToken);

        var depositInvoice =
            await dbContext.Invoices
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.InvoiceId ==
                        depositInvoiceId,
                    cancellationToken);

        return new CreatedReservationRecord(
            reservation.ReservationId,
            reservation.FacilityId,
            reservation.UnitTypeId,
            reservation.PolicyId,
            reservation.StartMonth,
            reservation.EndMonth,
            reservation.LockedRentalPrice,
            reservation.DepositAmount,
            reservation.Status,
            new DepositInvoiceRecord(
                depositInvoice.InvoiceId,
                depositInvoice.Status,
                depositInvoice.AmountDue,
                depositInvoice.DueDate),
            reservation.CreatedAt);
    }

    public Task<PagedReservationRecord> ListByCustomerAsync(
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken) {
        var query =
            dbContext.Reservations
                .AsNoTracking()
                .Where(
                    x =>
                        x.CustomerId ==
                        customerId);

        return ListAsync(
            query,
            page,
            pageSize,
            cancellationToken);
    }

    public Task<PagedReservationRecord> ListByFacilityAsync(
        Guid facilityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken) {
        var query =
            dbContext.Reservations
                .AsNoTracking()
                .Where(
                    x =>
                        x.FacilityId ==
                        facilityId);

        return ListAsync(
            query,
            page,
            pageSize,
            cancellationToken);
    }

    public Task<PagedReservationRecord> ListAllAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken) {
        var query =
            dbContext.Reservations
                .AsNoTracking();

        return ListAsync(
            query,
            page,
            pageSize,
            cancellationToken);
    }

    public async Task<ReservationDetailRecord?> GetByIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken) {
        var reservation =
            await dbContext.Reservations
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.ReservationId ==
                        reservationId,
                    cancellationToken);

        if (reservation is null) {
            return null;
        }

        var depositInvoice =
            await dbContext.Invoices
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.EntityId ==
                        reservationId &&
                        x.InvoiceType ==
                        "DEPOSIT",
                    cancellationToken);

        if (depositInvoice is null) {
            throw new InvalidOperationException(
                $"Reservation {reservationId} has no DEPOSIT invoice.");
        }

        var visit =
            await dbContext.Visits
                .AsNoTracking()
                .Where(
                    x =>
                        x.EntityId ==
                        reservationId &&
                        x.VisitType ==
                        "RESERVATION")
                .OrderByDescending(
                    x => x.VisitDate)
                .ThenByDescending(
                    x => x.VisitId)
                .FirstOrDefaultAsync(
                    cancellationToken);

        return MapDetail(
            reservation,
            depositInvoice,
            visit);
    }

    public async Task<ConfirmedReservationRecord> ConfirmAsync(
        Guid customerId,
        Guid reservationId,
        DateOnly reservationVisitDate,
        CancellationToken cancellationToken) {
        var ownedReservation =
            await dbContext.Reservations
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.ReservationId ==
                        reservationId &&
                        x.CustomerId ==
                        customerId,
                    cancellationToken);

        if (ownedReservation is null) {
            throw new StoredProcedureBusinessException(
                "RESOURCE_NOT_FOUND",
                "RESOURCE_NOT_FOUND");
        }

        if (ownedReservation.Status == "CONFIRMED") {
            var existing =
                await TryGetConfirmedAsync(
                    customerId,
                    reservationId,
                    cancellationToken);

            if (existing is not null) {
                return existing;
            }
        }

        Guid visitId;
        string reservationStatus;

        var connection =
            dbContext.Database
                .GetDbConnection();

        var shouldCloseConnection =
            connection.State !=
            ConnectionState.Open;

        try {
            if (shouldCloseConnection) {
                await connection.OpenAsync(
                    cancellationToken);
            }

            await using var command =
                connection.CreateCommand();

            command.CommandText =
                "dbo.usp_ConfirmReservation";

            command.CommandType =
                CommandType.StoredProcedure;

            var reservationParameter =
                command.CreateParameter();

            reservationParameter.ParameterName =
                "@ReservationId";

            reservationParameter.DbType =
                DbType.Guid;

            reservationParameter.Value =
                reservationId;

            command.Parameters.Add(
                reservationParameter);

            var visitDateParameter =
                command.CreateParameter();

            visitDateParameter.ParameterName =
                "@ReservationVisitDate";

            visitDateParameter.DbType =
                DbType.Date;

            visitDateParameter.Value =
                reservationVisitDate.ToDateTime(
                    TimeOnly.MinValue);

            command.Parameters.Add(
                visitDateParameter);

            await using var reader =
                await command.ExecuteReaderAsync(
                    cancellationToken);

            if (!await reader.ReadAsync(
                    cancellationToken)) {
                throw new InvalidOperationException(
                    "usp_ConfirmReservation returned no result.");
            }

            visitId =
                reader.GetGuid(
                    reader.GetOrdinal(
                        "VisitId"));

            reservationStatus =
                reader.GetString(
                    reader.GetOrdinal(
                        "Status"));
        }
        catch (SqlException ex)
            when (ex.Number is
                51107 or
                51108 or
                51109 or
                51110) {
            if (ex.Number is
                51107 or
                51110) {
                var existing =
                    await TryGetConfirmedAsync(
                        customerId,
                        reservationId,
                        cancellationToken);

                if (existing is not null) {
                    return existing;
                }
            }

            var code = ex.Number switch {
                51107 =>
                    "RESERVATION_INVALID_STATUS",

                51108 =>
                    "DEPOSIT_NOT_PAID",

                51109 =>
                    "VISIT_DATE_OUT_OF_POLICY",

                51110 =>
                    "RESERVATION_INVALID_STATUS",

                _ =>
                    throw new InvalidOperationException()
            };

            throw new StoredProcedureBusinessException(
                code,
                code);
        }
        finally {
            if (shouldCloseConnection &&
                connection.State ==
                ConnectionState.Open) {
                await connection.CloseAsync();
            }
        }

        var visit =
            await dbContext.Visits
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.VisitId ==
                        visitId,
                    cancellationToken);

        return new ConfirmedReservationRecord(
            reservationId,
            reservationStatus,
            new ReservationVisitRecord(
                visit.VisitId,
                visit.VisitType,
                visit.VisitDate,
                visit.Status));
    }

    private async Task<PagedReservationRecord> ListAsync(
        IQueryable<Reservation> scopedQuery,
        int page,
        int pageSize,
        CancellationToken cancellationToken) {
        var totalItems =
            await scopedQuery.CountAsync(
                cancellationToken);

        var reservations =
            await scopedQuery
                .OrderByDescending(
                    x => x.CreatedAt)
                .ThenByDescending(
                    x => x.ReservationId)
                .Skip(
                    (page - 1) *
                    pageSize)
                .Take(pageSize)
                .ToListAsync(
                    cancellationToken);

        var totalPages =
            totalItems == 0
                ? 0
                : (int)Math.Ceiling(
                    totalItems /
                    (double)pageSize);

        if (reservations.Count == 0) {
            return new PagedReservationRecord(
                Array.Empty<ReservationDetailRecord>(),
                page,
                pageSize,
                totalItems,
                totalPages);
        }

        var reservationIds =
            reservations
                .Select(
                    x => x.ReservationId)
                .ToArray();

        var depositInvoices =
            await dbContext.Invoices
                .AsNoTracking()
                .Where(
                    x =>
                        reservationIds.Contains(
                            x.EntityId) &&
                        x.InvoiceType ==
                        "DEPOSIT")
                .ToListAsync(
                    cancellationToken);

        var invoiceByReservationId =
            depositInvoices
                .ToDictionary(
                    x => x.EntityId);

        var reservationVisits =
            await dbContext.Visits
                .AsNoTracking()
                .Where(
                    x =>
                        reservationIds.Contains(
                            x.EntityId) &&
                        x.VisitType ==
                        "RESERVATION")
                .ToListAsync(
                    cancellationToken);

        var visitByReservationId =
            reservationVisits
                .GroupBy(
                    x => x.EntityId)
                .ToDictionary(
                    x => x.Key,
                    x =>
                        x.OrderByDescending(
                                v => v.VisitDate)
                            .ThenByDescending(
                                v => v.VisitId)
                            .First());

        var items =
            new List<ReservationDetailRecord>(
                reservations.Count);

        foreach (var reservation in reservations) {
            if (!invoiceByReservationId.TryGetValue(
                    reservation.ReservationId,
                    out var depositInvoice)) {
                throw new InvalidOperationException(
                    $"Reservation {reservation.ReservationId} has no DEPOSIT invoice.");
            }

            visitByReservationId.TryGetValue(
                reservation.ReservationId,
                out var reservationVisit);

            items.Add(
                MapDetail(
                    reservation,
                    depositInvoice,
                    reservationVisit));
        }

        return new PagedReservationRecord(
            items,
            page,
            pageSize,
            totalItems,
            totalPages);
    }

    private static ReservationDetailRecord MapDetail(
        Reservation reservation,
        Invoice depositInvoice,
        Visit? reservationVisit) {
        return new ReservationDetailRecord(
            reservation.ReservationId,
            reservation.CustomerId,
            reservation.FacilityId,
            reservation.UnitTypeId,
            reservation.PolicyId,
            reservation.StartMonth,
            reservation.EndMonth,
            reservation.LockedRentalPrice,
            reservation.DepositAmount,
            reservation.Status,
            new DepositInvoiceRecord(
                depositInvoice.InvoiceId,
                depositInvoice.Status,
                depositInvoice.AmountDue,
                depositInvoice.DueDate),
            reservationVisit is null
                ? null
                : new ReservationVisitRecord(
                    reservationVisit.VisitId,
                    reservationVisit.VisitType,
                    reservationVisit.VisitDate,
                    reservationVisit.Status),
            reservation.CreatedAt);
    }

    private async Task<ConfirmedReservationRecord?>
        TryGetConfirmedAsync(
            Guid customerId,
            Guid reservationId,
            CancellationToken cancellationToken) {
        var reservation =
            await dbContext.Reservations
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.ReservationId ==
                        reservationId &&
                        x.CustomerId ==
                        customerId &&
                        x.Status ==
                        "CONFIRMED",
                    cancellationToken);

        if (reservation is null) {
            return null;
        }

        var visit =
            await dbContext.Visits
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.EntityId ==
                        reservationId &&
                        x.VisitType ==
                        "RESERVATION",
                    cancellationToken);

        if (visit is null) {
            return null;
        }

        return new ConfirmedReservationRecord(
            reservation.ReservationId,
            reservation.Status,
            new ReservationVisitRecord(
                visit.VisitId,
                visit.VisitType,
                visit.VisitDate,
                visit.Status));
    }
}

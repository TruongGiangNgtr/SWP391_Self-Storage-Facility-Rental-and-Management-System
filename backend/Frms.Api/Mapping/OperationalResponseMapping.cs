using Frms.Api.DTOs.Responses;
using Frms.Business.Models;
using Frms.Business.Models.Results;

namespace Frms.Api.Mapping;

internal static class OperationalResponseMapping
{
    public static StorageUnitDetailResponse ToResponse(this StorageUnitResult result) => new(
        result.StorageUnitId, result.FacilityId, result.UnitTypeId,
        result.UnitCode, result.LocationInfo, result.Status);

    public static DiscountResponse ToResponse(this DiscountResult result) => new(
        result.DiscountId, result.CustomerId, result.Name, result.Percentage,
        result.Status, AsUtc(result.EffectiveFrom),
        result.EffectiveTo is null ? null : AsUtc(result.EffectiveTo.Value));

    public static SupportTicketDetailResponse ToResponse(this SupportTicketResult ticket) => new(
        ticket.SupportTicketId, ticket.ContractId, ticket.CustomerId,
        ticket.AssignedEmployeeId, ticket.Category, ticket.Description,
        ticket.Status, ticket.ResultNote,
        new DateTimeOffset(DateTime.SpecifyKind(ticket.CreatedAt, DateTimeKind.Utc)),
        ticket.CompletedAt.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(ticket.CompletedAt.Value, DateTimeKind.Utc))
            : null);

    private static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Utc
        ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}

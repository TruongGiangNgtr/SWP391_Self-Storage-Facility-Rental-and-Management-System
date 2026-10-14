namespace Frms.Api.DTOs.Responses;

public sealed record ExtraFeeTypeCatalogResponse(Guid ExtraFeeTypeId, string Name,
    decimal DefaultAmount, string Status);
public sealed record EmployeeDirectoryResponse(Guid EmployeeId, Guid? FacilityId,
    string FullName, string Role, string Status);

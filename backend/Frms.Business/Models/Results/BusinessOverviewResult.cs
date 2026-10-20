namespace Frms.Business.Models.Results;

public sealed record BusinessOverviewResult(
    int FacilityCount, int ActiveContractCount, decimal UsageRate, decimal RentalRevenue);

namespace Frms.DataAccess.Repositories.Models;

public sealed record BusinessPortfolioRecord(int FacilityCount, int ActiveContractCount,
    int InUseUnits, int TotalUnits);

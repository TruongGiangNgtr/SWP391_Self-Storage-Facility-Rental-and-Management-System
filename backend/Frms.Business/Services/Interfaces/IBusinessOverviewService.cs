using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface IBusinessOverviewService
{
    Task<BusinessOverviewResult> GetAsync(CancellationToken cancellationToken);
}

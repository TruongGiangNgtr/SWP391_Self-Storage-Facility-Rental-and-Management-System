using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Services.Implementations;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.UnitTests;

[TestFixture]
public sealed class StaffContractListScopeTests
{
    private static readonly Guid AssignedFacilityId = Guid.Parse("aaaaaaaa-1111-4444-8888-111111111111");

    [Test]
    public async Task FacilityStaff_ListContracts_UsesAssignedFacilityForCountAndPage()
    {
        var repository = new TrackingContractRepository();
        var facility = new StubFacilityAuthorization(AssignedFacilityId);
        var service = new ContractService(repository, new StaffContext(), facility);

        var result = await service.ListAccessibleAsync(2, 10);

        Assert.Multiple(() =>
        {
            Assert.That(facility.Reads, Is.EqualTo(1));
            Assert.That(repository.CountFacilityId, Is.EqualTo(AssignedFacilityId));
            Assert.That(repository.PageFacilityId, Is.EqualTo(AssignedFacilityId));
            Assert.That(repository.Skip, Is.EqualTo(10));
            Assert.That(repository.Take, Is.EqualTo(10));
            Assert.That(result.Page, Is.EqualTo(2));
            Assert.That(result.PageSize, Is.EqualTo(10));
            Assert.That(result.TotalItems, Is.EqualTo(25));
            Assert.That(result.TotalPages, Is.EqualTo(3));
            Assert.That(repository.CountAllCalled, Is.False);
            Assert.That(repository.ListAllCalled, Is.False);
            Assert.That(repository.CountCustomerCalled, Is.False);
        });
    }

    [Test]
    public void FacilityStaff_WithoutFacility_ReturnsForbiddenBeforeRepositoryRead()
    {
        var repository = new TrackingContractRepository();
        var facility = new StubFacilityAuthorization(null);
        var service = new ContractService(repository, new StaffContext(), facility);

        var ex = Assert.ThrowsAsync<BusinessException>(() => service.ListAccessibleAsync(1, 20));
        Assert.Multiple(() =>
        {
            Assert.That(ex!.Code, Is.EqualTo("FORBIDDEN"));
            Assert.That(ex.SuggestedStatusCode, Is.EqualTo(403));
            Assert.That(repository.CountFacilityId, Is.Null);
            Assert.That(repository.PageFacilityId, Is.Null);
        });
    }

    [TestCase(0, 20)]
    [TestCase(1, 0)]
    [TestCase(1, 101)]
    public void FacilityStaff_InvalidPagination_IsRejectedBeforeDatabase(int page, int pageSize)
    {
        var repository = new TrackingContractRepository();
        var service = new ContractService(repository, new StaffContext(), new StubFacilityAuthorization(AssignedFacilityId));
        var ex = Assert.ThrowsAsync<BusinessException>(() => service.ListAccessibleAsync(page, pageSize));
        Assert.That(ex!.Code, Is.EqualTo("VALIDATION_ERROR"));
        Assert.That(repository.CountFacilityId, Is.Null);
    }

    [Test]
    public void Administrator_CannotListContracts()
    {
        var service = new ContractService(new TrackingContractRepository(),
            new StaffContext("SYSTEM_ADMINISTRATOR"), new StubFacilityAuthorization(AssignedFacilityId));
        var ex = Assert.ThrowsAsync<BusinessException>(() => service.ListAccessibleAsync(1, 20));
        Assert.That(ex!.Code, Is.EqualTo("FORBIDDEN"));
    }

    private sealed class StaffContext(string role = "FACILITY_STAFF") : ICurrentUserContext
    {
        public bool IsAuthenticated => true;
        public Guid UserAccountId => Guid.Parse("bbbbbbbb-2222-4444-8888-222222222222");
        public string Role => role;
    }

    private sealed class StubFacilityAuthorization(Guid? facilityId) : IFacilityAuthorizationService
    {
        public int Reads { get; private set; }
        public Task<Guid> GetAssignedFacilityIdAsync(CancellationToken ct = default)
        {
            Reads++;
            if (facilityId is null)
                throw new BusinessException("FORBIDDEN", "Staff has no assigned Facility.", 403);
            return Task.FromResult(facilityId.Value);
        }
        public Task EnsureSameFacilityAsync(Guid targetFacilityId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class TrackingContractRepository : IContractRepository
    {
        public Guid? CountFacilityId { get; private set; }
        public Guid? PageFacilityId { get; private set; }
        public int Skip { get; private set; }
        public int Take { get; private set; }
        public bool CountAllCalled { get; private set; }
        public bool ListAllCalled { get; private set; }
        public bool CountCustomerCalled { get; private set; }

        public Task<int> CountByFacilityAsync(Guid facilityId, CancellationToken ct)
        {
            CountFacilityId = facilityId;
            return Task.FromResult(25);
        }
        public Task<IReadOnlyList<ContractSummaryRecord>> ListByFacilityAsync(Guid facilityId, int skip, int take, CancellationToken ct)
        {
            PageFacilityId = facilityId;
            Skip = skip;
            Take = take;
            return Task.FromResult<IReadOnlyList<ContractSummaryRecord>>(Array.Empty<ContractSummaryRecord>());
        }
        public Task<int> CountAllAsync(CancellationToken ct)
        {
            CountAllCalled = true;
            throw new InvalidOperationException("Staff must not read system-wide Contract counts.");
        }
        public Task<IReadOnlyList<ContractSummaryRecord>> ListAllAsync(int skip, int take, CancellationToken ct)
        {
            ListAllCalled = true;
            throw new InvalidOperationException("Staff must not list system-wide Contracts.");
        }
        public Task<int> CountByCustomerAsync(Guid customerId, CancellationToken ct)
        {
            CountCustomerCalled = true;
            throw new InvalidOperationException("Staff is not a Customer.");
        }
        public Task<Guid?> GetCustomerIdByUserAccountIdAsync(Guid accountId, CancellationToken ct) =>
            throw new InvalidOperationException("Unexpected Customer lookup.");
        public Task<IReadOnlyList<ContractSummaryRecord>> ListByCustomerAsync(Guid customerId, int skip, int take, CancellationToken ct) =>
            throw new InvalidOperationException("Unexpected Customer list.");
        public Task<ContractDetailRecord?> GetDetailAsync(Guid customerId, Guid contractId, CancellationToken ct) =>
            throw new InvalidOperationException("Unexpected detail query.");
        public Task<ContractDetailRecord?> GetDetailByIdAsync(Guid contractId, CancellationToken ct) =>
            throw new InvalidOperationException("Unexpected detail query.");
    }
}

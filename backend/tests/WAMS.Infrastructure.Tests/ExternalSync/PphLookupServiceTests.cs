using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Companies;
using WAMS.Application.Interfaces.Vendors;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.TaxTypes;
using WAMS.Domain.Entities.Vendors;
using WAMS.Domain.Enums;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.ExternalSync.ErpHttpClient;
using WAMS.Infrastructure.ExternalSync.Pph;
using WAMS.Infrastructure.Tests.Caching;
using Xunit;

namespace WAMS.Infrastructure.Tests.ExternalSync;

// Uses a real in-memory HybridCache via CacheTestFixture (Infrastructure.Tests/Caching/CacheTestFixture.cs)
// rather than mocking HybridCache directly - HybridCache's cache methods are awkward to mock and this
// codebase's existing cache-decorator tests already established the real-in-memory-instance convention.
public class PphLookupServiceTests : IDisposable
{
    private readonly CacheTestFixture _cacheFixture = new();

    public void Dispose() => _cacheFixture.Dispose();

    private static ITenantContext CreateTenantContext()
    {
        var tc = Substitute.For<ITenantContext>();
        tc.IsSet.Returns(false);
        tc.CompanyId.Returns((long?)null);
        return tc;
    }

    private static AppDbContext CreateDb()
        => new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            CreateTenantContext());

    private static ErpApiClient CreateErp(List<PphErpDto>? erpData)
    {
        var handler = new FakeHttpHandler(erpData);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://erp.test") };
        return new ErpApiClient(http, Substitute.For<ILogger<ErpApiClient>>());
    }

    private static async Task<(AppDbContext db, VendorShadow vendor)> SeedVendorAsync(AppDbContext db)
    {
        var company = new Company { Code = "GCU", Name = "Test Co", IsActive = true };
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var vendor = new VendorShadow
        {
            CompanyId = company.Id, CardCode = "V001", CardName = "Vendor One",
            IsActive = true, FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
        };
        db.VendorShadows.Add(vendor);
        await db.SaveChangesAsync();

        return (db, vendor);
    }

    private PphLookupService CreateService(AppDbContext db, ErpApiClient erp)
        => new(db, erp, new VendorShadowRepositoryStub(db), new CompanyRepositoryStub(db),
            _cacheFixture.Cache, Substitute.For<ICacheInvalidationService>(),
            Substitute.For<ILogger<PphLookupService>>());

    [Fact]
    public async Task GetOrRefreshAsync_NewAssignments_UpsertsTaxTypeAndAssignment()
    {
        await using var db = CreateDb();
        var (_, vendor) = await SeedVendorAsync(db);
        var erp = CreateErp([new("V001", "Vendor One", "P23c", "Hutang PPH Pasal 23 - 2", 2.0m)]);
        var svc = CreateService(db, erp);

        var result = await svc.GetOrRefreshAsync(vendor.Id, TestContext.Current.CancellationToken);

        result.Should().ContainSingle(t => t.Code == "P23c" && t.Rate == 2.0m);
        db.VendorPphAssignments.Count(a => a.VendorShadowId == vendor.Id && a.IsActive).Should().Be(1);
    }

    [Fact]
    public async Task GetOrRefreshAsync_EmptyResponse_DeactivatesAllPersistedAssignments()
    {
        await using var db = CreateDb();
        var (_, vendor) = await SeedVendorAsync(db);

        var taxType = new TaxType
        {
            CompanyId = vendor.CompanyId, Category = TaxCategory.Pph, Code = "P23c", Name = "Hutang PPH Pasal 23 - 2",
            Rate = 2.0m, IsActive = true, SyncedAt = DateTime.UtcNow, FirstSeenAt = DateTime.UtcNow,
        };
        db.TaxTypes.Add(taxType);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.VendorPphAssignments.Add(new VendorPphAssignment
        {
            VendorShadowId = vendor.Id, TaxTypeId = taxType.Id, IsActive = true,
            SyncedAt = DateTime.UtcNow, FirstSeenAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var erp = CreateErp([]);
        var svc = CreateService(db, erp);

        var result = await svc.GetOrRefreshAsync(vendor.Id, TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
        db.VendorPphAssignments.Single(a => a.VendorShadowId == vendor.Id).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrRefreshAsync_SapCallFails_FallsBackToPersistedAssignments()
    {
        await using var db = CreateDb();
        var (_, vendor) = await SeedVendorAsync(db);

        var taxType = new TaxType
        {
            CompanyId = vendor.CompanyId, Category = TaxCategory.Pph, Code = "P21a", Name = "Hutang PPH Pasal 21",
            Rate = 2.5m, IsActive = true, SyncedAt = DateTime.UtcNow, FirstSeenAt = DateTime.UtcNow,
        };
        db.TaxTypes.Add(taxType);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.VendorPphAssignments.Add(new VendorPphAssignment
        {
            VendorShadowId = vendor.Id, TaxTypeId = taxType.Id, IsActive = true,
            SyncedAt = DateTime.UtcNow, FirstSeenAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var erp = CreateErp(null); // simulates SAP call failure (5xx/network error -> ErpApiClient returns null)
        var svc = CreateService(db, erp);

        var result = await svc.GetOrRefreshAsync(vendor.Id, TestContext.Current.CancellationToken);

        result.Should().ContainSingle(t => t.Code == "P21a");
        db.VendorPphAssignments.Single(a => a.VendorShadowId == vendor.Id).IsActive.Should().BeTrue();
    }

    private sealed class FakeHttpHandler(object? data) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (data is null)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            var json = JsonSerializer.Serialize(data);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class VendorShadowRepositoryStub(AppDbContext db) : IVendorShadowRepository
    {
        public Task<(List<VendorShadow> Items, int TotalCount)> GetAllAsync(WAMS.Application.Common.DataTableQuery query, CancellationToken ct = default)
            => throw new NotImplementedException();
        public IAsyncEnumerable<WAMS.Application.DTOs.Vendors.VendorSummaryResponse> StreamAllAsync(WAMS.Application.Common.DataTableQuery query, int limit, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<VendorShadow?> GetByIdAsync(long id, CancellationToken ct = default)
            => db.VendorShadows.FirstOrDefaultAsync(v => v.Id == id, ct);
        public Task<List<VendorShadow>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task UpsertManyAsync(IEnumerable<VendorShadow> vendors, CancellationToken ct = default)
            => throw new NotImplementedException();
    }

    private sealed class CompanyRepositoryStub(AppDbContext db) : ICompanyRepository
    {
        public Task<Company?> GetByIdAsync(long id, CancellationToken ct = default)
            => db.Companies.FirstOrDefaultAsync(c => c.Id == id, ct);
        public Task<WAMS.Application.DTOs.Companies.CompanyResponse?> GetByIdWithCountsAsync(long id, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<Company?> GetByCodeAsync(string code, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<(List<WAMS.Application.DTOs.Companies.CompanyResponse> Items, int TotalCount)> GetAllAsync(WAMS.Application.Common.DataTableQuery query, CancellationToken ct = default)
            => throw new NotImplementedException();
        public IAsyncEnumerable<WAMS.Application.DTOs.Companies.CompanyResponse> StreamAllAsync(WAMS.Application.Common.DataTableQuery query, int limit, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<List<Company>> GetActiveAsync(string? code = null, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<Company> CreateAsync(Company company, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task UpdateAsync(Company company, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<bool> ExistsAsync(long id, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<bool> CodeExistsAsync(string code, CancellationToken ct = default)
            => throw new NotImplementedException();
    }
}

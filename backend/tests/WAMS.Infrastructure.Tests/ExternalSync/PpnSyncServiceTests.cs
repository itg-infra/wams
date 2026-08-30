using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WAMS.Application.Interfaces.Common;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.SyncLogs;
using WAMS.Domain.Entities.TaxTypes;
using WAMS.Domain.Enums;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.ExternalSync.ErpHttpClient;
using WAMS.Infrastructure.ExternalSync.Ppn;
using Xunit;

namespace WAMS.Infrastructure.Tests.ExternalSync;

public class PpnSyncServiceTests
{
    private static ITenantContext CreateTenantContext()
    {
        var tc = Substitute.For<ITenantContext>();
        tc.IsSet.Returns(false);
        tc.CompanyId.Returns((long?)null);
        return tc;
    }

    private static DbContextOptions<AppDbContext> CreateDbOptions()
        => new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static AppDbContext OpenDb(DbContextOptions<AppDbContext> opts)
        => new AppDbContext(opts, CreateTenantContext());

    private static IDbContextFactory<AppDbContext> CreateFactory(DbContextOptions<AppDbContext> opts)
    {
        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => new AppDbContext(opts, CreateTenantContext()));
        return factory;
    }

    private static PpnSyncService CreateService(
        DbContextOptions<AppDbContext> opts,
        List<PpnErpDto>? erpData)
    {
        var handler = new FakeHttpHandler(erpData);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://erp.test") };
        var erp = new ErpApiClient(http, Substitute.For<ILogger<ErpApiClient>>());
        return new PpnSyncService(
            erp,
            CreateFactory(opts),
            Substitute.For<ILogger<PpnSyncService>>());
    }

    private static async Task SeedCompanyAsync(DbContextOptions<AppDbContext> opts, long companyId = 1, string code = "GCU")
    {
        await using var db = new AppDbContext(opts, CreateTenantContext());
        db.Companies.Add(new Company { Id = companyId, Code = code, Name = "Test Co", IsActive = true });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task SyncAllAsync_AddsNewPpnCodes()
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);
        var svc = CreateService(opts, [new("PPNin0", "PPn In 0%", 0m), new("PPNin11", "PPn In 11%", 11m)]);

        var result = await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        result.Added.Should().Be(2);
        db.TaxTypes.IgnoreQueryFilters().Where(t => t.Category == TaxCategory.Ppn).Should().HaveCount(2);
    }

    [Fact]
    public async Task SyncAllAsync_UpdatesChangedRate()
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);

        await using (var seedDb = OpenDb(opts))
        {
            seedDb.TaxTypes.Add(new TaxType
            {
                CompanyId = 1, Category = TaxCategory.Ppn, Code = "PPNin11", Name = "PPn In 11%", Rate = 10m,
                IsActive = true, FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
            });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var svc = CreateService(opts, [new("PPNin11", "PPn In 11%", 11m)]);
        var result = await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        result.Updated.Should().Be(1);
        db.TaxTypes.IgnoreQueryFilters().Single(t => t.Code == "PPNin11").Rate.Should().Be(11m);
    }

    [Fact]
    public async Task SyncAllAsync_DeactivatesRemovedCode()
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);

        await using (var seedDb = OpenDb(opts))
        {
            seedDb.TaxTypes.Add(new TaxType
            {
                CompanyId = 1, Category = TaxCategory.Ppn, Code = "GONE", Name = "Gone", Rate = 5m,
                IsActive = true, FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
            });
            seedDb.TaxTypes.Add(new TaxType
            {
                CompanyId = 1, Category = TaxCategory.Ppn, Code = "KEEP", Name = "Keep", Rate = 11m,
                IsActive = true, FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
            });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var svc = CreateService(opts, [new("KEEP", "Keep", 11m)]);
        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        db.TaxTypes.IgnoreQueryFilters().Single(t => t.Code == "GONE").IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task SyncAllAsync_EmptyCode_WritesSchemaError()
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);
        var svc = CreateService(opts, [new("", "PPn In 0%", 0m)]);

        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        db.SyncLogs.Single().Outcome.Should().Be(SyncOutcome.SchemaError);
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
}

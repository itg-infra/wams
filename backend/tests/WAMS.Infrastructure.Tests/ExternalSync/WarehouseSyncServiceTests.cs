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
using WAMS.Domain.Entities.Warehouses;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.ExternalSync.ErpHttpClient;
using WAMS.Infrastructure.ExternalSync.Warehouse;
using Xunit;

namespace WAMS.Infrastructure.Tests.ExternalSync;

public class WarehouseSyncServiceTests
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

    private static WarehouseSyncService CreateService(
        DbContextOptions<AppDbContext> opts,
        List<WarehouseErpDto>? erpData)
    {
        var handler = new FakeHttpHandler(erpData);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://erp.test") };
        var erp = new ErpApiClient(http, Substitute.For<ILogger<ErpApiClient>>());
        return new WarehouseSyncService(
            erp,
            CreateFactory(opts),
            Substitute.For<ILogger<WarehouseSyncService>>());
    }

    private static async Task SeedCompanyAsync(DbContextOptions<AppDbContext> opts, long companyId = 1, string code = "C001")
    {
        await using var db = OpenDb(opts);
        db.Companies.Add(new Company
        {
            Id = companyId,
            Code = code,
            Name = "Test Co",
            IsActive = true,
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task SyncAllAsync_AddsNewWarehouses()
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);
        var svc = CreateService(opts, [new("WH01", "Warehouse 1", "Jakarta")]);

        var result = await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        result.Added.Should().Be(1);
        db.WarehouseShadows.Single().Code.Should().Be("WH01");
    }

    [Fact]
    public async Task SyncAllAsync_EmptyWhsCode_WritesSchemaError()
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);
        var svc = CreateService(opts, [new("", "Warehouse", null)]);

        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        db.SyncLogs.Single().Outcome.Should().Be(SyncOutcome.SchemaError);
    }

    [Fact]
    public async Task SyncAllAsync_EmptyWhsName_WritesSchemaError()
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);
        var svc = CreateService(opts, [new("WH01", "", null)]);

        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        db.SyncLogs.Single().Outcome.Should().Be(SyncOutcome.SchemaError);
    }

    [Fact]
    public async Task SyncAllAsync_DeactivatesRemovedWarehouse()
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);

        await using (var seedDb = OpenDb(opts))
        {
            seedDb.WarehouseShadows.Add(new WarehouseShadow
            {
                CompanyId = 1, Code = "GONE", Name = "Gone WH", IsActive = true,
                FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
            });
            seedDb.WarehouseShadows.Add(new WarehouseShadow
            {
                CompanyId = 1, Code = "KEEP01", Name = "Keep WH", IsActive = true,
                FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
            });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var erpData = new List<WarehouseErpDto> { new("KEEP01", "Keep WH", null) };
        var svc = CreateService(opts, erpData);

        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        db.WarehouseShadows.Single(w => w.Code == "GONE").IsActive.Should().BeFalse();
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

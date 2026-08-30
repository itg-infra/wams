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
using WAMS.Domain.Entities.Vendors;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.ExternalSync.ErpHttpClient;
using WAMS.Infrastructure.ExternalSync.Vendor;
using Xunit;

namespace WAMS.Infrastructure.Tests.ExternalSync;

public class VendorSyncServiceTests
{
    // Create a stub ITenantContext with IsSet=false so query filters bypass tenant scoping.
    private static ITenantContext CreateTenantContext()
    {
        var tc = Substitute.For<ITenantContext>();
        tc.IsSet.Returns(false);
        tc.CompanyId.Returns((long?)null);
        return tc;
    }

    // Each test gets its own in-memory database (unique name) so tests are isolated.
    // The factory creates a NEW AppDbContext on each call using the SAME options
    // (same database name), so all calls within a test share the same in-memory store.
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

    private static VendorSyncService CreateService(
        DbContextOptions<AppDbContext> opts,
        List<VendorErpDto>? erpData)
    {
        var handler = new FakeHttpHandler(erpData);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://erp.test") };
        var erp = new ErpApiClient(http, Substitute.For<ILogger<ErpApiClient>>());
        return new VendorSyncService(
            erp,
            CreateFactory(opts),
            Substitute.For<ILogger<VendorSyncService>>());
    }

    private static async Task SeedCompanyAsync(DbContextOptions<AppDbContext> opts, long companyId = 1, string code = "C001")
    {
        await using var db = new AppDbContext(opts, CreateTenantContext());
        db.Companies.Add(new Company { Id = companyId, Code = code, Name = "Test Co", IsActive = true });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task SyncAllAsync_AddsNewVendors()
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);
        var svc = CreateService(opts, [new("V001", "Vendor One"), new("V002", "Vendor Two")]);

        var result = await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        result.Added.Should().Be(2);
        db.VendorShadows.Should().HaveCount(2);
    }

    [Fact]
    public async Task SyncAllAsync_EmptyCardCode_WritesSchemaError()
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);
        var svc = CreateService(opts, [new("", "Vendor")]);

        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        db.SyncLogs.Single().Outcome.Should().Be(SyncOutcome.SchemaError);
    }

    [Fact]
    public async Task SyncAllAsync_EmptyCardName_WritesSchemaError()
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);
        var svc = CreateService(opts, [new("V001", "")]);

        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        db.SyncLogs.Single().Outcome.Should().Be(SyncOutcome.SchemaError);
    }

    [Fact]
    public async Task SyncAllAsync_DeactivatesRemovedVendor()
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);

        await using (var seedDb = OpenDb(opts))
        {
            seedDb.VendorShadows.Add(new VendorShadow
            {
                CompanyId = 1, CardCode = "GONE", CardName = "Gone Vendor",
                IsActive = true, FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
            });
            seedDb.VendorShadows.Add(new VendorShadow
            {
                CompanyId = 1, CardCode = "KEEP01", CardName = "Keep Vendor",
                IsActive = true, FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
            });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var erpData = new List<VendorErpDto> { new("KEEP01", "Keep Vendor") };
        var svc = CreateService(opts, erpData);

        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        db.VendorShadows.Single(v => v.CardCode == "GONE").IsActive.Should().BeFalse();
    }

    // Helpers 
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

// tests/WAMS.Infrastructure.Tests/ExternalSync/ItemSyncServiceTests.cs
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WAMS.Application.Interfaces.Common;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Items;
using WAMS.Domain.Entities.SyncLogs;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.ExternalSync.ErpHttpClient;
using WAMS.Infrastructure.ExternalSync.Item;
using Xunit;

namespace WAMS.Infrastructure.Tests.ExternalSync;

public class ItemSyncServiceTests
{
    // Create a stub ITenantContext with IsSet=false so query filters bypass tenant scoping.
    // This prevents NullReferenceException in EF InMemory when query filters evaluate
    // expressions like `_tenantContext == null || !_tenantContext.IsSet || ...`
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
    // This avoids ObjectDisposedException because BaseSyncService disposes each context
    // after each await using block.
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

    private static ItemSyncService CreateService(
        DbContextOptions<AppDbContext> opts,
        List<ItemErpDto>? erpData)
    {
        var handler = new FakeHttpHandler(erpData);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://erp.test") };
        var erp = new ErpApiClient(http, Substitute.For<ILogger<ErpApiClient>>());
        return new ItemSyncService(
            erp,
            CreateFactory(opts),
            Substitute.For<ILogger<ItemSyncService>>());
    }

    private static async Task SeedCompanyAsync(DbContextOptions<AppDbContext> opts, long companyId = 1, string code = "C001")
    {
        await using var db = new AppDbContext(opts, CreateTenantContext());
        db.Companies.Add(new Company
        {
            Id = companyId,
            Code = code,
            Name = "Test Co",
            IsActive = true,
        });
        await db.SaveChangesAsync();
    }

    // Pipeline tests (base class behavior) 
    [Fact]
    public async Task SyncAllAsync_NullErpResponse_WritesErpUnavailableLog()
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);
        var svc = CreateService(opts, erpData: null);

        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        var log = db.SyncLogs.Single();
        log.Outcome.Should().Be(SyncOutcome.ErpUnavailable);
        log.ServiceName.Should().Be("ItemSync");
        log.CompanyCode.Should().Be("C001");
    }

    [Fact]
    public async Task SyncAllAsync_NullErpResponse_ReturnsSkippedResult()
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);
        var svc = CreateService(opts, erpData: null);

        var result = await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        result.Skipped.Should().Be(1);
        result.Added.Should().Be(0);
    }

    [Fact]
    public async Task SyncAllAsync_EmptyErpResponse_WritesErpUnavailableLog()
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);
        var svc = CreateService(opts, erpData: []);

        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        db.SyncLogs.Single().Outcome.Should().Be(SyncOutcome.ErpUnavailable);
    }

    [Fact]
    public async Task SyncAllAsync_SchemaError_WritesSchemaErrorLogAndAbortsCompany()
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);
        // ItemCode is required - empty string triggers schema validation failure
        var svc = CreateService(opts, erpData: [new ItemErpDto("", "ItemName", "AcctCode", "AcctName")]);

        var result = await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        var log = db.SyncLogs.Single();
        log.Outcome.Should().Be(SyncOutcome.SchemaError);
        log.AbortReason.Should().Contain("ItemCode");
        result.Success.Should().BeFalse();
        db.ItemShadows.Should().BeEmpty();
    }

    // ItemSyncService-specific tests 

    [Fact]
    public async Task SyncAllAsync_AddsNewItems()
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);
        var erpData = new List<ItemErpDto>
        {
            new("CODE1", "Widget A", "ACC1", "Account 1"),
            new("CODE2", "Widget B", "ACC2", "Account 2"),
        };
        var svc = CreateService(opts, erpData);

        var result = await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        result.Added.Should().Be(2);
        result.Updated.Should().Be(0);
        db.ItemShadows.Should().HaveCount(2);
        db.ItemShadows.Single(i => i.ItemCode == "CODE1").ItemName.Should().Be("Widget A");
    }

    [Fact]
    public async Task SyncAllAsync_UpdatesChangedItem()
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);
        await using (var seedDb = OpenDb(opts))
        {
            seedDb.ItemShadows.Add(new ItemShadow
            {
                CompanyId = 1, ItemCode = "CODE1", ItemName = "Old Name",
                AcctCode = "OLD", AcctName = "Old Account", IsActive = true,
                FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
            });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var erpData = new List<ItemErpDto> { new("CODE1", "New Name", "NEW", "New Account") };
        var svc = CreateService(opts, erpData);

        var result = await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        result.Updated.Should().Be(1);
        result.Added.Should().Be(0);
        db.ItemShadows.Single().ItemName.Should().Be("New Name");
        db.ItemShadows.Single().AcctCode.Should().Be("NEW");
    }

    [Fact]
    public async Task SyncAllAsync_DeactivatesRemovedItem()
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);
        await using (var seedDb = OpenDb(opts))
        {
            seedDb.ItemShadows.Add(new ItemShadow
            {
                CompanyId = 1, ItemCode = "GONE", ItemName = "Gone Item",
                AcctCode = "ACC", AcctName = "Acct", IsActive = true,
                FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
            });
            seedDb.ItemShadows.Add(new ItemShadow
            {
                CompanyId = 1, ItemCode = "KEEP01", ItemName = "Keep Item",
                AcctCode = "ACC", AcctName = "Acct", IsActive = true,
                FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
            });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var erpData = new List<ItemErpDto> { new("KEEP01", "Keep Item", "ACC", "Acct") };
        var svc = CreateService(opts, erpData);

        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        db.ItemShadows.Single(i => i.ItemCode == "GONE").IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task SyncAllAsync_ReactivatesItemReturnedByErpAfterBeingInactive()
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);
        await using (var seedDb = OpenDb(opts))
        {
            seedDb.ItemShadows.Add(new ItemShadow
            {
                CompanyId = 1, ItemCode = "CODE1", ItemName = "Item",
                AcctCode = "ACC", AcctName = "Acct", IsActive = false,
                FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
            });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var svc = CreateService(opts, erpData: [new("CODE1", "Item", "ACC", "Acct")]);

        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        db.ItemShadows.Single().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SyncAllAsync_WritesSuccessLogWithCorrectCounts()
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);
        await using (var seedDb = OpenDb(opts))
        {
            seedDb.ItemShadows.Add(new ItemShadow
            {
                CompanyId = 1, ItemCode = "EXISTING", ItemName = "Old Name",
                AcctCode = "A", AcctName = "B", IsActive = true,
                FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
            });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var erpData = new List<ItemErpDto>
        {
            new("EXISTING", "New Name", "A", "B"),  // update
            new("NEW", "New Item", "A", "B"),         // add
        };
        var svc = CreateService(opts, erpData);

        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        var log = db.SyncLogs.Single();
        log.Outcome.Should().Be(SyncOutcome.Success);
        log.Added.Should().Be(1);
        log.Updated.Should().Be(1);
        log.Deactivated.Should().Be(0);
        log.FinishedAt.Should().NotBeNull();
    }

    // ValidateSchema tests 
    [Theory]
    [InlineData("", "ValidName", "ACC", "AcctName")]
    [InlineData("   ", "ValidName", "ACC", "AcctName")]
    public async Task SyncAllAsync_EmptyItemCode_WritesSchemaError(
        string itemCode, string itemName, string acctCode, string acctName)
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);
        var svc = CreateService(opts, [new(itemCode, itemName, acctCode, acctName)]);

        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        db.SyncLogs.Single().Outcome.Should().Be(SyncOutcome.SchemaError);
    }

    [Theory]
    [InlineData("CODE1", "", "ACC", "AcctName")]
    [InlineData("CODE1", "   ", "ACC", "AcctName")]
    public async Task SyncAllAsync_EmptyItemName_WritesSchemaError(
        string itemCode, string itemName, string acctCode, string acctName)
    {
        var opts = CreateDbOptions();
        await SeedCompanyAsync(opts);
        var svc = CreateService(opts, [new(itemCode, itemName, acctCode, acctName)]);

        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        db.SyncLogs.Single().Outcome.Should().Be(SyncOutcome.SchemaError);
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

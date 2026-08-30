using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WAMS.Application.Interfaces.Common;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Spk;
using WAMS.Domain.Entities.SyncLogs;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.ExternalSync.ErpHttpClient;
using WAMS.Infrastructure.ExternalSync.Spk;
using Xunit;

namespace WAMS.Infrastructure.Tests.ExternalSync;

public class SpkSyncServiceTests
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

    private static SpkSyncService CreateService(
        DbContextOptions<AppDbContext> opts,
        List<SpkErpDto>? erpData)
    {
        var handler = new FakeHttpHandler(erpData);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://erp.test") };
        return new SpkSyncService(
            new ErpApiClient(http, Substitute.For<ILogger<ErpApiClient>>()),
            CreateFactory(opts), Substitute.For<ILogger<SpkSyncService>>());
    }

    private static SpkErpDto MakeSpk(string docNo = "SPK001", string itemCode = "ITEM01")
        => new("LO", docNo, "SO", "SO001", "C001", "Customer", itemCode, "Item",
            10m, 5m, "KG", "PACK", "WH01", "Warehouse 1", "O", null);

    [Fact]
    public async Task SyncAllAsync_AddsNewSpks()
    {
        var opts = CreateDbOptions();
        await using (var seedDb = OpenDb(opts))
        {
            seedDb.Companies.Add(new Company { Id = 1, Code = "C001", Name = "Co", IsActive = true });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var svc = CreateService(opts, [MakeSpk("SPK001", "ITEM01"), MakeSpk("SPK002", "ITEM02")]);

        var result = await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        result.Added.Should().Be(2);
        db.SpkShadows.Should().HaveCount(2);
    }

    [Fact]
    public async Task SyncAllAsync_EmptyDocNo_WritesSchemaError()
    {
        var opts = CreateDbOptions();
        await using (var seedDb = OpenDb(opts))
        {
            seedDb.Companies.Add(new Company { Id = 1, Code = "C001", Name = "Co", IsActive = true });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var svc = CreateService(opts, [MakeSpk(docNo: "")]);

        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        db.SyncLogs.Single().Outcome.Should().Be(SyncOutcome.SchemaError);
    }

    [Fact]
    public async Task SyncAllAsync_EmptyItemCode_WritesSchemaError()
    {
        var opts = CreateDbOptions();
        await using (var seedDb = OpenDb(opts))
        {
            seedDb.Companies.Add(new Company { Id = 1, Code = "C001", Name = "Co", IsActive = true });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var svc = CreateService(opts, [MakeSpk(itemCode: "")]);

        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        db.SyncLogs.Single().Outcome.Should().Be(SyncOutcome.SchemaError);
    }

    [Fact]
    public async Task SyncAllAsync_DeactivatesRemovedSpk()
    {
        var opts = CreateDbOptions();
        await using (var seedDb = OpenDb(opts))
        {
            seedDb.Companies.Add(new Company { Id = 1, Code = "C001", Name = "Co", IsActive = true });
            seedDb.SpkShadows.Add(new SpkShadow
            {
                CompanyId = 1, DocNo = "GONE", ItemCode = "ITEM99", Type = "LO",
                BaseDoc = "SO", BaseDocNo = "SO001", CardCode = "C001", CardName = "Customer",
                ItemName = "Item", UoM = "KG", PackType = "PACK", WhsCode = "WH01",
                WhsName = "WH", DocStatus = "O", IsActive = true,
                FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
            });
            seedDb.SpkShadows.Add(new SpkShadow
            {
                CompanyId = 1, DocNo = "SPK001", ItemCode = "ITEM01", Type = "LO",
                BaseDoc = "SO", BaseDocNo = "SO001", CardCode = "C001", CardName = "Customer",
                ItemName = "Item", UoM = "KG", PackType = "PACK", WhsCode = "WH01",
                WhsName = "WH", DocStatus = "O", IsActive = true,
                FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
            });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var erpData = new List<SpkErpDto> { MakeSpk("SPK001", "ITEM01") };
        var svc = CreateService(opts, erpData);

        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        db.SpkShadows.Single(s => s.DocNo == "GONE").IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task SyncAllAsync_PersistsBillOfLadingStubRows()
    {
        var opts = CreateDbOptions();
        await using (var seedDb = OpenDb(opts))
        {
            seedDb.Companies.Add(new Company { Id = 1, Code = "C001", Name = "Co", IsActive = true });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var blStub = new SpkErpDto("BL", "", "", "", "", "", "", "", 0m, 0m, "Kg", "", "", "", "O", "BL12345");
        var svc = CreateService(opts, [MakeSpk("SPK001", "ITEM01"), blStub]);

        var result = await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        result.Added.Should().Be(2);
        result.Success.Should().BeTrue();
        db.SpkShadows.Should().HaveCount(2);
        db.SpkShadows.Should().Contain(s => s.Type == "BL" && s.BlNo == "BL12345" && s.DocNo == "");
        db.SyncLogs.Single().Outcome.Should().Be(SyncOutcome.Success);
    }

    [Fact]
    public async Task SyncAllAsync_DeduplicatesMultipleBlRowsByBlNo()
    {
        var opts = CreateDbOptions();
        await using (var seedDb = OpenDb(opts))
        {
            seedDb.Companies.Add(new Company { Id = 1, Code = "C001", Name = "Co", IsActive = true });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var bl1 = new SpkErpDto("BL", "", "", "", "", "", "", "", 0m, 0m, "Kg", "", "", "", "O", "BL-AAA");
        var bl2 = new SpkErpDto("BL", "", "", "", "", "", "", "", 0m, 0m, "Kg", "", "", "", "O", "BL-BBB");
        var bl3 = new SpkErpDto("BL", "", "", "", "", "", "", "", 0m, 0m, "Kg", "", "", "", "O", "BL-CCC");
        var svc = CreateService(opts, [bl1, bl2, bl3]);

        var result = await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        result.Added.Should().Be(3);
        db.SpkShadows.Select(s => s.BlNo).Should().BeEquivalentTo(["BL-AAA", "BL-BBB", "BL-CCC"]);
    }

    [Fact]
    public async Task SyncAllAsync_BlRowMissingBlNo_WritesSchemaError()
    {
        var opts = CreateDbOptions();
        await using (var seedDb = OpenDb(opts))
        {
            seedDb.Companies.Add(new Company { Id = 1, Code = "C001", Name = "Co", IsActive = true });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var badBl = new SpkErpDto("BL", "", "", "", "", "", "", "", 0m, 0m, "Kg", "", "", "", "O", "");
        var svc = CreateService(opts, [badBl]);

        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        db.SyncLogs.Single().Outcome.Should().Be(SyncOutcome.SchemaError);
    }

    [Fact]
    public async Task SyncAllAsync_DeduplicatesErpResponse()
    {
        var opts = CreateDbOptions();
        await using (var seedDb = OpenDb(opts))
        {
            seedDb.Companies.Add(new Company { Id = 1, Code = "C001", Name = "Co", IsActive = true });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        // Same DocNo+ItemCode twice - last-wins, should result in 1 row
        var svc = CreateService(opts, [MakeSpk("SPK001", "ITEM01"), MakeSpk("SPK001", "ITEM01")]);

        var result = await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        result.Added.Should().Be(1);
        db.SpkShadows.Should().HaveCount(1);
    }

    [Fact]
    public async Task SyncAllAsync_KeepsSameDocAndItemSplitAcrossDifferentBls()
    {
        var opts = CreateDbOptions();
        await using (var seedDb = OpenDb(opts))
        {
            seedDb.Companies.Add(new Company { Id = 1, Code = "C001", Name = "Co", IsActive = true });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        // Same DocNo+ItemCode but different BlNo - two legitimate partial-shipment lines,
        // must not collapse into one (regression for the SPK sync bug where MO/LO rows
        // sharing DocNo+ItemCode across BLs were silently dropped).
        var line1 = new SpkErpDto("MO", "260905", "SO", "260200260", "C001", "Customer",
            "SBM-US-46,5", "Item", 16000m, 0m, "Kg", "Bulk", "WH01", "Warehouse 1", "O", "VO45952");
        var line2 = new SpkErpDto("MO", "260905", "SO", "260200260", "C001", "Customer",
            "SBM-US-46,5", "Item", 4000m, 3928m, "Kg", "Bulk", "WH01", "Warehouse 1", "O", "MEDUJM036813");
        var svc = CreateService(opts, [line1, line2]);

        var result = await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        result.Added.Should().Be(2);
        db.SpkShadows.Select(s => s.BlNo).Should().BeEquivalentTo(["VO45952", "MEDUJM036813"]);
    }

    private sealed class FakeHttpHandler(object? data) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (data is null)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            var opts = new JsonSerializerOptions();
            opts.Converters.Add(new JsonStringEnumConverter());
            var json = JsonSerializer.Serialize(data, opts);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        }
    }
}

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
using WAMS.Domain.Entities.SyncLogs;
using WAMS.Domain.Entities.TransportOrders;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.ExternalSync.ErpHttpClient;
using WAMS.Infrastructure.ExternalSync.TransportOrder;
using Xunit;

namespace WAMS.Infrastructure.Tests.ExternalSync;

public class ToSyncServiceTests
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

    private static ToSyncService CreateService(
        DbContextOptions<AppDbContext> opts,
        List<ToErpDto>? erpData)
    {
        var handler = new FakeHttpHandler(erpData);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://erp.test") };
        return new ToSyncService(
            new ErpApiClient(http, Substitute.For<ILogger<ErpApiClient>>()),
            CreateFactory(opts), Substitute.For<ILogger<ToSyncService>>());
    }

    private static ToErpDto MakeTo(
        string docNo = "250124", string blNo = "BL001", string vehiclePlate = "TRUCK-1",
        string itemCode = "ITEM01", decimal quantity = 10m)
        => new("MO", docNo, "C001", "Customer", itemCode, "Item",
            quantity, "KG", "WH01", "Warehouse 1", "O", blNo, vehiclePlate, "Trailer");

    [Fact]
    public async Task SyncAllAsync_AddsNewTransportOrders()
    {
        var opts = CreateDbOptions();
        await using (var seedDb = OpenDb(opts))
        {
            seedDb.Companies.Add(new Company { Id = 1, Code = "C001", Name = "Co", IsActive = true });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var svc = CreateService(opts, [MakeTo("250124", "BL001", "TRUCK-1"), MakeTo("250125", "BL002", "TRUCK-2")]);

        var result = await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        result.Added.Should().Be(2);
        db.TransportOrderShadows.Should().HaveCount(2);
    }

    [Fact]
    public async Task SyncAllAsync_SameDocNoAndBlNo_DifferentVehicle_KeepsBothRows()
    {
        // Regression test: same (DocNo, BlNo) legitimately repeats per vehicle in real
        // ERP data - the sync must not collapse these into one row.
        var opts = CreateDbOptions();
        await using (var seedDb = OpenDb(opts))
        {
            seedDb.Companies.Add(new Company { Id = 1, Code = "C001", Name = "Co", IsActive = true });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var svc = CreateService(opts, [
            MakeTo("250124", "BL001", "TRUCK-1"),
            MakeTo("250124", "BL001", "TRUCK-2"),
            MakeTo("250124", "BL001", "TRUCK-3"),
        ]);

        var result = await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        result.Added.Should().Be(3);
        db.TransportOrderShadows.Should().HaveCount(3);
        db.TransportOrderShadows.Select(t => t.VehicleNo).Should()
            .BeEquivalentTo(["TRUCK-1", "TRUCK-2", "TRUCK-3"]);
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
        var svc = CreateService(opts, [MakeTo(docNo: "")]);

        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        db.SyncLogs.Single().Outcome.Should().Be(SyncOutcome.SchemaError);
    }

    [Fact]
    public async Task SyncAllAsync_EmptyBlNo_WritesSchemaError()
    {
        var opts = CreateDbOptions();
        await using (var seedDb = OpenDb(opts))
        {
            seedDb.Companies.Add(new Company { Id = 1, Code = "C001", Name = "Co", IsActive = true });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var svc = CreateService(opts, [MakeTo(blNo: "")]);

        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        db.SyncLogs.Single().Outcome.Should().Be(SyncOutcome.SchemaError);
    }

    [Fact]
    public async Task SyncAllAsync_DeactivatesRemovedTransportOrder()
    {
        var opts = CreateDbOptions();
        await using (var seedDb = OpenDb(opts))
        {
            seedDb.Companies.Add(new Company { Id = 1, Code = "C001", Name = "Co", IsActive = true });
            seedDb.TransportOrderShadows.Add(new TransportOrderShadow
            {
                CompanyId = 1, DocNo = "GONE", BlNo = "BL999", VehicleNo = "TRUCK-9", Type = "MO",
                CardCode = "C001", CardName = "Customer", VehicleType = "Trailer",
                ItemCode = "ITEM99", ItemName = "Item", UoM = "KG", WhsCode = "WH01",
                WhsName = "WH", DocStatus = "O", IsActive = true,
                FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
            });
            seedDb.TransportOrderShadows.Add(new TransportOrderShadow
            {
                CompanyId = 1, DocNo = "250124", BlNo = "BL001", VehicleNo = "TRUCK-1", Type = "MO",
                CardCode = "C001", CardName = "Customer", VehicleType = "Trailer",
                ItemCode = "ITEM01", ItemName = "Item", UoM = "KG", WhsCode = "WH01",
                WhsName = "WH", DocStatus = "O", IsActive = true,
                FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
            });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var svc = CreateService(opts, [MakeTo("250124", "BL001", "TRUCK-1")]);

        await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        db.TransportOrderShadows.Single(t => t.DocNo == "GONE").IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task SyncAllAsync_ExistingRow_ChangedItemCodeAndQuantity_UpdatesRow()
    {
        // Regression test: ItemCode was previously insert-only in ApplyDiff (excluded from
        // change-detection/update) even though it's not part of the matching key - a
        // corrected ItemCode from ERP would never propagate. Verifies the fix.
        var opts = CreateDbOptions();
        await using (var seedDb = OpenDb(opts))
        {
            seedDb.Companies.Add(new Company { Id = 1, Code = "C001", Name = "Co", IsActive = true });
            seedDb.TransportOrderShadows.Add(new TransportOrderShadow
            {
                CompanyId = 1, DocNo = "250124", BlNo = "BL001", VehicleNo = "TRUCK-1", Type = "MO",
                CardCode = "C001", CardName = "Customer", VehicleType = "Trailer",
                ItemCode = "ITEM01", ItemName = "Item", Quantity = 10m, UoM = "KG", WhsCode = "WH01",
                WhsName = "Warehouse 1", DocStatus = "O", IsActive = true,
                FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
            });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var svc = CreateService(opts, [MakeTo("250124", "BL001", "TRUCK-1", itemCode: "ITEM02", quantity: 99m)]);

        var result = await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        result.Updated.Should().Be(1);
        var row = db.TransportOrderShadows.Single(t => t.DocNo == "250124");
        row.ItemCode.Should().Be("ITEM02");
        row.Quantity.Should().Be(99m);
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
        // Same DocNo+BlNo+VehiclePlate twice - last-wins, should result in 1 row
        var svc = CreateService(opts, [MakeTo("250124", "BL001", "TRUCK-1"), MakeTo("250124", "BL001", "TRUCK-1")]);

        var result = await svc.SyncAllAsync(TestContext.Current.CancellationToken);

        await using var db = OpenDb(opts);
        result.Added.Should().Be(1);
        db.TransportOrderShadows.Should().HaveCount(1);
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

namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using NSubstitute;
using WAMS.Application.DTOs.Rca;
using WAMS.Application.Export;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Rca;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Application.Services.Rca;
using Xunit;

public class RcaServiceTests
{
    private readonly IRcaRepository _repo = Substitute.For<IRcaRepository>();
    private readonly IWarehouseContext _warehouseContext = Substitute.For<IWarehouseContext>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IRbacService _rbacService = Substitute.For<IRbacService>();
    private readonly IPdfMetadataResolver _metadataResolver = Substitute.For<IPdfMetadataResolver>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly RcaService _sut;

    private static readonly RcaQuery Query = new("WH001", new DateOnly(2026, 2, 13), new DateOnly(2026, 2, 19));

    public RcaServiceTests()
    {
        _sut = new RcaService(_repo, _warehouseContext, _userRepo, _rbacService, _metadataResolver, _tenantContext);
    }

    private void SetupDefaults()
    {
        _tenantContext.IsSet.Returns(true);
        _tenantContext.CompanyId.Returns((long?)1L);
        _metadataResolver.ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PdfReportMetadata("RCA", "PT. Gerbang Cahaya Utama", "GCU", null, DateTime.UtcNow));
        _repo.GetDataAsync(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<IReadOnlyList<long>?>(), Arg.Any<long?>(), Arg.Any<CancellationToken>())
            .Returns(new RcaRepoData([], [], new RcaSignatures(null, []), "Medan"));
    }

    [Fact]
    public async Task GetDocumentAsync_WithWarehouseHeader_PassesSingleWarehouseId()
    {
        SetupDefaults();
        _warehouseContext.IsSet.Returns(true);
        _warehouseContext.WarehouseId.Returns(7L);

        await _sut.GetDocumentAsync(Query, 1, CancellationToken.None);

        await _repo.Received(1).GetDataAsync(
            "WH001",
            Arg.Any<DateTime>(), Arg.Any<DateTime>(),
            Arg.Is<IReadOnlyList<long>?>(ids => ids != null && ids.SequenceEqual(new long[] { 7L })),
            Arg.Any<long?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDocumentAsync_WithGlobalAccess_PassesNullWarehouseIds()
    {
        SetupDefaults();
        _warehouseContext.IsSet.Returns(false);
        _rbacService.HasGlobalAccessAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        await _sut.GetDocumentAsync(Query, 1, CancellationToken.None);

        await _repo.Received(1).GetDataAsync(
            "WH001",
            Arg.Any<DateTime>(), Arg.Any<DateTime>(),
            Arg.Is<IReadOnlyList<long>?>(ids => ids == null),
            Arg.Any<long?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDocumentAsync_WithRestrictedUser_PassesUserWarehouseIds()
    {
        SetupDefaults();
        _warehouseContext.IsSet.Returns(false);
        _rbacService.HasGlobalAccessAsync(1, Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.GetUserWarehouseIdsAsync(1, Arg.Any<CancellationToken>()).Returns([3L, 9L]);

        await _sut.GetDocumentAsync(Query, 1, CancellationToken.None);

        await _repo.Received(1).GetDataAsync(
            "WH001",
            Arg.Any<DateTime>(), Arg.Any<DateTime>(),
            Arg.Is<IReadOnlyList<long>?>(ids => ids != null && ids.SequenceEqual(new long[] { 3L, 9L })),
            Arg.Any<long?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDocumentAsync_WhenTenantSet_PassesCompanyId()
    {
        SetupDefaults();
        _tenantContext.IsSet.Returns(true);
        _tenantContext.CompanyId.Returns((long?)42L);
        _warehouseContext.IsSet.Returns(true);
        _warehouseContext.WarehouseId.Returns(7L);

        await _sut.GetDocumentAsync(Query, 1, CancellationToken.None);

        await _repo.Received(1).GetDataAsync(
            Arg.Any<string>(),
            Arg.Any<DateTime>(), Arg.Any<DateTime>(),
            Arg.Any<IReadOnlyList<long>?>(),
            42L,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDocumentAsync_WhenTenantNotSet_PassesNullCompanyId()
    {
        SetupDefaults();
        _tenantContext.IsSet.Returns(false);
        _warehouseContext.IsSet.Returns(true);
        _warehouseContext.WarehouseId.Returns(7L);

        await _sut.GetDocumentAsync(Query, 1, CancellationToken.None);

        await _repo.Received(1).GetDataAsync(
            Arg.Any<string>(),
            Arg.Any<DateTime>(), Arg.Any<DateTime>(),
            Arg.Any<IReadOnlyList<long>?>(),
            Arg.Is<long?>(id => id == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDocumentAsync_ReturnsDocumentWithCorrectFields()
    {
        SetupDefaults();
        _warehouseContext.IsSet.Returns(true);
        _warehouseContext.WarehouseId.Returns(7L);
        var lines = new List<RcaLineItem>
        {
            new(new DateOnly(2026, 2, 13), "601010104", "VO45390", "Z.GDG009",
                "Muat Truk Bag", "US SBM", 18200m, "Kg",
                "B. Bongkar Muat Dari Gudang (Non Vendor)", null, 277550m)
        };
        var totals = new List<PosBiayaTotal> { new("Z.GDG009", "B. Bongkar Muat", 277550m) };
        var sigs = new RcaSignatures("Alice", new List<string?> { "Bob", "Carol" });
        _repo.GetDataAsync(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<IReadOnlyList<long>?>(), Arg.Any<long?>(), Arg.Any<CancellationToken>())
            .Returns(new RcaRepoData(lines, totals, sigs, "Medan"));

        var doc = await _sut.GetDocumentAsync(Query, 1, CancellationToken.None);

        doc.WarehouseCode.Should().Be("WH001");
        doc.Area.Should().Be("Medan");
        doc.DateFrom.Should().Be(Query.DateFrom);
        doc.DateTo.Should().Be(Query.DateTo);
        doc.Lines.Should().HaveCount(1);
        doc.Lines[0].AmountRupiah.Should().Be(277550m);
        doc.PosTotals.Should().HaveCount(1);
        doc.Signatures.Maker.Should().Be("Alice");
        doc.Signatures.Approvers.Should().Equal("Bob", "Carol");
        doc.RcaId.Should().MatchRegex(@"^RCA/[A-Z]+/WH001/\d{8}$");
        doc.RcaId.Should().Contain("/GCU/");
        doc.CompanyName.Should().Be("PT. Gerbang Cahaya Utama");
    }

    [Fact]
    public async Task GetDocumentAsync_PassesDateRangeAsUtcTimestamps()
    {
        SetupDefaults();
        _warehouseContext.IsSet.Returns(true);
        _warehouseContext.WarehouseId.Returns(7L);

        await _sut.GetDocumentAsync(Query, 1, CancellationToken.None);

        await _repo.Received(1).GetDataAsync(
            Arg.Any<string>(),
            new DateTime(2026, 2, 13, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 19, 23, 59, 59, DateTimeKind.Utc),
            Arg.Any<IReadOnlyList<long>?>(),
            Arg.Any<long?>(),
            Arg.Any<CancellationToken>());
    }
}

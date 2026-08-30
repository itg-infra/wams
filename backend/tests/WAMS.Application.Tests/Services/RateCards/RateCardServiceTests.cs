namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using WAMS.Application.DTOs.RateCards;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Items;
using WAMS.Application.Interfaces.RateCards;
using WAMS.Application.Interfaces.TaxTypes;
using WAMS.Application.Interfaces.Uoms;
using WAMS.Application.Interfaces.Vendors;
using WAMS.Application.Services.RateCards;
using WAMS.Application.Validators.RateCards;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Items;
using WAMS.Domain.Entities.RateCards;
using WAMS.Domain.Entities.TaxTypes;
using WAMS.Domain.Entities.Uoms;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Vendors;
using WAMS.Domain.Enums;
using WAMS.Domain.Exceptions;
using Xunit;

public class RateCardServiceTests
{
    private readonly IRateCardRepository _rateCardRepo = Substitute.For<IRateCardRepository>();
    private readonly IVendorShadowRepository _vendorRepo = Substitute.For<IVendorShadowRepository>();
    private readonly IItemShadowRepository _itemRepo = Substitute.For<IItemShadowRepository>();
    private readonly IUomMasterRepository _uomRepo = Substitute.For<IUomMasterRepository>();
    private readonly ITaxTypeRepository _taxTypeRepo = Substitute.For<ITaxTypeRepository>();
    private readonly IConfiguration _configuration = new ConfigurationBuilder().Build();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IValidator<CreateRateCardRequest> _createValidator = new CreateRateCardRequestValidator();
    private readonly IValidator<UpdateRateCardRequest> _updateValidator = new UpdateRateCardRequestValidator();
    private readonly RateCardService _sut;

    public RateCardServiceTests()
    {
        _sut = new RateCardService(
            _rateCardRepo, _vendorRepo, _itemRepo, _uomRepo, _taxTypeRepo, _configuration, _uow,
            _createValidator, _updateValidator);
    }

    private static void SetupHappyPathLookups(
        IVendorShadowRepository vendorRepo, IItemShadowRepository itemRepo, IUomMasterRepository uomRepo)
    {
        vendorRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new VendorShadow { Id = 1 });
        itemRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([new ItemShadow { Id = 10 }]);
        uomRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([new UomMaster { Id = 20 }]);
    }

    [Fact]
    public async Task CreateAsync_ItemWithValidPpnTaxTypeId_Succeeds()
    {
        SetupHappyPathLookups(_vendorRepo, _itemRepo, _uomRepo);
        _taxTypeRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([new TaxType { Id = 2, Category = TaxCategory.Ppn, IsActive = true }]);
        _rateCardRepo.GetByIdWithItemsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new RateCard { Id = 1, Vendor = new VendorShadow { Id = 1 }, CreatedBy = new User { Id = 1 }, Items = [] });

        var request = new CreateRateCardRequest(1, [new CreateRateCardItemRequest(10, 20, 100m, 2, null)]);

        await _sut.CreateAsync(userId: 1, request, ct: TestContext.Current.CancellationToken);

        await _rateCardRepo.Received(1).CreateAsync(
            Arg.Is<RateCard>(rc => rc.Items.Single().PpnTaxTypeId == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_PpnTaxTypeIdReferencesPphCategory_ThrowsValidationException()
    {
        SetupHappyPathLookups(_vendorRepo, _itemRepo, _uomRepo);
        _taxTypeRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([new TaxType { Id = 3, Category = TaxCategory.Pph, IsActive = true }]);

        var request = new CreateRateCardRequest(1, [new CreateRateCardItemRequest(10, 20, 100m, 3, null)]);

        var act = () => _sut.CreateAsync(userId: 1, request);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_TaxTypeIdInactive_ThrowsValidationException()
    {
        SetupHappyPathLookups(_vendorRepo, _itemRepo, _uomRepo);
        _taxTypeRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([new TaxType { Id = 2, Category = TaxCategory.Ppn, IsActive = false }]);

        var request = new CreateRateCardRequest(1, [new CreateRateCardItemRequest(10, 20, 100m, 2, null)]);

        var act = () => _sut.CreateAsync(userId: 1, request);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_UnknownTaxTypeId_ThrowsNotFoundException()
    {
        SetupHappyPathLookups(_vendorRepo, _itemRepo, _uomRepo);
        _taxTypeRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var request = new CreateRateCardRequest(1, [new CreateRateCardItemRequest(10, 20, 100m, 99, null)]);

        var act = () => _sut.CreateAsync(userId: 1, request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_ItemWithTaxTypes_SnapshotsCodeAndRateAtSelectionTime()
    {
        SetupHappyPathLookups(_vendorRepo, _itemRepo, _uomRepo);
        _taxTypeRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([
                new TaxType { Id = 2, Category = TaxCategory.Ppn, Code = "PPN11", Rate = 11m, IsActive = true },
                new TaxType { Id = 3, Category = TaxCategory.Pph, Code = "PPH23", Rate = 2m, IsActive = true }
            ]);
        _rateCardRepo.GetByIdWithItemsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new RateCard { Id = 1, Vendor = new VendorShadow { Id = 1 }, CreatedBy = new User { Id = 1 }, Items = [] });

        var request = new CreateRateCardRequest(1, [new CreateRateCardItemRequest(10, 20, 100m, 2, 3)]);

        await _sut.CreateAsync(userId: 1, request, ct: TestContext.Current.CancellationToken);

        await _rateCardRepo.Received(1).CreateAsync(
            Arg.Is<RateCard>(rc =>
                rc.Items.Single().PpnTaxTypeId == 2 &&
                rc.Items.Single().PpnTaxTypeCode == "PPN11" &&
                rc.Items.Single().PpnRate == 11m &&
                rc.Items.Single().PphTaxTypeCode == "PPH23" &&
                rc.Items.Single().PphRate == 2m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ReplacingItems_ResnapshotsRateFromCurrentTaxType()
    {
        SetupHappyPathLookups(_vendorRepo, _itemRepo, _uomRepo);
        var existing = new RateCard
        {
            Id = 1,
            VendorShadowId = 1,
            Vendor = new VendorShadow { Id = 1 },
            CreatedBy = new User { Id = 1 },
            Items = [new RateCardItem { Id = 9, ItemShadowId = 10, UomMasterId = 20, CostValue = 100m, PpnTaxTypeId = 2, PpnRate = 11m }],
        };
        var refetched = new RateCard { Id = 1, Vendor = new VendorShadow { Id = 1 }, CreatedBy = new User { Id = 1 }, Items = [] };
        _rateCardRepo.GetByIdWithItemsAsync(1, Arg.Any<CancellationToken>()).Returns(existing, refetched);
        _taxTypeRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([new TaxType { Id = 2, Category = TaxCategory.Ppn, Rate = 12m, IsActive = true }]);

        var request = new UpdateRateCardRequest(null, [new CreateRateCardItemRequest(10, 20, 100m, 2, null)]);

        await _sut.UpdateAsync(1, request, TestContext.Current.CancellationToken);

        await _rateCardRepo.Received(1).UpdateAsync(
            Arg.Is<RateCard>(rc => rc.Items.Single().PpnRate == 12m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ItemHasInvalidCostTreatment_ThrowsValidationException()
    {
        var existing = new RateCard { Id = 1, VendorShadowId = 1, Vendor = new VendorShadow { Id = 1 }, CreatedBy = new User { Id = 1 }, Items = [] };
        _rateCardRepo.GetByIdWithItemsAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var request = new UpdateRateCardRequest(null, [new CreateRateCardItemRequest(10, 20, 100m, null, null, CostTreatment: "Paid")]);

        var act = () => _sut.UpdateAsync(1, request);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_ItemHasCostValueZero_ThrowsValidationException()
    {
        var existing = new RateCard { Id = 1, VendorShadowId = 1, Vendor = new VendorShadow { Id = 1 }, CreatedBy = new User { Id = 1 }, Items = [] };
        _rateCardRepo.GetByIdWithItemsAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var request = new UpdateRateCardRequest(null, [new CreateRateCardItemRequest(10, 20, 0m, null, null)]);

        var act = () => _sut.UpdateAsync(1, request);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_ItemHasUomMasterIdZero_ThrowsValidationException()
    {
        var existing = new RateCard { Id = 1, VendorShadowId = 1, Vendor = new VendorShadow { Id = 1 }, CreatedBy = new User { Id = 1 }, Items = [] };
        _rateCardRepo.GetByIdWithItemsAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var request = new UpdateRateCardRequest(null, [new CreateRateCardItemRequest(10, 0, 100m, null, null)]);

        var act = () => _sut.UpdateAsync(1, request);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_ItemHasItemShadowIdZero_ThrowsValidationException()
    {
        var existing = new RateCard { Id = 1, VendorShadowId = 1, Vendor = new VendorShadow { Id = 1 }, CreatedBy = new User { Id = 1 }, Items = [] };
        _rateCardRepo.GetByIdWithItemsAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var request = new UpdateRateCardRequest(null, [new CreateRateCardItemRequest(0, 20, 100m, null, null)]);

        var act = () => _sut.UpdateAsync(1, request);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_persists_and_returns_CostTreatment()
    {
        SetupHappyPathLookups(_vendorRepo, _itemRepo, _uomRepo);
        _rateCardRepo.GetByIdWithItemsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new RateCard
            {
                Id = 1,
                Vendor = new VendorShadow { Id = 1 },
                CreatedBy = new User { Id = 1 },
                Items =
                [
                    new RateCardItem
                    {
                        Id = 1,
                        ItemShadowId = 10,
                        UomMasterId = 20,
                        CostValue = 10m,
                        CostTreatment = CostTreatments.Dibiayakan,
                        Item = new ItemShadow { Id = 10 },
                        Uom = new UomMaster { Id = 20 },
                    }
                ],
            });

        RateCard? captured = null;
        _rateCardRepo.CreateAsync(Arg.Do<RateCard>(rc => captured = rc), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<RateCard>());
        _taxTypeRepo.GetByCodeAsync(TaxCategory.Ppn, "PPNin0", Arg.Any<CancellationToken>())
            .Returns(new TaxType { Id = 99, Category = TaxCategory.Ppn, Code = "PPNin0", Rate = 0m });

        var request = new CreateRateCardRequest(1,
            [new CreateRateCardItemRequest(10, 20, 10m, null, null, CostTreatment: CostTreatments.Dibiayakan)]);

        var result = await _sut.CreateAsync(userId: 1, request, ct: TestContext.Current.CancellationToken);

        captured.Should().NotBeNull();
        captured!.Items.Should().ContainSingle()
            .Which.CostTreatment.Should().Be(CostTreatments.Dibiayakan);

        result.Items.Should().ContainSingle()
            .Which.CostTreatment.Should().Be(CostTreatments.Dibiayakan);
    }

    [Fact]
    public async Task UpdateAsync_SelectsInactiveTaxType_DoesNotThrow()
    {
        var existing = new RateCard { Id = 1, VendorShadowId = 1, Vendor = new VendorShadow { Id = 1 }, CreatedBy = new User { Id = 1 }, Items = [] };
        var refetched = new RateCard { Id = 1, Vendor = new VendorShadow { Id = 1 }, CreatedBy = new User { Id = 1 }, Items = [] };
        _rateCardRepo.GetByIdWithItemsAsync(1, Arg.Any<CancellationToken>()).Returns(existing, refetched);
        SetupHappyPathLookups(_vendorRepo, _itemRepo, _uomRepo);
        _taxTypeRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([new TaxType { Id = 2, Category = TaxCategory.Ppn, Code = "PPN11", Rate = 11m, IsActive = false }]);

        var request = new UpdateRateCardRequest(null, [new CreateRateCardItemRequest(10, 20, 100m, 2, null)]);

        var act = () => _sut.UpdateAsync(1, request);

        await act.Should().NotThrowAsync();
    }
}

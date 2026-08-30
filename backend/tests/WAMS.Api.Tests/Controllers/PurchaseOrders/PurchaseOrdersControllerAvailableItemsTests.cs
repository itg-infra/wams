namespace WAMS.Api.Tests.Controllers.PurchaseOrders;

using System.Security.Claims;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;
using WAMS.Api.Controllers.PurchaseOrders;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Application.Export;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.PurchaseOrders;
using WAMS.Application.Validators.PurchaseOrders;
using WAMS.Domain.Enums;
using Xunit;

public class PurchaseOrdersControllerAvailableItemsTests
{
    private readonly IPurchaseOrderService _service = Substitute.For<IPurchaseOrderService>();
    private readonly IValidator<CreatePurchaseOrderRequest> _validator = new CreatePurchaseOrderRequestValidator();
    private readonly PurchaseOrdersController _sut;

    public PurchaseOrdersControllerAvailableItemsTests()
    {
        _sut = new PurchaseOrdersController(
            _service,
            _validator,
            Substitute.For<IExportService>(),
            Options.Create(new ExportOptions()),
            Substitute.For<IAuditLogService>(),
            Substitute.For<IPurchaseOrderPdfRenderer>(),
            Substitute.For<IPdfMetadataResolver>());

        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _sut.ControllerContext.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, "9")], "jwt"));
        _sut.ControllerContext.HttpContext.Items["RequestId"] = "req-picker";
    }

    [Fact]
    public async Task GetAvailableItems_ReturnsPaginatedResponseWithCrossWarehouseRows()
    {
        _service.GetAvailableItemsAsync(
                9L, Arg.Any<AvailablePoItemQuery>(), Arg.Any<CancellationToken>())
            .Returns(([
                PickerRow(663L, 226L, 103L, "WHSBY010", "SBY - SPA", "BP-SPA", "Seed"),
                PickerRow(664L, 227L, 110L, "WHSBY017", "SBY - KK", "BP-KK", "Suggestion")
            ], 21));
        var query = new AvailablePoItemQuery { BudgetPlanId = 226L, Page = 2, Limit = 20 };

        var result = await _sut.GetAvailableItems(query, TestContext.Current.CancellationToken);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should()
            .BeAssignableTo<PaginatedResponse<AvailablePoItemResponse>>().Subject;
        payload.Data.Should().SatisfyRespectively(
            spa =>
            {
                spa.BudgetPlanItemId.Should().Be(663L);
                spa.BudgetPlanId.Should().Be(226L);
                spa.WarehouseShadowId.Should().Be(103L);
                spa.WarehouseCode.Should().Be("WHSBY010");
                spa.WarehouseName.Should().Be("SBY - SPA");
                spa.BudgetPlanCode.Should().Be("BP-SPA");
            },
            kk =>
            {
                kk.BudgetPlanItemId.Should().Be(664L);
                kk.BudgetPlanId.Should().Be(227L);
                kk.WarehouseShadowId.Should().Be(110L);
                kk.WarehouseCode.Should().Be("WHSBY017");
                kk.WarehouseName.Should().Be("SBY - KK");
                kk.BudgetPlanCode.Should().Be("BP-KK");
            });
        payload.Meta.Page.Should().Be(2);
        payload.Meta.Limit.Should().Be(20);
        payload.Meta.Total.Should().Be(21);
        payload.Meta.TotalPages.Should().Be(2);
        await _service.Received(1).GetAvailableItemsAsync(
            9L, query, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAvailableItemsForEdit_ForwardsPurchaseOrderAndTypedQuery()
    {
        _service.GetAvailableItemsForEditAsync(
                9L, 99L, Arg.Any<EditAvailablePoItemQuery>(), Arg.Any<CancellationToken>())
            .Returns(([], 42));
        var query = new EditAvailablePoItemQuery
        {
            IncludeGenerated = true,
            Search = "freight",
            Page = 3,
            Limit = 20,
        };

        var result = await _sut.GetAvailableItemsForEdit(
            99L, query, TestContext.Current.CancellationToken);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should()
            .BeAssignableTo<PaginatedResponse<AvailablePoItemResponse>>().Subject;
        payload.Meta.Should().Be(new PaginationMeta(3, 20, 42, 3));
        await _service.Received(1).GetAvailableItemsForEditAsync(
            9L,
            99L,
            Arg.Is<EditAvailablePoItemQuery>(q =>
                q.IncludeGenerated && q.Search == "freight" && q.Page == 3 && q.Limit == 20),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_WithSelectedItems_CallsCreateServiceAndReturnsDraft()
    {
        var request = new CreatePurchaseOrderRequest(1L, null, DateTime.UtcNow, [647L]);
        _service.CreateAsync(9L, request, Arg.Any<CancellationToken>()).Returns(DraftResponse());

        var result = await _sut.Create(request, TestContext.Current.CancellationToken);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeAssignableTo<ApiResponse<PurchaseOrderResponse>>().Subject;
        payload.Data!.Status.Should().Be(PurchaseOrderStatus.Draft.Value);
        await _service.Received(1).CreateAsync(9L, request, Arg.Any<CancellationToken>());
        await _service.DidNotReceive().CreateAndGenerateAsync(
            Arg.Any<long>(), Arg.Any<CreatePurchaseOrderRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_WithoutSelectedItems_RejectsBeforeCallingService()
    {
        var request = new CreatePurchaseOrderRequest(1L, null, DateTime.UtcNow, []);

        var act = () => _sut.Create(request, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>()
            .WithMessage("One or more validation errors occurred.");
        await _service.DidNotReceive().CreateAsync(
            Arg.Any<long>(), Arg.Any<CreatePurchaseOrderRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAndGenerate_WithoutSelectedItems_RejectsBeforeCallingService()
    {
        var request = new CreatePurchaseOrderRequest(1L, null, DateTime.UtcNow, []);

        var act = () => _sut.CreateAndGenerate(request, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>()
            .WithMessage("One or more validation errors occurred.");
        await _service.DidNotReceive().CreateAsync(
            Arg.Any<long>(), Arg.Any<CreatePurchaseOrderRequest>(), Arg.Any<CancellationToken>());
        await _service.DidNotReceive().CreateAndGenerateAsync(
            Arg.Any<long>(), Arg.Any<CreatePurchaseOrderRequest>(), Arg.Any<CancellationToken>());
    }

    private static PurchaseOrderResponse DraftResponse() => new(
        99L,
        "PO-2608000001",
        1L,
        "V-001",
        "Vendor One",
        PurchaseOrderStatus.Draft.Value,
        DateTime.UtcNow,
        null,
        null,
        [],
        [],
        0m,
        0m,
        0m,
        0m,
        DateTime.UtcNow,
        "Creator",
        null,
        null,
        []);

    private static AvailablePoItemResponse PickerRow(
        long budgetPlanItemId,
        long budgetPlanId,
        long warehouseShadowId,
        string warehouseCode,
        string warehouseName,
        string budgetPlanCode,
        string? budgetPlanRemark) => new(
        budgetPlanItemId,
        budgetPlanId,
        budgetPlanCode,
        budgetPlanRemark,
        new DateTime(2026, 8, 26),
        false,
        warehouseShadowId,
        warehouseCode,
        warehouseName,
        1L,
        "V-001",
        "Vendor One",
        10L,
        "ITEM-001",
        "Freight",
        "501010206",
        "Freight",
        false,
        "BOL-001",
        100m,
        1m,
        "PCS",
        "Pieces",
        false,
        null);
}

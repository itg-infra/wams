namespace WAMS.Api.Tests.Controllers.PurchaseOrders;

using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSubstitute;
using WAMS.Api.Controllers.PurchaseOrders;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Application.Export;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.PurchaseOrders;
using Xunit;

public class PurchaseOrdersControllerPdfTests
{
    private readonly IPurchaseOrderService _poService = Substitute.For<IPurchaseOrderService>();
    private readonly IPurchaseOrderPdfRenderer _renderer = Substitute.For<IPurchaseOrderPdfRenderer>();
    private readonly IPdfMetadataResolver _metadataResolver = Substitute.For<IPdfMetadataResolver>();
    private readonly PurchaseOrdersController _sut;

    private static PurchaseOrderResponse Po(string? sapPoNumber) =>
        new(
            Id: 7, Code: "PO-2608000009",
            VendorShadowId: 1, VendorCode: "V001", VendorName: "AMAYA LAND, CV",
            Status: "Draft",
            DocDate: new DateTime(2026, 10, 8),
            Remark: null,
            SapPoNumber: sapPoNumber,
            LinkedBudgetPlans: [],
            Items: [],
            GrandTotal: 0m, TotalPpnAmount: 0m, TotalPphAmount: 0m, TaxInclusiveGrandTotal: 0m,
            CreatedAt: new DateTime(2026, 10, 8),
            CreatedByName: "Tester",
            GeneratedAt: null, GeneratedByName: null, Approvers: []);

    public PurchaseOrdersControllerPdfTests()
    {
        _metadataResolver
            .ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PdfReportMetadata("Purchase Order", "PT. Gerbang Cahaya Utama", "GCU", null, DateTime.UtcNow, "Jakarta"));

        _renderer.Render(Arg.Any<PurchaseOrderResponse>(), Arg.Any<PdfReportMetadata>())
            .Returns([1, 2, 3]);

        _sut = new PurchaseOrdersController(
            _poService,
            Substitute.For<IValidator<CreatePurchaseOrderRequest>>(),
            Substitute.For<IExportService>(),
            Options.Create(new ExportOptions()),
            Substitute.For<IAuditLogService>(),
            _renderer,
            _metadataResolver);

        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _sut.ControllerContext.HttpContext.Items["RequestId"] = "req-test";
    }

    [Fact]
    public async Task ExportPdf_names_the_file_after_the_sap_number_when_present()
    {
        _poService.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(Po("SBY-260400607"));

        var result = await _sut.ExportPdf(7, CancellationToken.None) as FileContentResult;

        result.Should().NotBeNull();
        result!.ContentType.Should().Be("application/pdf");
        result.FileDownloadName.Should().Be("SBY-260400607.pdf");
        result.FileContents.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task ExportPdf_prefixes_the_filename_with_draft_when_not_generated()
    {
        _poService.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(Po(null));

        var result = await _sut.ExportPdf(7, CancellationToken.None) as FileContentResult;

        result.Should().NotBeNull();
        result!.FileDownloadName.Should().Be("DRAFT-PO-2608000009.pdf");
    }
}

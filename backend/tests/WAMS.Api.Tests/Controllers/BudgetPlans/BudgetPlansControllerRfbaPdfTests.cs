namespace WAMS.Api.Tests.Controllers.BudgetPlans;

using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WAMS.Api.Controllers.BudgetPlans;
using WAMS.Application.DTOs.BudgetPlans;
using WAMS.Application.DTOs.Rfba;
using WAMS.Application.Export;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.BudgetPlans;
using WAMS.Application.Interfaces.Rfba;
using WAMS.Domain.Exceptions;
using Xunit;

public class BudgetPlansControllerRfbaPdfTests
{
    private readonly IBudgetPlanService _service = Substitute.For<IBudgetPlanService>();
    private readonly IRfbaFormPdfRenderer _renderer = Substitute.For<IRfbaFormPdfRenderer>();
    private readonly IPdfMetadataResolver _metadataResolver = Substitute.For<IPdfMetadataResolver>();
    private readonly BudgetPlansController _sut;

    private static BudgetPlanItemResponse Item(bool isRfba) =>
        new(
            Id: 1, ItemShadowId: 1, CostDetail: "ITM-1", CostName: "Bongkar curah",
            Coa: "501010206", CoaName: "Biaya Bongkar",
            VendorShadowId: 1, VendorCode: "V001", VendorName: "AMAYA LAND, CV",
            UomMasterId: 1, UomCode: "KTR", UomName: "Kontainer",
            CostValue: 675_000m, Quantity: 9m, TotalValue: 6_075_000m, SortOrder: 0,
            Type: "External", IsRfba: isRfba, DocExternal: null, BillOfLading: "SSZI711911",
            Description: null, ActivityTypeId: 1, ActivityTypeCode: "BONGKAR", ActivityTypeName: "Bongkar",
            SpkShadowId: null, PpnTaxTypeCode: null, PpnRate: 0m, PphTaxTypeCode: null, PphRate: 0m,
            PpnAmount: 0m, PphAmount: 0m, GrandTotal: 6_075_000m, CostTreatment: null);

    private static BudgetPlanResponse Plan(bool withRfba) =>
        new(
            Id: 12, BudgetNo: "BP-2602000012",
            Template: new BudgetTemplateSummaryInfo(1, "TPL-001", null, null, null),
            WarehouseCode: "WH-MDN", WarehouseName: "Medan - Agung",
            Remark: null, DocDate: new DateTime(2026, 2, 3),
            Status: "Approved", StatusDisplay: "Approved",
            SpkItems: [], Items: [Item(withRfba)],
            GrandTotal: 6_075_000m, TotalPpnAmount: 0m, TotalPphAmount: 0m, TaxInclusiveGrandTotal: 6_075_000m,
            CreatedAt: new DateTime(2026, 2, 3), CreatedByName: "Tester",
            SubmittedAt: null, SubmittedByName: null,
            Approval: new BudgetPlanApprovalInfo(0, 0, []),
            RejectedAt: null, RejectedByName: null, RejectionReason: null);

    public BudgetPlansControllerRfbaPdfTests()
    {
        _metadataResolver
            .ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PdfReportMetadata("RFBA", "PT. Gerbang Cahaya Utama", "GCU", null, DateTime.UtcNow, "Jakarta"));

        _renderer.Render(Arg.Any<RfbaFormDocument>(), Arg.Any<PdfReportMetadata>()).Returns([1, 2, 3]);

        _sut = new BudgetPlansController(
            _service,
            Substitute.For<IValidator<CreateBudgetPlanRequest>>(),
            Substitute.For<IExportService>(),
            Options.Create(new ExportOptions()),
            Substitute.For<IAuditLogService>(),
            _renderer,
            _metadataResolver);

        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _sut.ControllerContext.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, "1")], "jwt"));
        _sut.ControllerContext.HttpContext.Items["RequestId"] = "req-test";
    }

    [Fact]
    public async Task ExportRfbaPdf_names_the_file_after_the_budget_number()
    {
        _service.GetByIdAsync(12, 1, Arg.Any<CancellationToken>()).Returns(Plan(withRfba: true));

        var result = await _sut.ExportRfbaPdf(12, CancellationToken.None) as FileContentResult;

        result.Should().NotBeNull();
        result!.ContentType.Should().Be("application/pdf");
        result.FileDownloadName.Should().Be("RFBA-BP-2602000012.pdf");
        result.FileContents.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task ExportRfbaPdf_throws_not_found_when_the_plan_has_no_rfba_items()
    {
        _service.GetByIdAsync(12, 1, Arg.Any<CancellationToken>()).Returns(Plan(withRfba: false));

        var act = async () => await _sut.ExportRfbaPdf(12, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

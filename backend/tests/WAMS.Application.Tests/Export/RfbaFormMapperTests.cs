namespace WAMS.Application.Tests.Export;

using FluentAssertions;
using WAMS.Application.DTOs.BudgetPlans;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Application.DTOs.Rfba;
using WAMS.Application.Export;
using Xunit;

public class RfbaFormMapperTests
{
    [Fact]
    public void FromRecapPurchaseOrder_uses_only_recap_items_and_po_code()
    {
        var detail = new RecapPurchaseOrderDetailResponse(
            9, "PO-009", "Vendor", "Generated", null,
            DateTime.UtcNow, DateTime.UtcNow, "Maker", null, null,
            [],
            [
                new PurchaseOrderItemResponse(
                    1, 11, 21, "ITEM", "RFBA item", "COA", "Cost", 31, "V", "Vendor",
                    41, "EA", "Each", true, "BL-1", 10m, 2m, 20m, 0,
                    null, 0m, null, 0m, 0m, 0m, 20m, null),
            ],
            20m, 1);

        var document = RfbaFormMapper.FromRecapPurchaseOrder(detail);

        document.Pages.Should().ContainSingle();
        document.Pages[0].RfbaId.Should().Be("PO-009");
        document.Pages[0].Rows.Should().ContainSingle();
        document.Pages[0].Total.Should().Be(20m);
    }

    private static BudgetPlanItemResponse Item(
        long id, bool isRfba, string? bl, string costName, decimal qty, decimal cost, decimal total, int sortOrder) =>
        new(
            Id: id,
            ItemShadowId: 1,
            CostDetail: "ITM-1",
            CostName: costName,
            Coa: "501010206",
            CoaName: "Biaya Bongkar",
            VendorShadowId: 1,
            VendorCode: "V001",
            VendorName: "AMAYA LAND, CV",
            UomMasterId: 1,
            UomCode: "KTR",
            UomName: "Kontainer",
            CostValue: cost,
            Quantity: qty,
            TotalValue: total,
            SortOrder: sortOrder,
            Type: "External",
            IsRfba: isRfba,
            DocExternal: null,
            BillOfLading: bl,
            Description: null,
            ActivityTypeId: 1,
            ActivityTypeCode: "BONGKAR",
            ActivityTypeName: "Bongkar",
            SpkShadowId: null,
            PpnTaxTypeCode: null,
            PpnRate: 0m,
            PphTaxTypeCode: null,
            PphRate: 0m,
            PpnAmount: 0m,
            PphAmount: 0m,
            GrandTotal: total,
            CostTreatment: null);

    private static BudgetPlanSpkItemResponse Spk(long id, string? blNo, string itemName) =>
        new(
            Id: id,
            SpkShadowId: id,
            Type: "LO",
            DocNo: $"SPK-{id}",
            BaseDoc: "SO",
            BaseDocNo: $"SO-{id}",
            CardCode: "C001",
            CardName: "Customer",
            ItemCode: "SKU-1",
            ItemName: itemName,
            Quantity: 100m,
            DeliveryQty: 100m,
            UoM: "Kg",
            PackType: "Curah",
            WhsCode: "WH-MDN",
            WhsName: "Medan - Agung",
            DocStatus: "O",
            BlNo: blNo,
            SortOrder: 0,
            ItemShadowId: null);

    private static BudgetPlanResponse Plan(
        string status = "Approved",
        List<BudgetPlanItemResponse>? items = null,
        List<BudgetPlanSpkItemResponse>? spkItems = null,
        BudgetPlanApprovalInfo? approval = null) =>
        new(
            Id: 1,
            BudgetNo: "BP-2602000012",
            Template: new BudgetTemplateSummaryInfo(1, "TPL-001", null, null, null),
            WarehouseCode: "WH-MDN",
            WarehouseName: "Medan - Agung",
            Remark: null,
            DocDate: new DateTime(2026, 2, 3),
            Status: status,
            StatusDisplay: status,
            SpkItems: spkItems ?? [],
            Items: items ?? [],
            GrandTotal: 0m,
            TotalPpnAmount: 0m,
            TotalPphAmount: 0m,
            TaxInclusiveGrandTotal: 0m,
            CreatedAt: new DateTime(2026, 2, 3),
            CreatedByName: "M Ridwan Nasution",
            SubmittedAt: new DateTime(2026, 2, 3),
            SubmittedByName: "M Ridwan Nasution",
            Approval: approval ?? new BudgetPlanApprovalInfo(TotalStages: 0, CurrentStageOrder: 0, Stages: []),
            RejectedAt: null,
            RejectedByName: null,
            RejectionReason: null);

    [Fact]
    public void FromBudgetPlan_emits_one_page_per_bill_of_lading_and_drops_non_rfba_items()
    {
        var plan = Plan(items:
        [
            Item(1, isRfba: true,  bl: "SSZI711911", costName: "Bongkar curah container 40 ft",     qty: 9m,  cost: 675_000m,   total: 6_075_000m, sortOrder: 0),
            Item(2, isRfba: true,  bl: "SSZI711911", costName: "Bongkar rebagging in container 40 ft", qty: 1m, cost: 1_240_000m, total: 1_240_000m, sortOrder: 1),
            Item(3, isRfba: true,  bl: "SSZI711911", costName: "Lintas timbang container",           qty: 287_000m, cost: 2m,    total: 574_000m,   sortOrder: 2),
            Item(4, isRfba: true,  bl: "MEDUJM026632", costName: "Bongkar curah container 40 ft",    qty: 22m, cost: 675_000m,   total: 14_850_000m, sortOrder: 3),
            Item(5, isRfba: false, bl: "SSZI711911", costName: "Sewa gudang",                        qty: 1m,  cost: 9_000_000m, total: 9_000_000m, sortOrder: 4)
        ]);

        var doc = RfbaFormMapper.FromBudgetPlan(plan);

        doc.Pages.Should().HaveCount(2);
        doc.Pages[0].BillOfLading.Should().Be("SSZI711911");
        doc.Pages[0].Rows.Should().HaveCount(3);
        doc.Pages[0].Rows.Select(r => r.Component).Should().NotContain("Sewa gudang");
        doc.Pages[1].BillOfLading.Should().Be("MEDUJM026632");
        doc.Pages[1].Rows.Should().HaveCount(1);
    }

    [Fact]
    public void FromBudgetPlan_totals_each_page_from_its_own_rows_only()
    {
        var plan = Plan(items:
        [
            Item(1, isRfba: true, bl: "SSZI711911", costName: "Bongkar curah container 40 ft",        qty: 9m,       cost: 675_000m,   total: 6_075_000m, sortOrder: 0),
            Item(2, isRfba: true, bl: "SSZI711911", costName: "Bongkar rebagging in container 40 ft", qty: 1m,       cost: 1_240_000m, total: 1_240_000m, sortOrder: 1),
            Item(3, isRfba: true, bl: "SSZI711911", costName: "Lintas timbang container",             qty: 287_000m, cost: 2m,         total: 574_000m,   sortOrder: 2),
            Item(4, isRfba: true, bl: "MEDUJM026632", costName: "Bongkar curah container 40 ft",      qty: 22m,      cost: 675_000m,   total: 14_850_000m, sortOrder: 3)
        ]);

        var doc = RfbaFormMapper.FromBudgetPlan(plan);

        // Page 1 of the client sample: 6,075,000 + 1,240,000 + 574,000
        doc.Pages[0].Total.Should().Be(7_889_000m);
        doc.Pages[1].Total.Should().Be(14_850_000m);
    }

    [Fact]
    public void FromBudgetPlan_resolves_produk_from_the_spk_item_carrying_the_same_bl()
    {
        var plan = Plan(
            items:
            [
                Item(1, isRfba: true, bl: "SSZI711911",   costName: "Bongkar curah container 40 ft", qty: 9m,  cost: 675_000m, total: 6_075_000m, sortOrder: 0),
                Item(2, isRfba: true, bl: "MEDUJM026632", costName: "Bongkar curah container 40 ft", qty: 22m, cost: 675_000m, total: 14_850_000m, sortOrder: 1)
            ],
            spkItems:
            [
                Spk(1, "SSZI711911", "DDGS Brazil"),
                Spk(2, "MEDUJM026632", "SBM Bolivia")
            ]);

        var doc = RfbaFormMapper.FromBudgetPlan(plan);

        doc.Pages[0].Produk.Should().Be("DDGS Brazil");
        doc.Pages[1].Produk.Should().Be("SBM Bolivia");
    }

    [Fact]
    public void FromBudgetPlan_groups_items_without_a_bl_onto_a_final_page_rather_than_dropping_them()
    {
        var plan = Plan(items:
        [
            Item(1, isRfba: true, bl: null,          costName: "Biaya lain-lain", qty: 1m, cost: 500_000m, total: 500_000m, sortOrder: 0),
            Item(2, isRfba: true, bl: "  ",          costName: "Biaya admin",     qty: 1m, cost: 250_000m, total: 250_000m, sortOrder: 1),
            Item(3, isRfba: true, bl: "SSZI711911",  costName: "Bongkar curah",   qty: 9m, cost: 675_000m, total: 6_075_000m, sortOrder: 2)
        ]);

        var doc = RfbaFormMapper.FromBudgetPlan(plan);

        doc.Pages.Should().HaveCount(2);
        doc.Pages[0].BillOfLading.Should().Be("SSZI711911");
        doc.Pages[^1].BillOfLading.Should().BeNull();
        doc.Pages[^1].Rows.Should().HaveCount(2);
        doc.Pages[^1].Total.Should().Be(750_000m);
    }

    [Fact]
    public void FromBudgetPlan_marks_everything_but_an_approved_plan_as_draft()
    {
        var items = new List<BudgetPlanItemResponse>
        {
            Item(1, isRfba: true, bl: "SSZI711911", costName: "Bongkar curah", qty: 9m, cost: 675_000m, total: 6_075_000m, sortOrder: 0)
        };

        RfbaFormMapper.FromBudgetPlan(Plan(status: "Approved", items: items)).IsDraft.Should().BeFalse();
        RfbaFormMapper.FromBudgetPlan(Plan(status: "Submitted", items: items)).IsDraft.Should().BeTrue();
        RfbaFormMapper.FromBudgetPlan(Plan(status: "Draft", items: items)).IsDraft.Should().BeTrue();
    }

    [Fact]
    public void FromBudgetPlan_returns_no_pages_when_the_plan_has_no_rfba_items()
    {
        var plan = Plan(items:
        [
            Item(1, isRfba: false, bl: "SSZI711911", costName: "Sewa gudang", qty: 1m, cost: 9_000_000m, total: 9_000_000m, sortOrder: 0)
        ]);

        RfbaFormMapper.FromBudgetPlan(plan).Pages.Should().BeEmpty();
    }

    [Fact]
    public void FromBudgetPlan_maker_is_always_the_plan_creator_with_its_created_date()
    {
        var plan = Plan();
        var doc = RfbaFormMapper.FromBudgetPlan(plan);

        doc.MakerName.Should().Be("M Ridwan Nasution");
        doc.MakerDate.Should().Be(new DateTime(2026, 2, 3));
    }

    [Fact]
    public void FromBudgetPlan_emits_one_approver_per_stage_in_stage_order_leaving_unapproved_stages_blank()
    {
        var approval = new BudgetPlanApprovalInfo(
            TotalStages: 3,
            CurrentStageOrder: 3,
            Stages:
            [
                new WorkflowStageInfo(2, "Manager", ["Manager"], "Approved", new DateTime(2026, 2, 2), "Budi Santoso", null, null, null),
                new WorkflowStageInfo(1, "Checker", ["Checker"], "Approved", new DateTime(2026, 2, 1), "Andi", null, null, null),
                new WorkflowStageInfo(3, "Director", ["Director"], "Pending", null, null, null, null, null)
            ]);

        var doc = RfbaFormMapper.FromBudgetPlan(Plan(approval: approval));

        doc.Approvers.Should().Equal(
            new RfbaApprover("Andi", new DateTime(2026, 2, 1)),
            new RfbaApprover("Budi Santoso", new DateTime(2026, 2, 2)),
            new RfbaApprover(null, null));
    }

    [Fact]
    public void FromBudgetPlan_leaves_a_rejected_stage_unnamed()
    {
        var approval = new BudgetPlanApprovalInfo(
            TotalStages: 1,
            CurrentStageOrder: 1,
            Stages:
            [
                new WorkflowStageInfo(1, "Checker", ["Checker"], "Rejected", null, null, new DateTime(2026, 2, 1), "Andi", "Salah COA")
            ]);

        var doc = RfbaFormMapper.FromBudgetPlan(Plan(approval: approval));

        doc.Approvers.Should().Equal(new RfbaApprover(null, null));
    }

    [Fact]
    public void FromBudgetPlan_emits_no_approvers_when_the_plan_has_no_workflow()
    {
        RfbaFormMapper.FromBudgetPlan(Plan()).Approvers.Should().BeEmpty();
    }
}

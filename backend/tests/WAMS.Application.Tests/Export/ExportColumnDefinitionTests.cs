namespace WAMS.Application.Tests.Export;

using FluentAssertions;
using Xunit;
using WAMS.Application.DTOs.AccountPayables;
using WAMS.Application.DTOs.BudgetPlans;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Application.DTOs.WorkOrders;
using WAMS.Application.DTOs.RecapWorkOrders;
using WAMS.Application.DTOs.TransportOrders;
using WAMS.Application.DTOs.Spk;
using WAMS.Application.DTOs.FinanceReports;
using WAMS.Application.DTOs.Users;
using WAMS.Application.DTOs.Roles;
using WAMS.Application.DTOs.Companies;
using WAMS.Application.DTOs.Warehouses;
using WAMS.Application.DTOs.Items;
using WAMS.Application.DTOs.Vendors;
using WAMS.Application.DTOs.AuditLogs;
using WAMS.Application.DTOs.RateCards;
using WAMS.Application.DTOs.BudgetTemplates;
using WAMS.Application.Export.Definitions;

public class ExportColumnDefinitionTests
{
    [Fact]
    public void WorkOrderExportColumns_AllAccessors_DoNotThrowOnMinimalRow()
    {
        var row = new WorkOrderSummaryResponse(
            Id: 1, Code: "WO-001", BudgetPlanId: 1, BudgetPlanCode: "BP-001",
            ActivityTypeCode: "ACT", ActivityTypeDisplay: "Activity",
            ItemShadowId: 1, ActivityName: "Loading",
            WarehouseCode: "WH01", WarehouseName: "Warehouse 1",
            PicName: null, IsRfba: false,
            StartDate: DateTime.UtcNow, EndDate: DateTime.UtcNow,
            Status: "Draft", CreatedAt: DateTime.UtcNow, CreatedByName: "Admin",
            BlNumber: null, ProductName: null, VesselName: null);

        var invoking = () =>
        {
            foreach (var col in WorkOrderExportColumns.Columns)
                col.Accessor(row);
        };

        invoking.Should().NotThrow();
    }

    [Fact]
    public void BudgetPlanExportColumns_AllAccessors_DoNotThrowOnMinimalRow()
    {
        var approval = new BudgetPlanApprovalInfo(
            TotalStages: 1,
            CurrentStageOrder: 0,
            Stages: []);
        var row = new BudgetPlanSummaryResponse(
            Id: 1, BudgetNo: "BP-001", TemplateCode: "TPL-001",
            Remark: null, Location: null, VendorName: null, MakerName: null,
            DocDate: DateTime.UtcNow, Status: "Draft", StatusDisplay: "Draft",
            Approval: approval);

        var invoking = () =>
        {
            foreach (var col in BudgetPlanExportColumns.Columns)
                col.Accessor(row);
        };

        invoking.Should().NotThrow();
    }

    [Fact]
    public void PurchaseOrderExportColumns_AllAccessors_DoNotThrowOnMinimalRow()
    {
        var row = new PurchaseOrderSummaryResponse(
            Id: 1, Code: "PO-001", VendorCode: "V001", VendorName: "Vendor",
            Status: "Draft", DocDate: DateTime.UtcNow, Remark: null,
            SapPoNumber: null, GrandTotal: 0m, ItemCount: 0,
            CreatedAt: DateTime.UtcNow, CreatedByName: "Admin");

        var invoking = () =>
        {
            foreach (var col in PurchaseOrderExportColumns.Columns)
                col.Accessor(row);
        };

        invoking.Should().NotThrow();
    }

    [Fact]
    public void AccountPayableExportColumns_AllAccessors_DoNotThrowOnMinimalRow()
    {
        var row = new AccountPayableSummaryResponse(
            Id: 1, Code: "AP-001", VendorCode: "V001", VendorName: "Vendor",
            Status: "Draft", DocDate: DateTime.UtcNow, Remark: null,
            SapApNumber: null, GrandTotal: 0m, ItemCount: 0,
            CreatedAt: DateTime.UtcNow, CreatedByName: "Admin");

        var invoking = () =>
        {
            foreach (var col in AccountPayableExportColumns.Columns)
                col.Accessor(row);
        };

        invoking.Should().NotThrow();
    }

    [Fact]
    public void RecapWorkOrderExportColumns_AllAccessors_DoNotThrowOnMinimalRow()
    {
        var row = new RecapWorkOrderSummaryResponse(
            Id: 1, BudgetPlanId: 1, BudgetPlanCode: "BP-001",
            TemplateCode: "TPL", Remark: null,
            WarehouseCode: "WH01", WarehouseName: "Warehouse",
            BlNumbers: null, ActivityTypes: null, PicNames: null,
            IsRfba: false, DocDate: DateTime.UtcNow,
            RecapStatus: "Draft", CreatedAt: DateTime.UtcNow);

        var invoking = () =>
        {
            foreach (var col in RecapWorkOrderExportColumns.Columns)
                col.Accessor(row);
        };

        invoking.Should().NotThrow();
    }

    [Fact]
    public void TransportOrderExportColumns_AllAccessors_DoNotThrowOnMinimalRow()
    {
        var row = new TransportOrderShadowResponse(
            Id: 1, DocNo: "TO-001", Type: "GR", CardCode: "C001", CardName: "Customer",
            VehicleNo: "B 1234 XX", VehicleType: "Truck", BlNo: "BL-001",
            ItemCode: "ITEM-001", ItemName: "Item",
            Quantity: null, UoM: "MT", WhsCode: "WH01",
            WhsName: "Warehouse", DocStatus: "Open");

        var invoking = () =>
        {
            foreach (var col in TransportOrderExportColumns.Columns)
                col.Accessor(row);
        };

        invoking.Should().NotThrow();
    }

    [Fact]
    public void SpkExportColumns_AllAccessors_DoNotThrowOnMinimalRow()
    {
        var row = new SpkShadowResponse(
            Id: 1, Type: "GR", DocNo: "SPK-001", BaseDoc: "PO",
            BaseDocNo: "PO-001", CardCode: "C001", CardName: "Customer",
            ItemCode: "ITEM-001", ItemName: "Item",
            Quantity: null, DeliveryQty: null, UoM: "MT",
            PackType: "BAG", WhsCode: "WH01", WhsName: "Warehouse",
            DocStatus: "Open", BlNo: null);

        var invoking = () =>
        {
            foreach (var col in SpkExportColumns.Columns)
                col.Accessor(row);
        };

        invoking.Should().NotThrow();
    }

    [Fact]
    public void UserExportColumns_AllAccessors_DoNotThrowOnMinimalRow()
    {
        var row = new UserResponse(
            Id: 1, Email: "user@example.com", Fullname: "User",
            EmployeeId: null, IsActive: true, CreatedAt: DateTime.UtcNow,
            Roles: [], Warehouses: [], Scopes: []);

        var invoking = () =>
        {
            foreach (var col in UserExportColumns.Columns)
                col.Accessor(row);
        };

        invoking.Should().NotThrow();
    }

    [Fact]
    public void RoleExportColumns_AllAccessors_DoNotThrowOnMinimalRow()
    {
        var row = new RoleResponse(
            Id: 1, Name: "Admin", DisplayName: null, Description: null,
            IsSystem: true, GlobalAccess: false,
            CreatedAt: DateTime.UtcNow, Permissions: []);

        var invoking = () =>
        {
            foreach (var col in RoleExportColumns.Columns)
                col.Accessor(row);
        };

        invoking.Should().NotThrow();
    }

    [Fact]
    public void CompanyExportColumns_AllAccessors_DoNotThrowOnMinimalRow()
    {
        var row = new CompanyResponse(
            Id: 1, Code: "COMP", Name: "Company", Address: null,
            Phone: null, Email: null, IsActive: true,
            CreatedAt: DateTime.UtcNow, UserCount: 0, WarehouseCount: 0,
            HasLogo: false);

        var invoking = () =>
        {
            foreach (var col in CompanyExportColumns.Columns)
                col.Accessor(row);
        };

        invoking.Should().NotThrow();
    }

    [Fact]
    public void WarehouseExportColumns_AllAccessors_DoNotThrowOnMinimalRow()
    {
        var row = new WarehouseResponse(
            Id: 1, Code: "WH01", Name: "Warehouse", Location: null,
            IsActive: true, FirstSeenAt: DateTime.UtcNow, SyncedAt: DateTime.UtcNow);

        var invoking = () =>
        {
            foreach (var col in WarehouseExportColumns.Columns)
                col.Accessor(row);
        };

        invoking.Should().NotThrow();
    }

    [Fact]
    public void ItemExportColumns_AllAccessors_DoNotThrowOnMinimalRow()
    {
        var row = new ItemSummaryResponse(1, "ITEM-001", "Item Name", "ACCT-001", "Account Name");

        var invoking = () =>
        {
            foreach (var col in ItemExportColumns.Columns)
                col.Accessor(row);
        };

        invoking.Should().NotThrow();
    }

    [Fact]
    public void VendorExportColumns_AllAccessors_DoNotThrowOnMinimalRow()
    {
        var row = new VendorSummaryResponse(1, "V001", "Vendor Name");

        var invoking = () =>
        {
            foreach (var col in VendorExportColumns.Columns)
                col.Accessor(row);
        };

        invoking.Should().NotThrow();
    }

    [Fact]
    public void AuditLogExportColumns_AllAccessors_DoNotThrowOnMinimalRow()
    {
        var row = new AuditLogResponse(
            Id: 1, Action: "CREATE", TableName: "users", RecordId: null,
            RecordKey: null, UserId: null, UserEmail: null, UserFullname: null,
            CompanyId: null, OldValues: null, NewValues: null,
            RequestId: null, RequestPath: null, HttpMethod: null,
            IpAddress: null, UserAgent: null, CreatedAt: DateTime.UtcNow);

        var invoking = () =>
        {
            foreach (var col in AuditLogExportColumns.Columns)
                col.Accessor(row);
        };

        invoking.Should().NotThrow();
    }

    [Fact]
    public void RateCardExportColumns_AllAccessors_DoNotThrowOnMinimalRow()
    {
        var vendor = new VendorSummaryResponse(1, "V001", "Vendor");
        var row = new RateCardSummaryResponse(
            Id: 1, Vendor: vendor, Status: "Draft",
            ItemCount: 0, CreatedAt: DateTime.UtcNow);

        var invoking = () =>
        {
            foreach (var col in RateCardExportColumns.Columns)
                col.Accessor(row);
        };

        invoking.Should().NotThrow();
    }

    [Fact]
    public void BudgetTemplateExportColumns_AllAccessors_DoNotThrowOnMinimalRow()
    {
        var row = new BudgetTemplateSummaryResponse(
            Id: 1, TemplateCode: "TPL-001",
            ProvinceId: null, ProvinceName: null, ProvinceDisplay: null, Date: DateTime.UtcNow, Status: "Draft");

        var invoking = () =>
        {
            foreach (var col in BudgetTemplateExportColumns.Columns)
                col.Accessor(row);
        };

        invoking.Should().NotThrow();
    }
}

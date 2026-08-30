namespace WAMS.Domain.Constants;

public static class Permissions
{
    public static class Budget
    {
        public const string PlanRead = "budget.plan.read";
        public const string PlanCreate = "budget.plan.create";
        public const string PlanUpdate = "budget.plan.update";
        public const string PlanDelete = "budget.plan.delete";
        public const string PlanSubmit = "budget.plan.submit";
        public const string PlanApprove = "budget.plan.approve";
        public const string PlanReject = "budget.plan.reject";
        public const string PlanExport = "budget.plan.export";

        public const string TemplateRead = "budget.template.read";
        public const string TemplateCreate = "budget.template.create";
        public const string TemplateUpdate = "budget.template.update";
        public const string TemplateDelete = "budget.template.delete";
        public const string TemplateSubmit = "budget.template.submit";
        public const string TemplateExport = "budget.template.export";

        public const string PoRead = "budget.po.read";
        public const string PoCreate = "budget.po.create";
        public const string PoUpdate = "budget.po.update";
        public const string PoDelete = "budget.po.delete";
        public const string PoGenerate = "budget.po.generate";
        public const string PoExport = "budget.po.export";

        public const string RateCardRead = "budget.rate_card.read";
        public const string RateCardCreate = "budget.rate_card.create";
        public const string RateCardUpdate = "budget.rate_card.update";
        public const string RateCardDelete = "budget.rate_card.delete";
        public const string RateCardSubmit = "budget.rate_card.submit";
        public const string RateCardExport = "budget.rate_card.export";

        public const string UomRead = "budget.uom.read";
        public const string UomCreate = "budget.uom.create";
        public const string UomUpdate = "budget.uom.update";
        public const string UomDelete = "budget.uom.delete";

        // Read-only: tax types are SAP-synced master data (PpnSyncService /
        // PphLookupService), so WAMS never creates or edits them.
        public const string TaxTypeRead = "budget.tax_type.read";

        public const string VendorRead = "budget.vendor.read";
        public const string VendorExport = "budget.vendor.export";

        public const string ItemRead = "budget.item.read";
        public const string ItemExport = "budget.item.export";
    }

    // module == resource → drop redundant prefix, use action directly
    public static class WorkOrder
    {
        public const string Read = "workorder.workorder.read";
        public const string Update = "workorder.workorder.update";
        public const string Delete = "workorder.workorder.delete";
        public const string Submit = "workorder.workorder.submit";
        public const string Export = "workorder.workorder.export";

        // Eligibility, not an endpoint gate: marks a role as field work, making its holders
        // assignable as work order PIC. Matched exactly -workorder.*.* does not confer it.
        public const string Execute = "workorder.workorder.execute";

        public const string ApRead = "workorder.ap.read";
        public const string ApCreate = "workorder.ap.create";
        public const string ApUpdate = "workorder.ap.update";
        public const string ApDelete = "workorder.ap.delete";
        public const string ApGenerate = "workorder.ap.generate";
        public const string ApExport = "workorder.ap.export";

        public const string RecapRead = "workorder.recap.read";
        public const string RecapApprove = "workorder.recap.approve";
        public const string RecapReject = "workorder.recap.reject";
        public const string RecapExport = "workorder.recap.export";
    }

    public static class System
    {
        public const string CompanyRead = "system.company.read";
        public const string CompanyCreate = "system.company.create";
        public const string CompanyUpdate = "system.company.update";
        public const string CompanyDelete = "system.company.delete";
        public const string CompanyExport = "system.company.export";
        public const string CompanyAssign = "system.company.assign";

        public const string SyncRead = "system.sync.read";
        public const string SyncExecute = "system.sync.execute";

        public const string ActivityTypeCreate = "system.activity_type.create";
        public const string ActivityTypeUpdate = "system.activity_type.update";
        public const string ActivityTypeDelete = "system.activity_type.delete";
    }

    // module == resource → use action directly for user.user.*
    public static class User
    {
        public const string Read = "user.user.read";
        public const string Create = "user.user.create";
        public const string Update = "user.user.update";
        public const string ResetPassword = "user.user.reset_password";
        public const string Delete = "user.user.delete";
        public const string Export = "user.user.export";

        public const string RoleRead = "user.role.read";
        public const string RoleCreate = "user.role.create";
        public const string RoleUpdate = "user.role.update";
        public const string RoleDelete = "user.role.delete";
        public const string RoleExport = "user.role.export";

        public const string WarehouseRead = "user.warehouse.read";
        public const string WarehouseCreate = "user.warehouse.create";
        public const string WarehouseDelete = "user.warehouse.delete";
        public const string WarehouseExport = "user.warehouse.export";

        public const string PermissionRead = "user.permission.read";
        public const string PermissionCreate = "user.permission.create";
        public const string PermissionDelete = "user.permission.delete";
    }

    public static class Report
    {
        public const string DashboardRead = "report.dashboard.read";

        public const string FinanceReportRead = "report.finance-report.read";
        public const string FinanceReportExport = "report.finance-report.export";
    }

    public static class Rca
    {
        // Deliberately a separate module (not "report") so it is not swept up by
        // wildcard grants like HO_SPV's report.*.* - only SUPER_ADMIN and FINANCE_USER
        // should be able to export the RCA report.
        public const string ReportExport = "rca.report.export";
    }

    public static class Approval
    {
        // Own module, same reasoning as Rca above: budget.*.* (HO_SPV, LOG_MGR, LOG_SPV) must
        // not pick this up. Only *.*.* (SUPER_ADMIN) grants it.
        public const string SelfApprove = "approval.self.approve";
    }

    public static class Audit
    {
        public const string LogRead = "audit.log.read";
        public const string LogExport = "audit.log.export";
    }

    public static class Workflow
    {
        public const string TemplateRead = "workflow.template.read";
        public const string TemplateCreate = "workflow.template.create";
        public const string TemplateUpdate = "workflow.template.update";
        public const string TemplateDelete = "workflow.template.delete";
    }


    public static class Wildcards
    {
        public const string All = "*.*.*";
        public const string AllRead = "*.*.read";
        public const string AllExport = "*.*.export";
    }

    // Module-name literals for module-wide seeder helpers
    // (AssignWildcardPermissionsAsync / AssignModuleReadPermissionsAsync / AssignModuleExportPermissionsAsync).
    public static class Modules
    {
        public const string Budget = "budget";
        public const string Report = "report";
        public const string User = "user";
        public const string WorkOrder = "workorder";
        public const string Workflow = "workflow";
        public const string System = "system";
    }
}

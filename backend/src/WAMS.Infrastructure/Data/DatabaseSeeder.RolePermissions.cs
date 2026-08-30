namespace WAMS.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Roles;

/// <summary>Role-to-permission assignment, kept separate from the rest of DatabaseSeeder.</summary>
public partial class DatabaseSeeder
{
    private async Task AssignRolePermissionsAsync()
    {
        var permissions = await _context.Permissions.ToDictionaryAsync(
            p => $"{p.Module}.{p.Resource}.{p.Action}",
            p => p.Id
        );

        // Use IgnoreQueryFilters - no tenant context during seeding
        var roles = await _context.Roles
            .IgnoreQueryFilters()
            .ToDictionaryAsync(r => r.Name, r => r.Id);

        // SUPER_ADMIN: *.*.*
        await AssignPermissionToRoleAsync(roles[RoleCodes.SuperAdmin], permissions[Permissions.Wildcards.All]);

        // IT_OPS: user + system administration + read-only visibility.
        // NOT full admin - *.*.* is reserved for SUPER_ADMIN only.
        await AssignWildcardPermissionsAsync(roles[RoleCodes.ItOps], permissions, Permissions.Modules.User);
        await AssignWildcardPermissionsAsync(roles[RoleCodes.ItOps], permissions, Permissions.Modules.System);
        await AssignPermissionToRoleAsync(roles[RoleCodes.ItOps], permissions[Permissions.Wildcards.AllRead]);

        // FAT_MGR: finance/report reads + budget reference-data reads, plus tax type read
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatMgr], permissions[Permissions.Report.DashboardRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatMgr], permissions[Permissions.Report.FinanceReportRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatMgr], permissions[Permissions.Budget.PlanRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatMgr], permissions[Permissions.Budget.TemplateRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatMgr], permissions[Permissions.Budget.VendorRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatMgr], permissions[Permissions.Budget.ItemRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatMgr], permissions[Permissions.Budget.RateCardRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatMgr], permissions[Permissions.Report.FinanceReportExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatMgr], permissions[Permissions.Rca.ReportExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatMgr], permissions[Permissions.Budget.PlanExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatMgr], permissions[Permissions.Budget.TemplateExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatMgr], permissions[Permissions.Budget.VendorExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatMgr], permissions[Permissions.Budget.ItemExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatMgr], permissions[Permissions.Budget.RateCardExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatMgr], permissions[Permissions.Budget.TaxTypeRead]);

        // FAT_OPS: same grant as FAT_MGR
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatOps], permissions[Permissions.Report.DashboardRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatOps], permissions[Permissions.Report.FinanceReportRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatOps], permissions[Permissions.Budget.PlanRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatOps], permissions[Permissions.Budget.TemplateRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatOps], permissions[Permissions.Budget.VendorRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatOps], permissions[Permissions.Budget.ItemRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatOps], permissions[Permissions.Budget.RateCardRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatOps], permissions[Permissions.Report.FinanceReportExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatOps], permissions[Permissions.Rca.ReportExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatOps], permissions[Permissions.Budget.PlanExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatOps], permissions[Permissions.Budget.TemplateExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatOps], permissions[Permissions.Budget.VendorExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatOps], permissions[Permissions.Budget.ItemExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatOps], permissions[Permissions.Budget.RateCardExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.FatOps], permissions[Permissions.Budget.TaxTypeRead]);

        // LOG_MGR: budget.*.*, report.*.*, workflow.*.*, user/workorder reads + exports, recap verdict, AP write
        await AssignWildcardPermissionsAsync(roles[RoleCodes.LogMgr], permissions, Permissions.Modules.Budget);
        await AssignWildcardPermissionsAsync(roles[RoleCodes.LogMgr], permissions, Permissions.Modules.Report);
        await AssignModuleReadPermissionsAsync(roles[RoleCodes.LogMgr], permissions, Permissions.Modules.User);
        await AssignModuleReadPermissionsAsync(roles[RoleCodes.LogMgr], permissions, Permissions.Modules.WorkOrder);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMgr], permissions[Permissions.WorkOrder.RecapApprove]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMgr], permissions[Permissions.WorkOrder.RecapReject]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMgr], permissions[Permissions.WorkOrder.ApCreate]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMgr], permissions[Permissions.WorkOrder.ApUpdate]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMgr], permissions[Permissions.WorkOrder.ApDelete]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMgr], permissions[Permissions.WorkOrder.ApGenerate]);
        await AssignModuleExportPermissionsAsync(roles[RoleCodes.LogMgr], permissions, Permissions.Modules.WorkOrder);
        await AssignModuleExportPermissionsAsync(roles[RoleCodes.LogMgr], permissions, Permissions.Modules.User);
        await AssignWildcardPermissionsAsync(roles[RoleCodes.LogMgr], permissions, Permissions.Modules.Workflow);

        // LOG_SPV: same grant as LOG_MGR
        await AssignWildcardPermissionsAsync(roles[RoleCodes.LogSpv], permissions, Permissions.Modules.Budget);
        await AssignWildcardPermissionsAsync(roles[RoleCodes.LogSpv], permissions, Permissions.Modules.Report);
        await AssignModuleReadPermissionsAsync(roles[RoleCodes.LogSpv], permissions, Permissions.Modules.User);
        await AssignModuleReadPermissionsAsync(roles[RoleCodes.LogSpv], permissions, Permissions.Modules.WorkOrder);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogSpv], permissions[Permissions.WorkOrder.RecapApprove]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogSpv], permissions[Permissions.WorkOrder.RecapReject]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogSpv], permissions[Permissions.WorkOrder.ApCreate]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogSpv], permissions[Permissions.WorkOrder.ApUpdate]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogSpv], permissions[Permissions.WorkOrder.ApDelete]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogSpv], permissions[Permissions.WorkOrder.ApGenerate]);
        await AssignModuleExportPermissionsAsync(roles[RoleCodes.LogSpv], permissions, Permissions.Modules.WorkOrder);
        await AssignModuleExportPermissionsAsync(roles[RoleCodes.LogSpv], permissions, Permissions.Modules.User);
        await AssignWildcardPermissionsAsync(roles[RoleCodes.LogSpv], permissions, Permissions.Modules.Workflow);

        // LOG_MKT_OPS: budget plan + PO write, workorder.*.*, budget reference-data reads + exports
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.User.WarehouseRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.PlanCreate]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.PlanRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.PlanUpdate]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.PlanSubmit]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.PlanDelete]);
        await AssignWildcardPermissionsAsync(roles[RoleCodes.LogMktOps], permissions, Permissions.Modules.WorkOrder);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.UomRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.RateCardRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.TemplateRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.TemplateUpdate]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.TaxTypeRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.ItemRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.VendorRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.PlanExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.TemplateExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.ItemExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.RateCardExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.VendorExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.PoExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.User.WarehouseExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.PoRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.PoCreate]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.PoUpdate]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.PoDelete]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Budget.PoGenerate]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.LogMktOps], permissions[Permissions.Workflow.TemplateRead]);

        // WH_MGR: plan approve/reject, read-only across budget + workorder, exports
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhMgr], permissions[Permissions.User.WarehouseRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhMgr], permissions[Permissions.Budget.PlanRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhMgr], permissions[Permissions.Budget.PlanApprove]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhMgr], permissions[Permissions.Budget.PlanReject]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhMgr], permissions[Permissions.Budget.TemplateRead]);
        await AssignModuleReadPermissionsAsync(roles[RoleCodes.WhMgr], permissions, Permissions.Modules.WorkOrder);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhMgr], permissions[Permissions.Budget.VendorRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhMgr], permissions[Permissions.Budget.ItemRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhMgr], permissions[Permissions.Budget.UomRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhMgr], permissions[Permissions.Budget.RateCardRead]);
        await AssignModuleExportPermissionsAsync(roles[RoleCodes.WhMgr], permissions, Permissions.Modules.WorkOrder);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhMgr], permissions[Permissions.Budget.PlanExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhMgr], permissions[Permissions.Budget.TemplateExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhMgr], permissions[Permissions.Budget.ItemExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhMgr], permissions[Permissions.Budget.RateCardExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhMgr], permissions[Permissions.Budget.PoExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhMgr], permissions[Permissions.Budget.PoRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhMgr], permissions[Permissions.Workflow.TemplateRead]);

        // WH_SPV: WH_MGR's grant plus the four field-supervision keys (recap verdict, realization entry, PIC eligibility).
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhSpv], permissions[Permissions.User.WarehouseRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhSpv], permissions[Permissions.Budget.PlanRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhSpv], permissions[Permissions.Budget.PlanApprove]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhSpv], permissions[Permissions.Budget.PlanReject]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhSpv], permissions[Permissions.Budget.TemplateRead]);
        await AssignModuleReadPermissionsAsync(roles[RoleCodes.WhSpv], permissions, Permissions.Modules.WorkOrder);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhSpv], permissions[Permissions.Budget.VendorRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhSpv], permissions[Permissions.Budget.ItemRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhSpv], permissions[Permissions.Budget.UomRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhSpv], permissions[Permissions.Budget.RateCardRead]);
        await AssignModuleExportPermissionsAsync(roles[RoleCodes.WhSpv], permissions, Permissions.Modules.WorkOrder);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhSpv], permissions[Permissions.Budget.PlanExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhSpv], permissions[Permissions.Budget.TemplateExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhSpv], permissions[Permissions.Budget.ItemExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhSpv], permissions[Permissions.Budget.RateCardExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhSpv], permissions[Permissions.Budget.PoExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhSpv], permissions[Permissions.Budget.PoRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhSpv], permissions[Permissions.Workflow.TemplateRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhSpv], permissions[Permissions.WorkOrder.RecapApprove]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhSpv], permissions[Permissions.WorkOrder.RecapReject]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhSpv], permissions[Permissions.WorkOrder.Update]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhSpv], permissions[Permissions.WorkOrder.Execute]);

        // WH_OPS: same grant as LOG_MKT_OPS
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.User.WarehouseRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.Budget.PlanCreate]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.Budget.PlanRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.Budget.PlanUpdate]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.Budget.PlanSubmit]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.Budget.PlanDelete]);
        await AssignWildcardPermissionsAsync(roles[RoleCodes.WhOps], permissions, Permissions.Modules.WorkOrder);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.Budget.UomRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.Budget.RateCardRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.Budget.TemplateRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.Budget.ItemRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.Budget.VendorRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.Budget.PlanExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.Budget.TemplateExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.Budget.ItemExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.Budget.RateCardExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.Budget.PoExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.User.WarehouseExport]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.Budget.PoRead]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.Budget.PoCreate]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.Budget.PoUpdate]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.Budget.PoDelete]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.Budget.PoGenerate]);
        await AssignPermissionToRoleAsync(roles[RoleCodes.WhOps], permissions[Permissions.Workflow.TemplateRead]);

        await _uow.CommitAsync();
        _logger.LogInformation("Assigned permissions to roles");
    }
    private async Task AssignPermissionToRoleAsync(long roleId, long permissionId)
    {
        if (!await _context.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId))
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });
        }
    }

    private async Task AssignWildcardPermissionsAsync(long roleId, Dictionary<string, long> permissions, string module)
    {
        foreach (var kvp in permissions)
        {
            if (kvp.Key.StartsWith($"{module}."))
            {
                await AssignPermissionToRoleAsync(roleId, kvp.Value);
            }
        }
    }

    private async Task AssignModuleReadPermissionsAsync(long roleId, Dictionary<string, long> permissions, string module)
    {
        foreach (var kvp in permissions)
        {
            if (kvp.Key.StartsWith($"{module}.") && kvp.Key.EndsWith(".read"))
            {
                await AssignPermissionToRoleAsync(roleId, kvp.Value);
            }
        }
    }

    private async Task AssignModuleExportPermissionsAsync(long roleId, Dictionary<string, long> permissions, string module)
    {
        foreach (var kvp in permissions)
        {
            if (kvp.Key.StartsWith($"{module}.") && kvp.Key.EndsWith(".export"))
                await AssignPermissionToRoleAsync(roleId, kvp.Value);
        }
    }
}

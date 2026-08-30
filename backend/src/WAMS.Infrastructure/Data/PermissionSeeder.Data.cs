namespace WAMS.Infrastructure.Data;

using WAMS.Domain.Entities.Roles;
using WAMS.Domain.Entities.Users;

/// <summary>
/// Every permission the app knows about. Pure data, no seeding logic - see
/// DatabaseSeeder.SeedPermissionsAsync for how this gets diffed against the DB.
/// </summary>
public static class PermissionSeeder
{
    public static List<Permission> All =>
    [
        // User module
        new() { Module = "user", Resource = "user", Action = "create", Description = "Create new users" },
        new() { Module = "user", Resource = "user", Action = "read", Description = "View user details" },
        new() { Module = "user", Resource = "user", Action = "update", Description = "Update user information" },
        new() { Module = "user", Resource = "user", Action = "reset_password", Description = "Reset another user's password" },
        new() { Module = "user", Resource = "user", Action = "delete", Description = "Delete users" },
        new() { Module = "user", Resource = "role", Action = "create", Description = "Create new roles" },
        new() { Module = "user", Resource = "role", Action = "read", Description = "View role details" },
        new() { Module = "user", Resource = "role", Action = "update", Description = "Update role information" },
        new() { Module = "user", Resource = "role", Action = "delete", Description = "Delete roles" },
        new() { Module = "user", Resource = "permission", Action = "create", Description = "Grant/deny user-level permissions" },
        new() { Module = "user", Resource = "permission", Action = "read", Description = "View permissions" },
        new() { Module = "user", Resource = "permission", Action = "delete", Description = "Remove user-level permission overrides" },
        // Warehouses themselves are SAP-synced shadows and cannot be created or edited here;
        // these govern which warehouses a user is assigned to.
        new() { Module = "user", Resource = "warehouse", Action = "create", Description = "Assign a warehouse to a user" },
        new() { Module = "user", Resource = "warehouse", Action = "read", Description = "View warehouses" },
        new() { Module = "user", Resource = "warehouse", Action = "delete", Description = "Remove a warehouse from a user" },

        // Budget module - split into template (HO) and plan (Warehouse)
        new() { Module = "budget", Resource = "template", Action = "create", Description = "Create budget templates (HO SPV)" },
        new() { Module = "budget", Resource = "template", Action = "read", Description = "View budget templates" },
        new() { Module = "budget", Resource = "template", Action = "update", Description = "Update budget templates" },
        new() { Module = "budget", Resource = "template", Action = "delete", Description = "Delete budget templates" },
        new() { Module = "budget", Resource = "template", Action = "submit", Description = "Submit budget template" },

        // System module - activity type management (SUPER_ADMIN only)
        new() { Module = "system", Resource = "activity_type", Action = "create", Description = "Create activity types" },
        new() { Module = "system", Resource = "activity_type", Action = "update", Description = "Update activity types" },
        new() { Module = "system", Resource = "activity_type", Action = "delete", Description = "Delete activity types" },

        new() { Module = "budget", Resource = "plan", Action = "create", Description = "Create budget plans (Warehouse Admin)" },
        new() { Module = "budget", Resource = "plan", Action = "read", Description = "View budget plans" },
        new() { Module = "budget", Resource = "plan", Action = "update", Description = "Update budget plans" },
        new() { Module = "budget", Resource = "plan", Action = "submit", Description = "Submit budget plan for approval" },
        new() { Module = "budget", Resource = "plan", Action = "approve", Description = "Approve budget plans" },
        new() { Module = "budget", Resource = "plan", Action = "reject", Description = "Reject budget plans" },
        new() { Module = "budget", Resource = "plan", Action = "delete", Description = "Delete budget plans" },

        new() { Module = "budget", Resource = "rate_card", Action = "create", Description = "Manage rate cards" },
        new() { Module = "budget", Resource = "rate_card", Action = "read", Description = "View rate cards" },
        new() { Module = "budget", Resource = "rate_card", Action = "update", Description = "Update draft rate cards" },
        new() { Module = "budget", Resource = "rate_card", Action = "submit", Description = "Submit rate card for approval" },
        new() { Module = "budget", Resource = "rate_card", Action = "delete", Description = "Soft-delete draft rate cards" },
        new() { Module = "budget", Resource = "vendor",    Action = "read",   Description = "View ERP vendor list" },
        new() { Module = "budget", Resource = "item",      Action = "read",   Description = "View ERP item/cost list" },
        new() { Module = "budget", Resource = "uom",       Action = "read",   Description = "View units of measure" },
        new() { Module = "budget", Resource = "uom",       Action = "create", Description = "Create units of measure" },
        new() { Module = "budget", Resource = "uom",       Action = "update", Description = "Update units of measure" },
        new() { Module = "budget", Resource = "uom",       Action = "delete", Description = "Delete units of measure" },

        // SAP-synced master data - read only, WAMS never writes these
        new() { Module = "budget", Resource = "tax_type",  Action = "read",   Description = "View tax types" },

        // Work order module
        new() { Module = "workorder", Resource = "workorder", Action = "read",     Description = "View work orders and their realization data" },
        new() { Module = "workorder", Resource = "workorder", Action = "update",   Description = "Record work order realization - field data entry, fumigation, QC/moisture" },
        new() { Module = "workorder", Resource = "workorder", Action = "delete",   Description = "Delete draft work orders" },
        new() { Module = "workorder", Resource = "workorder", Action = "submit",   Description = "Submit work orders for recap" },
        new() { Module = "workorder", Resource = "workorder", Action = "execute",  Description = "Can be assigned as work order PIC - tick this for field roles (Foreman and equivalents)" },
        new() { Module = "workorder", Resource = "recap", Action = "read",    Description = "View daily work order recap" },
        new() { Module = "workorder", Resource = "recap", Action = "approve", Description = "Verify and approve daily work order recap" },
        new() { Module = "workorder", Resource = "recap", Action = "reject",  Description = "Reject daily work order recap for correction" },

        // Report module
        new() { Module = "report", Resource = "dashboard", Action = "read", Description = "View dashboard KPIs" },
        new() { Module = "report", Resource = "finance-report", Action = "read", Description = "View finance reports" },

        // System module - company management (SUPER_ADMIN only)
        new() { Module = "system", Resource = "company", Action = "create", Description = "Create companies" },
        new() { Module = "system", Resource = "company", Action = "read", Description = "View all companies" },
        new() { Module = "system", Resource = "company", Action = "update", Description = "Update companies" },
        new() { Module = "system", Resource = "company", Action = "delete", Description = "Deactivate companies" },
        new() { Module = "system", Resource = "company", Action = "assign", Description = "Assign users to companies" },
        new() { Module = "system", Resource = "sync", Action = "execute", Description = "Trigger manual master data sync from ERP" },
        new() { Module = "system", Resource = "sync", Action = "read",    Description = "View ERP sync run history and latest status" },

        // Purchase order module
        new() { Module = "budget", Resource = "po", Action = "read",     Description = "View purchase orders" },
        new() { Module = "budget", Resource = "po", Action = "create",   Description = "Create purchase orders" },
        new() { Module = "budget", Resource = "po", Action = "update",   Description = "Update draft purchase orders" },
        new() { Module = "budget", Resource = "po", Action = "delete",   Description = "Delete draft purchase orders" },
        new() { Module = "budget", Resource = "po", Action = "generate", Description = "Generate purchase order to SAP" },

        // Account payable module
        new() { Module = "workorder", Resource = "ap", Action = "read",     Description = "View account payables and approved recaps" },
        new() { Module = "workorder", Resource = "ap", Action = "create",   Description = "Create account payables" },
        new() { Module = "workorder", Resource = "ap", Action = "update",   Description = "Update draft account payables" },
        new() { Module = "workorder", Resource = "ap", Action = "delete",   Description = "Delete draft account payables" },
        new() { Module = "workorder", Resource = "ap", Action = "generate", Description = "Generate account payable to SAP" },

        // Workflow module - approval matrix management
        new() { Module = "workflow", Resource = "template", Action = "read",   Description = "View workflow templates and approval matrix" },
        new() { Module = "workflow", Resource = "template", Action = "create", Description = "Create workflow templates" },
        new() { Module = "workflow", Resource = "template", Action = "update", Description = "Update workflow templates (including activate/deactivate)" },
        new() { Module = "workflow", Resource = "template", Action = "delete", Description = "Hard-delete workflow templates with no active instances" },

        // Wildcard permissions
        new() { Module = "*", Resource = "*", Action = "*", Description = "Full system access (Super Admin)" },
        new() { Module = "*", Resource = "*", Action = "read", Description = "Read-only access to everything (Viewer)" },

        // Audit module
        new() { Module = "audit",     Resource = "log",            Action = "read",   Description = "View audit logs" },

        // Export permissions - one per export endpoint + wildcard
        new() { Module = "*",         Resource = "*",              Action = "export", Description = "Export access to everything" },
        new() { Module = "audit",     Resource = "log",            Action = "export", Description = "Export audit logs" },
        new() { Module = "workorder", Resource = "workorder",      Action = "export", Description = "Export work orders and transport orders" },
        new() { Module = "workorder", Resource = "ap",             Action = "export", Description = "Export account payables" },
        new() { Module = "workorder", Resource = "recap",          Action = "export", Description = "Export recap work orders" },
        new() { Module = "budget",    Resource = "plan",           Action = "export", Description = "Export budget plans and SPK" },
        new() { Module = "budget",    Resource = "template",       Action = "export", Description = "Export budget templates" },
        new() { Module = "budget",    Resource = "rate_card",      Action = "export", Description = "Export rate cards" },
        new() { Module = "budget",    Resource = "vendor",         Action = "export", Description = "Export vendors" },
        new() { Module = "budget",    Resource = "item",           Action = "export", Description = "Export items" },
        new() { Module = "budget",    Resource = "po",             Action = "export", Description = "Export purchase orders" },
        new() { Module = "report",    Resource = "finance-report", Action = "export", Description = "Export finance reports" },
        new() { Module = "rca",       Resource = "report",         Action = "export", Description = "Export RCA reports (SUPER_ADMIN, FINANCE_USER only)" },
        new() { Module = "user",      Resource = "user",           Action = "export", Description = "Export users" },
        new() { Module = "user",      Resource = "role",           Action = "export", Description = "Export roles" },
        new() { Module = "user",      Resource = "warehouse",      Action = "export", Description = "Export warehouses" },
        new() { Module = "system",    Resource = "company",        Action = "export", Description = "Export companies" },

        // Own module so budget.*.* wildcard grants don't hand it out. Only *.*.* (SUPER_ADMIN) holds it.
        new() { Module = "approval", Resource = "self", Action = "approve", Description = "Approve a budget plan you submitted yourself (bypasses segregation of duties)" },
    ];
}

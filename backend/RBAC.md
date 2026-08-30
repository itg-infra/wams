# Role-Based Access Control (RBAC) Documentation

## Executive Summary

This document defines the access control system for the **Warehouse Management System (WAMS)**. It specifies what each role can do, which data it can access, and how permissions are enforced across the application.

---

## Table of Contents

1. [Permission Reference (FE Binding)](#permission-reference-fe-binding)
2. [Permission System Overview](#permission-system-overview)
3. [Role Summary](#role-summary)
4. [Detailed Role Privileges](#detailed-role-privileges)
5. [Feature Access Matrix](#feature-access-matrix)
6. [Warehouse & Province Access Rules](#warehouse--province-access-rules)
7. [Budget Plan Approval Workflow](#budget-plan-approval-workflow)
8. [Permission Resolution](#permission-resolution)
9. [System Roles vs Custom Roles](#system-roles-vs-custom-roles)
10. [Multi-Tenancy (Company Isolation)](#multi-tenancy-company-isolation)
11. [Caching](#caching)

---

## Permission Reference (FE Binding)

Flat list of every permission key defined in code (`src/WAMS.Domain/Constants/Permissions.cs`), grouped by module and then by resource (one table per resource) so it's scannable instead of one giant list. Use this to bind UI show/hide/enable logic to `PermissionMap` from `GET /api/v1/auth/me` (`PermissionMap[module][resource]` → list of actions).

> Note: presence of a key here means it is *defined and can be granted* - it does not guarantee an endpoint enforces it (see flagged exceptions) or that any role is currently seeded with it.

**"Roles with access" methodology:** derived directly from `DatabaseSeeder.AssignRolePermissionsAsync()`, not from prose elsewhere in this doc. Three seeder helpers matter for reading this correctly:
- `AssignPermissionToRoleAsync` - grants one exact key.
- `AssignWildcardPermissionsAsync(role, module)` - grants **every** key (any resource, any action) starting with `{module}.` - e.g. `budget.*.*` sweeps in `tax_type` and `uom` too, not just `plan`/`template`/`po`.
- `AssignModuleReadPermissionsAsync` / `AssignModuleExportPermissionsAsync(role, module)` - grants **every** key in that module ending in `.read` / `.export`, across *all* resources in the module, not just the "primary" one. This means e.g. WAREHOUSE_HEAD's "workorder module read" grant also includes `workorder.ap.read` and `workorder.recap.read`, and HO_SPV's "user module read" grant also includes `user.role.read` and `user.permission.read` - these sweeps are easy to undercount if you only skim the seeder comments.

`SUPER_ADMIN` holds `*.*.*` and therefore has access to **every** permission below - it is omitted from each row to cut repetition.

### Budget

#### `budget.plan.*`

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `budget.plan.read` | View budget plans | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_HEAD, COORDINATOR_WH, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_MGR, WH_OPS, FINANCE_USER, FAT_MGR, FAT_OPS, VIEWER |
| `budget.plan.create` | Show "New Budget Plan" | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_OPS |
| `budget.plan.update` | Show "Edit" on a budget plan | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_OPS |
| `budget.plan.delete` | Show "Delete" on a budget plan | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_OPS |
| `budget.plan.submit` | Show "Submit for Approval" | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_OPS |
| `budget.plan.approve` | Show "Approve" (current workflow stage) | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_HEAD (Stage 1 default), COORDINATOR_WH (Stage 2 default), WH_MGR |
| `budget.plan.reject` | Show "Reject" | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_HEAD, COORDINATOR_WH, WH_MGR |
| `budget.plan.export` | Show "Export" on budget plan list/detail | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_HEAD, COORDINATOR_WH, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_MGR, WH_OPS, FINANCE_USER, FAT_MGR, FAT_OPS, VIEWER |

#### `budget.template.*`

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `budget.template.read` | View budget templates | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_HEAD, COORDINATOR_WH, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_MGR, WH_OPS, FINANCE_USER, FAT_MGR, FAT_OPS, VIEWER |
| `budget.template.create` | Show "New Template" | HO_SPV/LOG_MGR/LOG_SPV only |
| `budget.template.update` | Show "Edit" on a template | HO_SPV/LOG_MGR/LOG_SPV only |
| `budget.template.delete` | Show "Delete" on a template | HO_SPV/LOG_MGR/LOG_SPV only |
| `budget.template.submit` | Show "Submit/Activate Template" | HO_SPV/LOG_MGR/LOG_SPV only |
| `budget.template.export` | Show "Export" on template list | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_HEAD, COORDINATOR_WH, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_MGR, WH_OPS, FINANCE_USER, FAT_MGR, FAT_OPS, VIEWER |

#### `budget.revision.*`

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `budget.revision.read` | View budget revisions | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_OPS, VIEWER |
| `budget.revision.create` | Show "New Revision" | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_OPS |
| `budget.revision.approve` | Show "Approve Revision" | HO_SPV/LOG_MGR/LOG_SPV only |
| `budget.revision.reject` | Show "Reject Revision" | HO_SPV/LOG_MGR/LOG_SPV only |

#### `budget.po.*`

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `budget.po.read` | View purchase orders | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_HEAD, COORDINATOR_WH, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_MGR, WH_OPS, VIEWER |
| `budget.po.create` | Show "New PO" | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_OPS |
| `budget.po.update` | Show "Edit" on a PO | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_OPS |
| `budget.po.delete` | Show "Delete" on a PO | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_OPS |
| `budget.po.generate` | Show "Generate PO" (from plan/template) | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_OPS |
| `budget.po.export` | Show "Export" on PO list | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_HEAD, COORDINATOR_WH, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_MGR, WH_OPS, VIEWER |

#### `budget.rate_card.*`

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `budget.rate_card.read` | View rate cards | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_HEAD, COORDINATOR_WH, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_MGR, WH_OPS, FINANCE_USER, FAT_MGR, FAT_OPS, VIEWER |
| `budget.rate_card.create` | Show "New Rate Card" | HO_SPV/LOG_MGR/LOG_SPV only |
| `budget.rate_card.update` | Show "Edit" on a rate card | HO_SPV/LOG_MGR/LOG_SPV only |
| `budget.rate_card.delete` | Show "Delete" on a rate card | HO_SPV/LOG_MGR/LOG_SPV only |
| `budget.rate_card.submit` | Show "Submit Rate Card" | HO_SPV/LOG_MGR/LOG_SPV only |
| `budget.rate_card.export` | Show "Export" on rate card list | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_HEAD, COORDINATOR_WH, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_MGR, WH_OPS, FINANCE_USER, FAT_MGR, FAT_OPS, VIEWER |

#### `budget.uom.*`

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `budget.uom.read` | View units of measure | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_HEAD, COORDINATOR_WH, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_MGR, WH_OPS, VIEWER |
| `budget.uom.create` | Show "New UoM" | HO_SPV/LOG_MGR/LOG_SPV only (via `budget.*.*` - no dedicated UoM-admin role) |
| `budget.uom.update` | Show "Edit" on a UoM | HO_SPV/LOG_MGR/LOG_SPV only |
| `budget.uom.delete` | Show "Delete" on a UoM | HO_SPV/LOG_MGR/LOG_SPV only |

#### `budget.tax_type.*`

Read-only: tax types are SAP-synced master data, so there is no create/update/delete key. Don't
bind "New Tax Type" / "Edit" / "Delete" controls to anything here.

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `budget.tax_type.read` | View tax types | HO_SPV/LOG_MGR/LOG_SPV, FAT_MGR, FAT_OPS, VIEWER |

#### `budget.vendor.*` / `budget.item.*`

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `budget.vendor.read` | View vendors | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_HEAD, COORDINATOR_WH, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_MGR, WH_OPS, FINANCE_USER, FAT_MGR, FAT_OPS, VIEWER |
| `budget.vendor.export` | Show "Export" on vendor list | same as above |
| `budget.item.read` | View items | same as above |
| `budget.item.export` | Show "Export" on item list | same as above |

### Work Order

#### `workorder.workorder.*`

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `workorder.workorder.read` | View work orders and their realization data | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_HEAD, COORDINATOR_WH, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_MGR, WH_OPS, FOREMAN, VIEWER |
| `workorder.workorder.update` | Show "Edit" / field data entry on a work order - this is what lets a Foreman record realization, fumigation and QC/moisture | WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_OPS, FOREMAN |
| `workorder.workorder.delete` | Show "Delete" on a work order | WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_OPS, FOREMAN |
| `workorder.workorder.submit` | Show "Submit" on a work order | WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_OPS, FOREMAN |
| `workorder.workorder.execute` | **Eligibility, not a button.** Members of a role holding this appear in the work order PIC picker. Grant it to any field role - this is how you add a second Foreman-type role without a code change | FOREMAN |
| `workorder.workorder.export` | Show "Export" on work order list | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_HEAD, COORDINATOR_WH, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_MGR, WH_OPS, FOREMAN, VIEWER |

#### `workorder.ap.*`

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `workorder.ap.read` | View account payables | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_HEAD, COORDINATOR_WH, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_MGR, WH_OPS, VIEWER |
| `workorder.ap.create` | Show "New AP Entry" | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_OPS |
| `workorder.ap.update` | Show "Edit" on an AP entry | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_OPS |
| `workorder.ap.delete` | Show "Delete" on an AP entry | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_OPS |
| `workorder.ap.generate` | Show "Generate AP" | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_OPS |
| `workorder.ap.export` | Show "Export" on AP list | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_HEAD, COORDINATOR_WH, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_MGR, WH_OPS, VIEWER |

#### `workorder.recap.*`

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `workorder.recap.read` | View work order recap | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_HEAD, COORDINATOR_WH, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_MGR, WH_OPS, VIEWER |
| `workorder.recap.approve` | Show "Approve Recap" | HO_SPV/LOG_MGR/LOG_SPV only |
| `workorder.recap.reject` | Show "Reject Recap" | HO_SPV/LOG_MGR/LOG_SPV only |
| `workorder.recap.export` | Show "Export" on recap list | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_HEAD, COORDINATOR_WH, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_MGR, WH_OPS, VIEWER |

### System

#### `system.company.*`

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `system.company.read` | View companies | IT_OPS, VIEWER |
| `system.company.create` | Show "New Company" | IT_OPS |
| `system.company.update` | Show "Edit" on a company | IT_OPS |
| `system.company.delete` | Show "Deactivate Company" | IT_OPS |
| `system.company.export` | Show "Export" on company list | IT_OPS, VIEWER |
| `system.company.assign` | Show "Move User to Company" | IT_OPS |

#### `system.sync.*`

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `system.sync.read` | View ERP sync status/history | IT_OPS, VIEWER |
| `system.sync.execute` | Show "Trigger Sync" | IT_OPS |

#### `system.activity_type.*`

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `system.activity_type.create` | Show "New Activity Type" | IT_OPS |
| `system.activity_type.update` | Show "Edit" on an activity type | IT_OPS |
| `system.activity_type.delete` | Show "Delete" on an activity type | IT_OPS |

Note: there's no `system.activity_type.read` key defined - no endpoint gates viewing activity types on a dedicated read permission.

### User

#### `user.user.*`

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `user.user.read` | View users | IT_OPS, HO_SPV/LOG_MGR/LOG_SPV, VIEWER |
| `user.user.create` | Show "New User" | IT_OPS only |
| `user.user.update` | Show "Edit" on a user | IT_OPS only |
| `user.user.reset_password` | Show "Reset Password" | IT_OPS only |
| `user.user.delete` | Show "Delete User" | IT_OPS only |
| `user.user.export` | Show "Export" on user list | IT_OPS, HO_SPV/LOG_MGR/LOG_SPV, VIEWER |

#### `user.role.*`

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `user.role.read` | View roles | IT_OPS, HO_SPV/LOG_MGR/LOG_SPV, VIEWER |
| `user.role.create` | Show "New Role"; also gates the "Assign Role to User" action | IT_OPS only |
| `user.role.update` | Show "Edit" on a role | IT_OPS only |
| `user.role.delete` | Show "Delete Role"; also gates "Remove Role from User" | IT_OPS only |
| `user.role.export` | Show "Export" on role list | IT_OPS, HO_SPV/LOG_MGR/LOG_SPV, VIEWER |

#### `user.warehouse.*`

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `user.warehouse.read` | View warehouses | IT_OPS, HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_HEAD, COORDINATOR_WH, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_MGR, WH_OPS, VIEWER |
| `user.warehouse.create` | Show "Assign Warehouse to User" | IT_OPS only |
| `user.warehouse.delete` | Show "Remove Warehouse from User" | IT_OPS only |
| `user.warehouse.export` | Show "Export" on warehouse list | IT_OPS, HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_HEAD, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_OPS, VIEWER (**not** COORDINATOR_WH/WH_MGR - no explicit export grant) |

#### `user.permission.*`

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `user.permission.read` | View a user's permission overrides | IT_OPS, HO_SPV/LOG_MGR/LOG_SPV, VIEWER |
| `user.permission.create` | Show "Grant/Deny Permission" on user detail | IT_OPS only |
| `user.permission.delete` | Show "Remove Permission Override" | IT_OPS only |

### Report

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `report.dashboard.read` | Show Dashboard nav item / page | HO_SPV/LOG_MGR/LOG_SPV, FINANCE_USER, FAT_MGR, FAT_OPS, IT_OPS, VIEWER |
| `report.finance-report.read` | View finance report | HO_SPV/LOG_MGR/LOG_SPV, FINANCE_USER, FAT_MGR, FAT_OPS, IT_OPS, VIEWER |
| `report.finance-report.export` | Show "Export" on finance report | HO_SPV/LOG_MGR/LOG_SPV, FINANCE_USER, FAT_MGR, FAT_OPS, IT_OPS, VIEWER |

### Approval

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `approval.self.approve` | **Not a button.** Allows approving a budget plan you submitted yourself, bypassing segregation of duties. Deliberately its own module so `budget.*.*` grants (HO_SPV, LOG_MGR, LOG_SPV) do **not** confer it | SUPER_ADMIN (via `*.*.*`) |

### RCA

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `rca.report.export` | Show "Export RCA Report" (deliberately separate from `report.*.*` so a broad `report.*.*` grant doesn't imply RCA export) | FINANCE_USER, FAT_MGR, FAT_OPS |

### Audit

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `audit.log.read` | Show Audit Log nav item / page | IT_OPS, VIEWER |
| `audit.log.export` | Show "Export" on audit log | VIEWER |

### Workflow

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `workflow.template.read` | View workflow templates | HO_SPV/LOG_MGR/LOG_SPV, WAREHOUSE_HEAD, COORDINATOR_WH, WAREHOUSE_ADMIN, LOG_MKT_OPS, WH_MGR, WH_OPS, VIEWER |
| `workflow.template.create` | Show "New Workflow Template" | HO_SPV/LOG_MGR/LOG_SPV only |
| `workflow.template.update` | Show "Edit" on a workflow template | HO_SPV/LOG_MGR/LOG_SPV only |
| `workflow.template.delete` | Show "Delete Workflow Template" | HO_SPV/LOG_MGR/LOG_SPV only |

### Quality

| Permission Key | UI Hint | Roles with access |
|---|---|---|

### Attachments

No permission keys. File attachments hang off a work order and inherit its access rules:
show "Upload Document/Photo" when the user can edit the work order (`workorder.workorder.update`),
and show the attachment list whenever they can read it (`workorder.workorder.read`). The server
enforces the same warehouse check on both paths.

### Wildcards

| Permission Key | UI Hint | Roles with access |
|---|---|---|
| `*.*.*` | God-mode - if present, show everything | SUPER_ADMIN only |
| `*.*.read` | If present without specific create/update/delete keys, render the app in read-only mode | VIEWER, IT_OPS |
| `*.*.export` | If present, show export buttons everywhere read access exists | VIEWER |

---

## Permission System Overview

### Permission Format

All permissions follow a structured format:

```
{Module}.{Resource}.{Action}
```

**Example:** `budget.plan.approve` means "approve a budget plan"

| Component | Description | Examples |
|-----------|-------------|----------|
| **Module** | Business domain | `user`, `budget`, `workorder`, `advance`, `system`, `report`, `rca`, `audit`, `workflow`, `quality`, `document` |
| **Resource** | Specific entity | `user`, `role`, `plan`, `template`, `po`, `rate_card`, `uom`, `tax_type`, `ap`, `recap` |
| **Action** | Operation allowed | `create`, `read`, `update`, `delete`, `approve`, `reject`, `submit`, `export`, `generate`, `close`, `execute` |

Where module equals resource (e.g. `user.user.*`, `workorder.workorder.*`), the redundant resource segment is still present in the key but source-code constant names drop it (`Permissions.User.Read` = `"user.user.read"`).

### Wildcard Permissions

| Wildcard | Meaning | Example |
|----------|---------|---------|
| `*.*.*` | **Full access** to everything (Super Admin only) | |
| `*.*.read` | **Read-only** access to everything (Viewer) | |
| `*.*.export` | **Export** access to everything (Viewer) | |
| `budget.*.*` | Full access to **all budget resources** | |
| `*.plan.read` | Can **read plans** across all modules | |

Every resource that supports mutation now also has a `.export` action (e.g. `budget.plan.export`, `workorder.workorder.export`), mirroring the older `.read` action. `*.*.export` is the export analogue of `*.*.read`.

`rca.report.export` is deliberately kept in its own module (not `report`) so a broad `report.*.*` grant (e.g. HO_SPV) does **not** implicitly unlock the RCA export - only SUPER_ADMIN and the finance-family roles (FINANCE_USER, FAT_MGR, FAT_OPS) get it explicitly.

Source of truth for every permission key: [`src/WAMS.Domain/Constants/Permissions.cs`](src/WAMS.Domain/Constants/Permissions.cs).

---

## Role Summary

There are now **16 seeded roles** - the original 8 plus 8 added later for a client-specific org chart (`RoleCodes.cs`, seeder comment: *"Client matrix (resources/user-matrix-from-client.png)"*). Most of the added roles are near-duplicates of an existing role under a different display name.

| Role | Type | Global Access | Primary Purpose |
|------|------|---------------|-----------------|
| **SUPER_ADMIN** | System | ✅ Yes | System-wide administration, `*.*.*` |
| **HO_SPV** | System | ✅ Yes | Head Office management |
| **WAREHOUSE_HEAD** | Standard | ❌ No | Workflow Stage 1 approval & warehouse supervision |
| **COORDINATOR_WH** | Standard | ❌ No | Workflow Stage 2 approval & warehouse coordination |
| **WAREHOUSE_ADMIN** | Standard | ❌ No | Daily warehouse operations |
| **FINANCE_USER** | Standard | ✅ Yes | Financial operations & reporting |
| **FOREMAN** | Standard | ❌ No | Field data entry |
| **VIEWER** | System | ✅ Yes | Read-only + export access |
| **IT_OPS** | Standard | ✅ Yes | User/system administration (not full super-admin) |
| **LOG_MGR** | Standard | ✅ Yes | Logistics manager - same grant shape as HO_SPV |
| **LOG_SPV** | Standard | ✅ Yes | Logistics supervisor - same grant shape as HO_SPV |
| **LOG_MKT_OPS** | Standard | ❌ No | Logistics market ops - same grant shape as WAREHOUSE_ADMIN |
| **WH_MGR** | Standard | ❌ No | Warehouse manager, **province-scoped** - same grant shape as COORDINATOR_WH |
| **WH_OPS** | Standard | ❌ No | Warehouse ops, **province-scoped** - same grant shape as WAREHOUSE_ADMIN |
| **FAT_MGR** | Standard | ✅ Yes | Finance/Accounting/Tax manager - FINANCE_USER + full tax type CRUD |
| **FAT_OPS** | Standard | ✅ Yes | Finance/Accounting/Tax ops - FINANCE_USER + tax type read |

Source of truth: [`src/WAMS.Infrastructure/Data/DatabaseSeeder.cs`](src/WAMS.Infrastructure/Data/DatabaseSeeder.cs) `SeedRolesAsync()` / `AssignRolePermissionsAsync()`, and [`src/WAMS.Domain/Constants/RoleCodes.cs`](src/WAMS.Domain/Constants/RoleCodes.cs).

### Global Access Explained

- **Global Access = YES**: role bypasses per-warehouse assignment checks and can see/manage all warehouses in the company (`Role.GlobalAccess = true`, or the user holds a role with the `*.*.*` permission key - `RbacService.HasGlobalAccessAsync` treats either as global).
- **Global Access = NO**: user can only access warehouses they are explicitly assigned to (`user_warehouses` table), and - for **WH_MGR / WH_OPS** specifically - is additionally scoped by assigned province(s).

---

## Detailed Role Privileges

### 🔴 SUPER_ADMIN
`*.*.*` only. Unlimited access to every module, every company. Cannot be deleted or modified (`IsSystem = true`).

### 🟠 HO_SPV (Head Office Admin)
`IsSystem = true`, `GlobalAccess = true`.

| Grant |
|---|
| `budget.*.*` (full budget module - templates, plans, revisions, POs, rate cards, UoM, tax types, vendors, items) |
| `report.*.*` |
| `user.*.read` + `user.*.export` (read/export only - no user/role mutation) |
| `workorder.*.read` + `workorder.*.export` |
| `workorder.recap.approve` / `.reject` |
| `workorder.ap.{create,update,delete,generate}` |
| `workflow.*.*` (full Workflow Template management) |

### 🟡 WAREHOUSE_HEAD
`GlobalAccess = false`.

| Grant |
|---|
| `user.warehouse.read` (+ `.export`) |
| `budget.plan.{read,approve,reject}` (+ `.export`) - Workflow **Stage 1** approver by default template config |
| `budget.template.{read,export}` |
| `budget.{vendor,item,uom,rate_card}.read` (+ export) |
| `budget.po.read` (+ `.export`, via workorder export mirror) |
| `workorder.*.read` (+ `.export`) |
| `workflow.template.read` |

> **Open question - recap verification.** This role was intended to verify work order
> realizations and was granted `workorder.realization.verify`, which no code ever checked. That
> key has been removed. The role does **not** hold `workorder.recap.{approve,reject}`, so it
> cannot approve recaps today and never could - only HO_SPV, LOG_MGR, LOG_SPV and SUPER_ADMIN
> can. Confirm with the client whether this role should gain real approval rights.

### 🟡 COORDINATOR_WH
`GlobalAccess = false`.

| Grant |
|---|
| `user.warehouse.read` |
| `budget.plan.{read,approve,reject,export}` - Workflow **Stage 2** approver by default template config |
| `budget.template.{read,export}` |
| `budget.{vendor,item,uom,rate_card,po}.read` (+ export where applicable) |
| `workorder.*.read` (+ `.export`) |
| `workflow.template.read` |

> **Open question - recap verification.** This role was intended to verify work order
> realizations and was granted `workorder.realization.verify`, which no code ever checked. That
> key has been removed. The role does **not** hold `workorder.recap.{approve,reject}`, so it
> cannot approve recaps today and never could - only HO_SPV, LOG_MGR, LOG_SPV and SUPER_ADMIN
> can. Confirm with the client whether this role should gain real approval rights.

### 🟢 WAREHOUSE_ADMIN
`GlobalAccess = false`.

| Grant |
|---|
| `user.warehouse.read` (+ `.export`) |
| `budget.plan.{create,read,update,submit,delete,export}` |
| `budget.template.read`, `budget.{uom,rate_card,item,vendor}.read` (+ export) - needed to populate dropdowns when authoring a Budget Plan |
| `budget.revision.{create,read}` |
| `budget.po.{read,create,update,delete,generate,export}` (full PO management) |
| `workorder.*.*` (full work-order module, including `.export`) |
| `workflow.template.read` |

### 🔵 FINANCE_USER
`GlobalAccess = true`.

| Grant |
|---|
| `report.dashboard.read`, `report.finance-report.{read,export}` |
| `rca.report.export` |
| `budget.{plan,template,vendor,item,rate_card}.read` (+ export) |

### 🟣 FOREMAN
`GlobalAccess = false`.

| Grant |
|---|
| `workorder.workorder.{read,update,delete,submit,export}` - `update` is the permission that actually allows field data entry (realization, fumigation, QC/moisture); revoking it takes away the Foreman's core job |

### ⚪ VIEWER
`IsSystem = true`, `GlobalAccess = true`. `*.*.read` + `*.*.export` - read and export everywhere, cannot create/update/delete/approve/submit anything.

### IT_OPS
`GlobalAccess = true`. `user.*.*` + `system.*.*` (full user & system administration) plus `*.*.read` (read visibility everywhere else). Deliberately **not** `*.*.*` - comment in seeder: *"NOT full admin - `*.*.*` is reserved for SUPER_ADMIN only."*

### LOG_MGR / LOG_SPV
`GlobalAccess = true`. Identical grant shape to HO_SPV (`budget.*.*`, `report.*.*`, `user.*.read/.export`, `workorder.*.read/.export`, recap approve/reject, AP create/update/delete/generate, `workflow.*.*`).

### LOG_MKT_OPS
`GlobalAccess = false`. Identical grant shape to WAREHOUSE_ADMIN (budget plan CRUD+submit+export, workorder/quality/document wildcards, PO full management, budget revision create/read, `workflow.template.read`).

### WH_MGR
`GlobalAccess = false`, **province-scoped**. Identical grant shape to COORDINATOR_WH, plus `budget.po.read`.

### WH_OPS
`GlobalAccess = false`, **province-scoped**. Identical grant shape to WAREHOUSE_ADMIN.

### FAT_MGR
`GlobalAccess = true`. Identical base grant to FINANCE_USER, plus `budget.tax_type.read`.

### FAT_OPS
`GlobalAccess = true`. Identical base grant to FINANCE_USER, plus `budget.tax_type.read`. (Same grant as FAT_MGR since tax types became read-only.)

**Note on Attachments / Quality / Cash Advance:**
- **The `document` module no longer exists.** A permission meaning "may upload files" cannot say *which* files, so it could never be the real control and was never checked. It was removed in migration `PruneOrphanedPermissions`.
- **Attachment access is inherited from the parent work order.** `FilesController` carries no `[RequirePermission]`; `WorkOrderFileAttachmentEntityHandler` enforces the same warehouse-access rule as `WorkOrderService`, so if you can see the work order you can see its files, and if you can't, you get a 403. Work orders are the only entity that accepts attachments.
- **The `quality` module no longer exists.** Fumigation and moisture were predicted as their own module but shipped as detail blocks on the work order (`WorkOrderFumigationDetail`, `WorkOrderQcDetail.MoisturePercent`), so they are governed by `workorder.workorder.update`. The `quality.*` keys were removed in migration `PruneOrphanedPermissions`.
- **Cash Advance does not exist at all.** `advance.advance.read` was dropped from the code in commit `68bbf44`; there is no constant, no seed row, no controller, service, or entity. Earlier revisions of this document described it as "seeded" - that was already wrong.

---

## Feature Access Matrix

Built directly from `[RequirePermission(...)]` attributes on controllers under `src/WAMS.Api/Controllers/`. ✅ = role holds the exact or wildcard-matching permission per the seeder; this does not account for user-level grant/deny overrides, which can change the effective result per-user.

### User & Access Management

| Feature | Permission | SUPER_ADMIN | HO_SPV / LOG_MGR / LOG_SPV | IT_OPS | VIEWER | Others |
|---------|---|:---:|:---:|:---:|:---:|:---:|
| Create Users | `user.user.create` | ✅ | ❌ | ✅ | ❌ | ❌ |
| View Users | `user.user.read` | ✅ | ✅ | ✅ | ✅ | ❌ |
| Update Users | `user.user.update` | ✅ | ❌ | ✅ | ❌ | ❌ |
| Delete Users | `user.user.delete` | ✅ | ❌ | ✅ | ❌ | ❌ |
| Reset Password | `user.user.reset_password` | ✅ | ❌ | ✅ | ❌ | ❌ |
| Export Users | `user.user.export` | ✅ | ✅ | ✅ | ✅ | ❌ |
| Create/Delete Roles | `user.role.{create,delete}` | ✅ | ❌ | ✅ | ❌ | ❌ |
| View Roles | `user.role.read` | ✅ | ❌* | ✅ | ✅ | ❌ |
| Assign/Remove User Role | `user.role.{create,delete}` (used on the assign-role endpoint too) | ✅ | ❌ | ✅ | ❌ | ❌ |
| View/Grant/Revoke User Permission Overrides | `user.permission.{read,create,delete}` | ✅ | ❌ | ✅ | ✅ (read only) | ❌ |

*\* HO_SPV/LOG_MGR/LOG_SPV get `user.*.read` which covers `user.user.read` and `user.warehouse.read` but **not** `user.role.read` or `user.permission.read` (those are separate resources not swept by a module-read helper the same way) - check current grants in the seeder before assuming.*

### Warehouse Management

Warehouses are **ERP-synced shadow entities** (`WarehouseShadowService`) - there is no create/update/delete endpoint for warehouses at all. `WarehousesController` only exposes read/export/locations/unmapped-list.

| Feature | Permission | Roles with access |
|---|---|---|
| View Warehouses | `user.warehouse.read` | SUPER_ADMIN, HO_SPV family, WAREHOUSE_HEAD, COORDINATOR_WH, WAREHOUSE_ADMIN, FOREMAN (via warehouse-scoped queries), WH_MGR, WH_OPS, LOG_MKT_OPS, VIEWER |
| Export Warehouses | `user.warehouse.export` | SUPER_ADMIN, WAREHOUSE_HEAD, WAREHOUSE_ADMIN, WH_MGR/WH_OPS, LOG_MKT_OPS, VIEWER |
| Create/Update/Delete Warehouses | *(no endpoint exists)* | N/A |

### Budget Management

| Feature | Permission | SUPER_ADMIN | HO_SPV family | WAREHOUSE_HEAD | COORDINATOR_WH / WH_MGR | WAREHOUSE_ADMIN / WH_OPS / LOG_MKT_OPS | FINANCE_USER / FAT_MGR / FAT_OPS | FOREMAN | VIEWER |
|---|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Create/Update/Delete/Submit Templates | `budget.template.{create,update,delete,submit}` | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| View/Export Templates | `budget.template.{read,export}` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ |
| Create Budget Plans | `budget.plan.create` | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| View/Export Plans | `budget.plan.{read,export}` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ |
| Update/Submit/Delete Plans | `budget.plan.{update,submit,delete}` | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| Approve/Reject Plans | `budget.plan.{approve,reject}` | ✅ | ❌ (unless assigned a stage's approver role) | ✅ (Stage 1) | ✅ (Stage 2) | ❌ | ❌ | ❌ | ❌ |
| Create/Read Budget Revisions | `budget.revision.{create,read}` | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| Approve/Reject Revisions | `budget.revision.{approve,reject}` | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| PO create/update/delete/generate | `budget.po.{create,update,delete,generate}` | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| View/Export POs | `budget.po.{read,export}` | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ |
| Rate Cards CRUD/Submit | `budget.rate_card.{create,update,delete,submit}` | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| View/Export Rate Cards | `budget.rate_card.{read,export}` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ |
| UoM CRUD | `budget.uom.{create,update,delete}` | ✅ | ❌** | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Tax Type Read | `budget.tax_type.read` | ✅ | ❌ | ❌ | ❌ | ❌ | FAT_MGR, FAT_OPS | ❌ | ✅ |
| View/Export Vendors, Items | `budget.{vendor,item}.{read,export}` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ |

*\*\* `budget.*.*` wildcard held by HO_SPV/LOG_MGR/LOG_SPV does technically cover `budget.uom.*` too - the table above lists explicit non-wildcard grants for other roles; HO_SPV family effectively has full UoM/tax-type CRUD via its `budget.*.*` wildcard except where a more specific deny would apply.*

Budget Templates can also be **province-scoped** (`BudgetTemplate.ProvinceId`); a user only sees templates matching their own assigned provinces, unless they hold a role that bypasses this filter - see [Warehouse & Province Access Rules](#warehouse--province-access-rules).

### Work Order Management

Work Orders have **no manual create endpoint** - they are system-generated in bulk (`woService.BulkCreateDraftAsync`) when a Budget Plan clears its final approval stage. `WorkOrdersController` only supports Read/Update/Delete/Submit/Close/Export/History.

| Feature | Permission | SUPER_ADMIN | HO_SPV family | WAREHOUSE_HEAD / COORDINATOR_WH / WH_MGR | WAREHOUSE_ADMIN / WH_OPS / LOG_MKT_OPS | FINANCE_USER | FOREMAN | VIEWER |
|---|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| View/Export Work Orders | `workorder.workorder.{read,export}` | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |
| Update/Delete/Submit | `workorder.workorder.{update,delete,submit}` | ✅ | ❌ | ❌ | ✅ | ❌ | ✅ | ❌ |
| Recap: view/export | `workorder.recap.{read,export}` | ✅ | ✅ | ✅ (via module-read/export) | ✅ (via wildcard) | ❌ | ❌ | ✅ |
| Recap: approve/reject | `workorder.recap.{approve,reject}` | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Account Payables: view/export | `workorder.ap.{read,export}` | ✅ | ✅ (read via module wildcard; export explicit) | ❌ | ✅ (via wildcard) | ❌ | ❌ | ✅ |
| Account Payables: create/update/delete/generate | `workorder.ap.{create,update,delete,generate}` | ✅ | ✅ (explicit) | ❌ | ✅ (via wildcard) | ❌ | ❌ | ❌ |
| Transport Orders (read-only) | `workorder.workorder.{read,export}` | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |
| SPK (read-only, reuses budget-plan permissions) | `budget.plan.{read,export}` | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ |

### Cash Advance Management

**Not implemented, and not even scaffolded.** The `advance.advance.read` permission was removed from the code in commit `68bbf44`; no permission, controller, service, or entity exists for cash advances. Do not build UI or workflows against this section until the backend feature ships.

### Quality Control

Quality data is **not a separate module**. Fumigation schedules and moisture/QC readings are
detail blocks on the work order itself (`WorkOrderFumigationDetail`, `WorkOrderQcDetail`), so they
are governed entirely by the work order permissions:

| Feature | Permission | SUPER_ADMIN | WAREHOUSE_ADMIN / WH_OPS / LOG_MKT_OPS | FOREMAN | VIEWER |
|---|---|:---:|:---:|:---:|:---:|
| Record fumigation detail | `workorder.workorder.update` | ✅ | ✅ | ✅ | ❌ |
| Record moisture / QC detail | `workorder.workorder.update` | ✅ | ✅ | ✅ | ❌ |
| View fumigation / moisture | `workorder.workorder.read` | ✅ | ✅ | ✅ | ✅ |

The former `quality.fumigation.*` and `quality.moisture.*` keys were removed in migration
`PruneOrphanedPermissions` - they were never checked by any code path.

Enforcement caveat: the real upload/download endpoints in `FilesController` only require `[Authorize]`, not a specific permission - the RBAC checks above describe the seeded intent, not what the endpoint actually enforces today.

### Reporting

| Feature | Permission | SUPER_ADMIN | HO_SPV family | WAREHOUSE_ADMIN / WH_OPS / LOG_MKT_OPS | FINANCE_USER / FAT_MGR / FAT_OPS | VIEWER |
|---|---|:---:|:---:|:---:|:---:|:---:|
| View Dashboard | `report.dashboard.read` | ✅ | ❌ (not explicitly granted) | ❌ | ✅ | ✅ |
| View Report / Finance Report | `report.{report,finance-report}.read` | ✅ | ✅ | ❌ | ✅ | ✅ |
| Export Finance Report | `report.finance-report.export` | ✅ | ❌ | ❌ | ✅ | ✅ |
| Export RCA Report | `rca.report.export` | ✅ | ❌ | ❌ | ✅ | ❌ (not part of `*.*.export`, separate module) |

### Workflow Templates *(new module)*

| Feature | Permission | SUPER_ADMIN | HO_SPV / LOG_MGR / LOG_SPV | WAREHOUSE_HEAD / COORDINATOR_WH / WAREHOUSE_ADMIN / WH_MGR / WH_OPS / LOG_MKT_OPS | Others |
|---|---|:---:|:---:|:---:|:---:|
| Create/Update/Delete Templates | `workflow.template.{create,update,delete}` | ✅ | ✅ | ❌ | ❌ |
| View Templates | `workflow.template.read` | ✅ | ✅ | ✅ | ❌ |

Governs the configurable multi-stage approval pipeline used by Budget Plans - see [Budget Plan Approval Workflow](#budget-plan-approval-workflow).

### Audit Logs *(new module)*

| Feature | Permission | Roles with access |
|---|---|---|
| View Audit Log | `audit.log.read` | Whoever is granted it explicitly - not part of any role's seeded grant above; check current role-permission assignments before relying on this |
| Export Audit Log | `audit.log.export` | Same as above |

### Sync *(new module - ERP integration)*

| Feature | Permission |
|---|---|
| Trigger Sync | `system.sync.execute` |
| View Sync Status/History | `system.sync.read` |

### Company Management (Super Admin Only)

| Feature | Permission | SUPER_ADMIN | Others |
|---|---|:---:|:---:|
| Create/Update/Deactivate Companies | `system.company.{create,update,delete}` | ✅ | ❌ |
| View All Companies | `system.company.read` | ✅ | ❌ |
| Export Companies | `system.company.export` | ✅ | ❌ |
| Move Users Between Companies | `system.company.assign` | ✅ | ❌ |

---

## Warehouse & Province Access Rules

### Three-Layer Access Control

WAMS now has **three** layers of access control, not two:

1. **RBAC Permissions** - what actions a user can perform
2. **Warehouse Scope** - which warehouses a user can access (`user_warehouses` + `Role.GlobalAccess`)
3. **Province Scope** - which provinces a user can see budget templates/plans for (`user_provinces`), relevant mainly to **WH_MGR** and **WH_OPS**, whose descriptions in the seeder explicitly call out "province-scoped"

### Global Access vs Assigned Access

| Role Type | GlobalAccess Flag | Warehouse Access |
|-----------|-------------------|------------------|
| **Global Roles** | `true` | Can access **all warehouses** in their company |
| **Scoped Roles** | `false` | Can only access **assigned warehouses** (`user_warehouses`) |

### Global Access Roles
SUPER_ADMIN (all companies), HO_SPV, FINANCE_USER, VIEWER, IT_OPS, LOG_MGR, LOG_SPV, FAT_MGR, FAT_OPS (all warehouses in their company).

### Scoped Roles (Assigned Warehouses Required)
WAREHOUSE_HEAD, COORDINATOR_WH, WAREHOUSE_ADMIN, FOREMAN, LOG_MKT_OPS, WH_MGR, WH_OPS.

### Warehouse Resolution Middleware (`WarehouseMiddleware`)

Behavior changed from the original design and now applies uniformly regardless of role:

1. If the request carries an `X-Warehouse-Id` header with a valid long value, that warehouse is set as the active context **for every authenticated user, including super-admins** (`WarehouseMiddleware.cs:19-23`).
2. Only if the header is **absent or unparseable**, and the caller's JWT carries a `permissions` claim containing `*.*.*`, does the middleware fall back to "bypass mode" (no warehouse filter - see all).
3. If the header is absent and the caller is not a wildcard user, no warehouse filter is set at all (`IsSet` stays false) - downstream services must apply their own default scoping (typically the user's assigned warehouses).

This means even a SUPER_ADMIN can be scoped to a single warehouse for a request simply by sending the header - the header always wins over role.

Source: [`src/WAMS.Api/Middleware/WarehouseMiddleware.cs`](src/WAMS.Api/Middleware/WarehouseMiddleware.cs).

### Warehouses are ERP-synced, not manually managed

Warehouses in WAMS are **shadow entities** synced from an external ERP system (`WarehouseShadowService`, `system.sync.*` permissions, `SyncController`). There is no "create a warehouse" workflow in the app itself - `WarehousesController` exposes only read/export/locations/unmapped-list endpoints.

### Assignment Sources

1. **`user_warehouses` table** - direct warehouse assignments
2. **`user_provinces` table** - province assignments (drives Budget Template visibility and, for WH_MGR/WH_OPS, effectively warehouse scope by proxy)
3. **Primary warehouse designation** - user's default warehouse

---

## Budget Plan Approval Workflow

The doc previously described a hardcoded two-stage pipeline (`Submitted → ApprovedStage1 → Approved`). That has been replaced by a **configurable Workflow Template engine** (`WorkflowTemplatesController`, `Workflow.template.*` permissions).

### How it works

1. `BudgetPlanStatus` is now: `Draft`, `Submitted`, `InApproval`, `Approved`, `Rejected` - there is no `ApprovedStage1` status anymore.
2. On submit, `BudgetPlanService.InitiateWorkflowAsync` looks up the company's **active** `WorkflowTemplate` for doc type `BudgetPlanApproval` and creates a fresh `WorkflowInstance` with one `WorkflowStage` per template stage, each carrying a list of `ApproverRoles`.
3. The **default seeded template** (`DatabaseSeeder.SeedWorkflowTemplatesAsync`) still uses the familiar two stages:
   - Stage 1 - `WAREHOUSE_HEAD`
   - Stage 2 - `COORDINATOR_WH`

   But this is **data**, editable via `WorkflowTemplatesController` by anyone with `workflow.template.{create,update,delete}` (HO_SPV family) - a company can add/remove stages or change approver roles without a code change.
4. On `ApproveAsync` (`BudgetPlanService.cs:251-315`):
   - The current pending stage's `ApproverRoles` are checked against the caller's roles - **or** the caller bypasses entirely if `HasGlobalAccessAsync` is true (i.e. any global-access role, not just the stage's named approver roles, can approve at any stage).
   - Self-approval requires the `approval.self.approve` permission; without it, approving a plan you submitted yourself is blocked. In practice only `SUPER_ADMIN` holds it, picked up via its `*.*.*` wildcard, so behaviour matches the old hardcoded `SUPER_ADMIN` role-name check it replaced. Grant it explicitly to give another role the same exception.
   - Approving the **final** stage flips the plan to `Approved` and **auto-generates Work Orders** in bulk (`woService.BulkCreateDraftAsync`) - this is why there's no manual "create work order" endpoint.
   - Approving a non-final stage advances `CurrentStageOrder` and sets plan status to `InApproval`.
5. `RejectAsync` rejects the current pending stage and the whole plan in one step - there's no per-stage-only rejection that keeps the plan alive.

**Implication for the Feature Access Matrix above:** any global-access role (HO_SPV, LOG_MGR, LOG_SPV, FINANCE_USER, VIEWER-if-ever-granted-approve, IT_OPS, FAT_MGR, FAT_OPS) that also happens to hold `budget.plan.approve` can approve at any stage, not just WAREHOUSE_HEAD/COORDINATOR_WH - in practice today, only WAREHOUSE_HEAD, COORDINATOR_WH, WH_MGR, and SUPER_ADMIN are actually granted `budget.plan.approve`, so the bypass mostly matters if that grant set changes.

Source: [`src/WAMS.Application/Services/BudgetPlanService.cs`](src/WAMS.Application/Services/BudgetPlanService.cs), [`src/WAMS.Api/Controllers/WorkflowTemplatesController.cs`](src/WAMS.Api/Controllers/WorkflowTemplatesController.cs).

---

## Permission Resolution

### How Permission Checks Work

```
┌─────────────────────────────────────────────────────────────┐
│  Step 1: Check User-Level DENY                              │
│  └── Is there an active DENY for this permission?           │
│      └── YES → ❌ Access Denied (403)                       │
│      └── NO → Continue to Step 2                            │
├─────────────────────────────────────────────────────────────┤
│  Step 2: Check User-Level GRANT                              │
│  └── Is there an active GRANT for this permission?          │
│      └── YES → ✅ Access Allowed                            │
│      └── NO → Continue to Step 3                            │
├─────────────────────────────────────────────────────────────┤
│  Step 3: Check Role Permissions (wildcard matching)          │
│  └── keys.Contains any of, in this order:                   │
│      *.*.*  →  *.*.{action}  →  {m}.*.*  →  {m}.{r}.*  →    │
│      {m}.*.{a}  →  *.{r}.{a}  →  exact "{m}.{r}.{a}"         │
│      └── Match → ✅ Access Allowed                          │
│      └── No match → ❌ Access Denied (403)                  │
└─────────────────────────────────────────────────────────────┘
```

This matches the doc's original claim exactly - **unchanged**. Verified against `RbacService.EvaluatePermission` (`src/WAMS.Application/Services/RbacService.cs:45-64`).

### Priority Order

**Explicit Deny > Explicit Grant > Role Grant > Default Deny** - unchanged.

### User-Level Permission Overrides

Unchanged in concept. `UserPermission.Constraints` (e.g. a max-approval-amount JSON blob) is stored on the entity but **is not currently evaluated** by `HasPermissionAsync` - a grant/deny override is all-or-nothing today; constraint enforcement is not wired up yet, so don't document it as an enforced feature.

Managed via `UsersController`'s `/api/v1/users/{id}/permissions` endpoints (`user.permission.{read,create,delete}`).

---

## System Roles vs Custom Roles

### System Roles

| Role | Protected | Description |
|------|-----------|--------------|
| SUPER_ADMIN | ✅ Cannot delete/modify | Full system access |
| HO_SPV | ✅ Cannot delete/modify | Head Office management |
| VIEWER | ✅ Cannot delete/modify | Read-only access |

All other roles - including the 8 newer client-matrix roles (IT_OPS, LOG_MGR, LOG_SPV, LOG_MKT_OPS, WH_MGR, WH_OPS, FAT_MGR, FAT_OPS) - are `IsSystem = false` and can, in principle, be modified/deleted like any custom role, even though they were seeded by the system rather than created by an admin.

### Custom Roles

`Role.CompanyId` exists on the entity (nullable) as scaffolding for future per-company custom roles, but `RbacService.CreateRoleAsync` never sets it - all created roles are effectively global today. Don't treat per-company custom roles as a shipped feature.

---

## Multi-Tenancy (Company Isolation)

### Data Isolation

| User Type | Data Visibility |
|-----------|-----------------|
| **Regular Users** | Only data from their company |
| **Super Admin** | Data from **all companies** |

### How It Works

1. **Login**: user selects company during login
2. **JWT Token**: contains `company_id` claim
3. **`TenantMiddleware`**: extracts `company_id` from the JWT and calls `tenantContext.SetCompanyId(companyId)` - nothing else; it does **not** itself special-case super-admins (`src/WAMS.Api/Middleware/TenantMiddleware.cs`)
4. **Super Admin Bypass**: happens at the **repository/query-filter level**, not in `TenantMiddleware` - services and EF global query filters decide whether to honor or ignore the tenant context based on whether the caller has `*.*.*`/global access
5. **Database Queries**: automatically filtered by company unless the repository layer explicitly bypasses (e.g. `GetByIdUnfilteredAsync` in `CompanyService`)

### Company Management

Only **SUPER_ADMIN** can create companies, view all companies, update company info, deactivate companies (soft delete, blocked for the `DEFAULT` company), and move users between companies.

---

## Caching

Permission checks are cached, not evaluated fresh on every request:

- `CachedRbacService` wraps `RbacService` behind a process-local `HybridCache`.
- Role/permission mutations (`CreateRoleAsync`, `UpdateRoleAsync`, `DeleteRoleAsync`, `AssignPermissionAsync`, `RemovePermissionAsync`, `SyncPermissionsAsync`) invalidate the `RbacAllPerms` tag globally; user-override mutations invalidate the specific user's `RbacUser(userId)` tag.
- Those same role/permission mutations **also** invalidate the global `WarehouseShadows` tag (and user-override mutations invalidate `WarehouseShadowsForUser(userId)`), because `CachedWarehouseShadowService` independently caches computed per-user warehouse lists derived from RBAC state - this cross-invalidation was added specifically to fix a bug where a role's global-access flag change could leave a user's cached warehouse list stale for up to 15 minutes after the RBAC cache itself had already refreshed.
- A direct database edit that bypasses these services (raw SQL against `role_permissions`, `roles`, etc.) does **not** trigger any invalidation - stale cache entries only clear once their TTL expires.

---

## Implementation Notes

### Source Code References

| Component | File Path |
|-----------|-----------|
| Permission Constants | [`src/WAMS.Domain/Constants/Permissions.cs`](src/WAMS.Domain/Constants/Permissions.cs) |
| Role Constants | [`src/WAMS.Domain/Constants/RoleCodes.cs`](src/WAMS.Domain/Constants/RoleCodes.cs) |
| Permission Filter | [`src/WAMS.Api/Filters/RequirePermissionAttribute.cs`](src/WAMS.Api/Filters/RequirePermissionAttribute.cs) |
| Permission Service | [`src/WAMS.Application/Services/RbacService.cs`](src/WAMS.Application/Services/RbacService.cs) |
| Cached Permission Service | [`src/WAMS.Infrastructure/Caching/CachedRbacService.cs`](src/WAMS.Infrastructure/Caching/CachedRbacService.cs) |
| Budget Plan Approval | [`src/WAMS.Application/Services/BudgetPlanService.cs`](src/WAMS.Application/Services/BudgetPlanService.cs) |
| Workflow Templates | [`src/WAMS.Api/Controllers/WorkflowTemplatesController.cs`](src/WAMS.Api/Controllers/WorkflowTemplatesController.cs) |
| Tenant Middleware | [`src/WAMS.Api/Middleware/TenantMiddleware.cs`](src/WAMS.Api/Middleware/TenantMiddleware.cs) |
| Warehouse Middleware | [`src/WAMS.Api/Middleware/WarehouseMiddleware.cs`](src/WAMS.Api/Middleware/WarehouseMiddleware.cs) |
| Role Seeding | [`src/WAMS.Infrastructure/Data/DatabaseSeeder.cs`](src/WAMS.Infrastructure/Data/DatabaseSeeder.cs) |
| Role Entity | [`src/WAMS.Domain/Entities/Role.cs`](src/WAMS.Domain/Entities/Role.cs) |
| Permission Entity | [`src/WAMS.Domain/Entities/Permission.cs`](src/WAMS.Domain/Entities/Permission.cs) |
| User Permission Override Entity | [`src/WAMS.Domain/Entities/UserPermission.cs`](src/WAMS.Domain/Entities/UserPermission.cs) |

### Security Features

- ✅ JWT-based authentication
- ✅ Token blacklisting with process-local `IMemoryCache` during API uptime
- ✅ Soft deletes (data recovery)
- ✅ Audit logging (`audit.log.*`) on permission-relevant changes
- ✅ User-level permission overrides with expiration (constraint evaluation not yet wired up)
- ✅ Wildcard permission support
- ✅ Automatic tenant/company filtering
- ✅ Warehouse and province scoping for field/regional users
- ✅ RBAC + warehouse-shadow result caching with cross-invalidation

---

*Document Version: 2.0*
*Last Updated: 2026-07-07*
*System: WAMS v1.0*

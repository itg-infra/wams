# Changelog

### v1.9.0 - Remove Budget Template Approval

**Changes**

- **Budget Template approval removed**: Templates now have only two statuses - `Draft` and `Submitted`. The `Approved` and `Rejected` statuses and their associated endpoints (`POST /{id}/approve`, `POST /{id}/reject`) are gone.
- **Edit allowed on Submitted templates**: `PUT /api/v1/budget-templates/{id}` now works in both `Draft` and `Submitted` states. History of edits is preserved via the audit log.
- **Delete restricted to Draft**: Delete is still only allowed in `Draft` status.
- **BudgetPlan gate updated**: BudgetPlan creation now requires the referenced template to be `Submitted` (was `Approved`).
- **Permissions removed**: `budget.template.approve` and `budget.template.reject` removed from seed and from `WAREHOUSE_HEAD` role.
- **Notifications removed**: `budget_template_approved` and `budget_template_rejected` notification types no longer exist.
- **DB migration**: `RemoveBudgetTemplateApproval` drops `approved_by_user_id`, `approved_at`, `rejected_by_user_id`, `rejected_at`, `rejection_reason` columns from `budget_templates`.

---

### v1.8.0 - Work Order Feature + File Attachment Auth Redesign

**Features**

- **Work Order CRUD + workflow**: FOREMAN creates WOs from `ApprovedStage2` budget plans with activity-specific payloads (Unloading, Loading, Fumigation, Storage, QC, Heavy Equipment, Unbagging, Rebagging). Each WO has a Load tab (`loadingItems`) and one activity-specific tab. Lifecycle: `Draft → Submitted`.
- **Approved Budget Plan sourcing**: `GET /api/v1/work-orders/approved-plans` returns BP list the FOREMAN can create a WO from.
- **Warehouse scoping on WOs**: WO list and approved-plans endpoints apply `ResolveWarehouseIdsAsync` - same pattern as BudgetPlanService. FOREMAN without header sees only their own warehouses.
- **WO file attachments**: `WorkOrderFileAttachmentEntityHandler` implements `IFileAttachmentEntityHandler` for `work-orders` entity type. Blocks all file mutations on `Submitted` WOs (`403`). Entity owner (WO creator) can delete any attachment on the WO; uploader can delete their own.
- **FIELD_USER renamed to FOREMAN** across all code, seeders, and documentation.
- **`workorder.workorder.read` added to FOREMAN** - was missing from initial seed.

**File attachment authorization redesign**

`RequirePermission` removed from `FilesController` entirely. The entity handler (`IFileAttachmentEntityHandler`) is now the sole authorization gate for file operations:
1. Entity existence check (→ `404`)
2. Tenant scoping (→ `404`)
3. Entity-specific locking, e.g. WO Draft guard (→ `403`)
4. Delete: uploader or entity owner (→ `403`)

This removes the `files.attachment.{create,read,delete}` permission requirement. Any authenticated user can operate on files for any entity they have access to - no separate files permission needed. `files.attachment.*` was never seeded; the attributes were the only enforcement.

**New files**

- `src/WAMS.Infrastructure/Services/WorkOrderFileAttachmentEntityHandler.cs`
- `resources/work-order/WORK_ORDER_API_GUIDE.md`

**Modified files**

- `FilesController` - removed all `[RequirePermission]` attributes; `[Authorize]` remains
- `IFileAttachmentEntityResolver` / `FileAttachmentEntityContext` - added `OwnerUserId` init property
- `FileAttachmentService.DeleteAsync` - delete allowed when uploader OR entity owner
- `IWorkOrderRepository` / `IWorkOrderService` - `GetAllAsync` and `GetApprovedBpListAsync` accept warehouse IDs / userId
- `WorkOrderService` - `ResolveWarehouseIdsAsync` helper; injects `IWarehouseContext` + `IUserRepository`
- `WorkOrderRepository` - warehouse filter on list and approved-plans queries; `VendorName`, `BlNumber`, `ProductName` added to responses
- `DatabaseSeeder` - FIELD_USER → FOREMAN; `workorder.workorder.read` added to FOREMAN

---

### v1.7.0 - Warehouse-Level Scoping for Budget Operations

**Features**

- **`IWarehouseContext` / `WarehouseMiddleware`**: New scoped context service mirrors the existing `ITenantContext` pattern. Frontend sets `X-Warehouse-Id: {id}` header; middleware reads it and populates context per request. SuperAdmin (`*.*.*`) gets bypass mode automatically - no filter applied.
- **Budget Template & Budget Plan warehouse scoping**: Both list and detail endpoints now enforce warehouse-level access on top of the existing company tenant filter.
- **`GET /api/v1/auth/me` enhancements**: Response now includes `hasGlobalAccess` (bool) and `warehouses` (list of assigned warehouses with `isPrimary` flag). Frontend uses this to populate the warehouse selector on login.

**Access control logic - list endpoints**

| Scenario | Result |
|----------|--------|
| `X-Warehouse-Id` header present (user's warehouse) | Filter to that warehouse only |
| `X-Warehouse-Id` header present (not user's warehouse) | 403 Forbidden |
| No header + user has global access role | No warehouse filter - sees all company data |
| No header + scoped user | Filter to all user's assigned warehouses |
| SuperAdmin (any/no header) | No filter - sees all data |

**Mutations also scoped**

All write operations that carry a `userId` now validate warehouse access before proceeding:

| Service | Scoped operations |
|---------|------------------|
| `BudgetTemplateService` | `CreateAsync`, `GetByIdAsync`, `SubmitAsync`, `ApproveAsync` |
| `BudgetPlanService` | `CreateAsync`, `GetByIdAsync`, `SubmitAsync`, `ApproveAsync`, `RejectAsync` |

**New files**

- `src/WAMS.Application/Interfaces/IWarehouseContext.cs`
- `src/WAMS.Infrastructure/Services/WarehouseContext.cs`
- `src/WAMS.Api/Middleware/WarehouseMiddleware.cs`

**Modified files**

- `IBudgetTemplateRepository` / `IBudgetPlanRepository` - `GetAllAsync` accepts `IReadOnlyList<long>?` warehouse IDs
- `IBudgetTemplateService` / `IBudgetPlanService` - `GetAllAsync` and `GetByIdAsync` accept `long userId`
- `BudgetTemplateService` / `BudgetPlanService` - warehouse access guard (`EnsureWarehouseAccessAsync`) on all scoped operations
- `MeResponse` - added `HasGlobalAccess: bool` and `Warehouses: List<MeWarehouseResponse>`

---

### v1.6.0 - Budget Template Feature

**Features**

- **ActivityType master table**: Global master for template activity types (e.g. Kegiatan Bongkar, Kegiatan Muat, Fumigasi, Opname). Managed by SUPER_ADMIN via `system.activity_type.*` permissions. Seeded with 4 defaults on startup (idempotent).
- **Budget Template CRUD + workflow**: HO SPV creates templates linking a warehouse + activity type + cost items from `ItemShadow`. Templates follow a `Draft → Submitted → Approved` lifecycle.
- **Template Code generation**: Auto-generated document code `T.{YYMM}{5-digit-seq}` (e.g. `T.260400001`). Unique across all companies; sequence resets per month prefix.
- **Cost item references**: `BudgetTemplateItem` stores only `ItemShadowId` and `SortOrder`. `ItemCode`, `ItemName`, `AcctCode`, `AcctName` are read directly from the joined `ItemShadow` at query time - no redundant storage.
- **Workflow actions**: `POST /{id}/submit` (HO SPV) and `POST /{id}/approve` (Warehouse Head) update status + timestamp + actor in a single commit.

**Business rules enforced**

- Update and delete are blocked unless `Status == Draft` (throws `ValidationException`)
- Submit is blocked unless `Status == Draft`; Approve is blocked unless `Status == Submitted`
- `ActivityType` must exist and `IsActive == true` at create/update time
- All `ItemShadowId` values are validated to exist before commit

**New permissions seeded**

| Module | Resource | Action | Description |
|--------|----------|--------|-------------|
| `budget` | `template` | `submit` | Submit budget template for approval |
| `budget` | `template` | `approve` | Approve budget templates (Warehouse Head) |
| `system` | `activity_type` | `create` | Create activity types |
| `system` | `activity_type` | `update` | Update activity types |
| `system` | `activity_type` | `delete` | Delete activity types |

**Role updates**

- `WAREHOUSE_HEAD` now receives `budget.template.read` and `budget.template.approve`

**New files**

- `src/WAMS.Domain/Enums/BudgetTemplateStatus.cs`
- `src/WAMS.Domain/Entities/ActivityType.cs`
- `src/WAMS.Domain/Entities/BudgetTemplate.cs`
- `src/WAMS.Domain/Entities/BudgetTemplateItem.cs`
- `src/WAMS.Application/DTOs/ActivityTypes/ActivityTypeDtos.cs`
- `src/WAMS.Application/DTOs/BudgetTemplates/BudgetTemplateDtos.cs`
- `src/WAMS.Application/Interfaces/IActivityTypeRepository.cs`
- `src/WAMS.Application/Interfaces/IActivityTypeService.cs`
- `src/WAMS.Application/Interfaces/IBudgetTemplateRepository.cs`
- `src/WAMS.Application/Interfaces/IBudgetTemplateService.cs`
- `src/WAMS.Application/Services/ActivityTypeService.cs`
- `src/WAMS.Application/Services/BudgetTemplateService.cs`
- `src/WAMS.Infrastructure/Data/Configurations/ActivityTypeConfiguration.cs`
- `src/WAMS.Infrastructure/Data/Configurations/BudgetTemplateConfiguration.cs`
- `src/WAMS.Infrastructure/Data/Configurations/BudgetTemplateItemConfiguration.cs`
- `src/WAMS.Infrastructure/Repositories/ActivityTypeRepository.cs`
- `src/WAMS.Infrastructure/Repositories/BudgetTemplateRepository.cs`
- `src/WAMS.Api/Controllers/ActivityTypesController.cs`
- `src/WAMS.Api/Controllers/BudgetTemplatesController.cs`
- Migration: `AddBudgetTemplateFeature`

---

### v1.5.0 - Error Handling Hardening

**Breaking changes**

- **Validation errors now return HTTP `422 Unprocessable Entity`** instead of `400 Bad Request`. This applies to all FluentValidation failures in request bodies. Malformed JSON/model-binding failures remain `400` (handled by ASP.NET Core before application code runs).

**Improvements**

- **Centralized error codes** (`ErrorCodes` static class in `WAMS.Application.Common`): all six error code string constants (`VALIDATION_ERROR`, `UNAUTHORIZED`, `FORBIDDEN`, `NOT_FOUND`, `CONFLICT`, `INTERNAL_ERROR`) now live in one place. No more scattered string literals across middleware, filters, and controllers.
- **Field-level validation errors preserved**: validation failures now return a field-keyed dictionary in `error.details` (`{ "email": ["Email is required"] }`) instead of a flat string array. Consumers can pinpoint exactly which field failed.
- **`RequirePermissionAttribute` unified with middleware**: the auth filter now throws `UnauthorizedException` / `ForbiddenException` instead of manually building an `ErrorResponse`. All error responses flow through `ExceptionHandlingMiddleware` - one code path, one format.
- **Controllers delegate not-found to middleware**: `ItemsController`, `VendorsController`, and `SyncController` no longer manually return `NotFound(ErrorResponse(...))`. They throw `NotFoundException` and let the middleware handle it, consistent with every other controller.
- **4xx errors logged at Warning level**: previously only 500s were logged. HTTP 4xx errors are now emitted as `LogWarning` with the status code and request ID, aiding observability for auth/permission/not-found anomalies.

**New files**

- `src/WAMS.Application/Common/ErrorCodes.cs`

---

### v1.4.0 - Docker Development Environment & Security Enhancements

**Features**

- **Complete Docker Compose Setup**: Full development environment with PostgreSQL, Redis, and application service including health checks, proper networking, and persistent volumes
- **Security-Hardened Dockerfile**: Multi-stage build with Alpine Linux, non-root user execution, read-only filesystem (except logs), and built-in health monitoring
- **Environment Variable Support**: Added DotNetEnv package for `.env` file loading in local development, with production-ready environment injection
- **Enhanced Make Commands**: New Docker management commands (`docker-logs`, `docker-clean`) and improved container compatibility with `--no-launch-profile` flags

**Improvements**

- **Production-Ready Container Image**: Optimized for Kubernetes and Docker Swarm deployment with proper security practices
- **Health Monitoring**: Application and database health checks with proper dependency management in Docker Compose
- **Flexible Configuration**: Removed hardcoded URLs from launch settings, supporting both local and containerized development
- **Better Developer Experience**: One-command setup (`make docker-up`) for complete development environment

**Docker Features**

- **Multi-Stage Build**: Smaller final image size with only runtime dependencies
- **Security Best Practices**: Non-root user, minimal attack surface, proper file permissions
- **Health Checks**: Built-in container health monitoring via `/health` endpoint
- **Volume Management**: Persistent data storage for PostgreSQL, Redis, and application logs
- **Network Isolation**: Dedicated Docker network for service communication

**Environment**

- **`.env` File Support**: Local development configuration with sensible defaults
- **Container Orchestration Ready**: Compatible with Docker Compose, Kubernetes, and Docker Swarm
- **Production Environment Variables**: Clear separation between development and production configurations

---

### v1.4.0 - Unit of Work + Architecture Cleanup

**Features**

- **Unit of Work pattern** (`IUnitOfWork` / `UnitOfWork`): Repositories no longer call `SaveChangesAsync` internally. Services stage all changes via the EF change tracker and flush with a single `await _uow.CommitAsync(ct)` at the end of each write operation. Multi-step operations are now atomic - if any step throws, no partial state reaches the database.
- **Two-stage commit in `UserService.CreateAsync`**: After the user INSERT is flushed (Stage 1), the DB-generated `user.Id` is used to stage warehouse junction rows which are then flushed together (Stage 2). This ensures user + warehouses are always consistent.
- **Atomic company reassignment**: `CompanyService.AssignUserToCompanyAsync` now clears warehouse assignments and updates the user's `CompanyId` in a single commit - previously two independent commits with a gap where stale state could persist.

**Architecture changes**

- **Repository interfaces moved** from `WAMS.Domain/Interfaces/` → `WAMS.Application/Interfaces/`. Application layer owns the ports (interfaces); Infrastructure implements them. Domain is now purely entities and exceptions with no cross-cutting query types.
- **`DataTableQuery` moved** from `WAMS.Domain/Common/` → `WAMS.Application/Common/`. Pagination/search/sort is an application concern, not a domain concept.
- `EffectivePermission` record co-located with `IRbacRepository` in `WAMS.Application/Interfaces/` - it is a query projection, not a domain entity.

**New files**

- `src/WAMS.Application/Interfaces/IUnitOfWork.cs`
- `src/WAMS.Application/Common/DataTableQuery.cs`
- `src/WAMS.Infrastructure/Data/UnitOfWork.cs`

**Removed files**

- `src/WAMS.Domain/Common/DataTableQuery.cs`
- `src/WAMS.Domain/Interfaces/I*Repository.cs` (5 files - moved to Application)

---

### v1.3.0 - Super Admin Tenant Bypass

**Features**

- **Super Admin tenant bypass**: `TenantMiddleware` now detects the `*.*.*` permission claim and calls `SetBypassMode()` instead of `SetCompanyId()`. EF Core query filters treat `null` CompanyId as "show all" - Super Admin sees records across all companies without per-endpoint `IgnoreQueryFilters()` calls.
- **`CreateUserRequest` `companyId` field**: Optional `companyId` in the create-user request body. Ignored for regular tenant users (always scoped to their own company); required for Super Admin to specify which company the new user belongs to.

**Changes**

- `ITenantContext`: `CompanyId` changed to `long?`; added `SetBypassMode()` method.
- `TenantContext`: Tracks bypass mode with `IsSet = true` and `CompanyId = null`.
- `AppDbContext` query filters updated to pass through when `CompanyId` is null (bypass mode).
- `UserService`: Injected `ITenantContext` + `ICompanyRepository`; `CreateAsync` explicitly resolves and validates `CompanyId` for Super Admin.

**Schema**

- `users.company_id` changed from `nullable` → `NOT NULL`.
- Migration `20260227120000_MakeUserCompanyIdNotNull`: backfills NULL rows to company 1 before altering the column.

---

### v1.2.0 - Enterprise RBAC Gaps

**Features**

- **User-Level Permission Overrides** (`user_permissions` table): Grant or deny individual permissions per user, independent of roles. Supports temporary overrides via `ExpiresAt` and audit trail via `Reason`. Resolution order: Explicit Deny > Explicit Grant > Role Grant > Default Deny.
- **Budget Permission Split**: Replaced coarse `budget.budget.*` with distinct resources:
  - `budget.template.*` - HO Admin creates/manages templates
  - `budget.plan.*` - Warehouse Admin creates plans; Warehouse Head/Super approves/rejects; includes `submit` and `reject` actions
- **New Work Order Permissions**: `workorder.realization.recap` (Warehouse Admin daily recap) and `workorder.realization.verify` (verification step).
- **Constraints Column** (jsonb, nullable) added to both `role_permissions` and `user_permissions` - stored for future approval-limit enforcement (e.g. `{"max_approval_amount": 500000000}`), not yet evaluated by permission checks.
- **5 new API endpoints** on `/api/v1/users/{id}/permissions` for managing user-level overrides and resolving effective permissions.

**Changes**

- `WAREHOUSE_HEAD` `GlobalAccess` changed from `true` → `false` - Warehouse Heads approve budgets for their assigned warehouses only, not globally.
- `user.permission.read` split into `user.permission.create/read/delete` to allow fine-grained control over who can manage permission overrides.
- `HO_SPV` now also gets `advance.advance.read` for financial oversight.

**Schema**

- New table: `user_permissions` (composite PK on `user_id` + `permission_id`)
- New column: `constraints` (jsonb) on `role_permissions`
- Migration: `20260226212148_AddUserPermissionsAndRbacGaps`

### v1.1.0 - Multi-Tenancy Support

**Features**
- **Multi-Tenancy**: Company-based data isolation with automatic tenant context filtering
- **Company Selection on Login**: Users must select company before authentication
- **Tenant Context Middleware**: Extracts company_id from JWT and sets tenant context
- **Automatic Query Filtering**: EF Core global query filters for tenant isolation
- **Auto Company Assignment**: New entities automatically get CompanyId from tenant context

**Changes**
- Login now requires `companyId` parameter
- JWT access tokens include `company_id` claim
- Users and Warehouses are now scoped to companies
- Roles can be system-wide (CompanyId null) or company-specific
- Database seeder creates default company first

**Seeded Data**
- Default company from configuration (InitialCompany)
- Admin user assigned to default company

### v1.0.0 - Initial .NET Release

**Features**
- **Clean Architecture**: Strict layer separation with Domain, Application, Infrastructure, and Presentation layers
- **JWT Authentication**: Access tokens (24h) + refresh tokens (7d) with Redis-backed blacklisting
- **RBAC System**: `Module.Resource.Action` permission structure with wildcard support
- **User Management**: CRUD operations with soft delete, role and warehouse assignments
- **Role Management**: Create, update, delete roles with permission assignments
- **Warehouse Management**: Multi-warehouse support with region/address
- **Database Migrations**: Entity Framework Core migrations with auto-apply on startup
- **Structured Logging**: Serilog with console and file output
- **Request Tracing**: Distributed request ID middleware
- **Swagger/OpenAPI**: Interactive API documentation

**Security**
- BCrypt password hashing
- JWT token validation with issuer signing key
- Token blacklisting for logout
- Permission-based authorization filter

**Seeded Data**
- 7 default roles (SUPER_ADMIN, HO_SPV, WAREHOUSE_HEAD, WAREHOUSE_ADMIN, FINANCE_USER, FIELD_USER, VIEWER)
- 40+ permissions across all modules
- Initial admin user from configuration

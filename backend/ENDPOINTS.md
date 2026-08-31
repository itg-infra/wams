# WAMS API Endpoints Documentation

This document provides a comprehensive reference for all API endpoints in the Warehouse Management System (WAMS) API.

**Base URL:** `/api/v1`

---

## Table of Contents

- [Health Check](#health-check)
- [List Query Reference (Pagination, Sort & Search)](#list-query-reference-pagination-sort--search)
- [Caching](#caching)
- [Authentication](#authentication)
- [Users](#users)
- [Roles](#roles)
- [Permissions](#permissions)
- [Companies](#companies)
- [Warehouses](#warehouses)
- [Sync](#sync)
- [Items](#items)
- [Vendors](#vendors)
- [UoMs](#uoms)
- [Tax Types](#tax-types)
- [Rate Cards](#rate-cards)
- [Activity Types](#activity-types)
- [Budget Templates](#budget-templates)
- [Budget Plans](#budget-plans)
- [Workflow Templates](#workflow-templates)
- [Notifications](#notifications)
- [Purchase Orders](#purchase-orders)
- [Work Orders](#work-orders)
- [Transport Orders](#transport-orders)
- [Recap Work Orders](#recap-work-orders)
- [Account Payables](#account-payables)
- [Finance Reports](#finance-reports)
- [RCA](#rca)
- [Dashboard](#dashboard)
- [SPK (Base Documents)](#spk-base-documents)
- [Export](#export)
- [Files](#files)
- [Audit Logs](#audit-logs)
- [Record History](#record-history)

---

## Common Response Formats

### Success Response
```json
{
  "success": true,
  "data": { ... },
  "message": "Operation successful",
  "requestId": "request-id"
}
```

### Error Response
```json
{
  "success": false,
  "message": "Error description",
  "error": {
    "code": "ERROR_CODE",
    "details": { ... }
  },
  "requestId": "request-id"
}
```

### HTTP Status Codes

| Status | Error Code | When |
|--------|------------|------|
| `400 Bad Request` | - | Malformed request body (JSON parse failure, model binding) - returned by ASP.NET Core before reaching application code |
| `401 Unauthorized` | `UNAUTHORIZED` | Missing/expired/blacklisted token, or missing `sub` claim |
| `403 Forbidden` | `FORBIDDEN` | Authenticated but lacking the required permission |
| `404 Not Found` | `NOT_FOUND` | Requested resource does not exist |
| `409 Conflict` | `CONFLICT` | Duplicate or constraint violation (e.g. email already in use) |
| `422 Unprocessable Entity` | `VALIDATION_ERROR` | Request was understood but semantically invalid (FluentValidation or domain validation failure). `message` is always non-empty; `error.details` is always an object and contains a field-keyed map when available: `{ "fieldName": ["message 1", ...] }` |
| `429 Too Many Requests` | - | Rate limit exceeded. Applies to `POST /auth/login`, `POST /auth/refresh`, `POST /auth/change-password`, and `POST /users/{id}/password` (10 requests/minute per IP, sliding window). No `Retry-After` header - client should back off exponentially. |
| `500 Internal Server Error` | `INTERNAL_ERROR` | Unexpected server-side fault |

### Validation Error Detail Shape

Validation errors (`422`) include a field-level breakdown in `error.details`:

```json
{
  "success": false,
  "message": "One or more validation errors occurred.",
  "error": {
    "code": "VALIDATION_ERROR",
    "details": {
      "email": ["Email is required", "Invalid email format"],
      "password": ["Password must be at least 8 characters"]
    }
  },
  "requestId": "request-id"
}
```

### Paginated Response
```json
{
  "success": true,
  "data": [ ... ],
  "meta": {
    "page": 1,
    "limit": 20,
    "total": 100,
    "totalPages": 5
  },
  "requestId": "request-id"
}
```

### Request ID

Every response - success, error, and paginated - includes a `requestId` field in the JSON body and a matching `X-Request-ID` response header. Both values are identical and can be used to correlate a specific API call with server-side logs.

**How it works:**
- If the client sends an `X-Request-ID` request header, that value is echoed back in the response header and body.
- If no `X-Request-ID` header is sent, the server assigns a unique trace identifier automatically.

**Usage:**
- Log `requestId` on the frontend alongside any error shown to the user.
- Pass it to backend engineers when reporting bugs - they can grep server logs by this value.

```
# Request
GET /api/v1/budget-plans
X-Request-ID: my-frontend-trace-abc123

# Response headers
X-Request-ID: my-frontend-trace-abc123

# Response body
{ "success": true, ..., "requestId": "my-frontend-trace-abc123" }
```

### Warehouse Scoping Header

Budget Plan and Work Order endpoints respect a warehouse scope set via request header.

> **Note:** Budget Templates are not filtered by the `X-Warehouse-Id` header. Any active user in the company can view a single template by ID; list/export results are filtered by **province** instead - see [Province & Region Scoping](#province--region-scoping).

Budget Plan, Work Order, and other warehouse-scoped endpoints respect a warehouse scope set via request header:

```
X-Warehouse-Id: {warehouseId}
```

| Scenario | Behaviour |
|----------|-----------|
| Header present - valid warehouse, user's assigned warehouse | Results filtered to that warehouse only |
| Header present - valid warehouse, not assigned to user | `403 Forbidden` |
| Header present - warehouse ID does not exist | `404 Not Found` |
| Header absent - user has global access role | No warehouse filter (sees all company data) |
| Header absent - scoped user | Filtered to all user's assigned warehouses |
| SuperAdmin (`*.*.*`) - any/no header | No warehouse filter (global access), but still scoped to the acting company chosen at login |

> **Frontend pattern:** After login, call `GET /api/v1/auth/me` to get the user's `warehouses` list, `scopes` list (the user's coarse province scope; each entry has `id`, `name`, `display`), and `hasGlobalAccess` flag. Populate the warehouse selector from `warehouses`. Set `X-Warehouse-Id` globally in your HTTP client (axios interceptor, fetch wrapper, etc.) whenever the active warehouse changes. Omit the header for users where `hasGlobalAccess = true` to show all company data, or include it to scope to a specific warehouse.

---

### Province & Region Scoping

Warehouses and Budget Templates are grouped into **provinces**, a master table (`provinces`, seeded with a fixed set of Indonesian provinces plus a synthetic `GLOBAL` entry) separate from company/warehouse scoping. A warehouse's province is resolved automatically from ERP data; a template's province is set directly by `provinceId` in the create/update request.

**How a warehouse gets a province:**

1. `WarehouseSyncService` normalizes the ERP `location` text (trim, collapse whitespace, uppercase) on every sync run.
2. It looks the normalized value up against `provinces.name` and `province_aliases.alias` (also stored normalized).
3. A match sets `WarehouseShadow.ProvinceId`. No match leaves it `null` and logs a warning; the warehouse still syncs normally and shows up in `GET /warehouses/unmapped`.

This free-text resolution only happens for ERP-synced warehouse data, where the source text is out of the API's control. Budget Templates skip it entirely: `POST`/`PUT /budget-templates` take `provinceId` directly, validated against the `provinces` table (`404` if it doesn't exist, `400` if inactive). Clients get valid IDs from `GET /warehouses/locations`, so there's nothing to normalize or mismatch.

Scope has two independent, additive grains with no cross-inference: a **province** assignment grants every warehouse in it (coarse), while a **warehouse** pin grants only that one warehouse (fine). A warehouse pin never confers its province - and thus never its province siblings or province-level data.

**How a user's accessible provinces are computed** (`GetUserProvinceIdsAsync`) - the union of:
- Provinces directly assigned to the user (`user_provinces` table)
- The `GLOBAL` province, always included regardless of assignment

A user's accessible **warehouses** (`GetUserWarehouseIdsAsync`) is the union of warehouses in those accessible provinces and warehouses the user is explicitly pinned to (`user_warehouses`). Global-access roles (`GlobalAccess=true`) skip this computation entirely and see everything.

**Where this applies:**

| Endpoint | Effect for non-global-access users |
|---|---|
| `GET /budget-templates`, `GET /budget-templates/export` | Only templates whose `provinceId` is in the caller's accessible-province set. Templates with `provinceId: null` are excluded - only visible to global-access users. |
| `GET /warehouses`, `GET /warehouses/export` | Only warehouses in the caller's accessible-warehouse set (as above). |
| `GET /warehouses/locations` | Only the caller's accessible provinces (global-access users get every active province). |
| `GET /warehouses/{id}`, budget plan creation | `403 Forbidden` if the target warehouse isn't in the caller's accessible-warehouse set. |
| `GET /warehouses/unmapped` | Global access only; `403` otherwise. |
| Budget plan create/update | `400` if the chosen warehouse's `provinceId` doesn't match the template's `provinceId` (both `null` is treated as a match). |

---

### Datatable Query Parameters

All list endpoints (`GET /users`, `/roles`, `/companies`, `/warehouses`, `/rate-cards`, `/budget-templates`, `/budget-plans`) accept the following query parameters:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `search` | string | - | Case-insensitive substring search across key fields |
| `sortBy` | string | entity default | Field name to sort by (see per-endpoint sortable fields) |
| `sortOrder` | `asc` \| `desc` | `asc` | Sort direction |
| `page` | integer | `1` | Page number (1-based) |
| `limit` | integer | `20` | Items per page |

**Validation:** `page` must be `>= 1`; `limit` must be between `1` and `100` (inclusive). Out-of-range values return `400 Bad Request` (ASP.NET model-binding validation, before application code runs - not the `422` `VALIDATION_ERROR` path). `sortBy` values not recognized by the target endpoint silently fall back to that endpoint's default sort - they never error.

**Search semantics:** `search` is a case-insensitive substring match (Postgres `ILIKE ... ESCAPE '\'`) across each endpoint's listed fields, OR'd together - a match on any one field includes the row. Literal `%`, `_`, and `\` in the search term are escaped so they match themselves instead of acting as SQL wildcards (`WAMS.Application.Common.LikePatternHelper`). Empty string or whitespace-only `search` is treated as "no filter" (same as omitting the parameter).

---

## List Query Reference (Pagination, Sort & Search)

Per-endpoint breakdown of every `GET` list endpoint's searchable and sortable fields. All endpoints below accept `page`/`limit` per the [validation rule above](#datatable-query-parameters); `sortOrder` defaults to `asc` unless noted otherwise. **Bold** marks the field used when `sortBy` is omitted or unrecognized.

### Core / RBAC

| Endpoint | Search fields | Sortable fields |
|---|---|---|
| `GET /companies` | `code`, `name`, `email`, `phone` | **`name`**, `code`, `isActive`, `createdAt` |
| `GET /users` | `email`, `fullname`, `employeeId` | `email`, `fullname`, `employeeId`, `isActive`, **`createdAt`** |
| `GET /roles` | `name`, `displayName`, `description` | **`name`**, `displayName`, `isSystem`, `globalAccess`, `createdAt` |

### Budget domain

| Endpoint | Search fields | Sortable fields |
|---|---|---|
| `GET /budget-templates` | `code`, `province.name` | `code`, `status`, **`createdAt`**, `submittedAt` |
| `GET /budget-plans` | `code`, `remark`, `warehouse.name` | `status`, **`createdAt`**, `docDate`, `submittedAt` |
| `GET /rate-cards` | `vendor.cardName` | `status`, **`createdAt`**, `submittedAt` |
| `GET /workflow-templates` | `name`, `docType` | `name`, `docType`, `isActive`, `updatedAt`, **`createdAt`** (desc by default) |

> `workflow-templates` also accepts a non-datatable `docType` exact-match filter alongside `search`.

### ERP shadow (read-only, synced) tables

| Endpoint | Search fields | Sortable fields |
|---|---|---|
| `GET /items` | `itemCode`, `itemName`, `acctCode` | **`itemCode`**, `itemName`, `acctCode`, `isActive` |
| `GET /vendors` | `cardCode`, `cardName` | **`cardCode`**, `cardName`, `isActive` |
| `GET /warehouses` | `code`, `name`, `location` | **`code`**, `name`, `location`, `isActive`, `syncedAt` |
| `GET /transport-orders` | `docNo`, `cardName`, `vehicleNo`, `blNo`, `itemName` | `docNo`, `vehicleNo`; default **`syncedAt` desc, then `docNo`** (no explicit `sortBy` match falls here) |
| `GET /spk` | `docNo`, `baseDocNo`, `cardName`, `itemCode`, `itemName`, `type` | `docNo` (**default, desc**), `cardName`, `syncedAt` |

> `warehouses` additionally accepts a `provinceId` filter (see [Province & Region Scoping](#province--region-scoping)); search/sort apply within that filtered set.

### Transactional documents

| Endpoint | Search fields | Sortable fields |
|---|---|---|
| `GET /purchase-orders` | `code`, `vendor.cardName`, `remark` | `status`, `docDate`, **`createdAt`** (desc) |
| `GET /purchase-orders/approved-budget-plans` | `budgetPlan.code`, `vendor.cardName` | `docDate`, `budgetPlanCode`, `vendorName`, `totalBudgetPlan`, `budgetApproved`, `budgetVariance`, `poNumber`; default **`createdAt` desc** |
| `GET /purchase-orders/recap/apdp`, `.../non-apdp` | `budgetPlan.code`, `vendor.cardName` | Same sortable set as `approved-budget-plans` above (shares its sort-column map) |
| `GET /work-orders` | `code` (own), `budgetPlan.code` | `status`, `startDate`, **`createdAt`** (desc) |
| `GET /recap-work-orders` | `budgetPlan.code` | `status`, `docDate`, **`createdAt`** (desc) |
| `GET /account-payables` | `code`, `vendor.cardName`, `remark` | `status`, `docDate`, **`createdAt`** (desc) |

### Audit & dashboard

| Endpoint | Search fields | Sortable fields |
|---|---|---|
| `GET /audit-logs` | `tableName`, `requestPath`, `requestId` | `tableName`, `action`, `userId`, **`createdAt`** (desc) |
| `GET /dashboard/activities` | `budgetPlan.code`, `warehouse.code` | **No `sortBy` support** - always `createdAt DESC`. Pagination and `search` still apply. |

> `audit-logs` also accepts `dateFrom`/`dateTo` filters (see [Audit Logs](#audit-logs)).

---

## Health Check

**`GET /health`** - No auth required. Returns PostgreSQL liveness status. Not rate-limited. Intended for load balancer probes and Kubernetes readiness/liveness checks.

**Healthy response** (`200 OK`):
```json
{
  "status": "healthy",
  "checks": [
    { "name": "postgres", "status": "healthy", "duration": 4.2 }
  ]
}
```

**Unhealthy response** (`503 Service Unavailable` - PostgreSQL down):
```json
{
  "status": "unhealthy",
  "checks": [
    { "name": "postgres", "status": "unhealthy", "duration": 5001.0 }
  ]
}
```

> **Failure semantics:** PostgreSQL failure → `503 Unhealthy`. Kubernetes readiness probes should gate on `503`.

---

## Metrics (Prometheus)

**`GET /metrics`** - No auth required. Exposed only when `OpenTelemetry__Prometheus__Enabled=true`. Returns Prometheus text format. Do **not** expose without an auth proxy in production.

Custom business counters emitted under the `WAMS` meter:

| Metric | Labels | Description |
|--------|--------|-------------|
| `wams.budget_plans.submitted` | `company_id` | Budget plans submitted for approval |
| `wams.budget_plans.approved` | `company_id`, `stage_order` | Budget plans approved (per workflow stage) |
| `wams.budget_plans.rejected` | `company_id` | Budget plans rejected |
| `wams.work_orders.submitted` | `company_id` | Work orders submitted |
| `wams.recap_work_orders.approved` | `company_id` | Recap work orders approved |
| `wams.recap_work_orders.rejected` | `company_id` | Recap work orders rejected |
| `wams.erp_sync.runs` | `service`, `success` | ERP sync runs (success + failure) |
| `wams.erp_sync.items_upserted` | `service` | Items added or updated per sync run |
| `wams.erp_sync.failures` | `service` | ERP sync service-level failures |
| `wams.erp_sync.duration` (histogram) | `service` | ERP sync duration in milliseconds |
| `wams.auth.logins` | `company_id` | Successful login attempts |
| `wams.auth.login_failures` | - | Failed login attempts |

> **Backend:** Any OTLP-compatible backend works - SigNoz, Grafana Tempo, Grafana `otel-lgtm`, Datadog, Honeycomb, etc. Set `OpenTelemetry__Enabled=true` and point `OpenTelemetry__OtlpEndpoint` at your backend's OTLP gRPC port (usually `4317`). For local dev, [SigNoz](https://signoz.io/docs/install/docker/) is the recommended all-in-one option (traces + metrics + logs in a single UI).

---

## Caching

WAMS uses **`HybridCache`** as a process-local in-memory cache implemented via the decorator pattern. Business logic is entirely cache-free; caching is transparent to controllers and services. See [README § Caching](README.md#caching) for architecture details.

### Cached endpoints and their cache behaviours

The following endpoints serve responses from cache on repeated requests. Cache is invalidated automatically on any write to the related resource.

| Endpoint(s) | Cached? | Tag | TTL | Invalidated by |
|---|---|---|---|---|---|
| `GET /uoms`, `GET /uoms/{id}` | Yes | `uom` | 300 s | POST/PUT/DELETE `/uoms` |
| `GET /activity-types`, `GET /activity-types/{id}` | Yes | `activity-types` | 300 s | POST/PUT/DELETE `/activity-types` |
| `GET /workflow-templates`, `GET /workflow-templates/{id}` | Yes (per company) | `workflow-templates:{companyId}` | 300 s | POST/PUT/PATCH/DELETE `/workflow-templates` |
| `GET /warehouses`, `GET /warehouses/{id}`, `GET /warehouses/locations` | Yes | `warehouse-shadows` | 120 s | ERP WarehouseSync - automatic (5-60 min interval, see [Sync](#sync)) or manual (`POST /sync/trigger`, `POST /sync/trigger/WarehouseSync`) |
| `GET /rate-cards/{id}` | Yes | `rate-cards` | 120 s | POST/PUT/DELETE/PATCH `/rate-cards`; **also** a successful `PpnSyncService` run or `PphLookupService` refresh (defensive - see README [Invalidation Map](README.md#invalidation-map)) |
| `GET /tax-types`, `GET /tax-types/{id}` | Yes | `tax-types` | 300 s | A successful `PpnSyncService` run (scheduled) or `PphLookupService` refresh (on-demand, triggered by `GET /rate-cards/vendors/{vendorId}/pph`) |
| RBAC permission checks (internal, every authenticated request) | Yes (per user+permission) | `rbac-user:{userId}`, `rbac-all-perms` | 60 s | Role/permission admin mutations; **also** `POST /users/{id}/roles/{roleId}` and `DELETE /users/{id}/roles/{roleId}` via `IUserPermissionInvalidator` |

### Cache invalidation via API

Performing any **write operation** on a cached resource clears the corresponding cache tag. No manual cache flush endpoint is exposed - invalidation is automatic and co-located with the write.

**Cross-service RBAC invalidation** - Role assignment and removal (`POST/DELETE /users/{id}/roles/{roleId}`) live in `UserService`, which is outside the `CachedRbacService` decorator. Invalidation is triggered explicitly via `IUserPermissionInvalidator`, which calls `HybridCache.RemoveByTagAsync("rbac-user:{userId}")` synchronously after the DB commit. The cache is cleared before the HTTP response is returned, so the **same access token** will see updated permissions on the very next request - no TTL wait.

### Configuration

Override TTLs without redeploying via environment variables:

```
Cache__Uom__TtlSeconds=300
Cache__RbacPermission__TtlSeconds=60
# ... (see .env.example for full list)
```

---

## Authentication

Base route: `/api/v1/auth`

> **Rate limiting:** `POST /login`, `POST /refresh`, and `POST /change-password` are rate-limited to **10 requests per minute per IP** (sliding window). Exceeding the limit returns `429 Too Many Requests`. Back off exponentially on `429`.

| Method | Endpoint | Auth Required | Description | Request Body | Response |
|--------|----------|---------------|-------------|--------------|----------|
| POST | `/api/v1/auth/login` | No | Authenticate user and receive access + refresh tokens. Rate-limited: 10/min per IP. | [`LoginRequest`](#loginrequest) | [`LoginResponse`](#loginresponse) |
| POST | `/api/v1/auth/refresh` | No | Refresh access token using refresh token. Rate-limited: 10/min per IP. | [`RefreshRequest`](#refreshrequest) | [`LoginResponse`](#loginresponse) |
| POST | `/api/v1/auth/logout` | Yes | Logout and invalidate tokens | [`LogoutRequest`](#logoutrequest) | Success message |
| GET | `/api/v1/auth/me` | Yes | Get current authenticated user info | - | [`MeResponse`](#meresponse) |
| POST | `/api/v1/auth/change-password` | Yes | Change own password. Requires current password; verified first. Revokes all refresh tokens for the account, except the caller's current session if its refresh token is included in the request. Rate-limited: 10/min per IP. | [`ChangePasswordRequest`](#changepasswordrequest-auth) | Success message |

### DTOs

#### LoginRequest
```json
{
  "email": "string (required)",
  "password": "string (required)",
  "companyId": "long (required)"
}
```

> **`companyId`**: For a regular user, must match their own `User.CompanyId` - otherwise `401 InvalidCredentials`. For a Super Admin (`*.*.*` wildcard permission), selects which active company to act as for this session - `401` if the company doesn't exist or is inactive. The JWT's `company_id` claim reflects this acting company, and it sticks for the lifetime of the refresh token (switching companies requires a fresh login).

#### LoginResponse
```json
{
  "accessToken": "string (JWT token)",
  "refreshToken": "string",
  "expiresIn": "integer (seconds)",
  "tokenType": "Bearer"
}
```

#### RefreshRequest
```json
{
  "refreshToken": "string (required)"
}
```

#### LogoutRequest
```json
{
  "refreshToken": "string (required)"
}
```

#### ChangePasswordRequest (auth)
```json
{
  "currentPassword": "string (required)",
  "newPassword": "string (required, min 8 characters)",
  "refreshToken": "string (optional) - if provided and it matches the caller's active refresh token, that token is excluded from the post-change revocation so the current session survives"
}
```

> Self-service password change. Verifies `currentPassword` against the caller's stored hash (`401` if it doesn't match), then hashes and persists `newPassword`. Always acts on the caller's own account - there is no `{id}` route param. Revokes all other refresh tokens for the account.

#### MeResponse
```json
{
  "id": "long",
  "email": "string",
  "fullname": "string",
  "isActive": "boolean",
  "hasGlobalAccess": "boolean",
  "companyId": "long",
  "companyName": "string",
  "companyCode": "string",
  "roles": ["string"],
  "permissions": ["string (format: module.resource.action)"],
  "permissionMap": {
    "module": {
      "resource": ["action1", "action2"]
    }
  },
  "warehouses": [
    {
      "id": "long",
      "code": "string",
      "name": "string",
      "location": "string | null",
      "isPrimary": "boolean"
    }
  ],
  "scopes": [
    {
      "id": "long",
      "name": "string (UPPER, e.g. LAMPUNG)",
      "display": "string (proper case, e.g. Lampung)"
    }
  ],
  "createdAt": "datetime"
}
```

> **`companyId`/`companyName`/`companyCode`**: The *acting* company for this session (the one selected at login), not necessarily the user's home company row. Only differs for a Super Admin who logged in as a company other than their own.
>
> **`hasGlobalAccess`**: `true` if the user holds any role with `GlobalAccess = true` (e.g. HO_SPV, FINANCE_USER, VIEWER). Global-access users see all warehouses in their company - hide the warehouse selector for them or show it as informational only.
>
> **`warehouses`**: List of warehouses the user is explicitly assigned to, sorted with primary warehouse first. Empty for global-access roles. Use this list to populate the warehouse selector dropdown on login.
>
> **`scopes`**: The user's coarse-grain province scope (independent of `warehouses`, see [Province & Region Scoping](#province--region-scoping)). A province listed here grants every warehouse in that province, not just the ones listed in `warehouses`. Empty for global-access roles (they see everything regardless).
>
> **Frontend usage:** Call `GET /api/v1/auth/me` immediately after login and after each token refresh. Store the result in app state (Redux/Zustand/Pinia) and use `permissionMap` for UI decisions (show/hide menus, enable/disable buttons). Example check: `permissionMap["user"]["role"].includes("read")`.
>
> **Why not use JWT claims?** The JWT carries only identity + roles. Permissions are always fresh from `/me` - avoids stale UI state and keeps token size small.

---

## Users

Base route: `/api/v1/users`

All endpoints require authentication.

| Method | Endpoint | Permission Required | Description | Request Body | Response |
|--------|----------|---------------------|-------------|--------------|----------|
| GET | `/api/v1/users` | `user.user.read` | List users with search, sorting, and pagination | Query: [datatable params](#datatable-query-parameters) | Paginated [`UserResponse`](#userresponse) |
| GET | `/api/v1/users/{id}` | `user.user.read` | Get user by ID | - | [`UserResponse`](#userresponse) |
| POST | `/api/v1/users` | `user.user.create` | Create a new user | [`CreateUserRequest`](#createuserrequest) | [`UserResponse`](#userresponse) |
| PUT | `/api/v1/users/{id}` | `user.user.update` | Update user information | [`UpdateUserRequest`](#updateuserrequest) | [`UserResponse`](#userresponse) |
| DELETE | `/api/v1/users/{id}` | `user.user.delete` | Delete user (soft delete) | - | Success message |
| POST | `/api/v1/users/{id}/password` | `user.user.reset_password` | Admin reset of a user's password (does not require the target's current password). Revokes all of the target's refresh tokens. Rate-limited: 10/min per IP. | [`ResetPasswordRequest`](#resetpasswordrequest) | Success message |
| POST | `/api/v1/users/{id}/roles/{roleId}` | `user.role.create` | Assign role to user | - | Success message |
| DELETE | `/api/v1/users/{id}/roles/{roleId}` | `user.role.delete` | Remove role from user | - | Success message |
| POST | `/api/v1/users/{id}/warehouses/{warehouseId}?isPrimary={bool}` | `user.warehouse.create` | Assign warehouse to user | - | Success message |
| DELETE | `/api/v1/users/{id}/warehouses/{warehouseId}` | `user.warehouse.delete` | Remove warehouse from user | - | Success message |

> **Province scope has no dedicated endpoint.** Unlike warehouses (fine grain, managed via the `POST`/`DELETE .../warehouses/...` endpoints above), a user's province scope (coarse grain) is set through the `provinceIds` field on `POST /users` and `PUT /users/{id}`. On update, `provinceIds` fully replaces the existing set (omit = leave untouched, `[]` = clear). See [`CreateUserRequest`](#createuserrequest) / [`UpdateUserRequest`](#updateuserrequest).

**Search fields:** `email`, `fullname`, `employeeId`

**Sortable fields:** `email`, `fullname`, `employeeId`, `isActive`, `createdAt` (default)

**Example:** `GET /api/v1/users?search=john&sortBy=email&sortOrder=asc&page=1&limit=20`

### User Permission Management

| Method | Endpoint | Permission Required | Description | Request Body | Response |
|--------|----------|---------------------|-------------|--------------|----------|
| GET | `/api/v1/users/{id}/permissions` | `user.permission.read` | List all permission overrides for a user | - | [`UserPermissionOverrideResponse[]`](#userpermissionoverrideresponse) |
| POST | `/api/v1/users/{id}/permissions/{permissionId}/grant` | `user.permission.create` | Grant an extra permission to a user | [`UserPermissionOverrideRequest`](#userpermissionoverriderequest) | Success message |
| POST | `/api/v1/users/{id}/permissions/{permissionId}/deny` | `user.permission.create` | Explicitly deny a permission for a user | [`UserPermissionOverrideRequest`](#userpermissionoverriderequest) | Success message |
| DELETE | `/api/v1/users/{id}/permissions/{permissionId}` | `user.permission.delete` | Remove a user-level permission override | - | Success message |
| GET | `/api/v1/users/{id}/permissions/effective` | `user.permission.read` | Get all effective permissions for a user | - | [`EffectivePermissionResponse[]`](#effectivepermissionresponse) |

### DTOs

#### CreateUserRequest
```json
{
  "email": "string (required, unique)",
  "password": "string (required)",
  "fullname": "string (required)",
  "employeeId": "string (optional)",
  "warehouseIds": "long[] (optional) - List of warehouse IDs to assign (fine grain: each pins that one warehouse)",
  "primaryWarehouseId": "long (optional) - Must be present in warehouseIds",
  "provinceIds": "long[] (optional) - Province scope (coarse grain: grants every warehouse in each province). Independent of and additive with warehouseIds. 404 if any province ID doesn't exist."
}
```

> The new user's `companyId` always comes from the caller's tenant context (the acting company from their JWT) - there is no way to create a user in a different company via this endpoint, including for Super Admin. To create a user under another company, log in as that company first (or use `POST /api/v1/companies/{companyId}/users/{userId}` to move an existing user afterward).

#### UpdateUserRequest
```json
{
  "fullname": "string (optional)",
  "employeeId": "string (optional)",
  "isActive": "boolean (optional)",
  "provinceIds": "long[] (optional) - Replaces the user's province scope. Omit/null = leave scope untouched; [] = clear all provinces; [ids] = replace with this set. 404 if any province ID doesn't exist."
}
```

#### ResetPasswordRequest
```json
{
  "newPassword": "string (required, min 8 characters)"
}
```

#### UserResponse
```json
{
  "id": "long",
  "email": "string",
  "fullname": "string",
  "employeeId": "string",
  "isActive": "boolean",
  "createdAt": "datetime",
  "roles": [
    {
      "roleId": "long",
      "roleName": "string",
      "displayName": "string"
    }
  ],
  "warehouses": [
    {
      "warehouseId": "long",
      "code": "string",
      "name": "string",
      "isPrimary": "boolean"
    }
  ],
  "scopes": [
    {
      "provinceId": "long",
      "name": "string (UPPER, e.g. LAMPUNG)",
      "display": "string (proper case, e.g. Lampung)"
    }
  ]
}
```

> `warehouses` are fine-grain pins; `scopes` is the coarse-grain province scope. They are independent - a province in `scopes` grants every warehouse in it, a warehouse pin grants only that one.

#### UserPermissionOverrideRequest
```json
{
  "expiresAt": "datetime (optional)",
  "reason": "string (optional)"
}
```

#### UserPermissionOverrideResponse
```json
{
  "permissionId": "long",
  "module": "string",
  "resource": "string",
  "action": "string",
  "isGranted": "boolean",
  "grantedBy": "long",
  "grantedAt": "datetime",
  "expiresAt": "datetime (nullable)",
  "reason": "string (nullable)"
}
```

#### EffectivePermissionResponse
```json
{
  "permissionId": "long",
  "permission": "string (format: module.resource.action)",
  "granted": "boolean",
  "source": "string (role | user_grant | user_deny)",
  "roleName": "string (nullable)",
  "reason": "string (nullable)",
  "expiresAt": "datetime (nullable)"
}
```

---

## Roles

Base route: `/api/v1/roles`

All endpoints require authentication.

| Method | Endpoint | Permission Required | Description | Request Body | Response |
|--------|----------|---------------------|-------------|--------------|----------|
| GET | `/api/v1/roles` | `user.role.read` | List roles with search, sorting, and pagination | Query: [datatable params](#datatable-query-parameters) | Paginated [`RoleResponse`](#roleresponse) |
| GET | `/api/v1/roles/{id}` | `user.role.read` | Get role by ID | - | [`RoleResponse`](#roleresponse) |
| POST | `/api/v1/roles` | `user.role.create` | Create a new role (with optional permissions) | [`CreateRoleRequest`](#createrolerequest) | [`RoleResponse`](#roleresponse) |
| PUT | `/api/v1/roles/{id}` | `user.role.update` | Update role information | [`UpdateRoleRequest`](#updaterolerequest) | [`RoleResponse`](#roleresponse) |
| DELETE | `/api/v1/roles/{id}` | `user.role.delete` | Delete role | - | Success message |
| PUT | `/api/v1/roles/{id}/permissions` | `user.role.update` | Sync (replace) all permissions for a role | [`SyncPermissionsRequest`](#syncpermissionsrequest) | Success message |
| POST | `/api/v1/roles/{id}/permissions/{permissionId}` | `user.role.update` | Assign single permission to role | - | Success message |
| DELETE | `/api/v1/roles/{id}/permissions/{permissionId}` | `user.role.update` | Remove single permission from role | - | Success message |

**Search fields:** `name`, `displayName`, `description`

**Sortable fields:** `name` (default), `displayName`, `isSystem`, `globalAccess`, `createdAt`

**Example:** `GET /api/v1/roles?search=warehouse&sortBy=name&page=1&limit=10`

### DTOs

#### CreateRoleRequest
```json
{
  "name": "string (required, unique)",
  "displayName": "string (optional)",
  "description": "string (optional)",
  "globalAccess": "boolean (default: false)",
  "permissionIds": "[long[] (optional)] - assign permissions atomically on create"
}
```

#### SyncPermissionsRequest
```json
{
  "permissionIds": "[long[]] - full desired set; missing IDs are removed, new IDs are added"
}
```

> **FE usage for edit form:** send the full checked set. Pass `[]` to clear all permissions.
> System roles (`isSystem: true`) will return `403`.

#### UpdateRoleRequest
```json
{
  "displayName": "string (optional)",
  "description": "string (optional)",
  "globalAccess": "boolean (optional)",
  "permissionIds": "[long[] (optional)] - if present, syncs permissions; if omitted, permissions unchanged"
}
```

#### RoleResponse
```json
{
  "id": "long",
  "name": "string",
  "displayName": "string",
  "description": "string",
  "isSystem": "boolean",
  "globalAccess": "boolean",
  "createdAt": "datetime",
  "permissions": [
    {
      "id": "long",
      "module": "string",
      "resource": "string",
      "action": "string",
      "description": "string"
    }
  ]
}
```

---

## Permissions

Base route: `/api/v1/permissions`

All endpoints require authentication.

| Method | Endpoint | Permission Required | Description | Request Body | Response |
|--------|----------|---------------------|-------------|--------------|----------|
| GET | `/api/v1/permissions` | `user.permission.read` | List all permissions | - | [`PermissionInfo[]`](#permissioninfo) |

### DTOs

#### PermissionInfo
```json
{
  "id": "long",
  "module": "string",
  "resource": "string",
  "action": "string",
  "description": "string"
}
```

**Permission Format:** `{module}.{resource}.{action}`

---

## Companies

Base route: `/api/v1/companies`

| Method | Endpoint | Permission Required | Description | Request Body | Response |
|--------|----------|---------------------|-------------|--------------|----------|
| GET | `/api/v1/companies/public` | No | Public list of active companies (for login dropdown) | Query (optional): `code` | [`CompanyPublicResponse[]`](#companypublicresponse) |
| GET | `/api/v1/companies` | `system.company.read` | List companies with search, sorting, and pagination | Query: [datatable params](#datatable-query-parameters) | Paginated [`CompanyResponse`](#companyresponse) |
| GET | `/api/v1/companies/{id}` | `system.company.read` | Get company by ID | - | [`CompanyResponse`](#companyresponse) |
| POST | `/api/v1/companies` | `system.company.create` | Create a new company | [`CreateCompanyRequest`](#createcompanyrequest) | [`CompanyResponse`](#companyresponse) |
| PUT | `/api/v1/companies/{id}` | `system.company.update` | Update company information | [`UpdateCompanyRequest`](#updatecompanyrequest) | [`CompanyResponse`](#companyresponse) |
| DELETE | `/api/v1/companies/{id}` | `system.company.delete` | Soft-deactivate a company | - | Success message |
| POST | `/api/v1/companies/{companyId}/users/{userId}` | `system.company.assign` | Move a user to a different company | - | Success message |
| GET | `/api/v1/companies/{id}/logo` | No | Fetch the company logo image bytes. Returns `404` if no logo is set. | - | Image (`image/png`, `image/jpeg`, or `image/webp`) |
| PUT | `/api/v1/companies/{id}/logo` | Auth required | Upload or replace the company logo. `multipart/form-data`. Max 2 MB. Accepted types: `image/png`, `image/jpeg`, `image/webp`. File signature is validated against the declared content type. | `file` (form field) | Success message |
| DELETE | `/api/v1/companies/{id}/logo` | Auth required | Remove the company logo | - | `204 No Content` |

**Search fields:** `code`, `name`, `email`, `phone`

**Sortable fields:** `name` (default), `code`, `isActive`, `createdAt`

**Example:** `GET /api/v1/companies?search=PT&sortBy=code&sortOrder=asc&page=1&limit=20`

**Public endpoint examples:**
- `GET /api/v1/companies/public`
- `GET /api/v1/companies/public?code=GCU`

> Note: `/api/v1/companies/public` is unauthenticated. Data shape is `CompanyPublicResponse` (`id`, `code`, `name` only - no sensitive fields). Supports optional exact-match filtering via `?code=...`. Does not support datatable params. Response follows the standard success envelope with `requestId`.

**Logo endpoint access rules:**
- `GET /{id}/logo`: public, no auth required. Safe to use as `<img src>` directly. Returns `404` if no logo is set.
- `PUT` / `DELETE`: caller's `company_id` claim must match `{id}`, otherwise `403` - including Super Admin, who must log in as the target company first to manage its logo.
- `PUT /{id}/logo` replaces any existing logo atomically - the old file is deleted from storage after the new one is saved.
- Controller-level pre-checks (missing file, size, content type) return `400`. Service-level signature mismatch returns `422`.

### DTOs

#### CreateCompanyRequest
```json
{
  "code": "string (required, unique)",
  "name": "string (required)",
  "address": "string (optional)",
  "phone": "string (optional)",
  "email": "string (optional)"
}
```

#### UpdateCompanyRequest
```json
{
  "name": "string (optional)",
  "address": "string (optional)",
  "phone": "string (optional)",
  "email": "string (optional)",
  "isActive": "boolean (optional)"
}
```

#### CompanyResponse
```json
{
  "id": "long",
  "code": "string",
  "name": "string",
  "address": "string",
  "phone": "string",
  "email": "string",
  "isActive": "boolean",
  "createdAt": "datetime",
  "userCount": "integer",
  "warehouseCount": "integer",
  "hasLogo": "boolean - true if a logo is stored; fetch it via GET /api/v1/companies/{id}/logo"
}
```

#### CompanyPublicResponse
```json
{
  "id": "long",
  "code": "string",
  "name": "string"
}
```

---

## Warehouses

Base route: `/api/v1/warehouses`

All endpoints require authentication. Warehouses are **read-only** - master data is synced automatically from the ERP. The API provides scoped access based on the user's warehouse assignments.

| Method | Endpoint | Permission Required | Description | Request Body | Response |
|--------|----------|---------------------|-------------|--------------|----------|
| GET | `/api/v1/warehouses` | `user.warehouse.read` | List warehouses with search, sorting, and pagination (filtered by user's warehouse access). Accepts optional `provinceId` query param to scope to a single province. | Query: [datatable params](#datatable-query-parameters) + `provinceId` | Paginated [`WarehouseResponse`](#warehouseresponse) |
| GET | `/api/v1/warehouses/{id}` | `user.warehouse.read` | Get warehouse by ID | - | [`WarehouseResponse`](#warehouseresponse) |
| GET | `/api/v1/warehouses/locations` | `user.warehouse.read` | Return the provinces visible to the caller (global-access users get every active province). Used to populate the province picker in Budget Template and Warehouse list filters. | - | [`LocationListResponse`](#locationlistresponse) |
| GET | `/api/v1/warehouses/unmapped` | `user.warehouse.read` | List active warehouses whose ERP `location` text didn't match a known province. Global access only. | - | `ApiResponse<List<WarehouseResponse>>` |

**Search fields:** `code`, `name`, `location`

**Sortable fields:** `code` (default), `name`, `location`, `isActive`, `syncedAt`

**Additional query parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `provinceId` | long | Filter warehouses to a single province by ID (from `GET /warehouses/locations`). Returns an empty list if no warehouses belong to that province. |

**Example:** `GET /api/v1/warehouses?search=jakarta&sortBy=name&page=1&limit=5`

**Example (province filter):** `GET /api/v1/warehouses?provinceId=3`

> Note: Users without global access only see their assigned warehouses - the union of warehouses in their assigned provinces, warehouses they're directly assigned to, and warehouses under the `GLOBAL` province. Search and sort apply within that filtered set.

> A warehouse's province is resolved from its ERP `location` text against the `provinces`/`province_aliases` tables during sync (see [Province & Region Scoping](#province--region-scoping)). Warehouses that don't match any province get `provinceId: null` and surface only to global-access users, via `GET /warehouses/unmapped`.

### DTOs

#### WarehouseResponse
```json
{
  "id": "long",
  "code": "string",
  "name": "string",
  "location": "string (nullable) - raw ERP location text, independent of provinceId",
  "isActive": "boolean",
  "firstSeenAt": "datetime",
  "syncedAt": "datetime",
  "provinceId": "long (nullable) - resolved province id; null when the ERP location could not be matched",
  "provinceName": "string (nullable) - normalized UPPER province name, e.g. 'LAMPUNG'",
  "provinceDisplay": "string (nullable) - proper-case province name for UI, e.g. 'Lampung'"
}
```

#### LocationListResponse
```json
{
  "locations": [
    { "id": "long", "name": "string (normalized UPPER, matching key)", "display": "string (proper case, for UI)" }
  ]
}
```

> Returned under `$.data.locations`. Sorted alphabetically by province name. Global-access users get every active province; other users get only the provinces they can access.

---

## Sync

Base route: `/api/v1/sync`

All endpoints require authentication. These endpoints allow administrators to manually trigger ERP master data synchronization on demand and query historical run logs. The background scheduler runs automatically on a variable interval: fast (default 5 min) inside a configurable peak window (default weekdays 08:00–17:00 WIB), slow (default 60 min) outside it - all six settings (`ErpApi:SyncIntervalMinutesPeak`, `SyncIntervalMinutes`, `SyncPeakWindowStartHour`, `SyncPeakWindowEndHour`, `SyncPeakWeekdaysOnly`, `SyncPeakTimeZoneId`) are configurable - see [Background Schedulers](README.md#background-schedulers) in the README.

| Method | Endpoint | Permission Required | Description | Request Body | Response |
|--------|----------|---------------------|-------------|--------------|----------|
| POST | `/api/v1/sync/trigger` | `system.sync.execute` | Trigger full sync for all registered sync services | - | `SyncResult[]` |
| POST | `/api/v1/sync/trigger/{serviceName}` | `system.sync.execute` | Trigger a specific sync service by name | - | `SyncResult` |
| GET | `/api/v1/sync/logs` | `system.sync.read` | Paginated history of all sync runs, filterable by service/company/outcome/date | Query params | `PaginatedResponse<SyncLogResponse>` |
| GET | `/api/v1/sync/logs/latest` | `system.sync.read` | Latest run per `(ServiceName, CompanyCode)` - for dashboard health cards | - | `SyncLogLatestResponse[]` |

**Available service names:** `WarehouseSync`, `VendorSync`, `ItemSync`, `SpkSync`, `ToSync`, `PpnSync`

> A successful `WarehouseSync` run (manual or scheduled) invalidates the `warehouse-shadows` cache tag, so `GET /warehouses`, `GET /warehouses/{id}`, and `GET /warehouses/locations` reflect the new data immediately - no TTL wait. See [Caching](#caching). A successful `PpnSync` run similarly invalidates the `tax-types` and `rate-cards` tags.

> **PPh is not in this list.** Unlike PPN (`PpnSync`, a scheduled per-company sync like the others above), PPh master data is vendor-scoped and fetched **on-demand only**, via [`GET /rate-cards/vendors/{vendorId}/pph`](#rate-cards) - there's no bulk scheduled job for it. Enumerating PPh for every vendor on a timer would mean one SAP call per vendor per tick regardless of whether anyone ever opens that vendor's Rate Card; fetching it live only when a Rate Card is actually opened for that vendor scales with real usage instead.

**Examples:**
- `POST /api/v1/sync/trigger/WarehouseSync`
- `GET /api/v1/sync/logs?serviceName=WarehouseSync&outcome=Success&page=1&limit=20`
- `GET /api/v1/sync/logs/latest`

**`GET /api/v1/sync/logs` query parameters** (all optional, extend standard `page`/`limit`):

| Parameter | Type | Description |
|-----------|------|-------------|
| `serviceName` | string | Filter by service name (e.g. `WarehouseSync`) |
| `companyCode` | string | Filter by company code |
| `outcome` | string | One of `Success`, `ErpUnavailable`, `SchemaError`, `Exception` |
| `dateFrom` | datetime | Include runs where `StartedAt >= dateFrom` |
| `dateTo` | datetime | Include runs where `StartedAt <= dateTo` |
| `page` | integer | Page number, default `1` |
| `limit` | integer | Page size, default `20` |

### DTOs

#### SyncResult
```json
{
  "serviceName": "string",
  "success": "boolean",
  "added": "integer",
  "updated": "integer",
  "deactivated": "integer",
  "skipped": "integer",
  "errorMessage": "string (nullable)"
}
```

#### SyncLogResponse
```json
{
  "id": "long",
  "serviceName": "string",
  "companyCode": "string",
  "startedAt": "datetime",
  "finishedAt": "datetime (nullable)",
  "outcome": "string - Success | ErpUnavailable | SchemaError | Exception",
  "added": "integer",
  "updated": "integer",
  "deactivated": "integer",
  "abortReason": "string (nullable)",
  "durationMs": "float (nullable - null when FinishedAt not yet set)"
}
```

#### SyncLogLatestResponse
```json
{
  "serviceName": "string",
  "companyCode": "string",
  "startedAt": "datetime",
  "finishedAt": "datetime (nullable)",
  "outcome": "string - Success | ErpUnavailable | SchemaError | Exception",
  "added": "integer",
  "updated": "integer",
  "deactivated": "integer",
  "abortReason": "string (nullable)",
  "durationMs": "float (nullable)"
}
```

> `skipped` (in `SyncResult`) counts companies for which the ERP returned no data (e.g., ERP down). Existing records for those companies are left unchanged - never deactivated on a failed ERP call.
>
> Every sync run (per company) is recorded in `sync_logs` with start/end time, row counts, outcome, and abort reason. `GET /sync/logs` results are ordered `StartedAt DESC`. `GET /sync/logs/latest` returns one row per `(ServiceName, CompanyCode)` ordered `ServiceName ASC, CompanyCode ASC` - suitable for a health dashboard. The scheduler also warns in logs when a service has not succeeded within 2× the configured interval.

---

## Items

Base route: `/api/v1/items`

All endpoints require authentication. Items are **read-only** - master data is synced automatically from the ERP.

| Method | Endpoint | Permission Required | Description | Request Body | Response |
|--------|----------|---------------------|-------------|--------------|----------|
| GET | `/api/v1/items` | `budget.item.read` | List items with search, sorting, and pagination | Query: [datatable params](#datatable-query-parameters) | Paginated [`ItemSummaryResponse`](#itemsummaryresponse) |
| GET | `/api/v1/items/{id}` | `budget.item.read` | Get item by ID | - | [`ItemSummaryResponse`](#itemsummaryresponse) |

**Search fields:** `itemCode`, `itemName`, `acctCode`

**Sortable fields:** `itemCode` (default), `itemName`, `acctCode`, `isActive`

**Example:** `GET /api/v1/items?search=bolt&sortBy=itemName&page=1&limit=20`

### DTOs

#### ItemSummaryResponse
```json
{
  "id": "long",
  "itemCode": "string",
  "itemName": "string",
  "acctCode": "string",
  "acctName": "string"
}
```

---

## Vendors

Base route: `/api/v1/vendors`

All endpoints require authentication. Vendors are **read-only** - master data is synced automatically from the ERP.

| Method | Endpoint | Permission Required | Description | Request Body | Response |
|--------|----------|---------------------|-------------|--------------|----------|
| GET | `/api/v1/vendors` | `budget.vendor.read` | List vendors with search, sorting, and pagination | Query: [datatable params](#datatable-query-parameters) | Paginated [`VendorSummaryResponse`](#vendorsummaryresponse) |
| GET | `/api/v1/vendors/{id}` | `budget.vendor.read` | Get vendor by ID | - | [`VendorSummaryResponse`](#vendorsummaryresponse) |

**Search fields:** `cardCode`, `cardName`

**Sortable fields:** `cardCode` (default), `cardName`, `isActive`

**Example:** `GET /api/v1/vendors?search=supplier&sortBy=cardName&page=1&limit=20`

### DTOs

#### VendorSummaryResponse
```json
{
  "id": "long",
  "cardCode": "string",
  "cardName": "string"
}
```

---

## UoMs

Base route: `/api/v1/uoms`

All endpoints require authentication. UoMs can be managed via API (create/update/delete) or synced from ERP.

| Method | Endpoint | Permission Required | Description | Request Body | Response |
|--------|----------|---------------------|-------------|--------------|----------|
| GET | `/api/v1/uoms` | `budget.uom.read` | List all UoMs | Query: `activeOnly` (bool, default: true) | [`UomResponse[]`](#uomresponse) |
| GET | `/api/v1/uoms/{id}` | `budget.uom.read` | Get UoM by ID | - | [`UomResponse`](#uomresponse) |
| POST | `/api/v1/uoms` | `budget.uom.create` | Create a new UoM - returns `201 Created` | [`CreateUomRequest`](#createuomrequest) | [`UomResponse`](#uomresponse) |
| PUT | `/api/v1/uoms/{id}` | `budget.uom.update` | Update a UoM | [`UpdateUomRequest`](#updateuomrequest) | [`UomResponse`](#uomresponse) |
| DELETE | `/api/v1/uoms/{id}` | `budget.uom.delete` | Delete a UoM | - | Success message |

### DTOs

#### UomResponse
```json
{
  "id": "long",
  "code": "string",
  "name": "string",
  "isActive": "boolean"
}
```

#### CreateUomRequest
```json
{
  "code": "string (required, unique)",
  "name": "string (required)"
}
```

#### UpdateUomRequest
```json
{
  "name": "string (optional)",
  "isActive": "boolean (optional)"
}
```

---

## Tax Types

Base route: `/api/v1/tax-types`

All endpoints require authentication. Tax types are Indonesia's **PPN** (VAT, added on top of a cost) and **PPh** (withholding tax, subtracted from a payment) master codes, sourced from the client's SAP B1 API and mirrored locally. Rate Card items reference these by ID to opt a line into tax. See [Tax Calculation (PPN & PPh)](README.md#tax-calculation-ppn--pph) in the README for a full plain-language explanation of how PPN/PPh amounts get computed and flow downstream into Budget Plans and Purchase Orders.

Tax types are **per-company** (`CompanyId`, unique with `Category` + `Code`) - SAP's PPN/PPh master data is scoped per SAP company database, so a code can mean different (or nonexistent) things in a different company.

**This table is read-only via the API.** There is no `POST`/`PUT`/`DELETE` - SAP is the sole source of truth, mirrored in by two independent background/on-demand processes (see [Sync](#sync) and below). Manually creating or editing a tax type here would just get overwritten (PPN) or silently ignored (PPh, which re-derives from SAP on every relevant request).

| Method | Endpoint | Permission Required | Description | Request Body | Response |
|--------|----------|---------------------|-------------|--------------|----------|
| GET | `/api/v1/tax-types` | `budget.tax_type.read` | List tax types | Query: `category` (`Ppn` \| `Pph`, optional), `activeOnly` (bool, default: true) | [`TaxTypeResponse[]`](#taxtyperesponse) |
| GET | `/api/v1/tax-types/{id}` | `budget.tax_type.read` | Get tax type by ID | - | [`TaxTypeResponse`](#taxtyperesponse) |

> **Business Rules:**
> - `GET /api/v1/tax-types?category=Ppn` feeds the "PPN" dropdown on the Rate Card form; `?category=Pph` feeds the "PPh" dropdown. Only active (`isActive: true`) rows should be shown to users picking a *new* selection.
> - `rate` is a **percentage**, not a fraction - `11.00` means 11%, not 1100% or 0.11.
> - **PPN rows** come from `PpnSyncService`, a scheduled background sync (`GET /WAMS/PPn?Company=` per company, same cadence as Vendor/Item/Warehouse/SPK sync - see [Sync](#sync)). A code SAP stops returning gets deactivated (`isActive: false`), never hard-deleted; a returned code with a changed name/rate gets updated in place going forward. Existing rate cards, budget plans, and purchase orders that already reference a since-changed or since-deactivated code keep computing exactly as before - only the dropdown for *new* selections is affected.
> - **PPh rows** (and the vendor-to-code assignments behind them) come from `PphLookupService`, called on-demand - see [`GET /rate-cards/vendors/{vendorId}/pph`](#rate-cards) below. There is no scheduled PPh sync; a vendor's PPh data is only ever as fresh as the last time someone opened that vendor's Rate Card.
> - The 4 rows this table originally shipped with (`PPN0`, `PPN11`, `PPH22`, `PPH23`) were hand-entered placeholders that never matched SAP's real codes/rates - they've been deactivated. Real PPN codes look like `PPNin0`/`PPNin11`; real PPh codes look like `P23c`/`P21a`.

### DTOs

#### TaxTypeResponse
```json
{
  "id": "long",
  "category": "string (Ppn | Pph)",
  "code": "string (e.g. PPNin11, P23c - the real SAP code)",
  "name": "string (e.g. \"PPn In 11%\", \"Hutang PPH Pasal 23 - 2\")",
  "rate": "decimal (percentage, e.g. 11.00 for 11%)",
  "isActive": "boolean"
}
```

---

## Rate Cards

Base route: `/api/v1/rate-cards`

All endpoints require authentication. Rate cards define vendor-specific pricing for items/materials. They follow a lifecycle: Draft → Submitted.

| Method | Endpoint | Permission Required | Description | Request Body | Response |
|--------|----------|---------------------|-------------|--------------|----------|
| GET | `/api/v1/rate-cards` | `budget.rate_card.read` | List rate cards with search, sorting, and pagination | Query: [datatable params](#datatable-query-parameters) + `status`, `vendorId` | Paginated [`RateCardSummaryResponse`](#ratecardsummaryresponse) |
| GET | `/api/v1/rate-cards/{id}` | `budget.rate_card.read` | Get rate card by ID with all items | - | [`RateCardResponse`](#ratecardresponse) |
| GET | `/api/v1/rate-cards/by-item/{itemShadowId}` | `budget.rate_card.read` | Get all vendors with a submitted rate for a given item. Used by Budget Plan form to populate vendor dropdowns. | - | [`VendorRateResponse[]`](#vendorrateresponse) |
| GET | `/api/v1/rate-cards/vendors/{vendorId}/pph` | `budget.rate_card.read` | Returns SAP's currently-assigned PPh (withholding tax) codes for one vendor, refreshing from SAP **live on every call** (no caching/TTL - see [Tax Types](#tax-types)). Call this right after the admin picks a vendor on the Rate Card form, before entering line items, to pre-select a default PPh code. The admin can still pick a different PPh type per line - this only supplies the default. | - | [`TaxTypeResponse[]`](#taxtyperesponse) |
| POST | `/api/v1/rate-cards` | `budget.rate_card.create` | Create a new rate card (in Draft status) | [`CreateRateCardRequest`](#createratecardrequest) | [`RateCardResponse`](#ratecardresponse) |
| POST | `/api/v1/rate-cards/submit` | `budget.rate_card.create` + `budget.rate_card.submit` | Create and immediately submit a rate card in one atomic operation | [`CreateRateCardRequest`](#createratecardrequest) | [`RateCardResponse`](#ratecardresponse) |
| PUT | `/api/v1/rate-cards/{id}` | `budget.rate_card.update` | Update a draft rate card | [`UpdateRateCardRequest`](#updateratecardrequest) | [`RateCardResponse`](#ratecardresponse) |
| POST | `/api/v1/rate-cards/{id}/submit` | `budget.rate_card.submit` | Submit an existing draft rate card | - | [`RateCardResponse`](#ratecardresponse) |
| DELETE | `/api/v1/rate-cards/{id}` | `budget.rate_card.delete` | Soft-delete a draft rate card | - | `204 No Content` |

> **`GET /rate-cards/vendors/{vendorId}/pph` failure behavior:** if the live SAP call fails (timeout, 5xx, network error), this endpoint falls back to whatever PPh data is already persisted for that vendor from a previous successful call, rather than erroring out - it never blocks the Rate Card form. If SAP has never been reached for that vendor, this returns an empty array. A successful call is always treated as fully authoritative, even when it returns an empty array (SAP saying "this vendor has no PPh liability" replaces whatever was persisted before, including deactivating stale assignments).

**Search fields:** Vendor `cardName`

**Sortable fields:** `status`, `createdAt` (default), `submittedAt`

**Additional query parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `status` | `Draft` \| `Submitted` | Filter by rate card status |
| `vendorId` | long | Filter by vendor ID |

**Example:** `GET /api/v1/rate-cards?status=Draft&vendorId=1&sortBy=createdAt&sortOrder=desc&page=1&limit=20`

> **Business Rules:**
> - Only draft rate cards can be updated, submitted, or deleted
> - Rate cards must have at least one item before submission
> - Items are fully replaced on update (not merged)
> - Rate cards are scoped to the authenticated user's company
> - Soft-deleted rate cards are excluded from queries
> - `POST /submit` creates and submits atomically in a single DB commit - use this when the user clicks "Submit" directly from the create form

### DTOs

#### RateCardSummaryResponse
```json
{
  "id": "long",
  "vendor": {
    "id": "long",
    "cardCode": "string",
    "cardName": "string"
  },
  "status": "string (Draft | Submitted)",
  "itemCount": "integer",
  "createdAt": "datetime"
}
```

#### RateCardResponse
```json
{
  "id": "long",
  "vendor": {
    "id": "long",
    "cardCode": "string",
    "cardName": "string"
  },
  "status": "string (Draft | Submitted)",
  "items": [
    {
      "id": "long",
      "item": {
        "id": "long",
        "itemCode": "string",
        "itemName": "string",
        "acctCode": "string"
      },
      "uom": {
        "id": "long",
        "code": "string",
        "name": "string",
        "isActive": "boolean"
      },
      "costValue": "decimal",
      "ppnTaxType": {
        "id": "long",
        "code": "string",
        "rate": "decimal"
      },
      "pphTaxType": {
        "id": "long",
        "code": "string",
        "rate": "decimal"
      },
      "costTreatment": "string (nullable) - \"Dibiayakan\" | \"TidakDibiayakan\" | null"
    }
  ],
  "createdAt": "datetime",
  "submittedAt": "datetime (nullable)"
}
```

> `ppnTaxType`/`pphTaxType` are nullable - `null` means this line has no PPN/PPh respectively. When present, they are a **slim snapshot** object (`RateCardItemTaxResponse`: `{ id, code, rate }`) - not the full [`TaxTypeResponse`](#taxtyperesponse). `category`, `name`, and `isActive` are intentionally omitted here: `RateCardItem` no longer holds a live foreign key to `TaxType`, so there's nothing to join against for those fields (see [Tax Module: PPN & PPh](README.md#tax-module-ppn--pph) in the README). See [Tax Calculation (PPN & PPh)](README.md#tax-calculation-ppn--pph) for what these drive downstream.
>
> **`rate` here is a snapshot, not a live value.** It's captured on the Rate Card item at the moment the tax type was selected (on Create, or on the most recent Update - every `PUT` fully replaces items and re-snapshots). A tax type's `rate` can never be edited after creation (see [Tax Types](#tax-types)), so this number only ever changes if the rate card item itself is re-saved against a different tax type. This is what Budget Plans actually calculate from when generated off this rate card item, so what you see here is exactly what a new Budget Plan line would use.
>
> **`costTreatment` is a label only.** It records whether this line's tax is `Dibiayakan` (tax becomes company cost) or `TidakDibiayakan` (tax is a pass-through/credit) for accounting/reporting purposes. It does **not** change `costValue`, `ppnTaxType.rate`/`pphTaxType.rate`, or any downstream total - `TaxCalculator` ignores it entirely. `null` means the caller didn't set it. It is captured on `CreateRateCardItemRequest`, then copied verbatim (never recomputed) rate card → budget plan item → purchase order item as a frozen snapshot, exactly like `ppnTaxType`/`pphTaxType`. See [Tax Module: PPN & PPh](README.md#tax-module-ppn--pph) in the README for the full explanation.

#### VendorRateResponse
```json
{
  "vendorShadowId": "long",
  "vendorCode": "string (VendorShadow.CardCode)",
  "vendorName": "string (VendorShadow.CardName)",
  "uomMasterId": "long",
  "uomCode": "string",
  "uomName": "string",
  "costValue": "decimal",
  "ppnTaxType": { "id": "long", "code": "string", "rate": "decimal" },
  "pphTaxType": { "id": "long", "code": "string", "rate": "decimal" },
  "costTreatment": "string (nullable) - \"Dibiayakan\" | \"TidakDibiayakan\" | null"
}
```

> Endpoint returns `VendorRateResponse[]` (array). Each entry is the **most recently submitted** rate for that vendor+item pair. If a vendor submitted multiple rate cards over time, only the latest one appears. Returns an empty array if no submitted rate cards cover this item.
>
> `ppnTaxType`/`pphTaxType` are nullable and, same as in [`RateCardResponse`](#ratecardresponse) above, are the slim `{ id, code, rate }` snapshot - not the full `TaxTypeResponse` - and `rate` is the **snapshotted** rate from the rate card item, not a live re-read of the tax type.
>
> `costTreatment` is the same label-only field described under [`RateCardResponse`](#ratecardresponse) above - copied as-is from the rate card item, no effect on `costValue`.

#### CreateRateCardRequest
```json
{
  "vendorShadowId": "long (required, must reference existing vendor)",
  "items": [
    {
      "itemShadowId": "long (required, must reference existing item)",
      "uomMasterId": "long (required, must reference existing UoM)",
      "costValue": "decimal (required, must be > 0)",
      "ppnTaxTypeId": "long (optional) - must reference a TaxType with category=Ppn; omit/null for no PPN (the default)",
      "pphTaxTypeId": "long (optional) - must reference a TaxType with category=Pph; omit/null for no PPh (the default)",
      "costTreatment": "string (optional, nullable) - \"Dibiayakan\" | \"TidakDibiayakan\"; omit/null when not applicable"
    }
  ]
}
```

#### UpdateRateCardRequest
```json
{
  "vendorShadowId": "long (optional, must reference existing vendor)",
  "items": [
    {
      "itemShadowId": "long (required, must reference existing item)",
      "uomMasterId": "long (required, must reference existing UoM)",
      "costValue": "decimal (required, must be > 0)",
      "ppnTaxTypeId": "long (optional) - see CreateRateCardRequest",
      "pphTaxTypeId": "long (optional) - see CreateRateCardRequest",
      "costTreatment": "string (optional, nullable) - see CreateRateCardRequest"
    }
  ]
}
```

> **Note:** The `items` array in `UpdateRateCardRequest` replaces all existing items. To add or remove individual items, include the complete desired state in the request.
>
> **Tax validation (Create):** on `POST /rate-cards`, `ppnTaxTypeId` must point to an *active* tax type in the `Ppn` category (`404` if the ID doesn't exist, `400` if it's the wrong category or inactive). Same rule for `pphTaxTypeId` with the `Pph` category. A line can have PPN only, PPh only, both, or neither - all four combinations are valid.
>
> **Tax validation (Update) - no deactivation lock:** `PUT /rate-cards/{id}` does **not** re-check that a selected tax type is still active. Any `ppnTaxTypeId`/`pphTaxTypeId` sent on update is accepted as-is (as long as the ID exists and is the right category), even if that tax type was deactivated after the card was created. Only `POST` enforces the active-only rule; `PUT` never did the "deactivation lock" this note used to describe.
>
> **`costTreatment` validation:** always optional; `null`/omitted is always allowed - it is not currently required even when the line has PPN/PPh. The canonical-value check (must be exactly `"Dibiayakan"` or `"TidakDibiayakan"`, else `400`) is enforced on **both** Create / Create-and-Submit (`CreateRateCardRequestValidator`) and Update (`UpdateRateCardRequestValidator`, which shares the same per-item rules via `CreateRateCardItemRequestValidator`) - `PUT /rate-cards/{id}` now validates every item the same way `POST` does, including `ItemShadowId > 0`, `UomMasterId > 0`, `CostValue > 0`, and `costTreatment`.

---

## HTTP Status Codes

| Code | Description |
|------|-------------|
| 200 OK | Request succeeded |
| 201 Created | Resource created successfully. Returned by all `POST` endpoints that create a new resource: `POST /users`, `POST /uoms`, `POST /activity-types`. |
| 204 No Content | Operation succeeded with no response body. Returned by `DELETE /activity-types/{id}`, `DELETE /rate-cards/{id}`, and workflow state transitions. |
| 400 Bad Request | Validation error or malformed request |
| 401 Unauthorized | Authentication required or token invalid |
| 403 Forbidden | Insufficient permissions |
| 404 Not Found | Resource not found |
| 409 Conflict | Resource already exists or business rule conflict |
| 422 Unprocessable Entity | Semantic validation failure (FluentValidation) |
| 500 Internal Server Error | Unexpected server error |

---

## Permission System

The WAMS API uses a granular permission system with the format: `{module}.{resource}.{action}`

### Available Modules

| Module | Description |
|--------|-------------|
| `user` | User management resources |
| `system` | System-wide resources |
| `budget` | Budget and master data (ERP-synced items, vendors) |
| `workflow` | Approval workflow template management |

### Available Resources

| Resource | Module | Description |
|----------|--------|-------------|
| `user` | `user` | User accounts |
| `role` | `user` | User roles |
| `permission` | `user` | Permissions |
| `warehouse` | `user` | Warehouse access and assignments |
| `company` | `system` | Companies |
| `sync` | `system` | ERP master data synchronization |
| `item` | `budget` | ERP-synced items |
| `vendor` | `budget` | ERP-synced vendors |
| `uom` | `budget` | Units of measure |
| `rate_card` | `budget` | Rate cards (vendor item pricing) |
| `tax_type` | `budget` | Tax rate reference entries (PPN/PPh) |
| `template` | `workflow` | Approval matrix / workflow templates |

### Available Actions

| Action | Description |
|--------|-------------|
| `create` | Create new resources or assignments |
| `read` | Read/View resources |
| `update` | Update existing resources |
| `delete` | Delete resources or remove assignments |
| `assign` | Assign resources to other entities |
| `execute` | Execute system operations (e.g. sync trigger) |

### Example Permissions
- `user.user.read` - Read user information
- `user.user.create` - Create new users
- `user.role.update` - Update roles
- `system.company.delete` - Delete companies
- `user.warehouse.read` - View ERP-synced warehouses
- `system.sync.execute` - Trigger manual ERP sync
- `system.sync.read` - View sync run history and health status
- `budget.item.read` - View ERP-synced items
- `budget.vendor.read` - View ERP-synced vendors
- `budget.uom.read` - View UoMs
- `budget.rate_card.read` - View rate cards
- `budget.rate_card.create` - Create rate cards
- `budget.tax_type.read` - View tax types (PPN/PPh reference rates)
- `budget.tax_type.create`, `budget.tax_type.update`, `budget.tax_type.delete` - **removed.** `tax_types` is SAP-synced (see [Tax Types](#tax-types)) and no endpoint ever checked these, so they were deleted from the catalog in migration `PruneOrphanedPermissions`. Nothing to gate UI on - tax types are read-only.
- `budget.rate_card.update` - Update draft rate cards
- `budget.rate_card.submit` - Submit draft rate cards
- `budget.rate_card.delete` - Soft-delete draft rate cards
- `workflow.template.read` - View approval matrix / workflow templates
- `workflow.template.create` - Create new workflow templates
- `workflow.template.update` - Update templates and activate/deactivate them
- `workflow.template.delete` - Hard-delete templates with no active instances

---

## Authentication

The API uses JWT Bearer tokens for authentication. Include the token in the Authorization header:

```
Authorization: Bearer {access_token}
```

Access tokens expire after **15 minutes** (`expiresIn: 900`). Refresh tokens are valid for **7 days**. Use the refresh token endpoint to obtain a new access token without re-authenticating. Re-call `GET /api/v1/auth/me` after each refresh to keep the frontend permission state current.

---

## Activity Types

Base route: `/api/v1/activity-types`

All endpoints require authentication. Activity types are the global master for template categories (e.g. Kegiatan Bongkar, Kegiatan Muat, Fumigasi, Opname). Read access is piggy-backed on `budget.template.read` so any user who can create templates can populate the dropdown. Write access requires `system.activity_type.*` (SUPER_ADMIN only).

| Method | Endpoint | Permission Required | Description | Request Body | Response |
|--------|----------|---------------------|-------------|--------------|----------|
| GET | `/api/v1/activity-types` | `budget.template.read` | List all active activity types | - | [`ActivityTypeResponse[]`](#activitytyperesponse) |
| GET | `/api/v1/activity-types/{id}` | `budget.template.read` | Get activity type by ID | - | [`ActivityTypeResponse`](#activitytyperesponse) |
| POST | `/api/v1/activity-types` | `system.activity_type.create` | Create a new activity type - returns `201 Created` | [`CreateActivityTypeRequest`](#createactivitytyperequest) | [`ActivityTypeResponse`](#activitytyperesponse) |
| PUT | `/api/v1/activity-types/{id}` | `system.activity_type.update` | Update an activity type | [`UpdateActivityTypeRequest`](#updateactivitytyperequest) | [`ActivityTypeResponse`](#activitytyperesponse) |
| DELETE | `/api/v1/activity-types/{id}` | `system.activity_type.delete` | Soft-delete an activity type | - | `204 No Content` |

### DTOs

#### ActivityTypeResponse
```json
{
  "id": "long",
  "code": "string",
  "name": "string",
  "isActive": "boolean"
}
```

#### CreateActivityTypeRequest
```json
{
  "code": "string (required, unique, max 50 chars)",
  "name": "string (required, max 200 chars)"
}
```

#### UpdateActivityTypeRequest
```json
{
  "code": "string (optional, max 50 chars)",
  "name": "string (optional, max 200 chars)",
  "isActive": "boolean (optional)"
}
```

> **Seeded defaults:** `K.BONGKAR` (Kegiatan Bongkar), `K.MUAT` (Kegiatan Muat), `FUMIGASI` (Fumigasi), `OPNAME` (Opname). These are seeded idempotently on startup.

---

## Budget Templates

Base route: `/api/v1/budget-templates`

All endpoints require authentication. Budget templates define the cost structure per activity type per province. They are created by HO SPV and follow a `Draft → Submitted` lifecycle. Once submitted, a template can be used as the cost reference for Budget Plan creation.

Templates are **company-scoped and province-scoped**. Any active user in the company can create and view a single template by ID, but list/export results are additionally filtered by province for non-global-access users (see [Province & Region Scoping](#province--region-scoping)).

| Method | Endpoint | Permission Required | Description | Request Body | Response |
|--------|----------|---------------------|-------------|--------------|----------|
| GET | `/api/v1/budget-templates` | `budget.template.read` | List budget templates with search, sorting, and pagination. Global-access users see every template; other users see only templates whose province is in their accessible-province set. | Query: [datatable params](#datatable-query-parameters) + `status`, `dateFrom`, `dateTo` | Paginated [`BudgetTemplateSummaryResponse`](#budgettemplatesummaryresponse) |
| GET | `/api/v1/budget-templates/{id}` | `budget.template.read` | Get budget template by ID with all items | - | [`BudgetTemplateResponse`](#budgettemplateresponse) |
| POST | `/api/v1/budget-templates` | `budget.template.create` | Create a new budget template (Draft) | [`CreateBudgetTemplateRequest`](#createbudgettemplaterequest) | [`BudgetTemplateResponse`](#budgettemplateresponse) |
| POST | `/api/v1/budget-templates/submit` | `budget.template.create` | Create a new budget template and immediately submit it | [`CreateBudgetTemplateRequest`](#createbudgettemplaterequest) | [`BudgetTemplateResponse`](#budgettemplateresponse) |
| PUT | `/api/v1/budget-templates/{id}` | `budget.template.update` | Update a Draft or Submitted budget template | [`UpdateBudgetTemplateRequest`](#updatebudgettemplaterequest) | [`BudgetTemplateResponse`](#budgettemplateresponse) |
| DELETE | `/api/v1/budget-templates/{id}` | `budget.template.delete` | Soft-delete a draft budget template | - | `204 No Content` |
| POST | `/api/v1/budget-templates/{id}/submit` | `budget.template.submit` | Submit a draft template | - | `204 No Content` |

**Search fields:** `code`, `Province.Name`

**Sortable fields:** `code`, `status`, `createdAt` (default), `submittedAt`

**Additional query parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `status` | `Draft` \| `Submitted` | Filter by template status |
| `dateFrom` | `DateOnly` - `yyyy-MM-dd` e.g. `2026-04-26` | Filter templates where `createdAt >= start of dateFrom (UTC)` |
| `dateTo` | `DateOnly` - `yyyy-MM-dd` e.g. `2026-04-26` | Filter templates where `createdAt < start of dateTo+1 (UTC)`, i.e. whole day inclusive |

**Example:** `GET /api/v1/budget-templates?status=Submitted&dateFrom=2026-04-01&dateTo=2026-04-30&page=1&limit=10`

> **Business Rules:**
> - Template code is auto-generated: `T.{YYMM}{5-digit-seq}` (e.g. `T.260400001` = April 2026, sequence 1)
> - Each item's `ActivityType` (`activityTypeId`, required per item) must exist and be active at create/update time
> - All `ItemShadowId` values are validated to exist before commit
> - `provinceId` in the request body must reference an existing province (`404` if not) that is active (`400` if not) - see [Province & Region Scoping](#province--region-scoping). Get valid IDs from `GET /warehouses/locations`.
> - A template created with no `provinceId` is excluded from every non-global-access user's list/export results until an admin assigns it a province
> - On update, omitting `provinceId` (or sending it as `null`) leaves the template's current province unchanged - there is currently no way to clear an already-assigned province back to `null` via update, only reassign to a different valid province
> - Update (PUT) allowed in both `Draft` and `Submitted` status - edit history tracked via audit log
> - Delete only allowed when `Status == Draft`
> - Submit transitions `Draft → Submitted`; only Draft templates can be submitted
> - `POST /api/v1/budget-templates/submit` is equivalent to create + submit in one request (atomic)
> - Cost item details (name, COA) are read directly from `ItemShadow` at query time - not stored redundantly
> - Templates are scoped to the authenticated user's company (tenant filter) and, for list/export, to the caller's accessible provinces

### Frontend Flow: Create Budget Template Form

The form has two action buttons - **Draft** and **Submit**. Here is the complete web dev flow:

#### 1. Load form (parallel on mount)
```
GET /api/v1/activity-types              → populate the per-row "Activity Type" dropdown
GET /api/v1/warehouses/locations        → populate "Location" dropdown
GET /api/v1/items?page=1&limit=100      → populate "Cost Detail" dropdown per item row
```
Store full response objects in state, not just IDs - you need display fields for auto-fill.

The location list comes from `$.data.locations` (array of `{ id, name, display }`). Store the selected province's `id` in the form state and send it as `provinceId` in the request body. Use `display` (proper case, e.g. "Lampung") in the dropdown UI - `name` is the normalized UPPER matching key.

#### 3. User adds item rows
Each row has a **Cost Detail** dropdown (shows `itemCode`).  
On selection, auto-fill the other columns from the selected item object:

| Column | Source field |
|--------|-------------|
| Cost Detail | `item.itemCode` |
| Cost Name | `item.itemName` |
| COA | `item.acctCode` |
| COA Name | `item.acctName` |

Activity Type is not auto-filled - the user picks it per row from the dropdown loaded in step 1. It's required on every item.

#### 4a. "Draft" button - save as Draft (1 call)
```http
POST /api/v1/budget-templates
{
  "provinceId": 7,
  "items": [
    { "itemShadowId": 10, "activityTypeId": 1 }
  ]
}
```
Response: `BudgetTemplateResponse` with `status: "Draft"` and the generated `templateCode`.  
Redirect to list or detail page.

#### 4b. "Submit" button - create and submit atomically (1 call)
```http
POST /api/v1/budget-templates/submit
{
  "provinceId": 7,
  "items": [
    { "itemShadowId": 10, "activityTypeId": 1 }
  ]
}
```
Response: `BudgetTemplateResponse` with `status: "Submitted"`.  
Redirect to list or detail page.

#### 5. Submit an existing Draft from the list (1 call)
```http
POST /api/v1/budget-templates/{id}/submit
→ 204 No Content
```
Refresh the list - the row's status badge changes to `Submitted`.

### DTOs

#### BudgetTemplateSummaryResponse
```json
{
  "id": "long",
  "templateCode": "string (e.g. T.260400001)",
  "provinceId": "long (nullable)",
  "provinceName": "string (nullable) - normalized UPPER name, used for matching/aliases, not for display",
  "provinceDisplay": "string (nullable) - proper-case name for frontend UI, e.g. \"Lampung\"",
  "date": "datetime (maps to createdAt)",
  "status": "string (Draft | Submitted)"
}
```

#### BudgetTemplateResponse
```json
{
  "id": "long",
  "templateCode": "string",
  "provinceId": "long (nullable)",
  "provinceName": "string (nullable) - normalized UPPER name, used for matching/aliases, not for display",
  "provinceDisplay": "string (nullable) - proper-case name for frontend UI, e.g. \"Lampung\"",
  "status": "string (Draft | Submitted)",
  "items": [
    {
      "id": "long",
      "itemShadowId": "long",
      "costDetail": "string (ItemShadow.ItemCode)",
      "costName": "string (ItemShadow.ItemName)",
      "coa": "string (ItemShadow.AcctCode)",
      "coaName": "string (ItemShadow.AcctName)",
      "sortOrder": "integer",
      "activityTypeId": "long (required) - each item carries its own activity type",
      "activityTypeCode": "string (nullable)",
      "activityTypeName": "string (nullable)"
    }
  ],
  "createdAt": "datetime",
  "createdByName": "string",
  "submittedAt": "datetime (nullable)",
  "submittedByName": "string (nullable)"
}
```

> **Note:** The `warehouse` object has been removed from `BudgetTemplateResponse`. Templates carry `provinceId`/`provinceName`/`provinceDisplay` instead of a raw `location` string - the warehouse is still linked at the Budget Plan level, and a plan's warehouse must share the template's `provinceId`.

#### CreateBudgetTemplateRequest
```json
{
  "provinceId": "long (optional) - must reference an existing, active Province (get valid IDs from GET /warehouses/locations); 404 if it doesn't exist, 400 if inactive",
  "items": [
    {
      "itemShadowId": "long (required, must reference existing ItemShadow)",
      "activityTypeId": "long (required) - must reference an active ActivityType"
    }
  ]
}
```

#### UpdateBudgetTemplateRequest
```json
{
  "provinceId": "long (optional) - replaces the existing ProvinceId when provided; omitting it (or null) leaves the current value unchanged",
  "items": [
    {
      "itemShadowId": "long",
      "activityTypeId": "long (required) - must reference an active ActivityType"
    }
  ]
}
```

> **Note:** `items` in `UpdateBudgetTemplateRequest` fully replaces all existing items when provided. Items are ordered by their position in the array (`sortOrder = index + 1`). Each item carries its own `activityTypeId` - there is no template-level activity type to fall back to.

---

## Budget Plans

Base route: `/api/v1/budget-plans`

All endpoints require authentication. Budget plans are created by Warehouse Admins based on an approved Budget Template. They record the full cost breakdown for a specific period and warehouse activity. Plans follow a **dynamic multi-stage approval lifecycle** driven by the active `WorkflowTemplate` for `BudgetPlanApproval`:

```
Draft → Submitted → InApproval → Approved
                ↘              ↘
              Rejected        Rejected
                ↓
          (edit allowed) → Submit → back to Submitted
```

| Method | Endpoint | Permission Required | Description | Request Body | Response |
|--------|----------|---------------------|-------------|--------------|----------|
| GET | `/api/v1/budget-plans` | `budget.plan.read` | List budget plans with search, sorting, and pagination. Scoped by [`X-Warehouse-Id`](#warehouse-scoping-header) header. | Query: [datatable params](#datatable-query-parameters) + `status`, `dateFrom`, `dateTo` | Paginated [`BudgetPlanSummaryResponse`](#budgetplansummaryresponse) |
| GET | `/api/v1/budget-plans/{id}` | `budget.plan.read` | Get budget plan by ID with all line items and SPK items | - | [`BudgetPlanResponse`](#budgetplanresponse) |
| POST | `/api/v1/budget-plans` | `budget.plan.create` | Create a new budget plan (Draft) | [`CreateBudgetPlanRequest`](#createbudgetplanrequest) | [`BudgetPlanResponse`](#budgetplanresponse) |
| POST | `/api/v1/budget-plans/submit` | `budget.plan.create` | Create a new budget plan and immediately submit it | [`CreateBudgetPlanRequest`](#createbudgetplanrequest) | [`BudgetPlanResponse`](#budgetplanresponse) |
| PUT | `/api/v1/budget-plans/{id}` | `budget.plan.update` | Update a Draft or Rejected budget plan | [`UpdateBudgetPlanRequest`](#updatebudgetplanrequest) | [`BudgetPlanResponse`](#budgetplanresponse) |
| DELETE | `/api/v1/budget-plans/{id}` | `budget.plan.delete` | Soft-delete a draft budget plan | - | `204 No Content` |
| POST | `/api/v1/budget-plans/{id}/submit` | `budget.plan.submit` | Submit a Draft or Rejected plan for approval | - | `204 No Content` |
| POST | `/api/v1/budget-plans/{id}/approve` | `budget.plan.approve` | Approve the current pending workflow stage. Caller must match current stage `approverRoles` (or be `SUPER_ADMIN`) | - | `204 No Content` |
| POST | `/api/v1/budget-plans/{id}/reject` | `budget.plan.reject` | Reject at any approval stage with a reason | [`RejectBudgetPlanRequest`](#rejectbudgetplanrequest) | `204 No Content` |
| GET | `/api/v1/budget-plans/{id}/rfba-pdf` | `budget.plan.export` | Print RFBA items for a budget plan, grouped one page per Bill of Lading. Retained for backward compatibility; new APDP printing is available from the recap PO detail. | - | PDF attachment |
| POST | `/api/v1/budget-plans/{id}/spk-items` | `budget.plan.update` | Link an SPK (base document) to a budget plan | [`AddSpkItemRequest`](#addspkitemrequest) | [`BudgetPlanSpkItemResponse`](#budgetplanspkitemresponse) |
| DELETE | `/api/v1/budget-plans/{id}/spk-items/{spkItemId}` | `budget.plan.update` | Remove an SPK link from a budget plan | - | `204 No Content` |

**Search fields:** `code`, `remark`, `Warehouse.Name`

**Sortable fields:** `status`, `createdAt` (default), `docDate`, `submittedAt`

**Additional query parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `status` | `Draft` \| `Submitted` \| `InApproval` \| `Approved` \| `Rejected` | Filter by plan status |
| `dateFrom` | `DateOnly` - `yyyy-MM-dd` e.g. `2026-04-26` | Filter plans where `docDate >= start of dateFrom (UTC)` |
| `dateTo` | `DateOnly` - `yyyy-MM-dd` e.g. `2026-04-26` | Filter plans where `docDate < start of dateTo+1 (UTC)`, i.e. whole day inclusive |

**Example:** `GET /api/v1/budget-plans?status=Submitted&dateFrom=2026-04-01&dateTo=2026-04-30&page=1&limit=10`

> **Dynamic Workflow Approval Rules:**
> - Approval stages are defined by the company's active `WorkflowTemplate` for `BudgetPlanApproval`.
> - The `/approve` endpoint advances the current `Pending` stage. Caller must hold one of that stage's `approverRoles` or be `SUPER_ADMIN`. Wrong role for the current stage returns `400`.
> - After the last stage is approved, plan status becomes `Approved`. Intermediate stages set status to `InApproval`.
> - **Rejection** is allowed at any pending stage (`Submitted` or `InApproval`). Any user with `budget.plan.reject` permission can reject. A plan can also end up `Rejected` indirectly: rejecting its [Recap Work Order](#recap-work-orders) sets the plan to `Rejected` too.
> - **Edit-and-resubmit**: A `Rejected` plan can be edited (PUT) and re-submitted (POST /submit). On re-submit a fresh `WorkflowInstance` is created from the current template and the full cycle restarts.

> **What can be edited on a Rejected plan, in plain terms:**
> A `Rejected` plan is edited exactly like a `Draft`. The creator can just call `PUT /{id}` and change whatever needs fixing, then `POST /{id}/submit` to send it through approval again.
> - **Warehouse, remark, document date** - freely changeable.
> - **SPK list** - freely replaceable, as long as no remaining cost item still points at an SPK you're removing.
> - **Cost items that have never had a Work Order created from them** - freely added, changed, or removed.
> - **Cost items that already have a Work Order** (even one that was later cancelled/deleted) - can **not** be removed from the plan, and can **not** be split into more than one row. You can still change their unit cost and quantity.
> - **Cost items with a Work Order that is still active (not cancelled)** - same as above, plus: the new total (`unitCost × quantity`) can't be dropped below what's already committed to that Work Order. This stops a plan from being edited down below money that's already been put to work.

> **Other Business Rules:**
> - Plan code is auto-generated: `BP.{YYMM}{6-digit-seq}` (e.g. `BP.260400000001`). Sequence is global across all tenants.
> - The referenced `BudgetTemplate` must be `Submitted`.
> - Each line item's `ItemShadowId` may come from template rows, manually added rows, or selected from the SPK items in the Base Document section.
> - **SPK → Cost item link:** each cost row may reference one of the plan's SPK items via `spkShadowId`. The server validates the SPK is in `spkShadowIds[]`; violations return `422`. `itemShadowId` is the cost/service item from the rate card, independent of the SPK's product item code.
> - **`costValue` (Unit Cost):** defaults to the most recently submitted rate card value for the given vendor+item pair. Send `costValue` in the item body to override it (must be `> 0`). The override is stored verbatim.
> - **`uomMasterId` (Unit of Measure):** defaults to the UoM from the rate card for the given vendor+item pair. Send `uomMasterId` in the item body to override it. The referenced UoM must exist; an unknown ID returns `404`.
> - **`quantity` (Unit Count) ceiling:** if a cost row is linked to an SPK (`spkShadowId` set) and that SPK has a `quantity` value, `quantity` must be `≤ spk.quantity`. Violations return `422`. SPKs with `null` quantity impose no ceiling.
> - `type` and `isRfba` are **per-line-item** fields - each row in the Cost Detail table has its own values. The plan header does not carry these fields.
> - `docExternal`, `billOfLading`, and `description` are **optional per-line-item** fields. Omit or send `null` to leave them blank.
> - `TotalValue` per item = `CostValue × Quantity` (stored). `GrandTotal` = sum of all items.
> - Update and delete are only allowed when `Status == Draft`. Edit (PUT) also allowed when `Status == Rejected`.
> - `BudgetTemplateId` is immutable after creation.
> - Plans are scoped to the authenticated user's company.
> - `type` (`External`/`Internal`) classifies whether the activity uses an external vendor or internal resources. Default: `External`.
> - `isRfba` flags whether an RFBA document is required. Default: `false`.
> - `activityTypeId` on each cost line is **required**, set by the caller. For template-derived rows, pre-populate it from `template.items[].activityTypeId`; for manually added rows, show the activity type dropdown (`GET /api/v1/activity-types`) and let the user pick.

### Frontend Flow: Create Budget Plan Form

The form has three sections - **Header**, **Base Document (SPK)**, and **Cost Detail** - plus two action buttons: **Draft** and **Submit**.

#### 1. Load form (parallel on mount)
```
GET /api/v1/budget-templates?status=Submitted&limit=100   → populate "Template Id" dropdown (Submitted only)
```

#### 2. User selects a Template
Call `GET /api/v1/budget-templates/{id}` to get the full template detail.

Auto-fill the read-only header fields from the response:

| UI Field | Source |
|---|---|
| Template Id | `template.templateCode` |
| Location | `template.provinceDisplay` |

Then load the warehouse picker **filtered to that province**:
```
GET /api/v1/warehouses?provinceId={template.provinceId}&limit=100
```
Populate the **Warehouse** dropdown from this result. The user selects a warehouse - its `id` is sent as `warehouseShadowId` in the create request. The server re-validates that the chosen warehouse's `provinceId` matches the template's (`400` on mismatch), so this filter is a UX convenience, not the enforcement point.

Auto-fill read-only warehouse display fields once the user selects a warehouse:
| UI Field | Source |
|---|---|
| Warehouse Code | `warehouse.code` |
| Warehouse Name | `warehouse.name` |

The template's `items` array also drives the **Cost Detail table** - each template item becomes one editable row.

#### 3. Pre-populate Cost Detail rows from template items

For each item in `template.items`:

| Column | Source |
|---|---|
| Cost ID | `item.costDetail` (ItemShadow.ItemCode) |
| Cost Name | `item.costName` (ItemShadow.ItemName) |
| COA | `item.coa` (ItemShadow.AcctCode) |
| COA Name | `item.coaName` (ItemShadow.AcctName) |
| Activity Type | required; pre-populate from `item.activityTypeId` / `item.activityTypeName`; user can override via dropdown |
| Vendor Code / Name | user selects from dropdown - call `GET /api/v1/rate-cards/by-item/{itemShadowId}` |
| Unit Cost | auto-filled from the selected vendor's rate (`VendorRateResponse.costValue`); user can edit - send as `costValue` in the request |
| UoM | auto-filled from selected vendor's rate (`VendorRateResponse.uomMasterId` / `uomCode`); user can edit - send as `uomMasterId` in the request |
| Unit Count | user enters; if the row is linked to an SPK, must be ≤ `spkItems[].quantity` (ceiling enforced server-side) |
| Type | user selects per row: `Internal` / `External` → sent as `type` |
| RFBA | user toggles per row → sent as `isRfba` |
| Doc External | user enters (optional) |
| Bill of Lading | user enters (optional) |
| Description | user enters (optional) |

> **Vendor dropdown per row:** Call `GET /api/v1/rate-cards/by-item/{itemShadowId}` when the user expands/focuses a row. Returns all vendors with a submitted rate for that item. On vendor selection, auto-fill `unitCost` from `VendorRateResponse.costValue` and pre-populate the UoM field from `VendorRateResponse.uomMasterId` / `uomCode`. Both fields remain editable - send `costValue` and/or `uomMasterId` in the item body only when overriding the rate card defaults.

#### 3b. User manually adds a Cost Detail row ("Add Item")

When the user clicks **Add Item** in the Cost Detail section, the row is blank - not derived from the template.

| Column | How to populate |
|---|---|
| Cost ID / Cost Name | dropdown: `GET /api/v1/items` (`budget.item.read`) - search by code or name |
| Activity Type | dropdown: `GET /api/v1/activity-types` (`budget.template.read`) |
| Vendor Code / Name | dropdown: `GET /api/v1/rate-cards/by-item/{itemShadowId}` (same as template rows) |
| Everything else | same as template-derived rows above |

Activity Type is required - the user must pick one before the row can be saved.

#### 4. User adds SPK (Base Document) rows

Let the user search: `GET /api/v1/spk?search=&type=LO&docStatus=O`

Each row the user picks is collected into `spkShadowIds[]`. No separate calls needed at this stage - the IDs are bundled into the create request.

| SPK Column | Source |
|---|---|
| SPK Type | `spkItems[].type` |
| SPK No | `spkItems[].docNo` |
| Document No | `spkItems[].baseDocNo` |
| Bill of Lading | `spkItems[].blNo` |
| Item Code | `spkItems[].itemCode` |
| Item Name | `spkItems[].itemName` |
| Qty | `spkItems[].quantity` |
| Kemasan | `spkItems[].packType` |
| UoM | `spkItems[].uoM` |

#### 4b. Cost Detail: linking a row to an SPK

Set `spkShadowId` to link a cost row to one of the plan's SPK items. Pick `itemShadowId` from the rate card as usual; `spkShadowId` is a reference link only and does not constrain which cost/service item you choose.

**What to send on submit:**

```json
{
  "itemShadowId": 6,
  "spkShadowId": 42,
  ...
}
```

`spkShadowId` must be in `spkShadowIds[]`; violations return `422`.

> Use `spkItems[].itemShadowId` (from GET or POST `/spk-items`) to display the product item in the UI. Returns `null` when the product code is not in the local item master.

#### 5. Fill Header fields (user input)
| Field | Input type | Sent as |
|---|---|---|
| Remark | text | `remark` |
| Document Date | date picker | `docDate` (ISO 8601) |

Budget No and Status are **read-only** - returned by the API after creation, never sent by the client.

#### 6a. "Draft" button - save as Draft (1 call, fully atomic)
```http
POST /api/v1/budget-plans
{
  "budgetTemplateId": 1,
  "warehouseShadowId": 3,
  "remark": "Bongkaran",
  "docDate": "2026-03-03T00:00:00",
  "spkShadowIds": [42, 43],
  "items": [
    {
      "itemShadowId": 10,
      "activityTypeId": 3,
      "vendorShadowId": 5,
      "quantity": 8000000,
      "type": "Internal",
      "isRfba": true,
      "docExternal": "2603000025",
      "billOfLading": "MEDUS23927878",
      "description": "Biaya Timbang Bulan Maret",
      "spkShadowId": 42
    }
  ]
}
```
Response: `BudgetPlanResponse` with `status: "Draft"` and auto-generated `budgetNo`. Plan header, cost items, and SPK links are all saved in one commit.

#### 6b. "Submit" button - create and submit atomically (1 call)
```http
POST /api/v1/budget-plans/submit
```
Same body. Response: `BudgetPlanResponse` with `status: "Submitted"`.

#### Post-creation: editing SPK rows on an existing Draft

Use the sub-resource endpoints to add or remove individual SPK links without touching cost items:
```http
POST   /api/v1/budget-plans/{id}/spk-items          body: { "spkShadowId": 44 }
DELETE /api/v1/budget-plans/{id}/spk-items/{itemId}
```

#### 7. Submit an existing Draft from the list (1 call)
```http
POST /api/v1/budget-plans/{id}/submit
→ 204 No Content
```

---

### DTOs

#### BudgetPlanSummaryResponse
```json
{
  "id": "long",
  "budgetNo": "string (e.g. BP.260400000001)",
  "templateCode": "string (e.g. T.260400001)",
  "remark": "string (nullable)",
  "location": "string (nullable, from BudgetPlan.Warehouse.Location)",
  "vendorName": "string (nullable, first item's vendor name)",
  "makerName": "string (nullable, name of the user who submitted the plan)",
  "docDate": "datetime",
  "status": "string (Draft | Submitted | InApproval | Approved | Rejected)",
  "statusDisplay": "string - human-readable label (e.g. \"In Approval\", \"Approved\")",
  "approval": "BudgetPlanApprovalInfo - see below"
}
```

#### BudgetPlanApprovalInfo
```json
{
  "totalStages": "integer - number of stages in the workflow template",
  "currentStageOrder": "integer - currently active stage order (1-based); points to the next pending stage",
  "stages": [
    {
      "stageOrder": "integer",
      "stageName": "string",
      "approverRoles": ["string"],
      "status": "string (Pending | Approved | Rejected)",
      "approvedAt": "datetime (nullable)",
      "approvedByName": "string (nullable)",
      "rejectedAt": "datetime (nullable)",
      "rejectedByName": "string (nullable)",
      "rejectionReason": "string (nullable)"
    }
  ]
}
```

#### BudgetPlanResponse
```json
{
  "id": "long",
  "budgetNo": "string",
  "warehouseCode": "string (from BudgetPlan.Warehouse.Code)",
  "warehouseName": "string (from BudgetPlan.Warehouse.Name)",
  "template": {
    "id": "long",
    "templateCode": "string",
    "provinceId": "long (nullable)",
    "provinceName": "string (nullable) - normalized UPPER name, used for matching/aliases, not for display",
    "provinceDisplay": "string (nullable) - proper-case name for frontend UI, e.g. \"Lampung\""
  },
  "remark": "string (nullable)",
  "docDate": "datetime",
  "status": "string (Draft | Submitted | InApproval | Approved | Rejected)",
  "statusDisplay": "string - human-readable label (e.g. \"In Approval\", \"Approved\")",
  "items": [
    {
      "id": "long",
      "itemShadowId": "long",
      "costDetail": "string (ItemShadow.ItemCode)",
      "costName": "string (ItemShadow.ItemName)",
      "coa": "string (ItemShadow.AcctCode)",
      "coaName": "string (ItemShadow.AcctName)",
      "vendorShadowId": "long",
      "vendorCode": "string",
      "vendorName": "string",
      "uomMasterId": "long",
      "uomCode": "string",
      "uomName": "string",
      "costValue": "decimal (rate-card value, or caller-supplied override if provided at creation/update)",
      "quantity": "decimal",
      "totalValue": "decimal (costValue × quantity)",
      "sortOrder": "integer",
      "type": "string (External | Internal)",
      "isRfba": "boolean",
      "docExternal": "string (nullable) - external document reference number",
      "billOfLading": "string (nullable) - bill of lading number",
      "description": "string (nullable) - free-text line item note",
      "activityTypeId": "long (required) - caller-supplied; pre-populated from template item for template-derived rows, user-selected for manual rows",
      "activityTypeCode": "string (nullable)",
      "activityTypeName": "string (nullable)",
      "spkShadowId": "long (nullable) - ID of the SpkShadow this cost item is linked to; null when no SPK link",
      "ppnTaxTypeCode": "string (nullable) - e.g. \"PPN11\"; null if this line has no PPN",
      "ppnRate": "decimal (percentage, e.g. 11.00) - 0 if no PPN",
      "pphTaxTypeCode": "string (nullable) - e.g. \"PPH23\"; null if this line has no PPh",
      "pphRate": "decimal (percentage, e.g. 2.00) - 0 if no PPh",
      "ppnAmount": "decimal - totalValue × (ppnRate / 100)",
      "pphAmount": "decimal - totalValue × (pphRate / 100)",
      "grandTotal": "decimal - totalValue + ppnAmount − pphAmount",
      "costTreatment": "string (nullable) - \"Dibiayakan\" | \"TidakDibiayakan\" | null; copied verbatim from the source RateCardItem when the Budget Plan line is created, label only - does not affect costValue/totalValue/grandTotal"
    }
  ],
  "grandTotal": "decimal - SUM(items[].totalValue), pre-tax. NOTE: this is a different field from the per-item \"grandTotal\" above - the document-level total intentionally stays on the untaxed cost×quantity basis (same figure used for budget allocation checks); each item's own grandTotal is the tax-inclusive figure for that single line.",
  "totalPpnAmount": "decimal - SUM(items[].ppnAmount) across every line, computed server-side so the frontend never has to sum this itself",
  "totalPphAmount": "decimal - SUM(items[].pphAmount) across every line",
  "taxInclusiveGrandTotal": "decimal - SUM(items[].grandTotal) across every line = grandTotal + totalPpnAmount − totalPphAmount. This is the one to show as the document's final, tax-inclusive total.",
  "createdAt": "datetime",
  "createdByName": "string",
  "submittedAt": "datetime (nullable)",
  "submittedByName": "string (nullable)",
  "approval": {
    "totalStages": "integer",
    "currentStageOrder": "integer",
    "stages": [
      {
        "stageOrder": "integer",
        "stageName": "string",
        "approverRoles": ["string"],
        "status": "string (Pending | Approved | Rejected)",
        "approvedAt": "datetime (nullable)",
        "approvedByName": "string (nullable)",
        "rejectedAt": "datetime (nullable)",
        "rejectedByName": "string (nullable)",
        "rejectionReason": "string (nullable)"
      }
    ]
  },
  "rejectedAt": "datetime (nullable)",
  "rejectedByName": "string (nullable)",
  "rejectionReason": "string (nullable)"
}
```

#### CreateBudgetPlanRequest
```json
{
  "budgetTemplateId": "long (required, must reference a Submitted template)",
  "warehouseShadowId": "long (required, must reference a warehouse whose provinceId matches the template's provinceId)",
  "remark": "string (optional)",
  "docDate": "datetime (required)",
  "spkShadowIds": ["long (optional) - list of SpkShadow IDs to link; duplicates ignored"],
  "items": [
    {
      "itemShadowId": "long (required) - any active ItemShadow; need not be in the template's item list",
      "activityTypeId": "long (required, must be > 0) - pre-populate from template item for template-derived rows; show activity type dropdown for manual rows",
      "vendorShadowId": "long (required, must have a submitted rate card for this item)",
      "quantity": "decimal (required, > 0; if spkShadowId is set and the SPK has a quantity, must be ≤ spk.quantity)",
      "costValue": "decimal (optional, > 0) - unit cost override; omit to use the rate-card value",
      "uomMasterId": "long (optional) - UoM override; omit to use the rate-card UoM. Referenced UoM must exist (404 otherwise)",
      "type": "string (required) - External | Internal",
      "isRfba": "boolean (required)",
      "docExternal": "string (optional, max 100 chars) - external document reference number",
      "billOfLading": "string (optional, max 100 chars) - bill of lading number",
      "description": "string (optional, max 500 chars) - free-text line item note",
      "spkShadowId": "long (optional) - links this cost row to an SPK; value must be in spkShadowIds[]"
    }
  ]
}
```

> **`type` and `isRfba` are per-line-item fields.** A single budget plan can mix Internal/External types and RFBA/non-RFBA lines.

> **Rate lookup failure (`404 Not Found`) names the specific reason**, not a generic "rate not found": `No rate card exists for vendor {vendorId} and item {itemId}` (no `RateCardItem` row at all for that pair) vs. `A rate card exists for vendor {vendorId} and item {itemId}, but it has not been submitted yet` (row exists, its `RateCard` is still `Draft`). Applies whenever a line item omits `costValue`/`uomMasterId` and the server has to resolve them from a submitted rate card.

#### UpdateBudgetPlanRequest
```json
{
  "remark": "string (optional)",
  "docDate": "datetime (optional)",
  "spkShadowIds": ["long (optional) - fully replaces the SPK list when provided; omit to leave unchanged"],
  "items": [
    {
      "itemShadowId": "long",
      "activityTypeId": "long (required, must be > 0)",
      "vendorShadowId": "long",
      "quantity": "decimal (> 0; if spkShadowId is set and SPK has a quantity, must be ≤ spk.quantity)",
      "costValue": "decimal (optional, > 0) - unit cost override; omit to use the rate-card value",
      "uomMasterId": "long (optional) - UoM override; omit to use the rate-card UoM. Referenced UoM must exist (404 otherwise)",
      "type": "string - External | Internal",
      "isRfba": "boolean",
      "docExternal": "string (optional, max 100 chars)",
      "billOfLading": "string (optional, max 100 chars)",
      "description": "string (optional, max 500 chars)",
      "spkShadowId": "long (optional) - see CreateBudgetPlanRequest; same rules apply on update"
    }
  ]
}
```

> **Note:** `items` and `spkShadowIds` each fully replace their respective lists when provided; omit a field to leave it unchanged. `budgetTemplateId` cannot be changed after creation. PUT is allowed for both `Draft` and `Rejected` plans.
>
> **SPK replacement constraint:** if you send `spkShadowIds` without sending `items`, the server checks that no existing cost item references an SPK being removed. If any do, the request is rejected with `422` listing the orphaned SPK IDs. To remove an SPK that cost items reference, re-send `items` in the same request with those `spkShadowId` fields cleared or pointing to a remaining SPK.
>
> **Item edit lock, once a Work Order exists off a cost item:** when you send `items`, each cost item that already has a Work Order (active or cancelled) must appear **exactly once** in the list, matched by `itemShadowId` - it can't be left out and can't be split into two rows. Only `costValue` and `quantity` can actually change for that row; everything else about it (which item, vendor, UoM, etc.) stays as-is. If the Work Order is still active, the row's new total also can't drop below what's already committed. Rows with no Work Order at all have none of these restrictions - add, edit, or remove them freely. See ["What can be edited on a Rejected plan"](#budget-plans) above for the plain-language version.

#### RejectBudgetPlanRequest
```json
{
  "reason": "string (required)"
}
```

### Error Responses

| HTTP Status | When |
|-------------|------|
| `400 Bad Request` | Plan is not `Draft`/`Rejected` on update/delete; not `Draft`/`Rejected` on submit; no items to submit; wrong role for the current approval stage; caller tries to approve their own submission |
| `403 Forbidden` | Caller lacks permission or warehouse access |
| `404 Not Found` | Plan, template, warehouse, or UoM ID does not exist |
| `422 Unprocessable Entity` | SPK/quantity/tax validation failures, e.g.: quantity above the SPK ceiling; SPK not in `spkShadowIds[]`; removing an SPK still referenced by a cost item; **removing or splitting a cost item that already has a Work Order**; **dropping a Work Order-linked item's total below what's already committed** |

---

## Workflow Templates

Base route: `/api/v1/workflow-templates`

Workflow templates define the approval chain for a document type. Each template holds ordered stages; each stage names the roles that can approve it. Only one template can be active per `(companyId, docType)` combination.

When a budget plan is submitted, the server copies the active template into `workflow_instances` + `workflow_instance_stages`. Later template edits do not affect in-flight approvals - those snapshots are frozen at submission time.

Currently one doc type exists: `BudgetPlanApproval`. Doc types come from code, not admin configuration.

> **Caching:** `GET /workflow-templates` and `GET /workflow-templates/{id}` are cached per company (tag `workflow-templates:{companyId}`, local TTL 5 min). Any write to this resource clears the local cache immediately.

### Endpoints

| Method | Path | Permission | Description | Body | Response |
|--------|------|-----------|-------------|------|----------|
| GET | `/api/v1/workflow-templates/doc-types` | `workflow.template.read` | List all valid document types | - | `List<WorkflowDocTypeInfo>` |
| GET | `/api/v1/workflow-templates` | `workflow.template.read` | List templates for the company (paginated) | Query: datatable + optional `docType` | Paginated `WorkflowTemplateSummaryResponse` |
| GET | `/api/v1/workflow-templates/{id}` | `workflow.template.read` | Get one template with all stages | - | `WorkflowTemplateResponse` |
| POST | `/api/v1/workflow-templates` | `workflow.template.create` | Create a template | `CreateWorkflowTemplateRequest` | `WorkflowTemplateResponse` |
| PUT | `/api/v1/workflow-templates/{id}` | `workflow.template.update` | Update name, stages (full replace), and/or active flag | `UpdateWorkflowTemplateRequest` | `WorkflowTemplateResponse` |
| POST | `/api/v1/workflow-templates/{id}/activate` | `workflow.template.update` | Activate this template; deactivates all others for the same docType | - | `204 No Content` |
| POST | `/api/v1/workflow-templates/{id}/deactivate` | `workflow.template.update` | Deactivate this template | - | `204 No Content` |
| DELETE | `/api/v1/workflow-templates/{id}` | `workflow.template.delete` | Hard-delete (blocked if used by any workflow instance) | - | `204 No Content` |

**Additional query parameters (GET list):**

| Parameter | Type | Description |
|-----------|------|-------------|
| `docType` | string | Filter by document type (e.g. `BudgetPlanApproval`) |

### Business Rules

- `docType` must be a value returned by `GET /doc-types`. Any other value returns `422`.
- Stage orders must be unique and > 0 within a template.
- Activating a template (`POST /{id}/activate` or `isActive: true` on create/update) deactivates all other templates for the same `(companyId, docType)` in a single DB transaction.
- `PUT` with `stages` replaces all stages (delete-and-insert). Omit `stages` to leave them unchanged.
- `DELETE` returns `409 Conflict` when any `workflow_instances` row references the template. Deactivate it instead.
- Budget plan submission returns `400` if no active template exists for `BudgetPlanApproval`.

### Frontend Flow

**Load the management page:**

1. `GET /doc-types` on mount to populate the doc type dropdown.
2. `GET /workflow-templates?docType=BudgetPlanApproval` to list existing templates.
3. The row with `isActive: true` is the live approval chain. Mark it with an "Active" badge.

**Create a new template and activate it:**

```http
POST /api/v1/workflow-templates
{
  "docType": "BudgetPlanApproval",
  "name": "Standard 2-Stage Approval",
  "isActive": true,
  "stages": [
    { "stageOrder": 1, "stageName": "Warehouse Head Review", "approverRoles": ["WAREHOUSE_HEAD"] },
    { "stageOrder": 2, "stageName": "Coordinator Approval",  "approverRoles": ["COORDINATOR_WH"] }
  ]
}
```

`isActive: true` deactivates any existing active template for this docType atomically.

**Switch to a different existing template:**

```http
POST /api/v1/workflow-templates/{id}/activate
→ 204 No Content
```

Budget plans already in approval continue with their snapshotted stages. Only new submissions pick up the newly active template.

**Update stages on an active template:**

```http
PUT /api/v1/workflow-templates/{id}
{
  "stages": [
    { "stageOrder": 1, "stageName": "Warehouse Head Review", "approverRoles": ["WAREHOUSE_HEAD", "SUPER_ADMIN"] },
    { "stageOrder": 2, "stageName": "Coordinator Approval",  "approverRoles": ["COORDINATOR_WH"] }
  ]
}
```

Omit `name` or `isActive` to leave those fields unchanged. The update does not affect budget plans currently in approval.

**Remove an unused template:**

`DELETE /{id}`. If any budget plan has ever used this template, the server returns `409`. Deactivate it instead - it stays for historical reference and no new submissions use it.

### DTOs

#### WorkflowDocTypeInfo
```json
{
  "value": "string - the docType key to use in create/filter requests",
  "label": "string - human-readable name for display"
}
```

Current values: `BudgetPlanApproval` ("Budget Plan Approval").

#### CreateWorkflowTemplateRequest
```json
{
  "docType": "string (required, must be a value from GET /doc-types)",
  "name": "string (required, max 200)",
  "isActive": "boolean - if true, deactivates existing active template for same docType",
  "stages": [
    {
      "stageOrder": "integer > 0 (must be unique within the template)",
      "stageName": "string (required, max 200)",
      "approverRoles": ["string (role name, required, non-empty)"]
    }
  ]
}
```

#### UpdateWorkflowTemplateRequest
```json
{
  "name": "string (optional, max 200)",
  "isActive": "boolean (optional - true activates, false deactivates)",
  "stages": [
    {
      "stageOrder": "integer > 0 (must be unique within the template)",
      "stageName": "string (required, max 200)",
      "approverRoles": ["string (role name, required, non-empty)"]
    }
  ]
}
```

> `stages` omitted = existing stages unchanged. `stages: [...]` = full replacement (all old stages deleted, new ones inserted).

#### WorkflowTemplateSummaryResponse
```json
{
  "id": "long",
  "docType": "string",
  "name": "string",
  "isActive": "boolean",
  "stageCount": "integer",
  "createdAt": "datetime",
  "updatedAt": "datetime (nullable)"
}
```

#### WorkflowTemplateResponse
```json
{
  "id": "long",
  "docType": "string",
  "name": "string",
  "companyId": "long",
  "isActive": "boolean",
  "stages": [
    {
      "id": "long",
      "stageOrder": "integer",
      "stageName": "string",
      "approverRoles": ["string"]
    }
  ],
  "createdAt": "datetime",
  "updatedAt": "datetime (nullable)"
}
```

## Purchase Orders

Base route: `/api/v1/purchase-orders`

Permission module/resource: `budget.po`

### Endpoints Overview

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| GET | `/api/v1/purchase-orders/approved-budget-plans` | `budget.po.read` | List `Approved` BPs with their PO generation status (used by the Generate PO list page and by `GET /finance-reports`); respects `X-Warehouse-Id` header | Query: `page`, `limit`, `search`, `sortBy`, `sortOrder` | `PaginatedResponse<ApprovedBudgetPlanPoStatusResponse>` |
| GET | `/api/v1/purchase-orders` | `budget.po.read` | List all purchase orders (paginated) |
| GET | `/api/v1/purchase-orders/{id}` | `budget.po.read` | Get purchase order details with items |
| GET | `/api/v1/purchase-orders/available-items` | `budget.po.read` | Paginated cross-warehouse PO item picker using an active-warehouse seed budget plan |
| GET | `/api/v1/purchase-orders/{id}/available-items` | `budget.po.read` | Same paginated picker while editing a Draft PO; the route ID is excluded server-side |
| POST | `/api/v1/purchase-orders` | `budget.po.create` | Create a new draft purchase order |
| POST | `/api/v1/purchase-orders/generate` | `budget.po.generate` | Create and generate in one call (no draft saved) |
| PUT | `/api/v1/purchase-orders/{id}` | `budget.po.update` | Update a draft purchase order |
| DELETE | `/api/v1/purchase-orders/{id}` | `budget.po.delete` | Soft-delete a draft purchase order |
| POST | `/api/v1/purchase-orders/{id}/generate` | `budget.po.generate` | Generate an existing draft PO to SAP and lock items |

### How One PO Spans Multiple Budget Plans

A `PurchaseOrder` is **vendor-centric**, not BP-centric. Multiple BPs belonging to the same vendor can have their cost items grouped into one PO:

```
Approved BPs (vendor: PT. XYZ)
  ├── BP 2603000001 (Bongkaran) - cost items: Z.GEN001, Z.GEN002
  └── BP 2603000002 (Muat)      - cost items: Z.GEN003
         └──► one PO (PO-2603000001) with items from both BPs
```

The `BudgetPlanItem` IDs passed to Create/Update select exactly which cost rows to include. After Generate, `linkedBudgetPlans` in the detail response shows all BPs whose items ended up in the PO, each with the other POs also linked to that BP.

### Status Lifecycle

```
Draft → Generated
```

Only `Draft` POs can be updated or deleted. Once `Generated`, the PO and its items are immutable. Budget plan items included in a `Draft` or `Generated` PO are reserved and cannot be used in another PO. During SAP generation, update/delete and competing Generate requests return `409 Conflict`; the server uses an owner token with a 15-minute recovery lease and only the owner can release or finalize the claim.

### Recap Purchase Order (APDP / Non-APDP)

Two report views over the same PO data, split by `PurchaseOrderItem.IsRfba`: **APDP** = `IsRfba == true`, **Non-APDP** = `IsRfba == false`. "APDP" = AP Down Payment, a SAP Business One document type. Generated RFBA POs can create their standalone SAP APDP through `POST /api/v1/purchase-orders/{id}/generate-apdp`; the resulting document entry is stored on the PO.

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| GET | `/api/v1/purchase-orders/recap/apdp` | `budget.po.read` | List Budget Plans with >=1 PO item where `IsRfba=true`; same shape as `approved-budget-plans` but PO chips are filtered to only POs containing a matching item |
| GET | `/api/v1/purchase-orders/recap/non-apdp` | `budget.po.read` | Same as above, `IsRfba=false` |
| GET | `/api/v1/purchase-orders/recap/apdp/{poId}` | `budget.po.read` | PO detail with `Items`/`GrandTotal`/`TotalItems`/`LinkedBudgetPlans` filtered to only `IsRfba=true` items on that PO. A PO with no matching items returns `200` with empty items/zero totals, not `404` |
| GET | `/api/v1/purchase-orders/recap/apdp/{poId}/rfba-pdf` | `budget.po.export` | Print the RFBA PDF from the APDP recap PO detail; only RFBA items are included, grouped one page per Bill of Lading |
| GET | `/api/v1/purchase-orders/recap/non-apdp/{poId}` | `budget.po.read` | Same as above, `IsRfba=false` |
| GET | `/api/v1/purchase-orders/recap/apdp/export` | `budget.po.export` | Export the APDP list (xlsx/csv/pdf via `format` query param) |
| GET | `/api/v1/purchase-orders/recap/non-apdp/export` | `budget.po.export` | Export the Non-APDP list |

#### Query Parameters - List (`GET .../recap/apdp`, `GET .../recap/non-apdp`)

| Parameter | Type | Description |
|-----------|------|-------------|
| `page` | int | Page number (default: 1) |
| `limit` | int | Items per page (default: 20) |
| `search` | string | Matches `BudgetPlan.Code` or the primary vendor's `card_name` (`ILIKE`) - identical to `approved-budget-plans` |
| `sortBy` | string | `docDate`, `budgetPlanCode`, `vendorName`, `totalBudgetPlan`, `budgetApproved`, `budgetVariance`, or `poNumber` (case-insensitive); any other/omitted value falls back to `created_at DESC` |
| `sortOrder` | string | `asc` (default) or `desc` |

There is no `isRfba`/type query parameter - the split is the route itself (`apdp` vs `non-apdp`), not a filter flag.

Respects the `X-Warehouse-Id` header, same warehouse scoping rules as `GET /api/v1/purchase-orders/approved-budget-plans`.

A Budget Plan row appears in `recap/apdp` only if at least one of its **generated, non-deleted** PO items has `IsRfba = true` (and only if `IsRfba = false` for `recap/non-apdp`). The `purchaseOrders` chip list is filtered the same way - only POs containing a matching item are listed, even if the Budget Plan has other POs that don't qualify. `totalBudgetPlan`/`budgetApproved`/`budgetVariance` are **whole-Budget-Plan totals** - not scoped to the matching items - so the same Budget Plan can show identical totals on both the APDP and Non-APDP tabs while listing different (or zero) PO chips on each.

**Response:** `PaginatedResponse<ApprovedBudgetPlanPoStatusResponse>` (same DTO as `approved-budget-plans`, see below)

```json
{
  "success": true,
  "message": "Purchase order recap list retrieved",
  "data": [
    {
      "budgetPlanId": 1,
      "budgetPlanCode": "BP.2606000094",
      "remark": "Bongkaran",
      "docDate": "2026-06-29T00:00:00Z",
      "budgetPlanStatus": "Approved",
      "budgetPlanStatusDisplay": "Approved",
      "hasRfbaItems": true,
      "allGenerated": true,
      "vendorShadowId": 3,
      "vendorCode": "V.OTHR0001",
      "vendorName": "AC INDO PERKASA",
      "makerName": "Budi Santoso",
      "approvalName": "Andi Wijaya",
      "purchaseOrders": [
        { "id": 75, "code": "PO-2606000075" }
      ],
      "location": "Lampung",
      "totalBudgetPlan": 1447500000,
      "budgetApproved": 30000000,
      "budgetVariance": -1417500000
    }
  ],
  "meta": { "page": 1, "limit": 20, "total": 1, "totalPages": 1 },
  "requestId": "..."
}
```

Requesting `recap/non-apdp` with the same underlying data returns the same `budgetPlanId`/`totalBudgetPlan`/`budgetApproved`/`budgetVariance`, but `purchaseOrders` would be empty (or list different PO codes) if none of that Budget Plan's items are `IsRfba = false`.

#### Query Parameters - Detail (`GET .../recap/apdp/{poId}`, `GET .../recap/non-apdp/{poId}`)

No query parameters - `poId` is a route parameter, the path segment (`apdp`/`non-apdp`) selects the `IsRfba` filter.

**Response:** `ApiResponse<RecapPurchaseOrderDetailResponse>`

```json
{
  "success": true,
  "message": "Purchase order recap retrieved",
  "data": {
    "id": 75,
    "code": "PO-2606000075",
    "vendorName": "AC INDO PERKASA",
    "status": "Generated",
    "remark": null,
    "docDate": "2026-06-29T00:00:00Z",
    "createdAt": "2026-06-29T10:21:00Z",
    "createdByName": "System Administrator",
    "generatedAt": "2026-06-29T10:21:00Z",
    "generatedByName": "System Administrator",
    "linkedBudgetPlans": [
      {
        "id": 1,
        "code": "BP.2606000094",
        "purchaseOrders": []
      }
    ],
    "items": [
      {
        "id": 10,
        "budgetPlanItemId": 100,
        "itemShadowId": 5,
        "itemCode": "Z.EMKL005",
        "itemName": "B. Bongkar",
        "coaCode": "501010211",
        "coaName": "B. Bongkar",
        "vendorShadowId": 3,
        "vendorCode": "V.OTHR0001",
        "vendorName": "AC INDO PERKASA",
        "uomMasterId": 2,
        "uomCode": "KG",
        "uomName": "Kilogram",
        "isRfba": true,
        "billOfLading": "MEDUJM026632",
        "costValue": 10000,
        "quantity": 3,
        "totalValue": 30000,
        "sortOrder": 1,
        "ppnTaxTypeCode": "PPN11",
        "ppnRate": 11,
        "pphTaxTypeCode": "PPH22",
        "pphRate": 0.5,
        "ppnAmount": 3300,
        "pphAmount": 150,
        "grandTotal": 33450,
        "costTreatment": null
      }
    ],
    "grandTotal": 33450,
    "totalItems": 1
  },
  "requestId": "..."
}
```

- `items`, `grandTotal`, `totalItems`, and `linkedBudgetPlans` are all filtered/recomputed from **only** the items matching this route's `IsRfba` value - not the PO's full item set. A PO with a mix of `IsRfba=true` and `IsRfba=false` items reports different values on `recap/apdp/{poId}` vs `recap/non-apdp/{poId}`.
- If the PO exists but has **zero** items matching this route's `IsRfba` value: returns `200` with `items: []`, `grandTotal: 0`, `totalItems: 0`, `linkedBudgetPlans: []` - not a `404`.
- If `poId` does not exist at all: `404` with the standard `ErrorResponse` shape.

#### RFBA PDF (`GET /api/v1/purchase-orders/recap/apdp/{poId}/rfba-pdf`)

This endpoint prints the RFBA form from the APDP recap PO detail. It uses only the PO's `IsRfba=true` items and groups them into one PDF page per normalized `billOfLading`; items without a Bill of Lading are placed on a separate final page. The response is an `application/pdf` attachment named `RFBA-{poCode}.pdf`. If the PO exists but has no RFBA items, it returns `404` with message `Purchase order {poId} has no RFBA items to print`.

#### Query Parameters - Export (`GET .../recap/apdp/export`, `GET .../recap/non-apdp/export`)

| Parameter | Type | Description |
|-----------|------|-------------|
| `format` | string | `Xlsx` (default), `Csv`, or `Pdf` |
| `search`, `sortBy`, `sortOrder` | - | Same as the list endpoint - the export streams the same filtered/sorted rows, unpaginated (capped by the server's configured max export rows) |

Streams a file attachment (`Content-Disposition: attachment; filename="recap-purchase-orders-{apdp|non-apdp}-{timestamp}.{ext}"`). Columns: Budget No, Vendor Name, Total Budget, Budget Approved, Budget Variance, Doc Date, PO Numbers (comma-joined).

### `GET /api/v1/purchase-orders/approved-budget-plans`

Returns one row per `Approved` budget plan. This is the data source for the **Generate PO list page** - it shows which BPs exist, all their vendors, and all POs linked to each BP.

**Pagination:** Accepts `page` (default `1`) and `limit` (default `20`) query parameters. Returns a standard paginated response with `meta.page`, `meta.limit`, `meta.total`, and `meta.totalPages`.

**Search:** `search` matches `BudgetPlan.Code` or any of the BP's item vendors' `card_name` (`ILIKE`).

**Sort:** `sortBy` accepts `docDate`, `budgetPlanCode`, `vendorName`, `totalBudgetPlan`, `budgetApproved`, `budgetVariance`, or `poNumber` (case-insensitive); `sortOrder` is `asc` (default) or `desc`. Any other/omitted `sortBy` falls back to `created_at DESC`.

Respects the `X-Warehouse-Id` header - filters by `BudgetPlan.WarehouseShadowId`. Same warehouse scoping rules as `GET /api/v1/budget-plans`.

This query is also the data source for `GET /api/v1/finance-reports` (see [Finance Reports](#finance-reports)) - both endpoints call `IPurchaseOrderService.GetApprovedBudgetPlansAsync` and return the identical `ApprovedBudgetPlanPoStatusResponse` shape.

**Response:** `List<ApprovedBudgetPlanPoStatusResponse>`

```json
[
  {
    "budgetPlanId": 1,
    "budgetPlanCode": "BP.260300000001",
    "remark": "Bongkaran",
    "docDate": "2026-03-02T00:00:00Z",
    "budgetPlanStatus": "Approved",
    "budgetPlanStatusDisplay": "Approved",
    "hasRfbaItems": true,
    "allGenerated": true,
    "vendorName": "PT. ABC, PT. XYZ",
    "makerName": "Budi Santoso",
    "approvalName": "Andi Wijaya",
    "purchaseOrders": [
      { "id": 7, "code": "PO-2603000001" }
    ]
  },
  {
    "budgetPlanId": 2,
    "budgetPlanCode": "BP.260300000002",
    "remark": "Muat",
    "docDate": "2026-03-02T00:00:00Z",
    "budgetPlanStatus": "Approved",
    "budgetPlanStatusDisplay": "Approved",
    "hasRfbaItems": false,
    "vendorName": "PT. XYZ",
    "makerName": "Budi Santoso",
    "approvalName": "Andi Wijaya",
    "purchaseOrders": []
  }
]
```

- `vendorName` - `STRING_AGG(DISTINCT ...)` of every vendor across the BP's items, comma-separated, alphabetical; `null` if the BP has no cost items. **Note:** `vendorShadowId`/`vendorCode` were removed (fixed 2026-07-13) - they used to reflect only the first item's vendor by sort order, which was wrong once a BP has items from multiple vendors. The frontend must get `vendorShadowId` for `GET /purchase-orders/available-items` from its own vendor dropdown/lookup, not from this response.
- `makerName` - `submittedByUser.fullname`; `null` if the BP was never submitted
- `approvalName` - `final approved stage actor fullname`; `null` if final approval has not occurred
- `purchaseOrders` - all non-deleted POs containing items from this BP, ordered by id ascending; empty array if none
- `allGenerated` - `true` only when the BP has items, every BP item belongs to a non-deleted Generated PO, and no linked non-deleted PO remains in another status. It is `false` for an empty BP or when any item/linked PO is not generated.
- Frontend: show **Generate PO** button when `purchaseOrders` is empty; show **View PO** / **Edit PO** links for each entry otherwise

### Query Parameters - `GET /api/v1/purchase-orders`

| Parameter | Type | Description |
|-----------|------|-------------|
| `page` | int | Page number (default: 1) |
| `limit` | int | Items per page (default: 10) |
| `search` | string | Search by PO code, vendor name, or remark |
| `status` | string | Filter by status (`Draft`, `Generated`) |
| `vendorShadowId` | long | Filter by vendor |
| `dateFrom` | DateOnly | Filter by DocDate ≥ dateFrom |
| `dateTo` | DateOnly | Filter by DocDate ≤ dateTo |
| `sortBy` | string | `status`, `docdate`, `createdat` (default) |
| `sortOrder` | string | `asc` or `desc` (default) |

### Query Parameters - `GET /api/v1/purchase-orders/available-items` and `GET /api/v1/purchase-orders/{id}/available-items`

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `budgetPlanId` | long | Yes (create route) | Seed approved Budget Plan; when `X-Warehouse-Id` is supplied, it must belong to that active warehouse |
| `vendorShadowId` | long | No (create route) | Optional vendor filter; when supplied it must be represented by the seed BP |
| `search` / `sortBy` / `sortOrder` / `page` / `limit` | - | No | Standard datatable filtering and pagination |
| `includeGenerated` | bool | No | Default `false`. Set `true` to include items held by another Draft or Generated PO. Use `availabilityStatus` for the UI state |

When `X-Warehouse-Id` is supplied, the seed Budget Plan must belong to that active warehouse.
Without the header, the API falls back to the caller's accessible warehouse scope. The create route returns items that are:
- In an `Approved` budget plan
- Matching the seed BP's vendors (or the optional `vendorShadowId`)
- Not already held by another PO when `includeGenerated=false` (Draft and Generated POs both reserve items)
- In every warehouse accessible to the caller

The edit route `/purchase-orders/{id}/available-items` optionally validates the active header warehouse, validates the Draft and its source warehouses, uses the PO vendor, and excludes only the current PO. The old `budgetPlanIds` query is removed. PO create/update bodies still accept only a vendor ID and BudgetPlanItem IDs; picker results are advisory and writes revalidate access and availability.

Each response row includes:
- `availabilityStatus: "Available"` - selectable; `takenByCode` is `null`
- `availabilityStatus: "TakenByDraft"` - held by another Draft PO; `takenByCode` contains its code
- `availabilityStatus: "AlreadyGenerated"` - held by a Generated PO; `takenByCode` contains its code

`isGenerated` and `takenByCode` remain available as metadata, but FE selection logic should use `availabilityStatus == "Available"`.

**UI flow:**
1. User picks vendor from dropdown (required before loading the item picker)
2. User adds BP chips (only BPs with items for that vendor are shown)
3. Call this endpoint with the selected seed `budgetPlanId` and required `vendorShadowId` → populates available items from that vendor across accessible approved BPs with checkboxes
4. User checks only rows with `availabilityStatus == "Available"` → on submit, pass the checked `BudgetPlanItem` IDs in the `items` array of `POST /purchase-orders` (save draft) or `POST /purchase-orders/generate` (create and generate immediately)

The picker is vendor-centric, not BP-centric: one PO may contain items from multiple approved BPs as long as all selected items belong to the same vendor. `budgetPlanId` identifies the seed BP and is not a restriction to that BP alone.

### DTOs

#### `ApprovedBudgetPlanPoStatusResponse`
```json
{
  "budgetPlanId": 1,
  "budgetPlanCode": "2603000001",
  "remark": "Bongkaran",
  "docDate": "2026-03-02T00:00:00Z",
  "budgetPlanStatus": "Approved",
  "budgetPlanStatusDisplay": "Approved",
  "hasRfbaItems": true,
  "vendorName": "PT. ABC, PT. XYZ",
  "makerName": "Budi Santoso",
  "approvalName": "Andi Wijaya",
  "purchaseOrders": [
    { "id": 7, "code": "PO-2603000001" }
  ],
  "location": "MAKASSAR",
  "totalBudgetPlan": 240000000.00,
  "budgetApproved": 180000000.00,
  "budgetVariance": 60000000.00
}
```

| Field | Description |
|-------|-------------|
| `location` | Warehouse location string from `WarehouseShadow.Location` (ERP-synced); `null` if not set |
| `totalBudgetPlan` | `SUM(bpi.cost_value × bpi.quantity)` - total value of all items in this budget plan |
| `budgetApproved` | `SUM(poi.cost_value × poi.quantity)` - sum of PO items already linked to this BP (non-deleted POs only) |
| `budgetVariance` | `totalBudgetPlan − budgetApproved` - remaining unallocated budget; `> 0` keeps the Generate button active |
| `allGenerated` | `true` only when the BP has items, every BP item belongs to a non-deleted `Generated` PO, and no linked non-deleted PO remains in another status; `false` for an empty BP or any ungenerated item/PO |

#### `PurchaseOrderSummaryResponse`
```json
{
  "id": 1,
  "code": "PO-2604000001",
  "vendorCode": "V001",
  "vendorName": "PT. Example Vendor",
  "status": "Draft",
  "docDate": "2026-04-30T00:00:00Z",
  "remark": "Monthly procurement",
  "sapPoNumber": null,
  "grandTotal": 15000000.00,
  "itemCount": 3,
  "createdAt": "2026-04-30T06:00:00Z",
  "createdByName": "John Doe"
}
```

#### `PurchaseOrderResponse` (detail)
```json
{
  "id": 1,
  "code": "PO-2604000001",
  "vendorShadowId": 5,
  "vendorCode": "V001",
  "vendorName": "PT. Example Vendor",
  "status": "Draft",
  "docDate": "2026-04-30T00:00:00Z",
  "remark": "Monthly procurement",
  "sapPoNumber": null,
  "linkedBudgetPlans": [
    {
      "id": 1,
      "code": "BP.260300000001",
      "purchaseOrders": [{ "id": 5, "code": "PO-2603000005" }]
    },
    {
      "id": 2,
      "code": "BP.260300000002",
      "purchaseOrders": []
    }
  ],
  "items": [...],
  "grandTotal": 15000000.00,
  "totalPpnAmount": 1650000.00,
  "totalPphAmount": 300000.00,
  "taxInclusiveGrandTotal": 16350000.00,
  "createdAt": "2026-04-30T06:00:00Z",
  "createdByName": "John Doe",
  "generatedAt": null,
  "generatedByName": null
}
```

#### `PurchaseOrderItemResponse`
```json
{
  "id": 10,
  "budgetPlanItemId": 42,
  "itemShadowId": 7,
  "itemCode": "ITM-001",
  "itemName": "Beras 50kg",
  "coaCode": "5001",
  "coaName": "Cost of Goods",
  "vendorShadowId": 5,
  "vendorCode": "V001",
  "vendorName": "PT. Example Vendor",
  "uomMasterId": 2,
  "uomCode": "SAK",
  "uomName": "Sack",
  "isRfba": false,
  "billOfLading": null,
  "costValue": 500000.00,
  "quantity": 10.0000,
  "totalValue": 5000000.00,
  "sortOrder": 1,
  "ppnTaxTypeCode": "PPN11",
  "ppnRate": 11.00,
  "pphTaxTypeCode": "PPH23",
  "pphRate": 2.00,
  "ppnAmount": 550000.00,
  "pphAmount": 100000.00,
  "grandTotal": 5450000.00,
  "costTreatment": "Dibiayakan"
}
```

> The 7 `ppn*`/`pph*`/`grandTotal` fields, plus `costTreatment`, are **copied as-is** from the source `BudgetPlanItem` when the PO is created - never recalculated. This guarantees a PO's tax figures always match exactly what was approved in its Budget Plan, even if a tax rate (e.g. PPN's national rate) changes afterward. See [Tax Calculation (PPN & PPh)](README.md#tax-calculation-ppn--pph) for the full explanation and worked example. The document-level `"grandTotal"` on `PurchaseOrderResponse` (see above) remains `SUM(items[].totalValue)`, pre-tax - same reasoning as Budget Plans.
>
> `costTreatment` (nullable string, `"Dibiayakan"` | `"TidakDibiayakan"`) is a **label only** at every stage of this flow (Rate Card → Budget Plan → Purchase Order) - it never affects `costValue`, `totalValue`, `ppnAmount`/`pphAmount`, or `grandTotal`. See [`RateCardResponse`](#ratecardresponse) above and [Tax Module: PPN & PPh](README.md#tax-module-ppn--pph) in the README for the full explanation.
>
> `totalPpnAmount`, `totalPphAmount`, and `taxInclusiveGrandTotal` on `PurchaseOrderResponse` (and the equivalent 3 fields on `BudgetPlanResponse`) are server-computed sums across `items[]` - `SUM(ppnAmount)`, `SUM(pphAmount)`, and `SUM(items[].grandTotal)` respectively. They exist so the frontend never has to add up tax across line items itself: `grandTotal` = pre-tax subtotal, `taxInclusiveGrandTotal` = the final number to display as "grand total" on the document.

#### `AvailablePoItemResponse`
```json
{
  "budgetPlanItemId": 42,
  "budgetPlanId": 3,
  "budgetPlanCode": "2603000001",
  "budgetPlanRemark": "Bongkaran",
  "itemShadowId": 7,
  "itemCode": "ITM-001",
  "itemName": "Beras 50kg",
  "coaCode": "5001",
  "coaName": "Cost of Goods",
  "vendorCode": "V001",
  "vendorName": "PT. Example Vendor",
  "isRfba": false,
  "billOfLading": null,
  "costValue": 500000.00,
  "quantity": 10.0000,
  "uomCode": "SAK",
  "uomName": "Sack",
  "isGenerated": false,
  "takenByCode": null,
  "availabilityStatus": "Available"
}
```

- `budgetPlanRemark` - use this for the BP chip label in the Generate PO form, e.g. `"BP.260300000001 - Bongkaran"`

#### `CreatePurchaseOrderRequest`
```json
{
  "vendorShadowId": 5,
  "remark": "Monthly procurement",
  "docDate": "2026-04-30T00:00:00Z",
  "items": [42, 43, 44]
}
```

- `items`: list of `BudgetPlanItem` IDs (the checked rows from the available-items table) - all must belong to the given vendor and be in an `Approved` plan
- Items from multiple BPs can be mixed here as long as they share the same vendor
- Snapshot (codes, names, prices) is captured at creation; frozen after Generate

#### `UpdatePurchaseOrderRequest`
```json
{
  "remark": "Updated remark",
  "docDate": "2026-05-01T00:00:00Z",
  "items": [42, 45]
}
```

All fields optional. If `items` is provided, the item list is fully replaced.

### Generate Endpoints

Two paths depending on whether the user needs a draft step:

**`POST /api/v1/purchase-orders/generate`** - create + generate in one call. Same request body as `POST /purchase-orders`. Returns a `Generated` PO directly; no draft is persisted if SAP fails.

**`POST /api/v1/purchase-orders/{id}/generate`** - generate an existing draft. Transitions status `Draft → Generated`. Use this when the user created a draft first and is now ready to submit.

Both call SAP B1 to create the PO and store the returned SAP PO number. Once generated, all items are locked (excluded from future `available-items` queries). A competing generation, update, or delete while generation is active returns `409 Conflict`; the claim owner token has a 15-minute recovery lease.

The SAP integration can be toggled between a mock and real client via `ErpApi:UseMockSap` in `appsettings.json`:
- `true` (default): `MockSapApiClient` - generates a fake `SAP-PO-{guid}` number and a fake numeric doc entry
- `false`: `SapApiClient` - real HTTP call to `POST /WAMS/PurchaseOrders`. The response's `sapPoNumber` is returned to the caller; the numeric `sapDocEntry` SAP returns is persisted on the PO record for internal tracking only, not part of the API response shape

### PO Code Format

`PO-YYMMnnnnnn` - e.g., `PO-2604000001`

The sequence is scoped to the year-month prefix. `IgnoreQueryFilters()` ensures soft-deleted POs are counted to avoid code reuse.

---

## Work Orders

Base route: `/api/v1/work-orders`

Permission module/resource: `workorder.workorder`

List endpoints (`/approved-plans` and `GET /`) are scoped by the [`X-Warehouse-Id`](#warehouse-scoping-header) header using the same rules as Budget Plans - scoped users without a header see only their assigned warehouses.

**Pagination on `/approved-plans`:** Accepts `page` (default `1`) and `limit` (default `20`) query parameters. Returns a standard paginated response with `meta.page`, `meta.limit`, `meta.total`, and `meta.totalPages`.

### Endpoints Overview

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| GET | `/api/v1/work-orders/approved-plans` | `workorder.workorder.read` | List `Approved` budget plans. Each activity entry includes the pre-created WO `id` and `code`. Scoped by [`X-Warehouse-Id`](#warehouse-scoping-header) header. | Query: `page`, `limit` | `PaginatedResponse<ApprovedBpForWoResponse>` |
| GET | `/api/v1/work-orders` | `workorder.workorder.read` | List work orders (paginated). Scoped by [`X-Warehouse-Id`](#warehouse-scoping-header) header. |
| GET | `/api/v1/work-orders/{id}` | `workorder.workorder.read` | Get work order detail by ID |
| PUT | `/api/v1/work-orders/{id}` | `workorder.workorder.update` | Fill in / update a draft work order |
| DELETE | `/api/v1/work-orders/{id}` | `workorder.workorder.delete` | Soft-delete a draft work order |
| POST | `/api/v1/work-orders/{id}/submit` | `workorder.workorder.submit` | Submit a draft work order |

### Status Lifecycle

```
Draft → Submitted
```

Only `Draft` work orders can be updated, deleted, or submitted.

> **Recap lock:** When the Budget Plan's Recap is `Approved`, all WOs under that BP are additionally locked - `PUT`, `DELETE`, and `POST .../submit` return `409 Conflict` regardless of WO status. The lock is derived from `RecapWorkOrder.Status`; no separate field or unlock call exists. If the recap is `Rejected`, WOs become mutable again automatically.

### Business Rules

- **WOs are auto-created by the server** when a Budget Plan reaches final approval. One stub Draft WO is created per `BudgetPlanItem` in the same atomic transaction as the approval - if any insert fails, the BP stays unapproved.
- Code format: `WO.YYMMnnnnnn` (e.g. `WO.260500000001`). Codes are reserved in bulk at BP approval time via a single atomic counter increment.
- **One BP item → one WO.** Each `BudgetPlanItem` maps to exactly one WO, always. The server derives `itemShadowId` and `activityTypeCode` from the BP item automatically.
- Auto-created stubs have `status: Draft` and empty operational fields (`picUserId`, `startDate`, `endDate`, detail payload). The frontend fills these in via `PUT /work-orders/{id}` before submitting.
- Detail payload is activity-specific, driven by `ActivityTypeCode` (inherited from `BudgetTemplate.ActivityType`):
  - `K.BONGKAR` → `unloadingItems`
  - `K.MUAT` → `loadingItems`
  - `FUMIGASI` → `fumigation`
  - `K.GUDANG` → `storage`
  - `QC` → `qc`
  - `ALAT_BERAT` → `heavyEquipment`
  - `UNBAGGING` → `unbagging`
  - `REBAGGING` → `rebagging`
- **GPS location** (`gpsLocation`) is optional on create and update, but **required before submit** - `POST .../submit` returns `422` if `gpsLocation` is null. The GPS check runs before any DB write, so a missing-GPS rejection leaves the WO in `Draft` with no side-effects.
- **`picUserId`** must reference an existing active user; a non-existent ID returns `422`.
- **`blNumber`** is required (non-empty) on every entry in `unloadingItems` and `loadingItems`; an empty or missing value returns `422`.

### File Attachments

Work orders support document/photo uploads via the generic file module:

```
POST   /api/v1/files/work-orders/{id}              - upload attachment
GET    /api/v1/files/work-orders/{id}              - list attachments
GET    /api/v1/files/work-orders/{id}/{fileId}     - download attachment
DELETE /api/v1/files/work-orders/{id}/{fileId}     - delete attachment
```

- Attachments can only be uploaded or deleted while the WO is in `Draft` status. Attempts on `Submitted` WOs return `403 Forbidden`.
- Delete is allowed for the file uploader **or** the WO creator.
- See [Files](#files) for upload rules, MIME types, and size limits.

### Query Parameters - `GET /api/v1/work-orders`

| Parameter | Type | Description |
|-----------|------|-------------|
| `page` | int | Page number (default: 1) |
| `limit` | int | Items per page (default: 10) |
| `search` | string | Search by WO code or budget plan code |
| `status` | string | Filter by status (`Draft`, `Submitted`) |
| `budgetPlanId` | long | Filter by budget plan |
| `budgetPlanItemId` | long | Filter by budget plan item (activity line) |
| `dateFrom` | DateOnly | Filter by `startDate >= dateFrom` |
| `dateTo` | DateOnly | Filter by `startDate < dateTo` (exclusive upper bound - pass the day after to include a date) |
| `sortBy` | string | `status`, `startdate`, `createdat` (default) |
| `sortOrder` | string | `asc` or `desc` (default) |

### DTOs

#### `ApprovedBpForWoResponse`
```json
{
  "budgetPlanId": 1,
  "budgetPlanCode": "BP.260500000001",
  "activityTypeCode": "K.BONGKAR",
  "activityTypeName": "Bongkar",
  "warehouseShadowId": 2,
  "warehouseCode": "WH-01",
  "warehouseName": "Gudang A",
  "remark": "optional",
  "isRfba": false,
  "docDate": "2026-05-01T00:00:00Z",
  "makerName": "John Doe",
  "vendorName": "PT. XYZ (from linked SPK CardName)",
  "isLocked": false,
  "activities": [
    {
      "budgetPlanItemId": 101,
      "itemShadowId": 5,
      "itemCode": "B.Timbang",
      "activityName": "Bongkar Timbang",
      "activityTypeCode": "K.BONGKAR",
      "activityTypeDisplay": "Kegiatan Bongkar",
      "workOrderId": 11,
      "workOrderCode": "WO.260500000001",
      "workOrderStatus": "Draft"
    },
    {
      "budgetPlanItemId": 102,
      "itemShadowId": 6,
      "itemCode": "B.Muat",
      "activityName": "Bongkar Muat",
      "activityTypeCode": null,
      "activityTypeDisplay": null,
      "workOrderId": null,
      "workOrderCode": null,
      "workOrderStatus": null
    }
  ]
}
```

- `activities` - one entry per `BudgetPlanItem` in the BP. Once the BP is approved, `workOrderId` and `workOrderCode` are always non-null - WOs are auto-created at approval time. Use `workOrderId` to open the WO fill-in form directly.
- `isLocked` - `true` when the BP's Recap Work Order has been `Approved`. All update/submit actions for WOs under this BP will return `409 Conflict`. **Disable the "Fill WO" and "Submit" buttons and show a locked badge when `isLocked: true`.** The lock is lifted automatically if the recap is rejected (`isLocked` returns to `false` on the next poll).

#### `WorkOrderSummaryResponse`
```json
{
  "id": 11,
  "code": "WO.260500000001",
  "budgetPlanId": 1,
  "budgetPlanCode": "BP.260500000001",
  "activityTypeCode": "K.BONGKAR",
  "activityTypeDisplay": "Kegiatan Bongkar",
  "itemShadowId": 5,
  "activityName": "Bongkar Timbang",
  "warehouseCode": "WH-01",
  "warehouseName": "Gudang A",
  "picName": "Jane Doe",
  "isRfba": false,
  "startDate": "2026-05-03T00:00:00Z",
  "endDate": "2026-05-03T23:59:59Z",
  "status": "Draft",
  "createdAt": "2026-05-03T05:00:00Z",
  "createdByName": "John Doe",
  "blNumber": "MEDUS23927878",
  "productName": "Kedelai",
  "vesselName": "ABC123"
}
```

- `blNumber`, `productName`, `vesselName` - all from the first linked SPK (`BlNo`, `ItemName`, `CardName`); `null` if the BP has no SPK items

#### `WorkOrderResponse` (detail)
```json
{
  "id": 11,
  "code": "WO.260500000001",
  "budgetPlanId": 1,
  "budgetPlanCode": "BP.260500000001",
  "activityTypeCode": "K.BONGKAR",
  "activityTypeDisplay": "Kegiatan Bongkar",
  "itemShadowId": 5,
  "activityName": "Bongkar Timbang",
  "warehouseShadowId": 2,
  "warehouseCode": "WH-01",
  "warehouseName": "Gudang A",
  "templateCode": "T.2605000001",
  "vendorName": "PT. Angkutan Jaya",
  "codeBlock": "A3-01",
  "picUserId": 9,
  "picName": "Jane Doe",
  "startDate": "2026-05-03T00:00:00Z",
  "endDate": "2026-05-03T23:59:59Z",
  "isRfba": false,
  "status": "Draft",
  "notes": null,
  "gpsLocation": {
    "latitude": -6.1077,
    "longitude": 106.8811,
    "accuracy": 12.5,
    "recordedAt": "2026-05-26T07:30:00Z"
  },
  "productName": "Kedelai",
  "quantity": 8000000.0,
  "uomCode": "Kg",
  "blNumber": "MEDUS23927878",
  "vesselName": "ABC123",
  "transportOrders": [
    { "shadowId": 7, "docNo": "250124", "type": "MO", "vehicleNo": "MRKU3078808", "cardName": "NEW HOPE JAWA TIMUR, PT" }
  ],
  "unloadingItems": [ /* see UnloadingItem schema below */ ],
  "loadingItems": null,
  "fumigation": null,
  "storage": null,
  "qc": null,
  "heavyEquipment": null,
  "unbagging": null,
  "rebagging": null,
  "createdAt": "2026-05-03T05:00:00Z",
  "createdByName": "John Doe",
  "submittedAt": null,
  "submittedByName": null
}
```

- `activityTypeCode` - raw code copied from the source `BudgetPlanItem.ActivityType.Code` at WO creation time (activity type is set per cost line, not per template); drives which detail sub-object is populated (`K.BONGKAR` → `unloadingItems`, etc.). Use `activityTypeDisplay` for UI labels - `activityTypeCode` is intended for programmatic branching only.
- `activityTypeDisplay` - human-readable name resolved from the `ActivityType` master (e.g. `"Kegiatan Bongkar"` for `"K.BONGKAR"`). Falls back to the raw code if the activity type is not found.
- `itemShadowId` - the specific cost activity this WO covers (from `BudgetPlanItem.ItemShadowId`); use `activityName` as the WO tab label in the UI
- `gpsLocation` - `null` when not yet set; present once the foreman supplies coordinates. Must be non-null before `POST .../submit` succeeds. Use `PUT /work-orders/{id}` with `gpsLocation` to set/update it on a Draft WO.
- `vesselName` - `SpkShadow.CardName` (the cargo owner / client name from SAP SPK)
- `transportOrders` - `null` for all activity types except `K.BONGKAR` and `K.MUAT`; when present, each entry represents a selected Transport Order (one chip = one distinct DocNo). Use `shadowId`s to reconstruct which shadow rows belong to each chip.

#### `TransportOrderRef`
```json
{ "shadowId": 7, "docNo": "250124", "type": "MO", "vehicleNo": "MRKU3078808", "cardName": "NEW HOPE JAWA TIMUR, PT" }
```

#### `GpsLocationRequest`
```json
{
  "latitude": -6.1077,
  "longitude": 106.8811,
  "accuracy": 12.5,
  "recordedAt": "2026-05-26T07:30:00Z"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `latitude` | decimal | Yes | Latitude (`-90` to `90`) |
| `longitude` | decimal | Yes | Longitude (`-180` to `180`) |
| `accuracy` | decimal? | No | GPS accuracy in metres |
| `recordedAt` | datetime | Yes | UTC timestamp when coordinates were captured; must not be in the future (5-minute tolerance) |

#### `GpsLocationResponse`
```json
{
  "latitude": -6.1077,
  "longitude": 106.8811,
  "accuracy": 12.5,
  "recordedAt": "2026-05-26T07:30:00Z"
}
```

### Frontend Flow: WO Fill-In Form

Work Orders are **auto-created by the server** when the Budget Plan reaches final approval. The frontend never calls `POST /work-orders`. Instead:

1. Call `GET /api/v1/work-orders/approved-plans` - each activity entry in `activities[]` contains a non-null `workOrderId` and `workOrderCode`.
2. Open the fill-in form using `workOrderId` - display `workOrderCode` as a read-only field.
3. The form has two action buttons: **Save Draft** and **Submit**.

#### Save Draft button → `PUT /api/v1/work-orders/{id}`
- GPS not required. Omit `gpsLocation` or send `null`.
- Activity-specific detail (e.g. `unloadingItems`) not required - foreman can fill it in later.
- Returns `WorkOrderResponse` with `status: "Draft"`.

#### Submit button → `PUT /api/v1/work-orders/{id}` then `POST /api/v1/work-orders/{id}/submit`
- Save any unsaved form changes first via `PUT`, then call submit.
- GPS must be set before submit. Returns `422` if `gpsLocation` is null.
- Activity-specific detail must be complete at this point.
- Returns `WorkOrderResponse` with `status: "Submitted"`.

> **GPS capture pattern (mobile):** Call `navigator.geolocation.getCurrentPosition` (web) or the platform location API (native) when the user opens the form or taps the GPS icon. Store `{ latitude, longitude, accuracy, recordedAt: new Date().toISOString() }` in form state. Pass it in the request when submitting. If location permission is denied or unavailable, disable the Submit button and show a prompt - Save Draft remains available.

#### `UpdateWorkOrderRequest`
```json
{
  "picUserId": 9,
  "startDate": "2026-05-03T00:00:00Z",
  "endDate": "2026-05-03T23:59:59Z",
  "codeBlock": "A3-01",
  "notes": null,
  "gpsLocation": {
    "latitude": -6.1077,
    "longitude": 106.8811,
    "accuracy": 12.5,
    "recordedAt": "2026-05-26T07:30:00Z"
  },
  "transportOrderShadowIds": [1, 2, 3],
  "unloadingItems": [ /* see UnloadingItem below */ ],
  "loadingItems": null,
  "fumigation": null,
  "storage": null,
  "qc": null,
  "heavyEquipment": null,
  "unbagging": null,
  "rebagging": null
}
```

- All fields optional. Providing `transportOrderShadowIds` fully replaces the existing transport order links.
- Providing `gpsLocation` replaces the current GPS coordinate. Omit the field to leave it unchanged.
- On update, each detail collection/object is replaced only if that field is provided in the payload.
- `unloadingItems` and `loadingItems` are ordered by `sortOrder`.

---

### Activity Detail Schemas

Each WO carries exactly one activity-specific detail block. Which field to populate is determined by the Budget Plan's `activityTypeCode` (inherited from its Budget Template). All other detail fields must be `null`.

#### UnloadingItem - `K.BONGKAR` (`unloadingItems: []`)

Used for **Kegiatan Bongkar** work orders. Each row represents one BL/container line.

```json
{
  "spkShadowId": 42,
  "blNumber": "MEDUS23927878",
  "productName": "Kedelai",
  "quantity": 200.0,
  "uomCode": "KG",
  "noVehicle": "D 5678 ZZ",
  "noContainer": "TCKU7654321",
  "noSeal": "SL-101",
  "grossWeight": 5000.0,
  "finalWeight": 5000.0,
  "nettWeight": 5000.0,
  "totalBag": 5000,
  "unitWeight": 10000.0,
  "isChecked": true,
  "sortOrder": 1
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `spkShadowId` | long? | No | Links this row to a `SpkShadow` record (pre-fill from TO chip selection); `null` if entered manually |
| `blNumber` | string | **Yes** | Bill of lading number - must be non-empty |
| `productName` | string | **Yes** | Product/commodity name |
| `quantity` | decimal | **Yes** | Quantity from TO shadow row |
| `uomCode` | string | **Yes** | Unit of measure code (e.g. `"KG"`, `"SAK"`) |
| `noVehicle` | string? | No | Vehicle plate number |
| `noContainer` | string? | No | Container number |
| `noSeal` | string? | No | Seal number |
| `grossWeight` | decimal? | No | Gross weight in the UoM unit |
| `finalWeight` | decimal? | No | Final weight after tare |
| `nettWeight` | decimal? | No | Net weight - used for actual cost computation |
| `totalBag` | int? | No | Number of bags/packages |
| `unitWeight` | decimal? | No | Weight per unit |
| `isChecked` | bool | **Yes** | Whether this row has been physically checked/verified |
| `sortOrder` | int | **Yes** | Display order (1-based) |

> **Pre-fill from TO chip:** When the user selects a Transport Order chip, call `GET /api/v1/transport-orders?docNo=XXX` and map each row: `blNumber ← blNo`, `productName ← itemName`, `quantity ← quantity`, `uomCode ← uoM`, `noVehicle ← vehicleNo`. There is no ERP source for `noContainer`/`noSeal` (the real Transport Order shadow no longer carries container/seal data) - leave those, along with the weight fields, empty for the foreman to fill in manually.

---

#### LoadingItem - `K.MUAT` (`loadingItems: []`)

Used for **Kegiatan Muat** work orders. Identical shape to `UnloadingItem`.

```json
{
  "spkShadowId": null,
  "blNumber": "MEDUS23927879",
  "productName": "Kedelai",
  "quantity": 150.0,
  "uomCode": "KG",
  "noVehicle": "B 9999 AA",
  "noContainer": "MSKU1234567",
  "noSeal": "SL-201",
  "grossWeight": 4500.0,
  "finalWeight": 4500.0,
  "nettWeight": 4500.0,
  "totalBag": 4500,
  "unitWeight": 9999.0,
  "isChecked": false,
  "sortOrder": 1
}
```

Field descriptions are identical to `UnloadingItem` above.

---

#### FumigationDetail - `FUMIGASI` (`fumigation: {}`)

Used for **Fumigasi** work orders. Single object, not an array.

```json
{
  "fumiId": "FMG-2026-001",
  "totalDuration": "72 jam",
  "blNumber": "MEDUS23927878",
  "mvName": "MV Pacific Star",
  "initialTemperature": 28.5,
  "finalTemperature": 32.0,
  "fumigationType": "Methyl Bromide",
  "methylBromideDosage": 32.0,
  "sulphurFluorideDosage": null,
  "phosphineDosage": null,
  "result": "Lulus"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `fumiId` | string? | No | Fumigation certificate / document ID |
| `totalDuration` | string? | No | Duration string, e.g. `"72 jam"` |
| `blNumber` | string? | No | Bill of lading reference |
| `mvName` | string? | No | Vessel name |
| `initialTemperature` | decimal? | No | Temperature at start (°C) |
| `finalTemperature` | decimal? | No | Temperature at end (°C) |
| `fumigationType` | string? | No | Type of fumigant used |
| `methylBromideDosage` | decimal? | No | Methyl bromide dosage (g/m³) |
| `sulphurFluorideDosage` | decimal? | No | Sulphuryl fluoride dosage (g/m³) |
| `phosphineDosage` | decimal? | No | Phosphine dosage (g/m³) |
| `result` | string? | No | Outcome, e.g. `"Lulus"` / `"Tidak Lulus"` |

---

#### StorageDetail - `K.GUDANG` (`storage: {}`)

Used for **Kegiatan Gudang** work orders. Single object.

```json
{
  "hasPindahStapel": false,
  "hasPembersihan": true,
  "hasPerapihan": true,
  "volumeWeight": 1500.0,
  "workerOnDuty": 5,
  "hasMask": true,
  "hasSafetyGlasses": false,
  "hasHandGloves": true,
  "hasHelmet": true,
  "hasSafetyShoes": true,
  "hasSafetyVest": true
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `hasPindahStapel` | bool | **Yes** | Whether pindah stapel (stack relocation) was performed |
| `hasPembersihan` | bool | **Yes** | Whether cleaning (pembersihan) was performed |
| `hasPerapihan` | bool | **Yes** | Whether tidying (perapihan) was performed |
| `volumeWeight` | decimal? | No | Volume weight in the applicable unit - used for actual cost computation |
| `workerOnDuty` | int? | No | Number of workers on duty |
| `hasMask` | bool | **Yes** | PPE: mask worn |
| `hasSafetyGlasses` | bool | **Yes** | PPE: safety glasses worn |
| `hasHandGloves` | bool | **Yes** | PPE: hand gloves worn |
| `hasHelmet` | bool | **Yes** | PPE: helmet worn |
| `hasSafetyShoes` | bool | **Yes** | PPE: safety shoes worn |
| `hasSafetyVest` | bool | **Yes** | PPE: safety vest worn |

---

#### QcDetail - `QC` (`qc: {}`)

Used for **Quality Control** work orders. Single object.

```json
{
  "moisturePercent": 12.5,
  "jamurPercent": 0.3,
  "bauPercent": 0.1,
  "qualityStatus": "Baik"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `moisturePercent` | decimal? | No | Moisture content (%) |
| `jamurPercent` | decimal? | No | Mold/fungus presence (%) |
| `bauPercent` | decimal? | No | Odor level (%) |
| `qualityStatus` | string? | No | Overall quality outcome, e.g. `"Baik"` / `"Rusak"` |

---

#### HeavyEquipmentDetail - `ALAT_BERAT` (`heavyEquipment: {}`)

Used for **Alat Berat** (heavy equipment) work orders. Single object. `totalCost` is used directly as actual cost (no rate × qty formula).

```json
{
  "blNumber": "MEDUS23927878",
  "startTime": "07:00:00",
  "endTime": "17:00:00",
  "standbyDuration1": "01:30",
  "standbyDuration2": "00:45",
  "minimumDuration": "08:00",
  "costPerHour": 500000.0,
  "totalCost": 4500000.0
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `blNumber` | string? | No | Bill of lading reference |
| `startTime` | `HH:mm:ss` string? | No | Work start time |
| `endTime` | `HH:mm:ss` string? | No | Work end time |
| `standbyDuration1` | string? | No | First standby period duration |
| `standbyDuration2` | string? | No | Second standby period duration |
| `minimumDuration` | string? | No | Minimum contracted duration |
| `costPerHour` | decimal? | No | Hourly rate |
| `totalCost` | decimal? | No | Total cost - used as `actualCost` directly in recap computation |

> `startTime` / `endTime` are serialized as `"HH:mm:ss"` strings (TimeOnly). Send as `"07:00:00"`, `"17:30:00"`, etc.

---

#### UnbaggingDetail - `UNBAGGING` (`unbagging: {}`)

Used for **Unbagging** work orders. Single object.

```json
{
  "noVehicle": "D 5678 ZZ",
  "noContainer": "TCKU7654321",
  "noSeal": "SL-101",
  "initialWeight": 5200.0,
  "finalWeight": 5000.0,
  "unitWeight": 50.0,
  "totalWeight": 5000.0,
  "totalBag": 100
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `noVehicle` | string? | No | Vehicle plate number |
| `noContainer` | string? | No | Container number |
| `noSeal` | string? | No | Seal number |
| `initialWeight` | decimal? | No | Weight before unbagging |
| `finalWeight` | decimal? | No | Weight after unbagging |
| `unitWeight` | decimal? | No | Weight per bag/unit |
| `totalWeight` | decimal? | No | Total net weight - used for actual cost computation |
| `totalBag` | int? | No | Total number of bags processed |

---

#### RebaggingDetail - `REBAGGING` (`rebagging: {}`)

Used for **Rebagging** work orders. Single object.

```json
{
  "receiver": "PT. Distributor Utama",
  "noVehicle": "B 1234 XX",
  "noContainer": "MSKU9876543",
  "noSeal": "SL-301",
  "initialWeight": 5000.0,
  "finalWeight": 4980.0,
  "totalWeight": 4980.0
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `receiver` | string? | No | Receiver / consignee name |
| `noVehicle` | string? | No | Vehicle plate number |
| `noContainer` | string? | No | Container number |
| `noSeal` | string? | No | Seal number |
| `initialWeight` | decimal? | No | Weight before rebagging |
| `finalWeight` | decimal? | No | Weight after rebagging |
| `totalWeight` | decimal? | No | Total net weight - used for actual cost computation |

---

## Transport Orders

Base route: `/api/v1/transport-orders`

Permission module/resource: `workorder.workorder` (read)

Transport Order shadows are synced from ERP (`GET /WAMS/LkTOMOLOPMS`, via `ToSyncService`). They represent **realization data** (actual vehicle trips) for shipments planned in Budget Plans. One DocNo groups multiple shadow rows - one per vehicle trip against the same `(DocNo, BlNo)` (a single shipment is commonly split across multiple trucks, so the same DocNo+BlNo legitimately repeats with a different `vehicleNo` per row; the shadow table's uniqueness key is `(DocNo, BlNo, VehicleNo)`, not just `(DocNo, BlNo)`). Foreman selects TO chips when creating a `K.BONGKAR` or `K.MUAT` Work Order; the shadow rows for each chip pre-fill the unloading/loading items table.

> **`type` is an ERP document-type code, not a loading/unloading indicator.** Real values are `MO` / `LO` (SAP doc-type codes - `MO` rows are linked to a Sales Order, `LO` rows to an inventory transfer). This is unrelated to WAMS's own `K.BONGKAR`/`K.MUAT` activity-type vocabulary used elsewhere in the API - do not branch FE logic on this field to decide loading vs. unloading. There is no reliable mapping between the two; if you need the bongkar/muat distinction, it comes from the Work Order's own activity type, not from this field.

### Endpoints Overview

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| GET | `/api/v1/transport-orders` | `workorder.workorder.read` | List transport order shadows (paginated) |
| GET | `/api/v1/transport-orders/{id}` | `workorder.workorder.read` | Get single transport order shadow by ID |
| GET | `/api/v1/transport-orders/export` | `workorder.workorder.export` | Export transport order shadows using the same filters as the list endpoint |

### Query Parameters - `GET /api/v1/transport-orders`

| Parameter | Type | Description |
|-----------|------|-------------|
| `page` | int | Page number (default: 1) |
| `limit` | int | Items per page (default: 10) |
| `search` | string | Search by `docNo`, `cardName`, `vehicleNo`, `blNo`, or `itemName` |
| `docNo` | string | Exact match on DocNo - use to fetch all rows for a specific chip |
| `type` | string | Filter by ERP doc-type code, e.g. `MO` or `LO` (see note above - not a loading/unloading filter) |
| `whsCode` | string | Filter by warehouse code |
| `docStatus` | string | `O` (open, default) or `C` (closed) |
| `budgetPlanId` | long | Filter by the budget plan's warehouse location; useful for the Work Order TO picker |
| `sortBy` | string | `docno`, `vehicleno` (default: `syncedat desc, docno asc`) |
| `sortOrder` | string | `asc` or `desc` |

### DTOs

#### `TransportOrderShadowResponse`
```json
{
  "id": 1,
  "docNo": "250124",
  "type": "MO",
  "cardCode": "C.FM0021",
  "cardName": "NEW HOPE JAWA TIMUR, PT",
  "vehicleNo": "MRKU3078808",
  "vehicleType": "Trailer",
  "blNo": "MAEU244664721",
  "itemCode": "DDGS-Brazil",
  "itemName": "Brazil DDG Hipro",
  "quantity": 28500.0,
  "uoM": "Kg",
  "whsCode": "WHSBY005",
  "whsName": "SBY - PLB TJ PERAK",
  "docStatus": "O"
}
```

> `docNo` is a bare ERP document number (no `TO-` prefix). `vehicleNo` holds the vehicle's plate/container identifier from ERP's `vehiclePlate`; `vehicleType` (e.g. `"Trailer"`, `"CDD"`, `"Pick Up"`) is new. There is no `docDate`, `containerNo`, or `sealNo` - the real ERP endpoint doesn't provide them.

The `budgetPlanId` filter is applied consistently to both the paginated list and `/export`: only active TO rows whose warehouse code belongs to the same location as the selected budget plan are returned.

### Frontend Usage Pattern

```
1. TO chip picker:
   GET /api/v1/transport-orders?limit=50
   → Group rows by docNo client-side → show as selectable chips

2. When chip is selected:
   GET /api/v1/transport-orders?docNo=250124
   → Each row maps to one unloading item row:
     blNumber    ← blNo
     productName ← itemName
     quantity    ← quantity
     uomCode     ← uoM
     noVehicle   ← vehicleNo
     (noContainer, noSeal have no ERP source - foreman fills these in manually)
     (weight fields left empty for foreman to fill)
   → Collect all row IDs → add to transportOrderShadowIds

3. On WO submit:
   POST /api/v1/work-orders
   { transportOrderShadowIds: [1,2,3], unloadingItems: [...] }
```

---

## Recap Work Orders

Base route: `/api/v1/recap-work-orders`

Permission module/resource: `workorder.recap`

A **Recap Work Order** is a single review record, one per Budget Plan, that groups all Work Orders under that plan and gives the Warehouse Admin a side-by-side view of the budget plan (Plan tab) versus what was actually executed (Realization tab). The WH Admin then approves or rejects the recap.

One `recap_work_orders` row exists per Budget Plan - it is created idempotently (`INSERT ... ON CONFLICT DO NOTHING` on the unique `budget_plan_id` index) the first time either trigger below fires for that BP, and only ever has one of three statuses: `Pending`, `Approved`, `Rejected`.

List and detail endpoints are scoped by warehouse: scoped users (no `GlobalAccess`) can only see recaps whose Budget Plan belongs to their assigned warehouses.

### Endpoints Overview

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| GET | `/api/v1/recap-work-orders` | `workorder.recap.read` | List all recap work orders (paginated) |
| GET | `/api/v1/recap-work-orders/{id}` | `workorder.recap.read` | Get recap detail with Plan + Realization tabs |
| POST | `/api/v1/recap-work-orders/{id}/approve` | `workorder.recap.approve` | Approve a Pending recap |
| POST | `/api/v1/recap-work-orders/{id}/reject` | `workorder.recap.reject` | Reject a Pending recap |

### Status Lifecycle

```
Pending approve > Approved  (all WOs under the BP are locked)
Pending reject > Rejected  (WOs remain mutable)
```

Only `Pending` recaps can be approved or rejected. Both operations return `400 Bad Request` if the recap is already reviewed.

> **WO lock side-effect:** Approving a recap immediately locks all Work Orders under the same Budget Plan. Mutations (`PUT /work-orders/{id}`, `DELETE /work-orders/{id}`, `POST /work-orders/{id}/submit`) on those WOs will return `409 Conflict` until the recap is rejected or replaced. The lock is derived from recap status - no schema change or explicit unlock call is needed.

### Trigger

Recap records are created automatically - there is no manual "create recap" endpoint. Two independent code paths can create the row for a given Budget Plan; whichever fires first wins (the second is a no-op via `ON CONFLICT DO NOTHING`):

**1. First WO submission for the BP.** When a foreman submits a WO (`POST /api/v1/work-orders/{id}/submit`), the server upserts the recap record inside the same explicit DB transaction as the submit:

```
BEGIN TRANSACTION
  → WO.SubmitAsync()                                    ← UPDATE work_orders SET status = 'Submitted'
  → recapRepo.UpsertForBudgetPlanAsync(budgetPlanId)   ← INSERT ... ON CONFLICT DO NOTHING
COMMIT  (or ROLLBACK on any failure)
```

Both operations are atomic - if either fails the entire transaction rolls back, so you will never see a `Submitted` WO without a corresponding recap row or vice versa.

**2. The Budget Plan reaching its final approval stage.** `BudgetPlanService.ApproveAsync` bulk-creates stub Draft WOs for every `BudgetPlanItem` at this point (see [Work Orders](#work-orders)), and now also upserts the recap row - so WH Admin sees the recap (`Pending`, `0` realization) immediately, without waiting for any foreman to submit a WO:

```
→ currentStage marked Approved, plan.Status = Approved
→ woService.BulkCreateDraftAsync(planId, userId, ct)
→ uow.CommitAsync(ct)                                    ← BP approval + stub WOs committed first
→ recapRepo.UpsertForBudgetPlanAsync(planId, companyId)  ← separate call, after that commit
```

This second upsert is deliberately **not** in the same transaction as the approval commit - the stub WOs must already be durably persisted before the raw-SQL upsert runs, so a failure in the upsert step can't leave a recap row with no WOs behind it (worst case: approval succeeds, recap creation is retried on the next trigger for that BP).

Concurrent triggers for the same BP are safe either way - the `ON CONFLICT DO NOTHING` clause on the unique index (`budget_plan_id`) ensures exactly one recap per BP regardless of which trigger wins the race.

### Role Access Matrix

| Role | Can List/View | Can Approve | Can Reject |
|------|---------------|-------------|------------|
| WAREHOUSE_ADMIN | ✅ (own warehouse only) | ✅ | ✅ |
| HO_SPV | ✅ (all) | ❌ | ❌ |
| WAREHOUSE_HEAD | ✅ (own warehouse only) | ❌ | ❌ |
| COORDINATOR_WH | ✅ (own warehouse only) | ❌ | ❌ |
| FOREMAN | ❌ | ❌ | ❌ |

### Query Parameters - `GET /api/v1/recap-work-orders`

| Parameter | Type | Description |
|-----------|------|-------------|
| `page` | int | Page number (default: 1) |
| `limit` | int | Items per page (default: 20, max: 100) |
| `search` | string | Search by budget plan code (ILIKE) |
| `status` | string | Filter by status: `Pending`, `Approved`, `Rejected` |
| `sortBy` | string | `status`, `docdate`, `createdat` (default) |
| `sortOrder` | string | `asc` or `desc` (default) |

### DTOs

#### `RecapWorkOrderSummaryResponse` - list item

```json
{
  "id": 1,
  "budgetPlanId": 42,
  "budgetPlanCode": "BP.260500000001",
  "templateCode": "T.260400001",
  "remark": "optional",
  "warehouseCode": "WHLPG01",
  "warehouseName": "MNP Blok A",
  "blNumbers": "MEDUS23927878, MEDUS23927879",
  "activityTypes": "K.BONGKAR, QC",
  "picNames": "Agam, Budi",
  "isRfba": true,
  "docDate": "2026-05-01T00:00:00Z",
  "recapStatus": "Pending",
  "createdAt": "2026-05-03T05:00:00Z"
}
```

- `blNumbers` - comma-separated distinct BL numbers across all WOs on this BP (`null` if none)
- `activityTypes` - comma-separated distinct activity type codes across all WOs (`null` if none)
- `picNames` - comma-separated distinct PIC names across all WOs (`null` if none)
- `recapStatus` - `Pending` | `Approved` | `Rejected`

#### `RecapWorkOrderDetailResponse` - detail

```json
{
  "id": 1,
  "budgetPlanId": 42,
  "recapStatus": "Pending",
  "reviewedBy": null,
  "reviewedAt": null,
  "rejectionReason": null,
  "plan": { ... },
  "realization": { ... }
}
```

##### `plan` - Plan tab

```json
{
  "header": {
    "budgetNo": "BP.260500000001",
    "templateCode": "T.260400001",
    "budgetPlanStatus": "Approved",
    "remark": "optional",
    "docDate": "2026-05-01T00:00:00Z",
    "warehouseCode": "WHLPG01",
    "warehouseName": "MNP Blok A",
    "location": "Lampung"
  },
  "spkDocuments": [
    {
      "spkType": "LO",
      "spkNo": "LO-2024-00001",
      "documentNo": "SO-2024-00001",
      "blNo": "MEDUS23927878",
      "itemCode": "ITEM-001",
      "itemName": "Kedelai",
      "quantity": 8000000.00,
      "deliveryQty": 4000000.00,
      "uoM": "Kg"
    }
  ],
  "costDetails": [
    {
      "type": "Bongkar",
      "vendorCode": "V001",
      "vendorName": "PT. Vendor Utama",
      "isRfba": false,
      "docExternal": null,
      "costName": "B.Timbang",
      "coaCode": "5101001",
      "coaName": "Biaya Bongkar Muat",
      "billOfLading": "MEDUS23927878",
      "unitCost": 50000.00,
      "unitCount": 100.00,
      "uomCode": "Ton",
      "description": null,
      "totalValue": 5000000.00
    }
  ],
  "budgetPlanTotal": 5000000.00,
  "budgetRealization": 4500000.00,
  "budgetVariance": 500000.00
}
```

- `budgetPlanTotal` - sum of all `BudgetPlanItem.TotalValue` in the plan
- `budgetRealization` - sum of computed `actualCost` across all non-deleted WOs for this BP
- `budgetVariance` - `budgetPlanTotal - budgetRealization`

##### `realization` - Realization tab

```json
{
  "header": { ... },
  "workOrders": [
    {
      "workOrderId": 11,
      "workOrderCode": "WO.260500000001",
      "blNumber": "MEDUS23927878",
      "picName": "Agam",
      "isRfba": true,
      "startDate": "2026-05-01T00:00:00Z",
      "endDate": "2026-05-03T00:00:00Z",
      "actualCost": 4320000.00,
      "workOrderStatus": "Submitted",
      "product": "Kedelai",
      "vehicleNo": "B 1234 XYZ"
    }
  ],
  "budgetPlanTotal": 5000000.00,
  "budgetRealization": 4320000.00,
  "budgetVariance": 680000.00
}
```

- `workOrders` - all non-deleted WOs for this BP (may include `Draft` and `Submitted`)
- `actualCost` per WO - computed from physical WO records using activity-type-specific formulas (see table below); never derived from plan item values
- `realizationPercent` - `(budgetRealization / budgetPlanTotal) × 100`, rounded to 2 decimal places; `0` when `budgetPlanTotal` is zero
- `product` - activity item name from `ItemShadow` (e.g. "Kedelai")
- `vehicleNo` - vehicle number from the first linked Transport Order; `null` if no TOs attached

**Actual cost computation formulas** (`rate = plannedTotal / plannedQty`, weighted avg across vendors on same activity):

| Activity type | Actual quantity source | Formula |
|---------------|------------------------|---------|
| `K.BONGKAR`   | `SUM(UnloadingItems.NettWeight)` | `rate × actualQty` |
| `K.MUAT`      | `SUM(LoadingItems.NettWeight)` | `rate × actualQty` |
| `K.GUDANG`    | `StorageDetail.VolumeWeight` | `rate × actualQty` |
| `ALAT_BERAT`  | `HeavyEquipDetail.TotalCost` (direct) | `TotalCost` |
| `UNBAGGING`   | `UnbaggingDetail.TotalWeight` | `rate × actualQty` |
| `REBAGGING`   | `RebaggingDetail.TotalWeight` | `rate × actualQty` |
| `FUMIGASI`    | Fixed fee - no physical qty | `plannedTotal` |
| `QC`          | Fixed fee - no physical qty | `plannedTotal` |

#### `RejectRecapRequest`

```json
{ "reason": "Budget variance too high" }
```

The `reason` field is optional. Send an empty body or `{}` for rejection without a reason.

### Business Rules

- Only `Pending` recaps can be approved or rejected - attempting either on an already-reviewed recap returns `400`.
- Approve and reject are warehouse-access guarded: the caller must have access to the recap's warehouse (same rules as WO detail - assigned warehouse IDs or `GlobalAccess=true`).
- After approval or rejection, `reviewedBy`, `reviewedAt`, and (for rejection) `rejectionReason` are set atomically.
- The recap record is **never deleted** - it lives as long as the Budget Plan lives.
- **Rejecting a recap also rejects its Budget Plan.** `POST .../reject` sets the parent Budget Plan's status to `Rejected` too (same `rejectedBy`/`rejectedAt`/reason), so the plan's creator can immediately edit and resubmit it - see [Budget Plans](#budget-plans) for what stays editable at that point.
- Approving a recap does not check `realizationPercent` against any threshold - there is no guardrail against a large cost overrun at approval time; it's a manual review call.

### Error Responses

| HTTP Status | When |
|-------------|------|
| `400 Bad Request` | Recap is already `Approved` or `Rejected` |
| `403 Forbidden` | Caller lacks permission or does not have access to this recap's warehouse |
| `404 Not Found` | Recap ID does not exist (or not accessible to tenant) |

---

## Account Payables

Base route: `/api/v1/account-payables`

Permission module/resource: `workorder.ap`

An **Account Payable** (AP) is generated by HO from approved Recap Work Orders. It groups cost items from one or more `Approved` recaps belonging to the same vendor into a single AP document, then generates an AP number via SAP B1 integration. The pattern mirrors Purchase Orders exactly - items are locked after Generate and cannot appear in another Generated AP.

### Endpoints Overview

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| GET | `/api/v1/account-payables/approved-recaps` | `workorder.ap.read` | List `Approved` recaps with their AP generation status (the Generate AP list page); respects `X-Warehouse-Id` header | Query: `page`, `limit` | `PaginatedResponse<ApprovedRecapApStatusResponse>` |
| GET | `/api/v1/account-payables` | `workorder.ap.read` | List all account payables (paginated) |
| GET | `/api/v1/account-payables/{id}` | `workorder.ap.read` | Get AP details with items |
| GET | `/api/v1/account-payables/available-items` | `workorder.ap.read` | List cost items available for a given vendor + selected budget plans - used to populate the Form Generate AP item table |
| GET | `/api/v1/account-payables/{id}/available-items` | `workorder.ap.read` | Same picker while editing a Draft AP; the route ID is excluded server-side after Draft/vendor validation |
| POST | `/api/v1/account-payables/preview` | `workorder.ap.read` | Stateless discount/totals preview - computes what Create/Update would return, without persisting anything. Used for live totals while the user is still editing (see [Discount](#discount) below) |
| POST | `/api/v1/account-payables` | `workorder.ap.create` | Create a new draft AP |
| POST | `/api/v1/account-payables/generate` | `workorder.ap.generate` | Create and generate in one call |
| PUT | `/api/v1/account-payables/{id}` | `workorder.ap.update` | Update a draft AP |
| DELETE | `/api/v1/account-payables/{id}` | `workorder.ap.delete` | Soft-delete a draft AP |
| POST | `/api/v1/account-payables/{id}/generate` | `workorder.ap.generate` | Generate an existing draft AP to SAP and lock items |

### How One AP Spans Multiple Budget Plans

Same vendor-centric model as PO. Multiple `Approved` recaps under the same vendor can have their cost items grouped into one AP:

```
Approved Recaps (vendor: PT. XYZ)
  ├Recap → BP 2603000001 (Bongkaran) - cost items: Z.GEN001, Z.GEN002
  └Recap → BP 2603000002 (Muat)      - cost items: Z.GEN003
         └► one AP (AP-2603000001) with items from both recaps
```

The `BudgetPlanItem` IDs passed to Create/Update select which cost rows to include. After Generate, `linkedBudgetPlanCodes` in the detail response shows all BPs whose items are in the AP.

### Item Locking Rule

A `BudgetPlanItem` is **unavailable** for AP if:
- It already appears in any non-deleted Draft or Generated AP (regardless of quantity).
- Its budget plan does **not** have an `Approved` recap work order.
- Vendor does not match the requested `vendorShadowId`.
- Its budget plan warehouse is outside the caller's warehouse scope.

Available items pass all four guards (handled by `AvailableItemsBaseQuery` in the repository).

### Status Lifecycle

```
Draft → Generated
```

Only `Draft` APs can be updated or deleted. Once `Generated`, the AP and its items are immutable. Budget plan items included in a `Draft` or `Generated` AP are reserved and cannot be used in another AP. During SAP generation, update/delete and competing Generate requests return `409 Conflict`; the server uses an owner token with a 15-minute recovery lease and only the owner can release or finalize the claim.

### `GET /api/v1/account-payables/approved-recaps`

Returns one row per `Approved` recap work order. This is the data source for the **Generate AP list page** - it shows which recaps are ready, their primary vendor, total budget plan value, and whether they have already been generated into an AP.

**Pagination:** Accepts `page` (default `1`) and `limit` (default `20`) query parameters. Returns a standard paginated response with `meta.page`, `meta.limit`, `meta.total`, and `meta.totalPages`.

Respects the `X-Warehouse-Id` header - filters by `BudgetPlan.WarehouseShadowId`.

**Response:** `List<ApprovedRecapApStatusResponse>`

```json
[
  {
    "recapWorkOrderId": 3,
    "budgetPlanId": 1,
    "budgetPlanCode": "BP.260300000001",
    "remark": "Bongkaran",
    "docDate": "2026-03-02T00:00:00Z",
    "hasRfbaItems": true,
    "vendorShadowId": null,
    "vendorCode": null,
    "vendorName": "PT. XYZ",
    "budgetPlanTotal": 240000000.00,
    "accountPayables": [],
    "isAllGenerated": false,
    "location": "JAKARTA",
    "budgetApproved": 0.00,
    "budgetVariance": 240000000.00
  },
  {
    "recapWorkOrderId": 4,
    "budgetPlanId": 2,
    "budgetPlanCode": "BP.260300000002",
    "remark": "Muat",
    "docDate": "2026-03-02T00:00:00Z",
    "hasRfbaItems": false,
    "vendorShadowId": null,
    "vendorCode": null,
    "vendorName": "PT. ABC, PT. XYZ",
    "budgetPlanTotal": 100000000.00,
    "accountPayables": [
      { "id": 1, "code": "AP-2603000001", "status": "Generated", "sapApNumber": "SAP-AP-abc123", "vendorCode": "V.LKL0001" },
      { "id": 2, "code": "AP-2603000002", "status": "Draft", "sapApNumber": null, "vendorCode": "V.ABC0002" }
    ],
    "isAllGenerated": false,
    "location": "MAKASSAR",
    "budgetApproved": 100000000.00,
    "budgetVariance": 0.00
  }
]
```

| Field | Description |
|-------|-------------|
| `vendorShadowId`, `vendorCode` | Always `null`. Kept only for wire-format stability - a recap's budget plan items can span multiple vendors, so a single vendor id/code no longer makes sense here. Use `accountPayables[].vendorCode` instead |
| `vendorName` | Distinct vendor names across all of the recap's budget plan items, comma-joined and alphabetized (`STRING_AGG(DISTINCT ...)`). A single-vendor recap shows one name, same as before |
| `accountPayables` | `List<ApLinkInfo>` - every non-deleted AP (any status, any vendor) that has at least one item from this recap's budget plan, ordered by `id`. Empty array (not `null`) when none exist yet. A recap can now link **multiple** APs - e.g. one per vendor, or a Draft alongside an already-Generated one |
| `ApLinkInfo` | `{ id, code, status, sapApNumber, vendorCode }` - `sapApNumber` is `null` until that AP is Generated |
| `isAllGenerated` | `true` only when **every** budget plan item under this recap (regardless of vendor) is covered by a `Generated` AP item. An item with zero AP at all counts as not-done - this is not "every AP that exists happens to be Generated" |
| `location` | Warehouse location string from `WarehouseShadow.Location` (ERP-synced); `null` if not set |
| `budgetApproved` | `SUM(api.budget_realization)` - sum of AP item realized amounts already linked to this BP (non-deleted APs only) |
| `budgetVariance` | `budgetPlanTotal − budgetApproved` - remaining unallocated budget; `> 0` keeps the Generate button active |

> **Frontend migration note:** the old singular `accountPayableId`/`accountPayableCode`/`accountPayableStatus`/`sapApNumber` fields are gone, replaced by `accountPayables[]` + `isAllGenerated`. A recap used to imply at most one AP; it no longer does.

### `GET /api/v1/account-payables/available-items?vendorShadowId=3&budgetPlanIds=1,2`

Use `GET /api/v1/account-payables/{id}/available-items?vendorShadowId=3&budgetPlanIds=1,2` when editing a Draft AP. The backend validates the route ID and excludes that AP's own items; do not send an `excludeAccountPayableId` query parameter.

Returns cost items from budget plans that have approved recaps, filtered by vendor. By default, excludes items held by any non-deleted Draft or Generated AP.

**Query Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `vendorShadowId` | `long` | Yes | Filter items by vendor |
| `budgetPlanIds` | `string` | No | Comma-separated BP IDs to narrow results; omit to return all available for vendor |
| `includeGenerated` | `bool` | No | Default `false`. Set `true` to include items held by another Draft or Generated AP. Use `availabilityStatus` for the UI state |

The picker is warehouse-scoped using the caller's assignments or `X-Warehouse-Id`. By default, Draft and Generated APs both reserve an item. Response rows expose:
- `availabilityStatus: "Available"` - selectable; `takenByCode` is `null`
- `availabilityStatus: "TakenByDraft"` - held by another Draft AP
- `availabilityStatus: "AlreadyGenerated"` - held by a Generated AP

FE should use `availabilityStatus == "Available"` for selection. `isGenerated` and `takenByCode` are supplemental metadata.

**Response:** `List<AvailableApItemResponse>`

```json
[
  {
    "budgetPlanItemId": 5,
    "budgetPlanId": 1,
    "budgetPlanCode": "BP.260300000001",
    "budgetPlanRemark": "Bongkaran",
    "vendorShadowId": 3,
    "vendorCode": "V.LKL0001",
    "vendorName": "PT. XYZ",
    "itemCode": "Z.GEN001",
    "itemName": "B.Timbang",
    "coaCode": "5010101001",
    "coaName": "B.Timbang",
    "uomCode": "Kg",
    "uomName": "Kilogram",
    "isRfba": false,
    "billOfLading": "MEDUS23927878",
    "unitCost": 10.00,
    "unitCount": 8000000.00,
    "budgetPlanTotal": 80000000.00,
    "isGenerated": false,
    "takenByCode": null,
    "availabilityStatus": "Available"
  }
]
```

### `POST /api/v1/account-payables`

Creates a draft AP. Items are locked against double-booking immediately.

**Request Body:** `CreateAccountPayableRequest`

```json
{
  "vendorShadowId": 3,
  "remark": "AP for Bongkaran & Muat - March 2026",
  "docDate": "2026-03-03T00:00:00Z",
  "items": [5, 6, 7],
  "discountAmount": 120000.00
}
```

`discountAmount` is optional (default `0`) - see [Discount](#discount) below. `PUT /api/v1/account-payables/{id}` accepts the same field (nullable, only-if-provided - omit it to leave the AP's discount unchanged).

**Response:** `AccountPayableResponse` (see below)

> **`warnings`:** Create and Update responses populate `AccountPayableResponse.warnings` (`List<string>?`, `null` when there's nothing to flag) with one message when any selected `BudgetPlanItem` has no corresponding **Generated** Purchase Order line - `"Budget plan items {ids} have no generated purchase order. Create and generate a PO for these items before generating the AP."` This is advisory only at Draft time; the same condition becomes a hard `400` at Generate (see below).

### Discount

A single, manually-entered **nominal Rupiah amount** per AP document (not a percentage - the `discountPercent` shown to the user is always derived, never stored). Set via `discountAmount` on Create/Update. AP-only - Purchase Orders have no discount field.

All totals that involve the discount are computed server-side by one function, `AccountPayableTotalsCalculator`, and never duplicated:

- `AccountPayableResponse` (persisted AP, see below) - `discountAmount`, `discountPercent`, `totalRealization`, `totalVariance` are new fields; `taxInclusiveGrandTotal`'s value now nets out the discount.
- `POST /api/v1/account-payables/preview` (below) - the same totals, computed live from a not-yet-saved item selection + discount amount, with nothing persisted.
- SAP posting (`POST .../generate`, below) - the nominal amount is converted to a single `discountPercent` (`discountAmount / dppTotal * 100`) applied identically to every AP Invoice line item. RFBA APDP documents are generated separately from their source POs.

**Validation:** `discountAmount` must be `>= 0` and `<= dppTotal` (sum of the AP's `items[].budgetPlanTotal`) - enforced on Create/Update, `400 Bad Request` otherwise. `POST /preview` does **not** enforce this bound; a preview showing a negative total is a deliberate live signal to the user that the discount is too large, not an error. Preview item lookup still enforces the caller's warehouse scope.

> **Caveat:** the nominal discount and its equivalent `discountPercent` are mathematically identical on WAMS's side - there is no drift in the number WAMS computes and displays. What can differ slightly is SAP's *own* rounding of that percentage back into currency when the discount doesn't divide evenly across line bases (e.g. an odd 3-way split producing a repeating-decimal percent). Any resulting difference is bounded by roughly (line count x one rounding unit) and is inherent to SAP having no nominal-discount field in its own contract - not a bug in this implementation.

### `POST /api/v1/account-payables/preview`

Computes the same totals `POST`/`PUT` would return, without creating or modifying anything - no `AccountPayable` row is inserted, nothing is persisted. Intended for the frontend to call (debounced) while the user is still typing a discount amount or changing item selection on the Create form, before they hit Save.

**Request Body:** `PreviewAccountPayableRequest`

```json
{
  "vendorShadowId": 3,
  "items": [5, 6, 7],
  "discountAmount": 120000.00
}
```

**Response:** `AccountPayableTotalsResponse`

```json
{
  "items": [ /* same AccountPayableItemResponse[] shape as AccountPayableResponse.items */ ],
  "dppTotal": 240000000.00,
  "totalPpnAmount": 1650000.00,
  "totalPphAmount": 300000.00,
  "taxInclusiveGrandTotal": 16230000.00,
  "discountAmount": 120000.00,
  "discountPercent": 0.05,
  "totalRealization": 0.00,
  "totalVariance": 239880000.00
}
```

No `id`/`code`/`status`/`sapApNumber`/`createdAt` - nothing has been saved yet, so there's nothing to report there.

### `POST /api/v1/account-payables/{id}/generate`

Sends the AP Invoice to SAP and transitions status to `Generated`. Items become immutable after this call. All items on one AP share a single `isRfba` value (enforced at create/update time - see below). APDP is never created by this endpoint; RFBA items must already belong to a generated PO whose standalone APDP has succeeded.

- **`isRfba=false`**: one call, `POST /WAMS/APInvoice`. `whTax` is built by grouping items with a non-null `pphTaxTypeCode` by that code, summing each group's `budgetPlanTotal` into a single `{wtCode, taxableAmount}` entry per code - not sent per line.
- **`isRfba=true`**: one call, `POST /WAMS/APInvoice`. The persisted APDP document entry for each source PO is sent through `tapdp` (`{baseEntryDP, amountToDraw}`); `whTax` is omitted entirely on this path, even if individual lines have a `pphTaxTypeCode`. If an RFBA source PO has no APDP, the request is rejected before SAP is called.

If `discountAmount > 0`, every invoice line carries the same `discountPercent` (`discountAmount / dppTotal * 100`) - see [Discount](#discount) above. When `discountAmount = 0`, `discountPercent` is omitted from the SAP payload entirely (not sent as `0`).

On success, `sapApNumber` and `sapDocEntry` are stored and `sapApNumber` is returned in the response. APDP references are stored on each source PO and sent through `tapdp`. SAP doc entries are not part of the AP API response shape.

> **Every item must have a Generated PO.** Before calling SAP, the AP's `BudgetPlanItem`s are checked against generated Purchase Order lines (`IPurchaseOrderRepository.GetGeneratedPoLineRefsAsync`). If any item has no matching Generated PO line, the whole Generate call is rejected with `400 Bad Request` (`ItemsMissingGeneratedPo` - same message as the Create/Update `warnings` field above, but blocking here instead of advisory).

**Response:** `AccountPayableResponse` with `status: "Generated"` and `sapApNumber` populated.

> **Mixed `isRfba` is rejected.** `POST /account-payables`, `POST /account-payables/{id}/generate` (via create), and `PUT /account-payables/{id}` all reject a request whose selected items resolve to more than one distinct `isRfba` value - `400 Bad Request`. Split into two APs instead.

### `POST /api/v1/purchase-orders/{id}/generate-apdp`

Generates the standalone SAP APDP for all RFBA lines in one generated purchase order. It requires a generated PO with a SAP PO reference, calls `POST /WAMS/APDP` only, and stores the returned document entry on the PO. A repeated request returns the existing APDP state without creating another SAP document. Failed requests retain a retryable failure state.

The response is the purchase-order detail shape with an `apdp` object:

```json
{
  "status": "Generated",
  "sapDocEntry": 801,
  "amount": 200000.00,
  "generatedAt": "2026-08-31T12:00:00Z",
  "error": null
}
```

`apdp.status` is one of `PendingPo`, `NotRequired`, `Required`, `Processing`, `Generated`, or `Failed`.

### AP Detail Response Shape

**`AccountPayableResponse`**

```json
{
  "id": 1,
  "code": "AP-2603000001",
  "vendorShadowId": 3,
  "vendorCode": "V.LKL0001",
  "vendorName": "PT. XYZ",
  "status": "Generated",
  "docDate": "2026-03-03T00:00:00Z",
  "remark": "AP for Bongkaran & Muat - March 2026",
  "sapApNumber": "SAP-AP-a1b2c3d4e5f6...",
  "linkedBudgetPlanCodes": ["BP.260300000001", "BP.260300000002"],
  "linkedBudgetPlans": [
    { "id": 101, "code": "BP.260300000001" },
    { "id": 102, "code": "BP.260300000002" }
  ],
  "items": [
    {
      "id": 1,
      "budgetPlanItemId": 5,
      "vendorShadowId": 3,
      "vendorCode": "V.LKL0001",
      "vendorName": "PT. XYZ",
      "itemCode": "Z.GEN001",
      "itemName": "B.Timbang",
      "coaCode": "5010101001",
      "coaName": "B.Timbang",
      "uomCode": "Kg",
      "uomName": "Kilogram",
      "isRfba": false,
      "billOfLading": "MEDUS23927878",
      "unitCost": 10.00,
      "unitCount": 8000000.00,
      "budgetPlanTotal": 80000000.00,
      "budgetRealization": 0.00,
      "budgetVariance": 80000000.00,
      "sortOrder": 1,
      "ppnTaxTypeCode": "PPN11",
      "ppnRate": 11.00,
      "pphTaxTypeCode": "PPH23",
      "pphRate": 2.00,
      "ppnAmount": 550000.00,
      "pphAmount": 100000.00,
      "grandTotal": 5450000.00,
      "costTreatment": "Dibiayakan"
    }
  ],
  "grandTotal": 240000000.00,
  "totalPpnAmount": 1650000.00,
  "totalPphAmount": 300000.00,
  "taxInclusiveGrandTotal": 16230000.00,
  "createdAt": "2026-03-03T07:00:00Z",
  "createdByName": "Admin HO",
  "generatedAt": "2026-03-03T07:05:00Z",
  "generatedByName": "Admin HO",
  "discountAmount": 120000.00,
  "discountPercent": 0.05,
  "totalRealization": 0.00,
  "totalVariance": 239880000.00,
  "warnings": null
}
```

> **Note:** `budgetRealization` is currently `0` (placeholder). Per-item realization computation from WO actual costs is a planned enhancement. `budgetVariance` = `budgetPlanTotal - budgetRealization`.
>
> The 7 `ppn*`/`pph*`/`grandTotal` fields, plus `costTreatment`, on the AP item are **copied as-is** from the source `BudgetPlanItem` - never recalculated. This guarantees an AP's tax figures always match exactly what was approved in its Budget Plan, even if a tax rate (e.g. PPN's national rate) changes afterward. See [Tax Calculation (PPN & PPh)](README.md#tax-calculation-ppn--pph) for the full explanation and worked example. The document-level `"grandTotal"` on `AccountPayableResponse` (see above) remains `SUM(items[].unitCount)`, pre-tax - same reasoning as Purchase Orders and Budget Plans.
>
> `costTreatment` (nullable string, `"Dibiayakan"` | `"TidakDibiayakan"`) is a **label only** - it never affects `unitCost`, `unitCount`, `ppnAmount`/`pphAmount`, or `grandTotal`.
>
> `totalPpnAmount`, `totalPphAmount`, and `taxInclusiveGrandTotal` on `AccountPayableResponse` are server-computed sums across `items[]` - `SUM(ppnAmount)`, `SUM(pphAmount)`, and `SUM(items[].grandTotal)` respectively. They exist so the frontend never has to add up tax across line items itself: `grandTotal` = pre-tax subtotal, `taxInclusiveGrandTotal` = the final number to display as "grand total" on the document.
>
> **Discount fields** (`discountAmount`, `discountPercent`, `totalRealization`, `totalVariance`) are also server-computed, never stored as columns - see [Discount](#discount) above. `taxInclusiveGrandTotal` is net of `discountAmount` (`SUM(items[].grandTotal) - discountAmount`); `discountPercent` is derived (`discountAmount / grandTotal * 100`, display-only); `totalRealization` = `SUM(items[].budgetRealization)`; `totalVariance` = `SUM(items[].budgetVariance) - discountAmount`.
>
> `warnings` (`List<string>?`, only populated by Create/Update responses - `null` on the plain `GET` detail and on Generate) - see the note under [`POST /api/v1/account-payables`](#post-apiv1account-payables) above.

### SAP Integration Toggle

Same toggle as PO - `ErpApi:UseMockSap` in `appsettings.json`:
- `true` (default): `MockSapApiClient` - returns fake doc entries for PO APDP and AP Invoice calls
- `false`: `SapApiClient` - real HTTP calls to `POST /WAMS/APDP` and/or `POST /WAMS/APInvoice` (see the PO APDP and AP Generate sections above for the call sequence)

### AP Code Format

`AP-YYMMnnnnnn` - e.g., `AP-2603000001`

Sequence is scoped to the year-month prefix. `IgnoreQueryFilters()` ensures soft-deleted APs are counted to avoid code reuse.

### Error Responses

| HTTP Status | When |
|-------------|------|
| `400 Bad Request` | AP is not in `Draft` status for update/delete/generate; SAP returned no AP number; item unavailable (already in Generated AP, no approved recap, or vendor mismatch); selected items mix `isRfba` values; `discountAmount` is negative or exceeds `dppTotal` (Create/Update only - not enforced on `POST /preview`); one or more items have no Generated PO line (Generate only - `ItemsMissingGeneratedPo`, flagged as a non-blocking `warnings` entry on Create/Update instead) |
| `409 Conflict` | Another request is generating the same AP, or update/delete races with an active generation claim |
| `404 Not Found` | AP ID or vendor not found |

---

## Finance Reports

Base route: `/api/v1/finance-reports`

Read-only view over `Approved` budget plans for finance users - list of budget plans with vendor/budget/variance/linked-PO info, and a detail page with a per-cost-line tax breakdown table, plus an export of the cost-detail rows.

Scoped by warehouse access (`X-Warehouse-Id` header or user's assigned warehouses). The single-warehouse path validates the header value against the caller's actual warehouse assignment (`ForbiddenException` if not assigned and no global access, `NotFoundException` if the warehouse doesn't exist) - not just trusted at face value. Global-access roles (FINANCE_USER, HO_SPV) see all company data when the header is omitted.

| Method | Endpoint | Permission Required | Description | Response |
|--------|----------|---------------------|-------------|----------|
| GET | `/api/v1/finance-reports` | `report.finance-report.read` | List `Approved` budget plans with search, sort, and pagination | Paginated [`ApprovedBudgetPlanPoStatusResponse`](#approvedbudgetplanpostatusresponse) |
| GET | `/api/v1/finance-reports/{budgetPlanId}` | `report.finance-report.read` | Full detail for one budget plan: header, per-cost-line breakdown, budget recap | [`FinanceReportDetailResponse`](#financereportdetailresponse) |
| GET | `/api/v1/finance-reports/{budgetPlanId}/export` | `report.finance-report.export` | Export the budget plan's cost-detail rows. Optional `workOrderId` query param scopes the export to a single Work Order's rows | `.xlsx` / `.csv` / `.pdf` file stream |

### `GET /api/v1/finance-reports`

Identical query to [`GET /api/v1/purchase-orders/approved-budget-plans`](#get-apiv1purchase-ordersapproved-budget-plans) - same `page`/`limit`/`search`/`sortBy`/`sortOrder` parameters, same `ApprovedBudgetPlanPoStatusResponse` shape, same warehouse scoping. `FinanceReportService.GetAllAsync` delegates straight to `IPurchaseOrderService.GetApprovedBudgetPlansAsync`; there is no separate query or DTO for this list.

**Example:** `GET /api/v1/finance-reports?search=AC+INDO&sortBy=docDate&sortOrder=desc&page=1&limit=20`

### `GET /api/v1/finance-reports/{budgetPlanId}`

Returns `404` if the budget plan doesn't exist or is outside the caller's tenant/warehouse scope.

**Cost Detail row semantics:** one row = one `PurchaseOrderItem`, left-joined to its `WorkOrder` via the shared `BudgetPlanItemId` (`WorkOrder.BudgetPlanItemId` is unique among active WOs; `PurchaseOrderItem.BudgetPlanItemId` is not, so multiple cost-detail rows can legitimately share the same `workOrderId` - e.g. several PO items generated against one budget line over time). WO-derived columns (`workOrderId`, `pic`, `startDate`, `endDate`) are `null` when no WorkOrder has been created yet for that budget item.

**Response:** `FinanceReportDetailResponse`

```json
{
  "header": {
    "budgetPlanId": 1,
    "budgetNo": "BP.2606000094",
    "templateId": "T.0001",
    "status": "Approved",
    "remark": "Bongkaran",
    "docDate": "2026-06-29T00:00:00Z",
    "warehouseCode": "WHLPG01",
    "warehouseName": "MNP Blok A",
    "location": "Lampung"
  },
  "costDetails": [
    {
      "purchaseOrderItemId": 101,
      "workOrderId": "26040901UD",
      "blNumber": "ONEYVTZF01978300",
      "vessel": "ABC123",
      "product": "Kedelai",
      "pic": "Mr. AB",
      "isRfba": true,
      "startDate": "2026-05-10T00:00:00Z",
      "endDate": "2026-05-10T00:00:00Z",
      "totalPrice": 30000000.00,
      "isPpnApplied": true,
      "ppnRatePercent": 11.00,
      "totalPricePpn": 33300000.00,
      "isPphApplied": true,
      "pphType": "PPh 22 (Barang)",
      "totalPricePph": 33450000.00,
      "grandTotal": 33450000.00,
      "paymentStatus": "Paid"
    }
  ],
  "dpp": 177285000.00,
  "totalPpn": 177285000.00,
  "totalPph": 177285000.00,
  "grandTotal": 177285000.00,
  "budgetRecap": {
    "budgetPlan": 160000000.00,
    "budgetRealization": 177285000.00,
    "budgetVariance": -17285000.00
  }
}
```

| Field | Description |
|-------|-------------|
| `header.templateId` | `BudgetTemplate.Code` |
| `costDetails[].workOrderId` | `WorkOrder.Code` (a string, not the numeric WO id); `null` if no WorkOrder is linked yet |
| `costDetails[].vessel` | `PurchaseOrderItem.BudgetPlanItem.Spk.CardName`; `null` if the budget item has no linked SPK |
| `costDetails[].isPpnApplied` / `isPphApplied` | `true` when `PpnTaxTypeCode` / `PphTaxTypeCode` is non-null on the `PurchaseOrderItem` |
| `costDetails[].pphType` | Resolved `TaxType.Name` for the item's `PphTaxTypeCode` (company + `Pph` category scoped); `null` if not applicable |
| `costDetails[].paymentStatus` | `Unpaid` \| `Paid` - new per-line field on `PurchaseOrderItem`, defaults to `Unpaid` |
| `dpp` / `totalPpn` / `totalPph` / `grandTotal` | Sums of `totalPrice` / `totalPricePpn` / `totalPricePph` / `grandTotal` across all `costDetails` rows |
| `budgetRecap.budgetPlan` | `SUM(bpi.cost_value × bpi.quantity)` over the budget plan's items |
| `budgetRecap.budgetRealization` | `SUM(poi.cost_value × poi.quantity)` over all non-deleted PO items linked to the plan - **not** filtered by `paymentStatus` (counts all generated POs, matching the list endpoint's `budgetApproved` semantics) |
| `budgetRecap.budgetVariance` | `budgetPlan − budgetRealization` |

### `GET /api/v1/finance-reports/{budgetPlanId}/export`

Exports the same `costDetails` rows returned by the detail endpoint above, as a downloadable file. Row shape mirrors `FinanceReportCostDetailResponse` (WO code, BL number, vessel, product, PIC, RFBA, dates, PPN/PPh breakdown, grand total, payment status).

| Query Parameter | Type | Description |
|---|---|---|
| `workOrderId` | string (optional) | Scope the export to rows matching this `WorkOrder.Code`. Omit to export every cost-detail row for the budget plan. |
| `format` | `Xlsx` \| `Csv` \| `Pdf` (default `Xlsx`) | Output format |

**Example (whole budget plan):** `GET /api/v1/finance-reports/1/export?format=Xlsx`

**Example (single Work Order):** `GET /api/v1/finance-reports/1/export?workOrderId=26040901UD&format=Xlsx`

Returns `404` under the same conditions as the detail endpoint (budget plan not found or outside the caller's tenant/warehouse scope). An unmatched `workOrderId` returns a `200` with zero rows rather than a `404`.

---

## RCA

Base route: `/api/v1/rca`

Generates an on-demand **Rekapitulasi Kas Operasional** PDF - a formal warehouse cost summary document used by finance to process cash transfers and collect approver signatures. Data is assembled from existing budget plan, work order, and workflow records; nothing is stored in the database.

All endpoints require authentication. Gated by a dedicated `rca.report.export` permission, granted only to SUPER_ADMIN (via `*.*.*`) and FINANCE_USER - WAREHOUSE_ADMIN and HO_SPV cannot call this endpoint. Warehouse access is further scoped using the same RBAC pattern as Finance Reports for roles that do have the permission.

| Method | Endpoint | Permission Required | Description | Response |
|--------|----------|---------------------|-------------|----------|
| GET | `/api/v1/rca/export` | `rca.report.export` | Generate and download the RCA PDF for a warehouse + date range | `application/pdf` |

**Query parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `warehouseCode` | string | Yes | Warehouse code (e.g. `WHS001`) |
| `dateFrom` | `DateOnly` (`yyyy-MM-dd`) | Yes | Start of the period (inclusive) |
| `dateTo` | `DateOnly` (`yyyy-MM-dd`) | Yes | End of the period (inclusive) |

**Response headers:**

```
Content-Type: application/pdf
Content-Disposition: attachment; filename="RCA-{warehouseCode}-{dateFrom:yyyyMMdd}-{dateTo:yyyyMMdd}.pdf"
```

**Error cases:**

| Status | When |
|--------|------|
| `400 Bad Request` | `dateFrom > dateTo` |
| `403 Forbidden` | Caller lacks `report.finance-report.export` permission, or the warehouse is not in the caller's allowed set |
| `404 Not Found` | `warehouseCode` does not exist in this company |

**Example:**

```bash
curl -OJ "http://localhost:8080/api/v1/rca/export?warehouseCode=WHS001&dateFrom=2026-02-13&dateTo=2026-02-19" \
  -H "Authorization: Bearer <token>"
```

### Document Structure

The PDF is A4 landscape, 7pt table cells, rendered with QuestPDF.

| Section | Content |
|---------|---------|
| **Header** | Company logo (if set), company name, form title "REKAPITULASI KAS OPERASIONAL", RCA ID, tanggal, area, gudang, RFBA (blank) |
| **Main table** | One row per cost line. Columns: Tanggal Kegiatan, COA & Component, Bill of Lading, Pos Biaya, Tipe Operasional, Product, Berat/Jumlah, Satuan, Keterangan Pos Biaya, Keterangan lain-lain, Jumlah dalam Rupiah. Footer row: grand total |
| **Per-pos summary** | Subtotal per Pos Biaya code. Right side: blank bank transfer block (Ditransfer ke / Nama / Acc no / Bank) |
| **Signatures** | Four slots (Dibuat oleh, Disetujui oleh, Diketahui oleh ×2) with underline and auto-filled approver names from the most recent budget plan workflow stages in the date range |

### RCA ID Format

```
{seq}/RCA/{companyCode}/{warehouseCode}/{dateTo:ddMMMyyyy}
```

Example: `215/RCA/GCU/WHS001/19Feb2026`

- `companyCode` - initials derived from company name words of 3+ letters (e.g. "PT. GERBANG CAHAYA UTAMA" → "GCU")
- `seq` - count of distinct (warehouseCode, week) combinations in the date range
- Generated at download time; not persisted

---

## Dashboard

Base route: `/api/v1/dashboard`

All endpoints require authentication. Scoped by warehouse access using the same resolution as Finance Reports - global-access roles see all company data; scoped users see their assigned warehouses.

Permission required: `report.dashboard.read` - held by `FINANCE_USER`, `HO_SPV`, and `WAREHOUSE_ADMIN`.

| Method | Endpoint | Permission Required | Description | Response |
|--------|----------|---------------------|-------------|----------|
| GET | `/api/v1/dashboard/summary` | `report.dashboard.read` | KPI cards: budget achievement %, POs without AP (+ new-in-7-days), open work orders (+ active warehouse count), pending approvals (+ overdue >48h) | [`DashboardSummaryResponse`](#dashboardsummaryresponse) |
| GET | `/api/v1/dashboard/activities` | `report.dashboard.read` | Today's budget plans, paginated and searchable | Paginated [`DashboardActivityResponse`](#dashboardactivityresponse) |
| GET | `/api/v1/dashboard/history` | `report.dashboard.read` | Calendar event dot counts + last 20 events for the given month | [`DashboardHistoryResponse`](#dashboardhistoryresponse) |

**Query parameters for `/activities`:** standard [datatable params](#datatable-query-parameters) (`search`, `sortBy`, `sortOrder`, `page`, `limit`)

**Query parameters for `/history`:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `year` | integer | Yes | Calendar year (e.g. `2026`) |
| `month` | integer | Yes | Month number 1–12 |

**Example:** `GET /api/v1/dashboard/history?year=2026&month=6`

### Implementation notes

- **Summary** - single DB round-trip via CTEs (`budget_kpi`, `po_qualifying`/`po_kpi`, `wo_kpi`, `pending_docs`/`approval_kpi`) cross-joined. No N+1.
- **Activities** - pagination (`OFFSET`/`LIMIT`) happens inside a `paged` CTE first; the per-plan vendor-name and RFBA lookups are correlated subqueries attached only to the already-paginated rows, and `COUNT(*) OVER()` returns the total in the same pass. Filters to budget plans created today (UTC); `date` in the response is `created_at`, not `docDate`.
- **History** - one UNION query; C# aggregates the calendar map and caps events at 20 without a second query.

### DTOs

#### DashboardSummaryResponse
```json
{
  "budgetAchievedPercent": "decimal (0–100, realized cost ÷ total budget × 100)",
  "totalBudgetValue": "decimal",
  "totalActualValue": "decimal",
  "activePoWithoutApCount": "integer (Generated POs with no matching AP)",
  "newPoWithoutApLast7DaysCount": "integer (subset of activePoWithoutApCount created in the last 7 days, WoW proxy)",
  "openWorkOrderCount": "integer (Submitted WOs not yet in a Closed recap)",
  "activeWarehouseCount": "integer (distinct warehouses with an open work order)",
  "pendingApprovalCount": "integer (BPs in InApproval + recap Pending, filtered to stages the caller's roles can approve)",
  "overdueApprovalCount": "integer (subset of pendingApprovalCount pending more than 48h)"
}
```

#### DashboardActivityResponse
```json
{
  "budgetPlanId": "long",
  "budgetNo": "string",
  "vendorName": "string | null (distinct vendor names across the plan's items, comma-separated)",
  "remark": "string | null",
  "anyRfba": "boolean (true if any item on the plan is flagged RFBA)",
  "location": "string | null",
  "date": "datetime (budget plan created_at)",
  "status": "string (raw BudgetPlanStatus value, e.g. InApproval)",
  "statusDisplay": "string (human-readable status, e.g. In Approval)"
}
```

#### DashboardHistoryResponse
```json
{
  "calendarDays": [
    {
      "date": "string (DateOnly, yyyy-MM-dd)",
      "eventCount": "integer"
    }
  ],
  "recentEvents": [
    {
      "occurredAt": "datetime",
      "eventType": "string (e.g. WorkOrderSubmitted)",
      "activityTypeName": "string",
      "warehouseCode": "string"
    }
  ]
}
```

> `calendarDays` contains only days with at least one event in the requested month. `recentEvents` is capped at 20 entries, ordered by `occurredAt` descending. Requesting a future month returns `calendarDays: []` and `recentEvents: []`.

---

## SPK (Base Documents)

Base route: `/api/v1/spk`

SPK = **Surat Perintah Kerja** (Work Order). Records are synced from the client's SAP B1 ERP via background scheduler (`SpkSyncService`) and stored locally in `spk_shadows`. They are read-only from the API side - linking to budget plans is done via the budget plan SPK-items sub-resource.

> **Warehouse scope:** `GET /api/v1/spk`, `GET /api/v1/spk/{id}`, and `GET /api/v1/spk/export` are all scoped to the caller's warehouse access, same convention as the budget plan list endpoint: an `X-Warehouse-Id` header pins results to that one warehouse (403 if the caller isn't assigned to it), otherwise results are restricted to the caller's assigned warehouses unless they hold a role with global access. Attaching an SPK to a budget plan (`POST /budget-plans/{id}/spk-items`, and `spkShadowIds` on create/update) applies this same caller-warehouse-scope check to the SPK - not the budget plan's own warehouse, so an SPK from any warehouse the caller can access may be linked, even if it differs from the plan's warehouse.

| Method | Endpoint | Permission Required | Description | Request Body | Response |
|--------|----------|---------------------|-------------|--------------|----------|
| GET | `/api/v1/spk` | `budget.plan.read` | Search and list synced SPK records | Query: [datatable params](#datatable-query-parameters) + `type`, `docStatus`, `whsCode` | Paginated [`SpkShadowResponse`](#spkshadowresponse) |
| GET | `/api/v1/spk/{id}` | `budget.plan.read` | Get a single SPK record by ID | - | [`SpkShadowResponse`](#spkshadowresponse) |

**Additional query parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `type` | `LO` \| `MO` \| `BL` | Filter by SPK type |
| `docStatus` | `O` \| `C` | Filter by document status (`O` = Open, `C` = Closed) |
| `whsCode` | string | Filter by warehouse code |

**Search fields:** `docNo`, `baseDocNo`, `cardName`, `itemCode`, `itemName`, `type`

**Sortable fields:** `docNo` (default desc), `cardName`, `syncedAt`

**`BL` rows:** ERP also returns rows with `type: "BL"` - bills of lading that don't have a matching MO/LO document yet. These carry only `blNo` (and constant `uoM`/`docStatus`); every order-specific field (`docNo`, `itemCode`, `cardCode`, `whsCode`, etc.) is blank and `quantity`/`deliveryQty` are `0`. They're deduplicated/keyed by `blNo` instead of `(docNo, itemCode)` during sync, and can be linked to a budget plan the same way as any other SPK row - the "quantity exceeds SPK" check is skipped when the linked row's `quantity` is `0`/absent, so the budget item's quantity is user-entered freely.

### SpkShadowResponse

```json
{
  "id": 1,
  "type": "LO",
  "docNo": "LO-2024-00001",
  "baseDoc": "SO",
  "baseDocNo": "SO-2024-00001",
  "cardCode": "V001",
  "cardName": "PT Vendor Utama",
  "itemCode": "ITEM-001",
  "itemName": "Besi Beton 10mm",
  "quantity": 100.0000,
  "deliveryQty": 50.0000,
  "uoM": "TON",
  "packType": "BULK",
  "whsCode": "WH-001",
  "whsName": "Gudang Utama",
  "docStatus": "O",
  "blNo": null
}
```

A `BL`-type row looks like this instead (every order field blank, `quantity`/`deliveryQty` `0`, only `blNo` populated):

```json
{
  "id": 2,
  "type": "BL",
  "docNo": "",
  "baseDoc": "",
  "baseDocNo": "",
  "cardCode": "",
  "cardName": "",
  "itemCode": "",
  "itemName": "",
  "quantity": 0.0000,
  "deliveryQty": 0.0000,
  "uoM": "Kg",
  "packType": "",
  "whsCode": "",
  "whsName": "",
  "docStatus": "O",
  "blNo": "COAU7256636630"
}
```

### AddSpkItemRequest

Used with `POST /api/v1/budget-plans/{id}/spk-items`:

```json
{
  "spkShadowId": 1
}
```

### BudgetPlanSpkItemResponse

```json
{
  "id": 1,
  "spkShadowId": 42,
  "type": "LO",
  "docNo": "LO-2024-00001",
  "baseDoc": "SO",
  "baseDocNo": "SO-2024-00001",
  "cardCode": "V001",
  "cardName": "PT Vendor Utama",
  "itemCode": "ITEM-001",
  "itemName": "Besi Beton 10mm",
  "quantity": 100.0000,
  "deliveryQty": 50.0000,
  "uoM": "TON",
  "packType": "BULK",
  "whsCode": "WH-001",
  "whsName": "Gudang Utama",
  "docStatus": "O",
  "blNo": null,
  "sortOrder": 1,
  "itemShadowId": 10
}
```

> `itemShadowId` is resolved by joining `item_shadows` on `itemCode` + `companyId`. It is `null` if the ERP item code has not been synced to the local shadow table yet. The frontend uses this to populate the **Item Code dropdown** in the Cost Detail section (see [4b](#4b-cost-detail--item-code-dropdown-populated-from-base-document)).

---

## Notifications

Base route: `/api/v1/notifications`

All endpoints require authentication. Notifications are stored per recipient user and delivered in realtime via **Server-Sent Events (SSE)**. See [Notification Events](#notification-events) for what triggers each one and who receives it.

### Notification Events

Every notification has a `type` (what happened) and a `referenceType`/`referenceId` (which record it's about, e.g. which budget plan). This table covers every event the backend currently sends - if a `type` isn't listed here, it was sent through the manual test endpoint and isn't a real business event.

| `type` | When it fires | Who receives it | Example title | Example message |
|---|---|---|---|---|
| `budget_plan_pending_approval` | A budget plan moves into a new approval stage | Everyone with an approver role for that stage (not the person who just approved) | "Budget Plan Waiting for Approval" | "Budget plan BP.2604000001 is waiting for your stage 2 approval (Finance Review)." |
| `budget_plan_stage_approved` | A budget plan clears one approval stage, but has more stages left | The plan's creator, unless they were the one approving | "Budget Plan Approved - Stage 1" | "Budget plan BP.2604000001 passed stage 1 approval (Warehouse Head)." |
| `budget_plan_approved_final` | A budget plan clears its last approval stage | The plan's creator, unless they were the one approving | "Budget Plan Fully Approved" | "Budget plan BP.2604000001 has completed all approval stages." |
| `budget_plan_rejected` | A budget plan is rejected, at any stage | The plan's creator, unless they were the one rejecting | "Budget Plan Rejected" | "Budget plan BP.2604000001 has been rejected." |
| `budget_plan_approval_reminder` | A budget plan has sat in one approval stage too long without action (background job - see [Approval Reminder Scheduler](#approval-reminder-scheduler)) | Every approver for that stage, grouped into one notification per approver, at most once per day | "3 Budget Plans Pending Approval - Stage 1" | "You have 3 budget plans waiting for your stage 1 approval (Warehouse Head)." |
| any other string | Manual test via `POST /api/v1/notifications/test` - not a real business event, only sends to yourself | The caller | Caller-supplied | Caller-supplied |

What this means for the frontend:
- Use `type` to decide how a notification looks (icon, color, grouping) - it tells you *what happened*.
- Use `route` to decide where clicking it goes - it already points at the right page, no need to inspect `type` or `referenceType` yourself. See [Route Resolution](#route-resolution).
- The list endpoint (`GET /api/v1/notifications`) and the SSE stream return the exact same JSON shape, so the same rendering code handles both a page load and a live push.

### Route Resolution

Every `NotificationResponse` includes a `route` field: a relative frontend path computed from `referenceType`/`referenceId`, so the frontend never has to derive it - it can redirect on click/tap with no lookup. Resolution lives in `NotificationRouteResolver` (`WAMS.Application/Services/Notifications`); unmapped `referenceType`s resolve to `null`, and the frontend should skip the redirect action when `route` is `null`.

| `referenceType` | `referenceId` | `route` |
|---|---|---|
| `budget_plan` | plan ID | `/budgeting/plan/{id}` |
| `budget_plan_batch` | `stage_1` | `/budgeting/plan?status=Submitted` |
| `budget_plan_batch` | `stage_{N}`, N > 1 | `/budgeting/plan?status=InApproval` |
| anything else (e.g. `test` from the manual test endpoint) | - | `null` |

| Method | Endpoint | Permission Required | Description | Request Body | Response |
|--------|----------|---------------------|-------------|--------------|----------|
| GET | `/api/v1/notifications` | Authenticated user | List the current user's notifications with pagination and optional unread filtering | Query: `page`, `limit`, `unreadOnly` | Paginated [`NotificationResponse`](#notificationresponse) |
| POST | `/api/v1/notifications/{id}/read` | Authenticated user | Mark one notification as read | - | `204 No Content` |
| POST | `/api/v1/notifications/read-all` | Authenticated user | Mark all of the current user's unread notifications as read | - | `200 OK` `{ "updatedCount": number }` |
| POST | `/api/v1/notifications/test` | Authenticated user | Send a manual test notification to the currently authenticated user | [`SendTestNotificationRequest`](#sendtestnotificationrequest) | `202 Accepted` |
| GET | `/api/v1/notifications/stream` | Authenticated user | Open SSE stream for realtime notification delivery | - | `text/event-stream` |

### Query Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | int | `1` | Page number |
| `limit` | int | `20` | Items per page |
| `unreadOnly` | bool | `null` | When `true`, only unread notifications are returned |

### SSE Behaviour

On connection, the stream sends:

```text
event: connected
data: {"user_id":123}
```

On notification publish, the stream sends:

```text
event: notification
data: {"id":12,"type":"budget_plan_approved_final","title":"Budget Plan Fully Approved","message":"Budget plan BP.2604000001 has completed final approval.","referenceType":"budget_plan","referenceId":"7","status":"unread","createdAt":"2026-04-30T09:15:00Z","readAt":null,"recipientUserId":123,"actorUserId":45,"route":"/budgeting/plan/7"}
```

Keepalive comments are emitted periodically:

```text
: ping
```

### Example: List Notifications

```http
GET /api/v1/notifications?unreadOnly=true&page=1&limit=10
Authorization: Bearer <access_token>
```

**Response**

```json
{
  "success": true,
  "data": [
    {
      "id": 12,
      "type": "budget_plan_approved_final",
      "title": "Budget Plan Fully Approved",
      "message": "Budget plan BP.2604000001 has completed final approval.",
      "referenceType": "budget_plan",
      "referenceId": "7",
      "status": "unread",
      "createdAt": "2026-04-30T09:15:00Z",
      "readAt": null,
      "recipientUserId": 123,
      "actorUserId": 45,
      "route": "/budgeting/plan/7"
    }
  ],
  "meta": {
    "page": 1,
    "limit": 10,
    "total": 1,
    "totalPages": 1
  },
  "requestId": "request-id"
}
```

### Example: Mark Notification as Read

```http
POST /api/v1/notifications/12/read
Authorization: Bearer <access_token>
```

Response: `204 No Content`

### Example: Mark All Notifications as Read

```http
POST /api/v1/notifications/read-all
Authorization: Bearer <access_token>
```

**Response**

```json
{
  "updatedCount": 4
}
```

`updatedCount` is the number of previously-unread notifications that were marked read (`0` if there were none).

### Example: Send Test Notification

```http
POST /api/v1/notifications/test
Authorization: Bearer <access_token>
Content-Type: application/json
```

```json
{
  "type": "test_notification",
  "title": "Realtime Test",
  "message": "This is a manual test notification",
  "referenceType": "test",
  "referenceId": "manual-1"
}
```

This endpoint always sends the notification to the **currently authenticated user**, which makes it safe for manual SSE testing.

If realtime (SSE) delivery fails for any notification, the approval/rejection action still succeeds - the notification is already persisted, so it just shows up next time the frontend calls `GET /api/v1/notifications`.

### Approval Reminder Scheduler

`BudgetPlanReminderBackgroundService` runs within a configurable active window (default 09:00–17:00 WIB) and sends reminders when a BP has been pending approval longer than the configured threshold. It does **not** run outside the window - it computes the delay to the next valid slot and sleeps until then.

| Config key | Default | Description |
|---|---|---|
| `BudgetPlanReminder:Enabled` | `true` | Enable or disable the scheduler |
| `BudgetPlanReminder:IntervalMinutes` | `60` | Interval between runs within the active window |
| `BudgetPlanReminder:ThresholdHours` | `24` | Hours a BP must be pending before a reminder fires |
| `BudgetPlanReminder:CooldownHours` | `24` | Minimum hours between reminders for the same BP |
| `BudgetPlanReminder:ActiveWindowStartHour` | `9` | First hour of day (local time) when runs are allowed |
| `BudgetPlanReminder:ActiveWindowEndHour` | `17` | Exclusive end of active window - no runs at or after this hour |
| `BudgetPlanReminder:TimeZoneId` | `Asia/Jakarta` | IANA timezone for interpreting the active window |

**Reminder logic:**
- Overdue BPs are grouped by `(company, warehouse, stage)`. Approvers are queried once per group - not once per BP.
- `Submitted` BPs (stage 1) → each `WAREHOUSE_HEAD` assigned to that warehouse receives **one** aggregated notification listing all pending BPs for their warehouse
- `Overdue BPs are grouped by current pending stage; recipients are resolved from that stage's `approverRoles` snapshot
- Notification count is **O(approvers)**, not O(BPs) - whether there are 1 or 100 overdue BPs, each approver gets exactly one notification per cooldown window
- **Spam prevention:** cooldown is checked per approver (`notifications` table). If a `budget_plan_approval_reminder` was already sent to that user within `CooldownHours`, the entire batch for them is skipped. With defaults, each approver receives at most **one reminder per day**
- Notification `referenceType = "budget_plan_batch"`, `referenceId = "stage_1"` or `"stage_2"` - the returned `route` already resolves this to `/budgeting/plan?status=Submitted` (stage 1) or `/budgeting/plan?status=InApproval` (stage 2+), so the frontend can redirect directly without deriving the filter itself
- When `Email:Enabled=true`, one summary email per approver is sent listing all pending BPs and how long each has been waiting

### DTOs

#### NotificationResponse

```json
{
  "id": 12,
  "type": "budget_plan_approved_final",
  "title": "Budget Plan Fully Approved",
  "message": "Budget plan BP.2604000001 has completed final approval.",
  "referenceType": "budget_plan",
  "referenceId": "7",
  "status": "unread",
  "createdAt": "2026-04-30T09:15:00Z",
  "readAt": null,
  "recipientUserId": 123,
  "actorUserId": 45,
  "route": "/budgeting/plan/7"
}
```

`route` is a ready-to-use frontend path derived server-side from `referenceType`/`referenceId` - the frontend can redirect to it directly (e.g. `router.push(notification.route)`) with no client-side mapping. It is `null` for notification types with no known destination page (e.g. the `test` endpoint below). See [Route Resolution](#route-resolution).

#### SendTestNotificationRequest

```json
{
  "type": "test_notification",
  "title": "Realtime Test",
  "message": "This is a manual test notification",
  "referenceType": "test",
  "referenceId": "manual-1"
}
```

---

## Files

Base route: `/api/v1/files/{entityType}/{entityId}`

> **Authorization.** These routes carry no `RequirePermission` attribute - access is inherited from the parent work order. The same warehouse-access check that guards `GET /work-orders/{id}` runs here, so a user outside the work order's warehouse gets `403` on every file operation. There is no separate `document.*` permission.

All endpoints require authentication. The file module is entity-generic: one controller handles attachments for multiple modules. File bytes are stored in the configured storage backend, while metadata is stored in the database.

| Method | Endpoint | Permission Required | Description | Request Body | Response |
|--------|----------|---------------------|-------------|--------------|----------|
| POST | `/api/v1/files/{entityType}/{entityId}` | JWT only | Upload one or more files and attach them to the target record | `multipart/form-data` with `files[]` | [`FileAttachmentResponse[]`](#fileattachmentresponse) |
| GET | `/api/v1/files/{entityType}/{entityId}` | JWT only | List all files attached to the target record | - | [`FileAttachmentResponse[]`](#fileattachmentresponse) |
| GET | `/api/v1/files/{entityType}/{entityId}/{fileId}` | JWT only | Stream/download a specific file by attachment ID | - | Binary stream |
| DELETE | `/api/v1/files/{entityType}/{entityId}/{fileId}` | JWT only | Delete a specific attachment (uploader or entity owner) | - | `204 No Content` |

### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `entityType` | string | Target module key. Only supported value: `work-orders`. Anything else returns `404` |
| `entityId` | long | ID of the record the file belongs to |
| `fileId` | long | Attachment metadata ID in `file_attachments` |

### Upload Rules

Upload validation runs in the **Application layer** in three sequential gates. All gates must pass before any file is stored - if any file fails, zero files are saved.

**Gate 1 - list validation (fast, no DB):**
- at least one file must be present (field name: `files[]`)
- the number of files in the request must not exceed `FileAttachments:MaxAttachmentsPerEntity` (default: 10)
- each file: size must be > 0 and ≤ `FileAttachments:MaxFileSizeBytes` (default: 20 MB)
- each file: content type must be in the configured allowlist (PDF, PNG, JPEG, DOC, DOCX, XLS, XLSX)
- each file: filename must not be empty

**Gate 2 - entity + count/size check (DB):**
- entity must exist before files are attached
- `(existing attachment count) + (new file count)` must not exceed `MaxAttachmentsPerEntity`
- `(existing total attachment size) + (new file sizes)` must not exceed `FileAttachments:MaxTotalSizeBytesPerEntity` (default: 50 MB) - a separate, per-record aggregate cap on top of the per-file `MaxFileSizeBytes` limit

**Gate 3 - magic-byte signature check (in-memory):**
- each file's first 8 bytes are verified against the declared content type
- a mismatch on any file rejects the entire batch

**Other rules:**
- original filename is sanitized for display only; storage key is generated internally using the entity path plus a UUID
- entity-specific locking applies - for example, `work-orders` rejects all attachment mutations (`403`) once the WO is `Submitted`

### Download Behaviour

- the API resolves files by `fileId`, not by storage path
- the stored `contentType` is returned to the client
- file responses are streamed
- HTTP range processing is enabled, which is important for PDF viewers and large media clients
- **`src`-usable auth (added 2026-07-13):** all `/api/v1/files/**` routes still require a valid JWT, but the token may be passed as `?token={accessToken}` instead of the `Authorization` header. This lets the download route be used directly as an `<img src>`, `<a href>`, or `<iframe src>` - browsers don't attach custom headers to those requests. `Authorization: Bearer` still works and takes priority; the query param is only read when no `Authorization` header is present. Use the normal short-lived (15 min) access token - there is no separate long-lived/signed download token.

### Delete Permission

Deletion is allowed when **either** condition is true:
- The requesting user is the original **uploader** of the file
- The requesting user is the **owner** (creator) of the parent entity

Entity-specific locking may also apply - for example, `work-orders` rejects all attachment mutations (`403`) once the WO is `Submitted`.

### Storage Design

- **Database stores metadata only**: original filename, MIME type, size, storage key, uploader, timestamps, and entity linkage
- **Storage backend is swappable** through `IFileAttachmentStorage` - no code change required to switch providers
- **Local disk** (development default): used when `ObjectStorage:Endpoint` is not set; files written under `FileAttachments:RootPath`
- **S3-compatible** (production): set `ObjectStorage:Endpoint` at startup; supports MinIO, AWS S3, Cloudflare R2, Backblaze B2, and any NAS with an S3-compatible API
- **Delete flow** removes the DB row first, then attempts storage deletion; storage cleanup failure is logged but does not block the HTTP response

### DTOs

#### FileAttachmentResponse
```json
{
  "id": "long",
  "entityType": "string",
  "entityId": "long",
  "originalFileName": "string",
  "contentType": "string",
  "fileSize": "string",
  "fileSizeRaw": "long",
  "uploadedByUserId": "long",
  "uploadedByName": "string | null",
  "uploadedAt": "datetime",
  "url": "string"
}
```

`url` is a relative path to the download endpoint (no leading `/`), e.g. `api/v1/files/work-orders/11/4`. Prepend the API base URL to get a fully qualified URL. For `<img src>`/`<a href>`/`<iframe src>` usage, append `?token={accessToken}` since those tags can't send an `Authorization` header - see [Download Behaviour](#download-behaviour).

### Example: Upload Files

Single file or multiple files - same endpoint. Repeat `-F "files[]=@..."` for each file.

```bash
# Single file
curl -X POST "http://localhost:8080/api/v1/files/work-orders/1" \
  -H "Authorization: Bearer <access_token>" \
  -F "files[]=@./budget-plan-april.pdf"

# Multiple files
curl -X POST "http://localhost:8080/api/v1/files/work-orders/1" \
  -H "Authorization: Bearer <access_token>" \
  -F "files[]=@./budget-plan-april.pdf" \
  -F "files[]=@./invoice.png"
```

**Response** - always an array, even for a single file upload
```json
{
  "success": true,
  "message": "Files uploaded",
  "requestId": "req_123",
  "data": [
    {
      "id": 17,
      "entityType": "work-orders",
      "entityId": 1,
      "originalFileName": "budget-plan-april.pdf",
      "contentType": "application/pdf",
      "fileSize": "240.1 KB",
      "fileSizeRaw": 245812,
      "uploadedByUserId": 5,
      "uploadedByName": "System Administrator",
      "uploadedAt": "2026-04-24T03:15:00Z"
    },
    {
      "id": 18,
      "entityType": "work-orders",
      "entityId": 1,
      "originalFileName": "invoice.png",
      "contentType": "image/png",
      "fileSize": "18.0 KB",
      "fileSizeRaw": 18432,
      "uploadedByUserId": 5,
      "uploadedByName": "System Administrator",
      "uploadedAt": "2026-04-24T03:15:01Z"
    }
  ]
}
```

### Example: List Files

```bash
curl "http://localhost:8080/api/v1/files/work-orders/1" \
  -H "Authorization: Bearer <access_token>"
```

**Response**
```json
{
  "success": true,
  "message": "Files retrieved",
  "requestId": "req_123",
  "data": [
    {
      "id": 17,
      "entityType": "work-orders",
      "entityId": 1,
      "originalFileName": "budget-plan-april.pdf",
      "contentType": "application/pdf",
      "fileSize": "240.1 KB",
      "fileSizeRaw": 245812,
      "uploadedByUserId": 5,
      "uploadedByName": "System Administrator",
      "uploadedAt": "2026-04-24T03:15:00Z"
    }
  ]
}
```

### Example: Download File

```bash
curl "http://localhost:8080/api/v1/files/work-orders/1/17" \
  -H "Authorization: Bearer <access_token>" \
  --output budget-plan-april.pdf
```

### Example: Delete File

```bash
curl -X DELETE "http://localhost:8080/api/v1/files/work-orders/1/17" \
  -H "Authorization: Bearer <access_token>"
```

### Delete Ownership Rule

Only the user who uploaded a file, or the work order's creator, can delete it. Attempting to delete another user's attachment returns `403 Forbidden`.

### Server-Side Size Limits

The server enforces request body and multipart limits derived from `FileAttachments:MaxFileSizeBytes` at startup (Kestrel + IIS + FormOptions). Requests exceeding the limit are rejected before reaching application code - the client receives a `413 Request Entity Too Large` response rather than a validation error.

### Common Error Cases

| Status | Error Code | Scenario | Error field |
|--------|------------|----------|-------------|
| `401 Unauthorized` | `UNAUTHORIZED` | Missing or invalid bearer token | - |
| `403 Forbidden` | `FORBIDDEN` | Entity not modifiable (e.g. Submitted WO), or deleting another user's file | - |
| `404 Not Found` | `NOT_FOUND` | Entity type unsupported, entity missing, or `fileId` not found for that entity | - |
| `413 Request Entity Too Large` | - | Request body exceeds server limit (rejected by Kestrel before validation) | - |
| `422 Unprocessable Entity` | `VALIDATION_ERROR` | No files sent | `files` |
| `422 Unprocessable Entity` | `VALIDATION_ERROR` | Any file exceeds size limit | `files[N].length` |
| `422 Unprocessable Entity` | `VALIDATION_ERROR` | Any file has a disallowed MIME type | `files[N].contentType` |
| `422 Unprocessable Entity` | `VALIDATION_ERROR` | Any file fails magic-byte signature check | `files` |
| `422 Unprocessable Entity` | `VALIDATION_ERROR` | Batch would exceed `MaxAttachmentsPerEntity` | `files` |
| `422 Unprocessable Entity` | `VALIDATION_ERROR` | Batch would exceed `MaxTotalSizeBytesPerEntity` for this record | `files` |

### Client Collection

Import-ready API client files for this module:

- `apidog/wams-file-attachments.postman_collection.json`
- `apidog/wams-local.postman_environment.json`

---

## Audit Logs

Base route: `/api/v1/audit-logs`

All endpoints require authentication and `audit.log.read` permission. Audit logs are **immutable** - there are no create, update, or delete endpoints. Regular users see only their own company's logs; Super Admin sees all and can filter by `companyId`.

| Method | Endpoint | Permission Required | Description | Response |
|--------|----------|--------------------|-------------|----------|
| GET | `/api/v1/audit-logs` | `audit.log.read` | Paginated list of audit log entries with filters | Paginated [`AuditLogResponse`](#auditlogresponse) |
| GET | `/api/v1/audit-logs/{id}` | `audit.log.read` | Get a single audit log entry by ID | [`AuditLogResponse`](#auditlogresponse) |
| GET | `/api/v1/audit-logs/record/{tableName}/{recordId}` | `audit.log.read` | Full change history for a specific entity (e.g. all changes to `budget_plans` record `42`) | Paginated [`AuditLogResponse`](#auditlogresponse) |

### Query Parameters - `GET /api/v1/audit-logs`

Accepts all standard [datatable params](#datatable-query-parameters) plus:

| Parameter | Type | Description |
|-----------|------|-------------|
| `tableName` | string | Filter by table (e.g. `users`, `budget_plans`) |
| `recordId` | long | Filter by record ID |
| `userId` | long | Filter by actor user ID |
| `action` | string | Filter by action (`CREATE`, `UPDATE`, `DELETE`, `LOGIN`, etc.) |
| `dateFrom` | datetime | Lower bound on `created_at` (inclusive, UTC) |
| `dateTo` | datetime | Upper bound on `created_at` (inclusive, UTC) |
| `companyId` | long | **Super Admin only** - filter by company |

**Search fields:** `tableName`, `requestPath`, `requestId`

**Sortable fields:** `tableName`, `action`, `userId`, `createdAt` (default desc)

**Example:**
```
GET /api/v1/audit-logs?tableName=budget_plans&action=UPDATE&dateFrom=2026-04-01&dateTo=2026-04-30&page=1&limit=20
```

### Query Parameters - `GET /api/v1/audit-logs/record/{tableName}/{recordId}`

Accepts standard [datatable params](#datatable-query-parameters). Sorted newest-first by default.

**Example:**
```
GET /api/v1/audit-logs/record/budget_plans/42?page=1&limit=50
```

### DTOs

#### AuditLogResponse

```json
{
  "id": "long",
  "action": "string (CREATE | UPDATE | DELETE | LOGIN | ...)",
  "tableName": "string",
  "recordId": "long (nullable) - single-PK entities",
  "recordKey": "string (nullable) - JSON key for composite-PK entities, e.g. {\"UserId\":1,\"RoleId\":3}",
  "userId": "long (nullable)",
  "userEmail": "string (nullable) - snapshotted at time of action",
  "userFullname": "string (nullable) - snapshotted at time of action",
  "companyId": "long (nullable)",
  "oldValues": "object (nullable) - JSON of fields before change",
  "newValues": "object (nullable) - JSON of fields after change",
  "requestId": "string (nullable) - distributed trace ID",
  "requestPath": "string (nullable) - HTTP path or [SYSTEM] for background jobs",
  "httpMethod": "string (nullable) - HTTP verb or SYSTEM",
  "ipAddress": "string (nullable) - client IP, X-Forwarded-For aware",
  "userAgent": "string (nullable) - browser/client identifier",
  "createdAt": "datetime"
}
```

> **`oldValues` / `newValues` notes:**
> - Returned as structured JSON objects (not raw strings) - no double-parsing needed on the frontend.
> - For `CREATE`: `oldValues` is null; `newValues` contains the full new record.
> - For `DELETE`: `newValues` is null; `oldValues` contains the full record as it was.
> - For `UPDATE` on regular tables: only modified fields are included (plus the PK).
> - For `UPDATE` on sensitive tables (`users`, `roles`, `permissions`, `budget_plans`): full record snapshot before and after.
> - `PasswordHash` and `TokenHash` are always excluded.

### Logged Operations Reference

| Operation | `table_name` | `action` | `record_id` | `record_key` | `old_values` | `new_values` |
|---|---|---|---|---|---|---|
| Create user | `users` | `CREATE` | new user ID | - | null | full record |
| Update user / deactivate | `users` | `UPDATE` | user ID | - | full record before | full record after |
| Soft-delete user | `users` | `DELETE` | user ID | - | full record before | null |
| Change own password (`POST /auth/change-password`) | `users` | `CHANGE_PASSWORD` | user ID | - | null | null |
| Admin reset password (`POST /users/{id}/password`) | `users` | `RESET_PASSWORD` | target user ID | - | null | null |
| Assign role to user | `user_roles` | `CREATE` | null | `{"UserId":N,"RoleId":M}` | null | junction fields |
| Remove role from user | `user_roles` | `DELETE` | null | `{"UserId":N,"RoleId":M}` | junction fields | null |
| Assign warehouse to user | `user_warehouses` | `CREATE` | null | `{"UserId":N,"WarehouseShadowId":M}` | null | junction fields |
| Remove warehouse from user | `user_warehouses` | `DELETE` | null | `{"UserId":N,"WarehouseShadowId":M}` | junction fields | null |
| Grant/deny user permission | `user_permissions` | `CREATE` | new ID | - | null | override fields |
| Remove user permission | `user_permissions` | `DELETE` | ID | - | override fields | null |
| Create / update / delete role | `roles` | `CREATE` / `UPDATE` / `DELETE` | role ID | - | full snapshot | full snapshot |
| Assign permission to role | `role_permissions` | `CREATE` | null | `{"RoleId":N,"PermissionId":M}` | null | junction fields |
| Remove permission from role | `role_permissions` | `DELETE` | null | `{"RoleId":N,"PermissionId":M}` | junction fields | null |
| Create / update company | `companies` | `CREATE` / `UPDATE` | company ID | - | null / diff | full / diff |
| Deactivate company | `companies` | `UPDATE` | company ID | - | `{IsActive: true}` | `{IsActive: false}` |
| Create rate card | `rate_cards` | `CREATE` | new ID | - | null | full record |
| Submit rate card | `rate_cards` | `UPDATE` | ID | - | `{Status: "Draft"}` | `{Status: "Submitted", SubmittedAt}` |
| Create budget template | `budget_templates` | `CREATE` | new ID | - | null | full record |
| Submit budget template | `budget_templates` | `UPDATE` | ID | - | `{Status: "Draft"}` | `{Status: "Submitted", SubmittedAt}` |
| Update budget template (Draft or Submitted) | `budget_templates` | `UPDATE` | ID | - | diff of changed fields | diff of changed fields |
| Create budget plan | `budget_plans` | `CREATE` | new ID | - | null | full snapshot |
| Submit budget plan | `budget_plans` | `UPDATE` | ID | - | `{Status: "Draft"}` | `{Status: "Submitted", SubmittedAt}` |
| Approve budget plan stage N (non-final) | `budget_plans` | `UPDATE` | ID | - | `{Status: "Submitted"\|"InApproval"}` | `{Status: "InApproval"}` |
| Approve budget plan final stage | `budget_plans` | `UPDATE` | ID | - | `{Status: "InApproval"}` | `{Status: "Approved"}` |
| Reject budget plan | `budget_plans` | `UPDATE` | ID | - | full snapshot before | `{Status: "Rejected", RejectedAt, RejectionReason}` |
| Soft-delete budget plan | `budget_plans` | `DELETE` | ID | - | full snapshot before | null |
| ERP sync (warehouse / vendor / item) | `*_shadows` | `CREATE` or `UPDATE` | shadow ID | - | null / diff | full / diff |
| Login | `users` | `LOGIN` | user ID | - | null | null |

> `old_values` / `new_values` for `UPDATE` operations: **diff-only** for most tables (changed fields + PK only); **full snapshot** for `users`, `roles`, `permissions`, `budget_plans`.
> Junction table entries always use `record_key` (JSON composite PK); `record_id` is null for these rows.
> ERP sync rows have `user_email = "system@internal"`, `request_path = "[SYSTEM]"`.
> New `action` values can be added freely in code - no DB constraint restricts the column.
> `CHANGE_PASSWORD` / `RESET_PASSWORD` are explicit rows written directly by `AuthService`/`UserService` (same pattern as `LOGIN`, not the automatic diff interceptor) - `old_values`/`new_values` are always null since only the hash changes, which is never logged. Persisting the new hash still also fires the standard automatic `UPDATE` row on `users` (`PasswordHash` excluded from its diff), so **each password change writes two audit rows**. For `RESET_PASSWORD`, `user_id` on the row is the **acting admin**, not the target - `record_id` holds the target user's ID.

### Example Responses

**Single entry:**
```json
{
  "success": true,
  "data": {
    "id": 1042,
    "action": "UPDATE",
    "tableName": "budget_plans",
    "recordId": 7,
    "recordKey": null,
    "userId": 3,
    "userEmail": "admin@acme.com",
    "userFullname": "Budi Santoso",
    "companyId": 2,
    "oldValues": { "Status": "Draft", "SubmittedAt": null },
    "newValues": { "Status": "Submitted", "SubmittedAt": "2026-04-17T08:30:00Z" },
    "requestId": "f3a1c2d4-...",
    "requestPath": "/api/v1/budget-plans/7/submit",
    "httpMethod": "POST",
    "ipAddress": "192.168.1.10",
    "userAgent": "Mozilla/5.0 ...",
    "createdAt": "2026-04-17T08:30:00.123Z"
  }
}
```

**Login event:**
```json
{
  "id": 1043,
  "action": "LOGIN",
  "tableName": "users",
  "recordId": 3,
  "recordKey": null,
  "userId": 3,
  "userEmail": "admin@acme.com",
  "userFullname": "Budi Santoso",
  "companyId": 2,
  "oldValues": null,
  "newValues": null,
  "requestPath": "/api/v1/auth/login",
  "httpMethod": "POST",
  "ipAddress": "192.168.1.10",
  "createdAt": "2026-04-17T08:29:55.000Z"
}
```

**Junction table entry (composite PK):**
```json
{
  "id": 1044,
  "action": "CREATE",
  "tableName": "user_roles",
  "recordId": null,
  "recordKey": "{\"UserId\":5,\"RoleId\":2}",
  "userId": 1,
  "userEmail": "superadmin@default.com",
  "companyId": 1,
  "oldValues": null,
  "newValues": { "UserId": 5, "RoleId": 2, "CreatedAt": "2026-04-17T08:31:00Z" },
  "createdAt": "2026-04-17T08:31:00.456Z"
}
```

---

## Record History

Each of the following modules exposes a scoped `GET /{id}/history` endpoint. These endpoints return a **slim** audit trail for a specific record, gated by the module's own `read` permission - no `audit.log.read` required. Forensic fields (IP address, user agent, request path, HTTP method, request ID) are intentionally omitted; those are available via `GET /api/v1/audit-logs/record/{tableName}/{recordId}` for callers with `audit.log.read`.

| Method | Endpoint | Permission Required | Description | Response |
|--------|----------|--------------------|-------------|----------|
| GET | `/api/v1/budget-templates/{id}/history` | `budget.template.read` | Change history for a budget template | Paginated [`RecordHistoryResponse`](#recordhistoryresponse) |
| GET | `/api/v1/budget-plans/{id}/history` | `budget.plan.read` | Change history for a budget plan | Paginated [`RecordHistoryResponse`](#recordhistoryresponse) |
| GET | `/api/v1/work-orders/{id}/history` | `workorder.workorder.read` | Change history for a work order | Paginated [`RecordHistoryResponse`](#recordhistoryresponse) |
| GET | `/api/v1/purchase-orders/{id}/history` | `budget.po.read` | Change history for a purchase order | Paginated [`RecordHistoryResponse`](#recordhistoryresponse) |
| GET | `/api/v1/account-payables/{id}/history` | `workorder.ap.read` | Change history for an account payable | Paginated [`RecordHistoryResponse`](#recordhistoryresponse) |
| GET | `/api/v1/recap-work-orders/{id}/history` | `workorder.recap.read` | Change history for a recap work order | Paginated [`RecordHistoryResponse`](#recordhistoryresponse) |

All history endpoints accept standard [datatable params](#datatable-query-parameters) (`page`, `limit`, `sortBy`, `sortOrder`). Default sort: `createdAt DESC` (newest first).

A valid `id` with no audit rows returns `200` with an empty `data` array - never `404`.

### DTOs

#### RecordHistoryResponse

```json
{
  "id": "long",
  "action": "string (CREATE | UPDATE | DELETE | ...)",
  "userId": "long (nullable)",
  "userEmail": "string (nullable) - snapshotted at time of action",
  "userFullname": "string (nullable) - snapshotted at time of action",
  "oldValues": "object (nullable) - JSON of fields before change",
  "newValues": "object (nullable) - JSON of fields after change",
  "createdAt": "datetime"
}
```

> **vs `AuditLogResponse`:** `RecordHistoryResponse` omits `tableName`, `recordId`, `recordKey`, `companyId`, `requestId`, `requestPath`, `httpMethod`, `ipAddress`, `userAgent`. Use `GET /api/v1/audit-logs/record/{tableName}/{recordId}` (requires `audit.log.read`) when you need the full forensic record.

### Example Response

```json
{
  "success": true,
  "data": [
    {
      "id": 1085,
      "action": "CREATE",
      "userId": 4,
      "userEmail": "wh.admin@acme.com",
      "userFullname": "Sari Dewi",
      "oldValues": null,
      "newValues": { "Status": "Draft", "BudgetNo": "BP.260600000012" },
      "createdAt": "2026-06-23T07:10:00.000Z"
    },
    {
      "id": 1091,
      "action": "UPDATE",
      "userId": 4,
      "userEmail": "wh.admin@acme.com",
      "userFullname": "Sari Dewi",
      "oldValues": { "Status": "Draft" },
      "newValues": { "Status": "Submitted", "SubmittedAt": "2026-06-23T08:00:00Z" },
      "createdAt": "2026-06-23T08:00:01.000Z"
    }
  ],
  "meta": {
    "page": 1,
    "limit": 20,
    "total": 2,
    "totalPages": 1
  },
  "requestId": "abc-123"
}
```

---

## Export

All 16 paginated list endpoints support file export. Add `/export` to the list URL and pass `?format=Xlsx`, `?format=Csv`, or `?format=Pdf`. The response is a file download, not JSON.

All filter, search, and sort params from the list endpoint are accepted. Pagination params (`page`, `limit`) are ignored - export always returns every matching row.

Export uses the same permission as the list endpoint. No separate export permission exists.

**Libraries:** SpreadCheetah 1.24.0 (XLSX), CsvHelper 33.1.0 (CSV), QuestPDF 2025.7.0 (PDF). XLSX and CSV write directly to the response stream (peak memory under 10 KB regardless of row count). PDF generation is offloaded to a background thread via `Task.Run`.

**PDF report layout (A4 landscape):**
- **Header** (repeated every page): company name (from tenant context, falls back to `"System"` for super-admin), optional logo fetched from the company's stored logo (via `IFileAttachmentStorage`; omitted if none set or file missing), report title (e.g. `"Work Orders Report"`), and UTC generation timestamp
- **Body**: styled data table - dark header row (`#1F2937`), alternating white/light-gray rows, column widths from `ExportColumnDefinition`
- **Footer** (repeated every page): `Page N of M` centered

### Response

```
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet  (Xlsx)
Content-Type: text/csv                                                            (Csv)
Content-Type: application/pdf                                                     (Pdf)
Content-Disposition: attachment; filename="work-orders-20260602-143022.xlsx"
```

The filename uses the resource name and the UTC timestamp at request time.

### Query Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `format` | `Xlsx` \| `Csv` \| `Pdf` | `Xlsx` | Output format |
| _(list filters)_ | varies | - | Same params as the corresponding list endpoint |

### PDF Logo

PDF reports automatically include the company logo when one is uploaded (`PUT /api/v1/companies/{id}/logo`). The logo is read from `IFileAttachmentStorage` at report generation time using the tenant's `Company.LogoStorageKey`. If the key is null or the file is missing in storage, the logo is silently omitted and the report generates normally.

### Endpoints

| Resource | Endpoint | Permission |
|----------|----------|------------|
| Work Orders | `GET /api/v1/work-orders/export` | `workorder.workorder.read` |
| Budget Plans | `GET /api/v1/budget-plans/export` | `budget.plan.read` |
| Purchase Orders | `GET /api/v1/purchase-orders/export` | `budget.po.read` |
| Account Payables | `GET /api/v1/account-payables/export` | `workorder.ap.read` |
| Recap Work Orders | `GET /api/v1/recap-work-orders/export` | `workorder.recap.read` |
| Transport Orders | `GET /api/v1/transport-orders/export` | `workorder.workorder.read` |
| SPK | `GET /api/v1/spk/export` | `budget.plan.read` |
| Users | `GET /api/v1/users/export` | `user.user.read` |
| Roles | `GET /api/v1/roles/export` | `user.role.read` |
| Companies | `GET /api/v1/companies/export` | `system.company.read` |
| Warehouses | `GET /api/v1/warehouses/export` | `user.warehouse.read` |
| Items | `GET /api/v1/items/export` | `budget.item.read` |
| Vendors | `GET /api/v1/vendors/export` | `budget.vendor.read` |
| Audit Logs | `GET /api/v1/audit-logs/export` | `audit.log.read` |
| Rate Cards | `GET /api/v1/rate-cards/export` | `budget.rate_card.read` |
| Budget Templates | `GET /api/v1/budget-templates/export` | `budget.template.read` |

### Exported Columns

Columns are fixed per resource. Date fields use `yyyy-MM-dd`, timestamps use `yyyy-MM-dd HH:mm`. Decimal quantities use `#,##0.###`, money fields use `#,##0.00`. Booleans export as `Yes` / `No`. Null fields export as empty string.

| Resource | Columns |
|----------|---------|
| Work Orders | Code, Budget Plan, Activity, Activity Type, Warehouse, Warehouse Code, Status, Is RFBA, Start Date, End Date, PIC, BL Number, Product, Vessel, Created By, Created At |
| Budget Plans | Budget No, Template Code, Vendor, Maker, Location, Status, Doc Date, Remark |
| Purchase Orders | Code, Vendor Code, Vendor Name, Status, Doc Date, SAP PO Number, Grand Total, Item Count, Remark, Created By, Created At |
| Account Payables | Code, Vendor Code, Vendor Name, Status, Doc Date, SAP AP Number, Grand Total, Item Count, Remark, Created By, Created At |
| Recap Work Orders | Budget Plan, Template Code, Warehouse, Warehouse Code, Is RFBA, BL Numbers, Activity Types, PIC Names, Status, Doc Date, Remark, Created At |
| Transport Orders | Doc No, Type, Card Code, Card Name, Vehicle No, Vehicle Type, BL No, Item Code, Item Name, Quantity, UoM, Warehouse Code, Warehouse Name, Status |
| SPK | Doc No, Type, Base Doc, Base Doc No, Card Code, Card Name, Item Code, Item Name, Quantity, Delivery Qty, UoM, Pack Type, Warehouse Code, Warehouse Name, BL No, Status |
| Users | Email, Full Name, Employee ID, Active, Roles, Warehouses, Created At |
| Roles | Name, Display Name, Description, System Role, Global Access, Permission Count, Created At |
| Companies | Code, Name, Address, Phone, Email, Active, Users, Warehouses, Created At |
| Warehouses | Code, Name, Location, Active, First Seen At, Synced At |
| Items | Item Code, Item Name, Account Code, Account Name |
| Vendors | Card Code, Card Name |
| Audit Logs | Action, Table, Record ID, Record Key, User Email, User Name, HTTP Method, Request Path, IP Address, Created At |
| Rate Cards | Vendor Code, Vendor Name, Status, Item Count, Created At |
| Budget Templates | Template Code, Location, Status, Date |

### Example

```bash
# Export all approved work orders as CSV
GET /api/v1/work-orders/export?format=Csv&status=Approved
Authorization: Bearer <token>

# Export budget plans as Excel
GET /api/v1/budget-plans/export?format=Xlsx&status=InApproval
Authorization: Bearer <token>

# Export items as a PDF report (company name resolved from tenant context)
GET /api/v1/items/export?format=Pdf
Authorization: Bearer <token>
```

---

## Multi-Tenancy

The API supports multi-tenancy through companies:
- Users belong to a specific company
- Warehouses belong to a specific company
- Users can only access warehouses within their assigned company
- The company context is established during login

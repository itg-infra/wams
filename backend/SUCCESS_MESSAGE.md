# Success Messages

All success messages live in `src/WAMS.Domain/Constants/SuccessMessages.cs`. Controllers pass these strings as the `message` field in the `ApiResponse` wrapper returned on every successful request.

---

## General

Used when no domain-specific message fits.

| Constant | Message | When |
|----------|---------|------|
| `General.DataRetrieved` | `Data retrieved` | Generic list/read fallback |
| `General.OperationSuccessful` | `Operation successful` | Generic mutation fallback |
| `General.HistoryRetrieved` | `History retrieved` | Generic audit history reads |
| `General.AvailableItemsRetrieved` | `Available items retrieved` | Available-items query endpoints |
| `General.ApprovedBudgetPlansRetrieved` | `Approved budget plans retrieved` | Approved BP filter queries |

---

## Auth

| Constant | Message | Endpoint |
|----------|---------|----------|
| `Auth.LoginSuccessful` | `Login successful` | `POST /auth/login` |
| `Auth.TokenRefreshed` | `Token refreshed` | `POST /auth/refresh` |
| `Auth.LoggedOut` | `Logged out successfully` | `POST /auth/logout` |

---

## Dashboard

| Constant | Message | Endpoint |
|----------|---------|----------|
| `Dashboard.SummaryRetrieved` | `Dashboard summary retrieved` | `GET /dashboard/summary` |
| `Dashboard.ActivitiesRetrieved` | `Today's activities retrieved` | `GET /dashboard/activities` |
| `Dashboard.HistoryRetrieved` | `Dashboard history retrieved` | `GET /dashboard/history` |

---

## User

| Constant | Message | Endpoint |
|----------|---------|----------|
| `User.ListRetrieved` | `Users retrieved` | `GET /users` |
| `User.Retrieved` | `User retrieved` | `GET /users/{id}`, `GET /auth/me` |
| `User.Created` | `User created` | `POST /users` |
| `User.Updated` | `User updated` | `PUT /users/{id}` |
| `User.Deleted` | `User deleted` | `DELETE /users/{id}` |
| `User.PasswordChanged` | `Password changed` | `POST /users/{id}/password`, `POST /auth/change-password` |
| `User.RoleAssigned` | `Role assigned` | `POST /users/{id}/roles/{roleId}` |
| `User.RoleRemoved` | `Role removed` | `DELETE /users/{id}/roles/{roleId}` |
| `User.WarehouseAssigned` | `Warehouse assigned` | `POST /users/{id}/warehouses/{warehouseId}` |
| `User.WarehouseRemoved` | `Warehouse removed` | `DELETE /users/{id}/warehouses/{warehouseId}` |
| `User.PermissionOverridesRetrieved` | `Permission overrides retrieved` | `GET /users/{id}/permissions` |
| `User.PermissionGranted` | `Permission granted` | `POST /users/{id}/permissions/{permissionId}/grant` |
| `User.PermissionDenied` | `Permission denied` | `POST /users/{id}/permissions/{permissionId}/deny` |
| `User.PermissionOverrideRemoved` | `Permission override removed` | `DELETE /users/{id}/permissions/{permissionId}` |
| `User.EffectivePermissionsRetrieved` | `Effective permissions retrieved` | `GET /users/{id}/permissions/effective` |

---

## Role

| Constant | Message | Endpoint |
|----------|---------|----------|
| `Role.ListRetrieved` | `Roles retrieved` | `GET /roles` |
| `Role.Retrieved` | `Role retrieved` | `GET /roles/{id}` |
| `Role.Created` | `Role created` | `POST /roles` |
| `Role.Updated` | `Role updated` | `PUT /roles/{id}` |
| `Role.Deleted` | `Role deleted` | `DELETE /roles/{id}` |
| `Role.PermissionsUpdated` | `Role permissions updated` | `PUT /roles/{id}/permissions` |
| `Role.PermissionAssigned` | `Permission assigned` | `POST /roles/{id}/permissions/{permissionId}` |
| `Role.PermissionRemoved` | `Permission removed` | `DELETE /roles/{id}/permissions/{permissionId}` |

---

## Permission

| Constant | Message | Endpoint |
|----------|---------|----------|
| `Permission.ListRetrieved` | `Permissions retrieved` | `GET /permissions` |

---

## Company

| Constant | Message | Endpoint |
|----------|---------|----------|
| `Company.ListRetrieved` | `Companies retrieved` | `GET /companies` |
| `Company.Retrieved` | `Company retrieved` | `GET /companies/{id}` |
| `Company.Created` | `Company created` | `POST /companies` |
| `Company.Updated` | `Company updated` | `PUT /companies/{id}` |
| `Company.Deactivated` | `Company deactivated` | `DELETE /companies/{id}` |
| `Company.UserAssigned` | `User assigned to company` | `POST /companies/{id}/users/{userId}` |
| `Company.LogoUploaded` | `Logo uploaded` | `POST /companies/{id}/logo` |

---

## Warehouse

| Constant | Message | Endpoint |
|----------|---------|----------|
| `Warehouse.ListRetrieved` | `Warehouses retrieved` | `GET /warehouses` |
| `Warehouse.Retrieved` | `Warehouse retrieved` | `GET /warehouses/{id}` |
| `Warehouse.LocationsRetrieved` | `Locations retrieved` | `GET /warehouses/locations` |

---

## Activity Type

| Constant | Message | Endpoint |
|----------|---------|----------|
| `ActivityType.ListRetrieved` | `Activity types retrieved` | `GET /activity-types` |
| `ActivityType.Retrieved` | `Activity type retrieved` | `GET /activity-types/{id}` |
| `ActivityType.Created` | `Activity type created` | `POST /activity-types` |
| `ActivityType.Updated` | `Activity type updated` | `PUT /activity-types/{id}` |

---

## UoM

| Constant | Message | Endpoint |
|----------|---------|----------|
| `Uom.ListRetrieved` | `UoMs retrieved` | `GET /uoms` |
| `Uom.Retrieved` | `UoM retrieved` | `GET /uoms/{id}` |
| `Uom.Created` | `UoM created` | `POST /uoms` |
| `Uom.Updated` | `UoM updated` | `PUT /uoms/{id}` |
| `Uom.Deleted` | `UoM deleted` | `DELETE /uoms/{id}` |

---

## Rate Card

| Constant | Message | Endpoint |
|----------|---------|----------|
| `RateCard.VendorRatesRetrieved` | `Vendor rates retrieved` | `GET /rate-cards/vendor-rates` |
| `RateCard.ListRetrieved` | `Rate cards retrieved` | `GET /rate-cards` |
| `RateCard.Retrieved` | `Rate card retrieved` | `GET /rate-cards/{id}` |
| `RateCard.Created` | `Rate card created` | `POST /rate-cards` |
| `RateCard.CreatedAndSubmitted` | `Rate card created and submitted` | `POST /rate-cards` (with `submit: true`) |
| `RateCard.Updated` | `Rate card updated` | `PUT /rate-cards/{id}` |
| `RateCard.Submitted` | `Rate card submitted` | `POST /rate-cards/{id}/submit` |

---

## Budget Template

| Constant | Message | Endpoint |
|----------|---------|----------|
| `BudgetTemplate.ListRetrieved` | `Budget templates retrieved` | `GET /budget-templates` |
| `BudgetTemplate.Retrieved` | `Budget template retrieved` | `GET /budget-templates/{id}` |
| `BudgetTemplate.Created` | `Budget template created` | `POST /budget-templates` |
| `BudgetTemplate.CreatedAndSubmitted` | `Budget template created and submitted` | `POST /budget-templates` (with `submit: true`) |
| `BudgetTemplate.Updated` | `Budget template updated` | `PUT /budget-templates/{id}` |

---

## Budget Plan

| Constant | Message | Endpoint |
|----------|---------|----------|
| `BudgetPlan.ListRetrieved` | `Budget plans retrieved` | `GET /budget-plans` |
| `BudgetPlan.Retrieved` | `Budget plan retrieved` | `GET /budget-plans/{id}` |
| `BudgetPlan.Created` | `Budget plan created` | `POST /budget-plans` |
| `BudgetPlan.CreatedAndSubmitted` | `Budget plan created and submitted` | `POST /budget-plans` (with `submit: true`) |
| `BudgetPlan.Updated` | `Budget plan updated` | `PUT /budget-plans/{id}` |
| `BudgetPlan.SpkItemAdded` | `SPK item added` | `POST /budget-plans/{id}/spk-items` |

---

## Budget Revision

| Constant | Message | Endpoint |
|----------|---------|----------|
| `BudgetRevision.ListRetrieved` | `Budget revisions retrieved` | `GET /budget-revisions` |
| `BudgetRevision.Retrieved` | `Budget revision retrieved` | `GET /budget-revisions/{id}` |
| `BudgetRevision.Submitted` | `Budget revision submitted` | `POST /budget-revisions` |
| `BudgetRevision.Approved` | `Budget revision approved` | `POST /budget-revisions/{id}/approve` |
| `BudgetRevision.Rejected` | `Budget revision rejected` | `POST /budget-revisions/{id}/reject` |

---

## Work Order

| Constant | Message | Endpoint |
|----------|---------|----------|
| `WorkOrder.ListRetrieved` | `Work orders retrieved` | `GET /work-orders` |
| `WorkOrder.Retrieved` | `Work order retrieved` | `GET /work-orders/{id}` |
| `WorkOrder.Created` | `Work order created` | `POST /work-orders` |
| `WorkOrder.Updated` | `Work order updated` | `PUT /work-orders/{id}` |
| `WorkOrder.Submitted` | `Work order submitted` | `POST /work-orders/{id}/submit` |

---

## Recap Work Order

| Constant | Message | Endpoint |
|----------|---------|----------|
| `RecapWorkOrder.ListRetrieved` | `Recap work orders retrieved` | `GET /recap-work-orders` |
| `RecapWorkOrder.Retrieved` | `Recap work order retrieved` | `GET /recap-work-orders/{id}` |
| `RecapWorkOrder.Approved` | `Recap work order approved` | `POST /recap-work-orders/{id}/approve` |
| `RecapWorkOrder.Rejected` | `Recap work order rejected` | `POST /recap-work-orders/{id}/reject` |

---

## Purchase Order

| Constant | Message | Endpoint |
|----------|---------|----------|
| `PurchaseOrder.ListRetrieved` | `Purchase orders retrieved` | `GET /purchase-orders` |
| `PurchaseOrder.Retrieved` | `Purchase order retrieved` | `GET /purchase-orders/{id}` |
| `PurchaseOrder.Created` | `Purchase order created` | `POST /purchase-orders` |
| `PurchaseOrder.CreatedAndGenerated` | `Purchase order created and generated` | `POST /purchase-orders` (with `generate: true`) |
| `PurchaseOrder.Updated` | `Purchase order updated` | `PUT /purchase-orders/{id}` |
| `PurchaseOrder.Generated` | `Purchase order generated` | `POST /purchase-orders/{id}/generate` |

---

## Account Payable

| Constant | Message | Endpoint |
|----------|---------|----------|
| `AccountPayable.ApprovedRecapsRetrieved` | `Approved recaps retrieved` | `GET /account-payables/approved-recaps` |
| `AccountPayable.ListRetrieved` | `Account payables retrieved` | `GET /account-payables` |
| `AccountPayable.Retrieved` | `Account payable retrieved` | `GET /account-payables/{id}` |
| `AccountPayable.Created` | `Account payable created` | `POST /account-payables` |
| `AccountPayable.CreatedAndGenerated` | `Account payable created and generated` | `POST /account-payables` (with `generate: true`) |
| `AccountPayable.Updated` | `Account payable updated` | `PUT /account-payables/{id}` |
| `AccountPayable.Generated` | `Account payable generated` | `POST /account-payables/{id}/generate` |

---

## SPK

| Constant | Message | Endpoint |
|----------|---------|----------|
| `Spk.ListRetrieved` | `SPK list retrieved` | `GET /spk` |
| `Spk.Retrieved` | `SPK retrieved` | `GET /spk/{id}` |

---

## Transport Order

| Constant | Message | Endpoint |
|----------|---------|----------|
| `TransportOrder.ListRetrieved` | `Transport orders retrieved` | `GET /transport-orders` |
| `TransportOrder.Retrieved` | `Transport order retrieved` | `GET /transport-orders/{id}` |

---

## Vendor

| Constant | Message | Endpoint |
|----------|---------|----------|
| `Vendor.ListRetrieved` | `Vendors retrieved` | `GET /vendors` |
| `Vendor.Retrieved` | `Vendor retrieved` | `GET /vendors/{id}` |

---

## Item

| Constant | Message | Endpoint |
|----------|---------|----------|
| `Item.ListRetrieved` | `Items retrieved` | `GET /items` |
| `Item.Retrieved` | `Item retrieved` | `GET /items/{id}` |

---

## Finance Report

| Constant | Message | Endpoint |
|----------|---------|----------|
| `FinanceReport.ListRetrieved` | `Finance reports retrieved` | `GET /finance-reports` |
| `FinanceReport.Retrieved` | `Finance report retrieved` | `GET /finance-reports/{id}` |

---

## Audit Log

| Constant | Message | Endpoint |
|----------|---------|----------|
| `AuditLog.ListRetrieved` | `Audit logs retrieved` | `GET /audit-logs` |
| `AuditLog.Retrieved` | `Audit log retrieved` | `GET /audit-logs/{id}` |
| `AuditLog.RecordHistory` | `Record history retrieved` | `GET /audit-logs/history/{entityType}/{entityId}` |

---

## Notification

| Constant | Message | Endpoint |
|----------|---------|----------|
| `Notification.ListRetrieved` | `Notifications retrieved` | `GET /notifications` |
| `Notification.TestDispatched` | `Test notification dispatched` | `POST /notifications/test` |

---

## Workflow Template

| Constant | Message | Endpoint |
|----------|---------|----------|
| `WorkflowTemplate.DocumentTypesRetrieved` | `Document types retrieved` | `GET /workflow-templates/document-types` |
| `WorkflowTemplate.ListRetrieved` | `Workflow templates retrieved` | `GET /workflow-templates` |
| `WorkflowTemplate.Retrieved` | `Workflow template retrieved` | `GET /workflow-templates/{id}` |
| `WorkflowTemplate.Created` | `Workflow template created` | `POST /workflow-templates` |
| `WorkflowTemplate.Updated` | `Workflow template updated` | `PUT /workflow-templates/{id}` |

---

## File

| Constant | Message | Endpoint |
|----------|---------|----------|
| `File.Uploaded` | `Files uploaded` | `POST /files` |
| `File.Retrieved` | `Files retrieved` | `GET /files` |

---

## Sync

| Constant | Message | Endpoint |
|----------|---------|----------|
| `Sync.Completed` | `Sync completed` | `POST /sync` |
| `Sync.LatestPerService` | `Latest sync per service` | `GET /sync/latest` |

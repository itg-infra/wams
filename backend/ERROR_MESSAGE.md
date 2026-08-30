# Error Messages

All error messages live in `src/WAMS.Domain/Constants/ErrorMessages.cs`. Services throw domain exceptions (`NotFoundException`, `ValidationException`, `ForbiddenException`, `ConflictException`) with these strings; the global exception handler maps each to the appropriate HTTP status.

Static methods (e.g. `NotFound(long id)`) interpolate the entity ID into the message. Constants are plain `string` values.

---

## Permission

Thrown by `RequirePermissionAttribute` before any RBAC check.

| Constant | Message | HTTP |
|----------|---------|------|
| `Permission.AuthenticationRequired` | `Authentication required` | 401 |
| `Permission.TokenRevoked` | `Token has been revoked` | 401 |
| `Permission.InvalidTokenSubject` | `Invalid token: missing or invalid subject claim` | 401 |
| `Permission.InvalidKeyFormat(key)` | `Permission key must have exactly 3 dot-separated segments (module.resource.action), got: '{key}'` | 500 |
| `Permission.MissingPermission(permission)` | `Missing permission: {permission}` | 403 |

---

## Auth

| Constant | Message | HTTP |
|----------|---------|------|
| `Auth.InvalidCredentials` | `Invalid email or password` | 401 |
| `Auth.AccountInactive` | `Account is inactive` | 401 |
| `Auth.InvalidRefreshToken` | `Invalid refresh token` | 400 |
| `Auth.RefreshTokenExpiredOrRevoked` | `Refresh token is expired or revoked` | 400 |

---

## User

| Constant | Message | HTTP |
|----------|---------|------|
| `User.NotFound(id)` | `User {id} not found` | 404 |
| `User.EmailConflict(email)` | `User with email '{email}' already exists` | 409 |
| `User.AlreadyHasRole(name)` | `User already has role '{name}'` | 409 |
| `User.CompanyIdRequired` | `companyId is required when creating a user as Super Admin` | 400 |
| `User.AlreadyAssignedToCompany` | `User is already assigned to this company` | 409 |

---

## Role

| Constant | Message | HTTP |
|----------|---------|------|
| `Role.AlreadyExists(name)` | `Role '{name}' already exists` | 409 |
| `Role.SystemRoleCannotBeModified` | `System roles cannot be modified` | 403 |
| `Role.SystemRoleCannotBeDeleted` | `System roles cannot be deleted` | 403 |
| `Role.SystemRolePermissionsCannotBeModified` | `System role permissions cannot be modified` | 403 |

---

## Company

| Constant | Message | HTTP |
|----------|---------|------|
| `Company.CodeConflict(code)` | `Company with code '{code}' already exists` | 409 |
| `Company.CannotDeactivateDefault` | `Cannot deactivate the default company` | 403 |
| `Company.AlreadyAssigned` | `User is already assigned to this company` | 409 |
| `Company.TenantContextNotSet` | `Tenant context not set` | 500 |
| `Company.AccessDeniedLogo` | `Access denied to this company's logo` | 403 |

---

## Warehouse

| Constant | Message | HTTP |
|----------|---------|------|
| `Warehouse.NotFound(id)` | `Warehouse {id} not found` | 404 |
| `Warehouse.AccessDenied` | `You do not have access to this warehouse` | 403 |

---

## Activity Type

| Constant | Message | HTTP |
|----------|---------|------|
| `ActivityType.NotFound(id)` | `Activity type {id} not found` | 404 |
| `ActivityType.CodeConflict(code)` | `Activity type with code '{code}' already exists` | 409 |
| `ActivityType.NotActive(name)` | `Activity type '{name}' is not active` | 400 |

---

## UoM

| Constant | Message | HTTP |
|----------|---------|------|
| `Uom.NotFound(id)` | `UoM {id} not found` | 404 |
| `Uom.CodeConflict(code)` | `UoM code '{code}' already exists` | 409 |
| `Uom.ReferencedByRateCard` | `Cannot delete UoM that is referenced by rate card items` | 409 |

---

## Rate Card

| Constant | Message | HTTP |
|----------|---------|------|
| `RateCard.NotFound(id)` | `Rate card {id} not found` | 404 |
| `RateCard.NotFoundAfterCreation` | `Rate card not found after creation` | 500 |
| `RateCard.CannotSubmitOnlyDraft` | `Only draft rate cards can be submitted` | 400 |
| `RateCard.MustHaveItemBeforeSubmit` | `Rate card must have at least one item before submitting` | 400 |
| `RateCard.ItemNotFound(id)` | `Item {id} not found` | 404 |
| `RateCard.UomNotFound(id)` | `UOM {id} not found` | 404 |
| `RateCard.SubmittedRateNotFound(vendorId, itemId)` | `No submitted rate card found for vendor {vendorId} and item {itemId}. Please ensure a rate card exists and is submitted.` | 404 |

---

## Budget Template

| Constant | Message | HTTP |
|----------|---------|------|
| `BudgetTemplate.NotFound(id)` | `Budget template {id} not found` | 404 |
| `BudgetTemplate.NotFoundAfterCreation` | `Budget template not found after creation` | 500 |
| `BudgetTemplate.CannotUpdateOnlyDraftOrSubmitted` | `Template can only be updated in Draft or Submitted status` | 400 |
| `BudgetTemplate.CannotSubmitOnlyDraft` | `Only Draft templates can be submitted` | 400 |
| `BudgetTemplate.CannotDeleteOnlyDraft` | `Only Draft templates can be deleted` | 400 |
| `BudgetTemplate.OnlySubmittedCanBeUsed` | `Only Submitted templates can be used for budget plans` | 400 |

---

## Province

| Constant | Message | HTTP |
|----------|---------|------|
| `Province.NotFound(id)` | `Province {id} not found` | 404 |
| `Province.NotActive(name)` | `Province '{name}' is not active` | 400 |

---

## Budget Plan

| Constant | Message | HTTP |
|----------|---------|------|
| `BudgetPlan.NotFound(id)` | `Budget plan {id} not found` | 404 |
| `BudgetPlan.WarehouseProvinceMismatch` | `The selected warehouse is not in the same province as the budget template.` | 400 |
| `BudgetPlan.NotFoundAfterCreation` | `Budget plan not found after creation` | 500 |
| `BudgetPlan.NotFoundAfterSubmit(id)` | `Budget plan {id} not found after submit` | 500 |
| `BudgetPlan.NotFoundAfterUpdate(id)` | `Budget plan {id} not found after update` | 500 |
| `BudgetPlan.CannotUpdateOnlyDraftOrRejected` | `Only Draft or Rejected plans can be updated` | 400 |
| `BudgetPlan.CannotSubmitWithNoItems` | `Cannot submit a plan with no items` | 400 |
| `BudgetPlan.CannotSubmitOnlyDraftOrRejected` | `Only Draft or Rejected plans can be submitted` | 400 |
| `BudgetPlan.CannotApprove(status)` | `Budget plan in status '{status}' cannot be approved` | 400 |
| `BudgetPlan.NoWorkflow` | `Budget plan has no active workflow instance` | 400 |
| `BudgetPlan.NoPendingApprovalStage` | `No pending approval stage found` | 400 |
| `BudgetPlan.NotAuthorizedAtStage(stage)` | `Not authorized to approve at stage {stage}` | 403 |
| `BudgetPlan.CannotApproveOwnSubmission` | `You cannot approve a budget plan you submitted` | 403 |
| `BudgetPlan.CannotRejectOnlySubmittedOrInApproval` | `Only Submitted or InApproval plans can be rejected` | 400 |
| `BudgetPlan.RejectionReasonRequired` | `Rejection reason is required` | 400 |
| `BudgetPlan.CannotDeleteOnlyDraft` | `Only Draft plans can be deleted` | 400 |
| `BudgetPlan.UnitCostOverrideMustBePositive(itemId)` | `Unit cost override for item {itemId} must be greater than zero` | 400 |
| `BudgetPlan.ItemNotBelongToPlan` | `The specified budget plan item does not belong to this budget plan` | 400 |
| `BudgetPlan.WorkOrderAlreadyExists` | `A work order already exists for this budget plan item` | 409 |
| `BudgetPlan.RequiresApprovedPlan` | `Work orders can only be created against fully approved budget plans` | 400 |

---

## Budget Revision

| Constant | Message | HTTP |
|----------|---------|------|
| `BudgetRevision.NotFound(id)` | `Budget revision {id} not found` | 404 |
| `BudgetRevision.AlreadyPending` | `A Budget Revision is already pending for this recap` | 409 |
| `BudgetRevision.CannotSubmitForApprovedRecap` | `Cannot submit a Budget Revision for an already-approved recap` | 400 |
| `BudgetRevision.CannotApproveOnlyPending` | `Only Pending revisions can be approved` | 400 |
| `BudgetRevision.CannotRejectOnlyPending` | `Only Pending revisions can be rejected` | 400 |

---

## Work Order

| Constant | Message | HTTP |
|----------|---------|------|
| `WorkOrder.NotFound(id)` | `Work order {id} not found` | 404 |
| `WorkOrder.LockedRecapApproved` | `Work order is locked: the associated recap has been approved` | 409 |
| `WorkOrder.CannotUpdateOnlyDraft` | `Only Draft work orders can be updated` | 400 |
| `WorkOrder.CannotDeleteOnlyDraft` | `Only Draft work orders can be deleted` | 400 |
| `WorkOrder.CannotSubmitOnlyDraft` | `Only Draft work orders can be submitted` | 400 |
| `WorkOrder.GpsRequiredBeforeSubmit` | `GPS location is required before submitting a work order` | 400 |
| `WorkOrder.PicUserNotFound(id)` | `PIC user {id} not found` | 404 |
| `WorkOrder.RequiresHeavyEquipmentDetail` | `{AlatBerat} work orders require heavy equipment detail before submission` | 400 |
| `WorkOrder.RequiresUnbaggingDetail` | `{Unbagging} work orders require unbagging detail before submission` | 400 |
| `WorkOrder.RequiresRebaggingDetail` | `{Rebagging} work orders require rebagging detail before submission` | 400 |
| `WorkOrder.RequiresFumigationDetail` | `{Fumigasi} work orders require fumigation detail before submission` | 400 |
| `WorkOrder.RequiresQcDetail` | `{Qc} work orders require QC detail before submission` | 400 |
| `WorkOrder.RequiresUnloadingItem` | `{Bongkar} work orders require at least one unloading item before submission` | 400 |
| `WorkOrder.RequiresLoadingItem` | `{Muat} work orders require at least one loading item before submission` | 400 |
| `WorkOrder.RequiresStorageHandlingDetail(code)` | `{code} work orders require storage/handling detail before submission` | 400 |
| `WorkOrder.UnloadingBlNumberRequired` | `UnloadingItem BlNumber is required and cannot be empty` | 400 |
| `WorkOrder.LoadingBlNumberRequired` | `LoadingItem BlNumber is required and cannot be empty` | 400 |

> Activity type codes (`AlatBerat`, `Unbagging`, etc.) are interpolated at runtime from `ActivityTypeCodes` constants.

---

## Recap Work Order

| Constant | Message | HTTP |
|----------|---------|------|
| `RecapWorkOrder.NotFound(id)` | `Recap work order {id} not found` | 404 |
| `RecapWorkOrder.CannotApproveOnlyPending` | `Only Pending recaps can be approved` | 400 |
| `RecapWorkOrder.CannotRejectOnlyPending` | `Only Pending recaps can be rejected` | 400 |
| `RecapWorkOrder.CannotApproveHasDraftWorkOrders(count)` | `Cannot approve recap: {count} work order(s) are still in Draft. All must be Submitted first.` | 400 |
| `RecapWorkOrder.AccessDeniedDifferentWarehouse` | `Access denied: recap belongs to a different warehouse` | 403 |
| `RecapWorkOrder.RealizationThresholdExceeded(effectivePercent, threshold)` | `Realization ({effectivePercent}%) exceeds the allowed threshold of {threshold}%. Submit a Budget Revision and have it approved before approving this recap.` | 400 |

---

## Purchase Order

| Constant | Message | HTTP |
|----------|---------|------|
| `PurchaseOrder.NotFound(id)` | `Purchase order {id} not found` | 404 |
| `PurchaseOrder.NotFoundAfterCreation` | `Purchase order not found after creation` | 500 |
| `PurchaseOrder.CannotUpdateOnlyDraft` | `Only Draft purchase orders can be updated` | 400 |
| `PurchaseOrder.CannotDeleteOnlyDraft` | `Only Draft purchase orders can be deleted` | 400 |
| `PurchaseOrder.CannotGenerateOnlyDraft` | `Only Draft purchase orders can be generated` | 400 |
| `PurchaseOrder.SapNoPoNumber` | `SAP integration returned no PO number. Please try again.` | 502 |
| `PurchaseOrder.ItemUnavailable(itemId)` | `Budget plan item {itemId} is unavailable: already in a generated PO, not found, or vendor mismatch.` | 400 |

---

## Account Payable

| Constant | Message | HTTP |
|----------|---------|------|
| `AccountPayable.NotFound(id)` | `Account payable {id} not found` | 404 |
| `AccountPayable.CannotUpdateOnlyDraft` | `Only Draft account payables can be updated` | 400 |
| `AccountPayable.CannotDeleteOnlyDraft` | `Only Draft account payables can be deleted` | 400 |
| `AccountPayable.CannotGenerateOnlyDraft` | `Only Draft account payables can be generated` | 400 |
| `AccountPayable.NoItemsCannotGenerate` | `Account payable has no items and cannot be generated` | 400 |
| `AccountPayable.SapNoApNumber` | `SAP integration returned no AP number. Please try again.` | 502 |
| `AccountPayable.ItemUnavailable(itemId)` | `Budget plan item {itemId} is unavailable: already in a generated AP, not found, or vendor mismatch.` | 400 |

---

## SPK

| Constant | Message | HTTP |
|----------|---------|------|
| `Spk.NotFound(id)` | `SPK {id} not found` | 404 |
| `Spk.ItemNotFound(spkItemId, planId)` | `SPK item {spkItemId} not found on plan {planId}` | 404 |
| `Spk.AlreadyLinked(docNo)` | `SPK {docNo} is already linked to this budget plan` | 409 |
| `Spk.NotLinkedToPlan(spkShadowId)` | `SPK {spkShadowId} is not linked to this budget plan` | 400 |
| `Spk.QuantityExceedsSpk(quantity, spkQuantity, spkShadowId)` | `Quantity {quantity} exceeds SPK quantity {spkQuantity} for SPK {spkShadowId}` | 400 |
| `Spk.CannotReplaceSpkListOrphanedItems(ids)` | `Cannot replace SPK list: cost items still reference SPK(s) {ids}. Re-send Items without those references, or keep the SPKs.` | 409 |

---

## Workflow Template

| Constant | Message | HTTP |
|----------|---------|------|
| `WorkflowTemplate.NotFound(id)` | `Workflow template {id} not found` | 404 |
| `WorkflowTemplate.NoStagesConfigured` | `Workflow template has no stages configured` | 400 |
| `WorkflowTemplate.HasActiveInstances` | `Cannot delete a template that has associated workflow instances. Deactivate it instead.` | 409 |
| `WorkflowTemplate.NoActiveTemplate` | `No active workflow template found for budget plan approval. Please configure a workflow template for 'BudgetPlanApproval'.` | 400 |

---

## File Attachment

| Constant | Message | HTTP |
|----------|---------|------|
| `FileAttachment.AtLeastOneRequired` | `At least one file is required` | 400 |
| `FileAttachment.CannotModifyCurrentState` | `Attachments cannot be modified in the current state` | 409 |
| `FileAttachment.NoPermissionToDelete` | `You do not have permission to delete this file` | 403 |
| `FileAttachment.FileTypeNotSupported` | `File type not supported or content is invalid` | 400 |
| `FileAttachment.StoredFileNotFound` | `Stored file not found` | 404 |
| `FileAttachment.WouldExceedMax(count, max)` | `Uploading {count} file(s) would exceed the maximum of {max} attachments allowed for this record` | 400 |

---

## Object Storage

Thrown as `InvalidOperationException` at startup (not mapped to HTTP - causes app boot failure).

| Constant | Message |
|----------|---------|
| `ObjectStorage.BucketNameRequired` | `ObjectStorage:BucketName is required when Endpoint is set.` |
| `ObjectStorage.AccessKeyRequired` | `ObjectStorage:AccessKey is required when Endpoint is set.` |
| `ObjectStorage.SecretKeyRequired` | `ObjectStorage:SecretKey is required when Endpoint is set.` |

---

## Export

| Constant | Message | HTTP |
|----------|---------|------|
| `Export.PdfMaxRowsExceeded(max)` | `PDF export exceeds the maximum row limit of {max}. Apply filters to reduce the result set.` | 400 |

---

## Sync

| Constant | Message | HTTP |
|----------|---------|------|
| `Sync.MissingRequiredField(fieldName, recordJson)` | `Missing required field '{fieldName}'. Record={recordJson}` | 400 |

---

## Validation

Used by FluentValidation validators in `src/WAMS.Application/Validators/` (always 400). Kept separate from the domain-exception messages above since wording sometimes intentionally overlaps (e.g. `Validation.Common.VendorRequired` vs a domain not-found message).

### Common (shared across multiple validators)

| Constant | Message |
|----------|---------|
| `Validation.Common.EmailRequired` | `Email is required` |
| `Validation.Common.InvalidEmailFormat` | `Invalid email format` |
| `Validation.Common.PasswordRequired` | `Password is required` |
| `Validation.Common.PasswordMinLength` | `Password must be at least 8 characters` |
| `Validation.Common.NewPasswordRequired` | `New password is required` |
| `Validation.Common.NewPasswordMinLength` | `New password must be at least 8 characters` |
| `Validation.Common.VendorRequired` | `Vendor is required` |
| `Validation.Common.DocDateRequired` | `Document date is required` |
| `Validation.Common.AtLeastOneLineItemRequired` | `At least one line item is required` |
| `Validation.Common.InvalidBudgetPlanItemId` | `Invalid budget plan item ID` |

### Auth (`LoginRequestValidator`, `ChangePasswordRequestValidator`)

| Constant | Message |
|----------|---------|
| `Validation.Auth.CurrentPasswordRequired` | `Current password is required` |
| `Validation.Auth.CompanySelectionRequired` | `Company selection is required` |

### User (`CreateUserRequestValidator`)

| Constant | Message |
|----------|---------|
| `Validation.User.FullnameRequired` | `Fullname is required` |
| `Validation.User.WarehouseIdsMustHaveEntry` | `WarehouseIds must contain at least one entry if provided` |
| `Validation.User.WarehouseIdsNoDuplicates` | `WarehouseIds must not contain duplicates` |
| `Validation.User.PrimaryWarehouseIdMustBeInWarehouseIds` | `PrimaryWarehouseId must be present in WarehouseIds` |

### Company (`CreateCompanyRequestValidator`)

| Constant | Message |
|----------|---------|
| `Validation.Company.CodeRequired` | `Company code is required` |
| `Validation.Company.CodeFormat` | `Code must be uppercase alphanumeric with hyphens/underscores` |
| `Validation.Company.NameRequired` | `Company name is required` |

### Rate Card (`CreateRateCardRequestValidator`)

| Constant | Message |
|----------|---------|
| `Validation.RateCard.AtLeastOneItemRequired` | `At least one item is required` |

### File Upload (`FileUploadRequestValidator`)

| Constant | Message |
|----------|---------|
| `Validation.FileUpload.EntityTypeRequired` | `Entity type is required` |
| `Validation.FileUpload.EntityTypeTooLong` | `Entity type is too long` |
| `Validation.FileUpload.EntityTypeFormat` | `Entity type must contain only lowercase letters, numbers, and hyphens` |
| `Validation.FileUpload.EntityTypeInvalidPathCharacters` | `Entity type contains invalid path characters` |
| `Validation.FileUpload.EntityIdRequired` | `Entity ID is required` |
| `Validation.FileUpload.MaxAttachmentsExceeded(max)` | `Cannot upload more than {max} files at once` |
| `Validation.FileUpload.FileRequired` | `File is required` |
| `Validation.FileUpload.FileSizeExceeds(maxSize)` | `File size exceeds the maximum allowed size of {maxSize}` |
| `Validation.FileUpload.ContentTypeRequired` | `File content type is required` |
| `Validation.FileUpload.FileTypeNotAllowed` | `File type is not allowed` |
| `Validation.FileUpload.FileNameRequired` | `File name is required` |

> "at least one file" checks reuse `FileAttachment.AtLeastOneRequired` (see above) since the wording is identical.

### Workflow Template (`CreateWorkflowTemplateRequestValidator`, `UpdateWorkflowTemplateRequestValidator`)

| Constant | Message |
|----------|---------|
| `Validation.WorkflowTemplate.NameRequired` | `Name is required` |
| `Validation.WorkflowTemplate.NameMustNotBeEmpty` | `Name must not be empty` |
| `Validation.WorkflowTemplate.NameMaxLength` | `Name must not exceed 200 characters` |
| `Validation.WorkflowTemplate.AtLeastOneStageRequired` | `At least one stage is required` |
| `Validation.WorkflowTemplate.StagesMustNotBeEmptyWhenProvided` | `Stages must not be empty when provided` |
| `Validation.WorkflowTemplate.StageOrderGreaterThanZero` | `StageOrder must be greater than 0` |
| `Validation.WorkflowTemplate.StageNameRequired` | `StageName is required` |
| `Validation.WorkflowTemplate.StageNameMaxLength` | `StageName must not exceed 200 characters` |
| `Validation.WorkflowTemplate.ApproverRolesRequired` | `Each stage must have at least one approver role` |
| `Validation.WorkflowTemplate.ApproverRoleNameRequired` | `Approver role name must not be empty` |
| `Validation.WorkflowTemplate.StageOrdersMustBeUnique` | `Stage orders must be unique` |
| `Validation.WorkflowTemplate.DocTypeRequired` | `DocType is required` |
| `Validation.WorkflowTemplate.DocTypeMustBeOneOf(values)` | `DocType must be one of: {values}` |

### Account Payable (`CreateAccountPayableRequestValidator`)

| Constant | Message |
|----------|---------|
| `Validation.AccountPayable.RemarkMaxLength` | `Remark must not exceed 500 characters` |

### Budget Revision (`SubmitBudgetRevisionRequestValidator`)

| Constant | Message |
|----------|---------|
| `Validation.BudgetRevision.RecapWorkOrderIdRequired` | `Recap work order ID is required` |
| `Validation.BudgetRevision.RevisedTotalMustBeGreaterThanZero` | `Revised total must be greater than zero` |
| `Validation.BudgetRevision.ReasonRequired` | `Reason is required` |
| `Validation.BudgetRevision.ReasonMaxLength` | `Reason must not exceed 1000 characters` |

### Budget Plan (`CreateBudgetPlanRequestValidator`)

| Constant | Message |
|----------|---------|
| `Validation.BudgetPlan.BudgetTemplateRequired` | `Budget template is required` |
| `Validation.BudgetPlan.ItemRequired` | `Item is required` |
| `Validation.BudgetPlan.QuantityMustBeGreaterThanZero` | `Quantity must be greater than zero` |
| `Validation.BudgetPlan.UnitCostOverrideMustBePositive` | `Unit cost override must be greater than zero` |
| `Validation.BudgetPlan.SpkReferenceMustBeInBaseList` | `Each cost item's SPK reference must be included in the base document list` |

### Notification (`SendTestNotificationRequestValidator`)

| Constant | Message |
|----------|---------|
| `Validation.Notification.TypeRequired` | `Type is required` |
| `Validation.Notification.TitleRequired` | `Title is required` |
| `Validation.Notification.MessageRequired` | `Message is required` |
| `Validation.Notification.ReferenceTypeRequired` | `ReferenceType is required` |
| `Validation.Notification.ReferenceIdRequired` | `ReferenceId is required` |

### Work Order (`WorkOrderRequestValidator` - `FumigationDetailValidator`)

| Constant | Message |
|----------|---------|
| `Validation.WorkOrder.TemperatureRange(field, min, max)` | `{field} temperature must be between {min} and {max}` |
| `Validation.WorkOrder.DosageRange(name, max)` | `{name} dosage must be between 0 and {max}` |

---

## Vendor

| Constant | Message | HTTP |
|----------|---------|------|
| `Vendor.NotFound(id)` | `Vendor {id} not found` | 404 |

---

## Item

| Constant | Message | HTTP |
|----------|---------|------|
| `Item.ShadowNotFound(id)` | `Item shadow {id} not found` | 404 |
| `Item.DuplicateShadow(id)` | `Duplicate item shadow {id} in request` | 400 |

---

## Transport Order

| Constant | Message | HTTP |
|----------|---------|------|
| `TransportOrder.ShadowNotFound(id)` | `Transport order shadow {id} not found or inactive` | 404 |

---

## Notification

| Constant | Message | HTTP |
|----------|---------|------|
| `Notification.NotFound(id)` | `Notification {id} not found` | 404 |

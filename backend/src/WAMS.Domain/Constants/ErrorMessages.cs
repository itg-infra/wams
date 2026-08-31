namespace WAMS.Domain.Constants;

public static class ErrorMessages
{
    public static class Permission
    {
        public const string AuthenticationRequired = "Authentication required";
        public const string TokenRevoked = "Token has been revoked";
        public const string InvalidTokenSubject = "Invalid token: missing or invalid subject claim";
        public const string UserIdClaimNotFound = "User ID claim not found in token";
        public const string CompanyIdClaimNotFound = "Company ID claim not found in token";
        public static string MissingPermission(string permission) => $"Missing permission: {permission}";
        public static string InvalidPermissionKey(string key) =>
            $"Permission key must have exactly 3 dot-separated segments (module.resource.action), got: '{key}'";
    }

    public static class Auth
    {
        public const string InvalidCredentials = "Invalid email or password";
        public const string AccountInactive = "Account is inactive";
        public const string InvalidRefreshToken = "Invalid refresh token";
        public const string RefreshTokenExpiredOrRevoked = "Refresh token is expired or revoked";
        public const string CompanyNotFoundOrInactive = "Selected company does not exist or is inactive";
        public const string SessionIdleTimeout = "Session expired due to inactivity";
    }

    public static class Notification
    {
        public static string NotFound(long id) => $"Notification {id} not found";
    }

    public static class Province
    {
        public static string NotFound(long id) => $"Province {id} not found";
        public static string NotActive(string name) => $"Province '{name}' is not active";
    }

    public static class BudgetPlan
    {
        public static string NotFound(long id) => $"Budget plan {id} not found";
        public const string WarehouseProvinceMismatch =
            "The selected warehouse is not in the same province as the budget template.";
        public const string NotFoundAfterCreation = "Budget plan not found after creation";
        public static string NotFoundAfterSubmit(long id) => $"Budget plan {id} not found after submit";
        public static string NotFoundAfterUpdate(long id) => $"Budget plan {id} not found after update";
        public const string CannotUpdateOnlyDraftOrRejected = "Only Draft or Rejected plans can be updated";
        public const string CannotSubmitWithNoItems = "Cannot submit a plan with no items";
        public const string CannotSubmitOnlyDraftOrRejected = "Only Draft or Rejected plans can be submitted";
        public static string CannotApprove(string status) => $"Budget plan in status '{status}' cannot be approved";
        public const string NoWorkflow = "Budget plan has no active workflow instance";
        public const string NoPendingApprovalStage = "No pending approval stage found";
        public static string NotAuthorizedAtStage(int stage) => $"Not authorized to approve at stage {stage}";
        public const string CannotApproveOwnSubmission = "You cannot approve a budget plan you submitted";
        public const string CannotRejectOnlySubmittedOrInApproval = "Only Submitted or InApproval plans can be rejected";
        public const string RejectionReasonRequired = "Rejection reason is required";
        public const string AlreadyProcessed = "This budget plan was already processed by another request.";
        public const string CannotDeleteOnlyDraft = "Only Draft plans can be deleted";
        public static string NoRfbaItems(long id) => $"Budget plan {id} has no RFBA items to print";
        public static string UnitCostOverrideMustBePositive(long itemId) =>
            $"Unit cost override for item {itemId} must be greater than zero";
        public const string ItemNotBelongToPlan = "The specified budget plan item does not belong to this budget plan";
        public const string WorkOrderAlreadyExists = "A work order already exists for this budget plan item";
        public const string RequiresApprovedPlan = "Work orders can only be created against fully approved budget plans";
        public static string CannotRemoveItemWithWorkOrders(long itemShadowId) =>
            $"Cannot remove item {itemShadowId}: it already has work orders against it";
        public static string CannotReduceItemBelowCommitted(long itemShadowId, decimal committedTotal) =>
            $"Cannot reduce item {itemShadowId} below its already-committed total of {committedTotal:N2}";
        public static string CannotSplitItemWithWorkOrders(long itemShadowId) =>
            $"Cannot split item {itemShadowId} across multiple rows: it already has work orders against it";
    }

    public static class BudgetTemplate
    {
        public static string NotFound(long id) => $"Budget template {id} not found";
        public const string NotFoundAfterCreation = "Budget template not found after creation";
        public const string CannotUpdateOnlyDraftOrSubmitted = "Template can only be updated in Draft or Submitted status";
        public const string CannotSubmitOnlyDraft = "Only Draft templates can be submitted";
        public const string CannotDeleteOnlyDraft = "Only Draft templates can be deleted";
        public const string OnlySubmittedCanBeUsed = "Only Submitted templates can be used for budget plans";
    }

    public static class WorkOrder
    {
        public static string NotFound(long id) => $"Work order {id} not found";
        public const string LockedRecapApproved = "Work order is locked: the associated recap has been approved";
        public const string CannotUpdateOnlyDraft = "Only Draft work orders can be updated";
        public const string CannotDeleteOnlyDraft = "Only Draft work orders can be deleted";
        public const string CannotSubmitOnlyDraft = "Only Draft work orders can be submitted";
        public const string GpsRequiredBeforeSubmit = "GPS location is required before submitting a work order";
        public const string DatesRequiredBeforeSubmit = "Start date and end date are required before submitting a work order";
        public const string PicRequiredBeforeSubmit = "PIC is required before submitting a work order";
        public static string PicUserNotFound(long id) => $"PIC user {id} not found";
        public const string RequiresHeavyEquipmentDetail = ActivityTypeCodes.AlatBerat + " work orders require heavy equipment detail before submission";
        public const string RequiresUnbaggingDetail = ActivityTypeCodes.Unbagging + " work orders require unbagging detail before submission";
        public const string RequiresRebaggingDetail = ActivityTypeCodes.Rebagging + " work orders require rebagging detail before submission";
        public const string RequiresFumigationDetail = ActivityTypeCodes.Fumigasi + " work orders require fumigation detail before submission";
        public const string RequiresQcDetail = ActivityTypeCodes.Qc + " work orders require QC detail before submission";
        public const string RequiresUnloadingItem = ActivityTypeCodes.Bongkar + " work orders require at least one unloading item before submission";
        public const string RequiresLoadingItem = ActivityTypeCodes.Muat + " work orders require at least one loading item before submission";
        public static string RequiresStorageHandlingDetail(string code) =>
            $"{code} work orders require storage/handling detail before submission";
        public const string UnloadingBlNumberRequired = "UnloadingItem BlNumber is required and cannot be empty";
        public const string LoadingBlNumberRequired = "LoadingItem BlNumber is required and cannot be empty";
    }

    public static class RecapWorkOrder
    {
        public static string NotFound(long id) => $"Recap work order {id} not found";
        public const string CannotApproveOnlyPending = "Only Pending recaps can be approved";
        public const string CannotRejectOnlyPending = "Only Pending recaps can be rejected";
        public static string CannotApproveHasDraftWorkOrders(int count) =>
            $"Cannot approve recap: {count} work order(s) are still in Draft. All must be Submitted first.";
        public const string AccessDeniedDifferentWarehouse = "Access denied: recap belongs to a different warehouse";
    }

    public static class AccountPayable
    {
        public static string NotFound(long id) => $"Account payable {id} not found";
        public const string CannotUpdateOnlyDraft = "Only Draft account payables can be updated";
        public const string CannotDeleteOnlyDraft = "Only Draft account payables can be deleted";
        public const string CannotGenerateOnlyDraft = "Only Draft account payables can be generated";
        public const string NoItemsCannotGenerate = "Account payable has no items and cannot be generated";
        public const string SapNoApNumber = "SAP integration returned no AP number. Please try again.";
        public static string GenerationInProgress(long id) =>
            $"Another request is already generating account payable {id}.";
        public static string ItemUnavailable(long itemId) =>
            $"Budget plan item {itemId} is unavailable.";
        public static string ItemNotFound(long itemId) =>
            $"Budget plan item {itemId} was not found.";
        public static string ItemVendorMismatch(long itemId) =>
            $"Budget plan item {itemId} belongs to a different vendor.";
        public static string ItemWarehouseNotAccessible(long itemId) =>
            $"Budget plan item {itemId} belongs to a warehouse you do not have access to.";
        public static string ItemRecapNotApproved(long itemId) =>
            $"Budget plan item {itemId}'s recap has not been approved yet.";
        public static string ItemAlreadyGenerated(long itemId) =>
            $"Budget plan item {itemId} is already included in a generated account payable.";
        public static string ItemAlreadyTaken(long itemId, string code) =>
            $"Budget plan item {itemId} is already used in account payable {code}.";
        public const string DiscountNegative = "Discount amount must not be negative.";
        public static string DiscountExceedsDpp(decimal discountAmount, decimal dppTotal) =>
            $"Discount amount ({discountAmount}) cannot exceed the total DPP ({dppTotal}).";
        public static string ItemsMissingGeneratedPo(IEnumerable<long> itemIds) =>
            $"Budget plan items {string.Join(", ", itemIds)} have no generated purchase order. "
            + "Create and generate a PO for these items before generating the AP.";
    }

    public static class PurchaseOrder
    {
        public static string SeedBudgetPlanWarehouseMismatch(long budgetPlanId, long warehouseId) =>
            $"Budget plan {budgetPlanId} does not belong to active warehouse {warehouseId}.";
        public static string SeedVendorMismatch(long vendorId, long budgetPlanId) =>
            $"Vendor {vendorId} is not present in budget plan {budgetPlanId}.";
        public static string NotFound(long id) => $"Purchase order {id} not found";
        public const string NotFoundAfterCreation = "Purchase order not found after creation";
        public const string CannotUpdateOnlyDraft = "Only Draft purchase orders can be updated";
        public const string CannotDeleteOnlyDraft = "Only Draft purchase orders can be deleted";
        public const string CannotGenerateOnlyDraft = "Only Draft purchase orders can be generated";
        public const string NoItemsCannotGenerate = "Purchase order has no items and cannot be generated";
        public const string SapNoPoNumber = "SAP integration returned no PO number. Please try again.";
        public const string CannotGenerateApdpOnlyGenerated = "APDP can only be generated after the purchase order is generated";
        public const string NoRfbaItemsCannotGenerateApdp = "This purchase order has no RFBA items, so APDP is not required";
        public const string MixedRfbaItemsNotAllowed = "RFBA Yes and RFBA No items cannot be generated in the same purchase order.";
        public const string SapNoApdpDocument = "SAP integration returned no APDP document. Please try again.";
        public static string ApdpGenerationInProgress(long id) =>
            $"Another request is already generating APDP for purchase order {id}.";
        public static string ApdpRequiredBeforeInvoice(IEnumerable<long> itemIds) =>
            $"RFBA items {string.Join(", ", itemIds)} require APDP generation from their purchase order before generating the AP invoice.";
        public static string GenerationInProgress(long id) =>
            $"Another request is already generating purchase order {id}.";
        public static string ItemUnavailable(long itemId) =>
            $"Budget plan item {itemId} is unavailable.";
        public static string ItemNotFound(long itemId) =>
            $"Budget plan item {itemId} was not found.";
        public static string ItemVendorMismatch(long itemId) =>
            $"Budget plan item {itemId} belongs to a different vendor.";
        public static string ItemWarehouseNotAccessible(long itemId) =>
            $"Budget plan item {itemId} belongs to a warehouse you do not have access to.";
        public static string ItemPlanNotApproved(long itemId) =>
            $"Budget plan item {itemId}'s budget plan has not been approved yet.";
        public static string ItemAlreadyGenerated(long itemId) =>
            $"Budget plan item {itemId} is already included in a generated purchase order.";
        public static string ItemAlreadyTaken(long itemId, string code) =>
            $"Budget plan item {itemId} is already used in purchase order {code}.";
        public static string NoRfbaItems(long id) => $"Purchase order {id} has no RFBA items to print";
    }

    public static class User
    {
        public static string NotFound(long id) => $"User {id} not found";
        public static string EmailConflict(string email) => $"User with email '{email}' already exists";
        public static string AlreadyHasRole(string name) => $"User already has role '{name}'";
        public const string AlreadyAssignedToCompany = "User is already assigned to this company";
    }

    public static class Role
    {
        public static string NotFound(long id) => $"Role {id} not found";
        public static string AlreadyExists(string name) => $"Role '{name}' already exists";
        public const string SystemRoleCannotBeModified = "System roles cannot be modified";
        public const string SystemRoleCannotBeDeleted = "System roles cannot be deleted";
        public const string SystemRolePermissionsCannotBeModified = "System role permissions cannot be modified";
    }

    public static class Company
    {
        public static string NotFound(long id) => $"Company {id} not found";
        public static string CodeConflict(string code) => $"Company with code '{code}' already exists";
        public const string CannotDeactivateDefault = "Cannot deactivate the default company";
        public const string AlreadyAssigned = "User is already assigned to this company";
        public const string TenantContextNotSet = "Tenant context not set";
        public const string AccessDeniedLogo = "Access denied to this company's logo";
        public const string LogoFileRequired = "File is required";
        public const string LogoExceedsMaxSize = "File exceeds maximum size of 2 MB";
        public static string LogoContentTypeNotAllowed(string contentType) =>
            $"Content type '{contentType}' is not allowed";
    }

    public static class Gps
    {
        public const string LatitudeOutOfRange = "GPS latitude must be between -90 and 90";
        public const string LongitudeOutOfRange = "GPS longitude must be between -180 and 180";
        public const string RecordedAtInFuture = "GPS recorded time cannot be in the future";
    }

    public static class Warehouse
    {
        public static string NotFound(long id) => $"Warehouse {id} not found";
        public const string AccessDenied = "You do not have access to this warehouse";
    }

    public static class ActivityType
    {
        public static string NotFound(long id) => $"Activity type {id} not found";
        public static string CodeConflict(string code) => $"Activity type with code '{code}' already exists";
        public static string NotActive(string name) => $"Activity type '{name}' is not active";
    }

    public static class Uom
    {
        public static string NotFound(long id) => $"UoM {id} not found";
        public static string CodeConflict(string code) => $"UoM code '{code}' already exists";
        public const string ReferencedByRateCard = "Cannot delete UoM that is referenced by rate card items";
    }

    public static class TaxType
    {
        public static string NotFound(long id) => $"Tax type {id} not found";
        public static string CodeConflict(string code) => $"Tax type code '{code}' already exists";
        public static string WrongCategory(long id, string expectedCategory) =>
            $"Tax type {id} is not a valid {expectedCategory} type";
        public static string Inactive(string code) => $"Tax type '{code}' is inactive";
    }

    public static class RateCard
    {
        public static string NotFound(long id) => $"Rate card {id} not found";
        public const string NotFoundAfterCreation = "Rate card not found after creation";
        public const string CannotSubmitOnlyDraft = "Only draft rate cards can be submitted";
        public const string MustHaveItemBeforeSubmit = "Rate card must have at least one item before submitting";
        public static string ItemNotFound(long id) => $"Item {id} not found";
        public static string UomNotFound(long id) => $"UOM {id} not found";
        public static string SubmittedRateNotFound(long vendorId, long itemId) =>
            $"No submitted rate card found for vendor {vendorId} and item {itemId}. " +
            "Please ensure a rate card exists and is submitted.";
        public static string RateCardNotFoundForVendorItem(long vendorId, long itemId) =>
            $"No rate card exists for vendor {vendorId} and item {itemId}.";
        public static string RateCardNotSubmitted(long vendorId, long itemId) =>
            $"A rate card exists for vendor {vendorId} and item {itemId}, but it has not been submitted yet.";
    }

    public static class Vendor
    {
        public static string NotFound(long id) => $"Vendor {id} not found";
    }

    public static class Item
    {
        public static string ShadowNotFound(long id) => $"Item shadow {id} not found";
        public static string DuplicateShadow(long id) => $"Duplicate item shadow {id} in request";
    }

    public static class TransportOrder
    {
        public static string NotFound(long id) => $"Transport order {id} not found";
        public static string ShadowNotFound(long id) => $"Transport order shadow {id} not found or inactive";
    }

    public static class Spk
    {
        public static string NotFound(object id) => $"SPK {id} not found";
        public static string ItemNotFound(long spkItemId, long planId) =>
            $"SPK item {spkItemId} not found on plan {planId}";
        public static string AlreadyLinked(string docNo) => $"SPK {docNo} is already linked to this budget plan";
        public static string NotLinkedToPlan(long spkShadowId) =>
            $"SPK {spkShadowId} is not linked to this budget plan";
        public static string QuantityExceedsSpk(decimal quantity, decimal spkQuantity, long spkShadowId) =>
            $"Quantity {quantity} exceeds SPK quantity {spkQuantity} for SPK {spkShadowId}";
        public static string CannotReplaceSpkListOrphanedItems(string ids) =>
            $"Cannot replace SPK list: cost items still reference SPK(s) {ids}. Re-send Items without those references, or keep the SPKs.";
    }

    public static class WorkflowTemplate
    {
        public static string NotFound(long id) => $"Workflow template {id} not found";
        public const string NoStagesConfigured = "Workflow template has no stages configured";
        public const string HasActiveInstances =
            "Cannot delete a template that has associated workflow instances. Deactivate it instead.";
        public const string NoActiveTemplate =
            "No active workflow template found for budget plan approval. " +
            "Please configure a workflow template for 'BudgetPlanApproval'.";
    }

    public static class Export
    {
        public static string PdfMaxRowsExceeded(int max) =>
            $"PDF export exceeds the maximum row limit of {max}. Apply filters to reduce the result set.";
    }

    public static class Sync
    {
        public static string MissingRequiredField(string fieldName, string recordJson) =>
            $"Missing required field '{fieldName}'. Record={recordJson}";
        public static string ServiceNotFound(string serviceName) => $"Sync service '{serviceName}' not found.";
    }

    public static class Rca
    {
        public const string InvalidDateRange = "dateFrom must be on or before dateTo";
    }

    public static class FileAttachment
    {
        public const string AtLeastOneRequired = "At least one file is required";
        public const string CannotModifyCurrentState = "Attachments cannot be modified in the current state";
        public const string NoPermissionToDelete = "You do not have permission to delete this file";
        public const string FileTypeNotSupported = "File type not supported or content is invalid";
        public const string StoredFileNotFound = "Stored file not found";
        public static string WouldExceedMax(int count, int max) =>
            $"Uploading {count} file(s) would exceed the maximum of {max} attachments allowed for this record";
        public static string WouldExceedTotalSize(string maxTotalSize) =>
            $"Uploading these file(s) would exceed the maximum total size of {maxTotalSize} allowed for this record";
    }

    public static class ObjectStorage
    {
        public const string BucketNameRequired = "ObjectStorage:BucketName is required when Endpoint is set.";
        public const string AccessKeyRequired = "ObjectStorage:AccessKey is required when Endpoint is set.";
        public const string SecretKeyRequired = "ObjectStorage:SecretKey is required when Endpoint is set.";
    }

    /// <summary>
    /// Messages used by FluentValidation validators (400 Bad Request), kept separate from the
    /// domain-exception messages above since wording sometimes intentionally overlaps.
    /// </summary>
    public static class Validation
    {
        public static class Common
        {
            public const string EmailRequired = "Email is required";
            public const string InvalidEmailFormat = "Invalid email format";
            public const string PasswordRequired = "Password is required";
            public const string PasswordMinLength = "Password must be at least 8 characters";
            public const string NewPasswordRequired = "New password is required";
            public const string NewPasswordMinLength = "New password must be at least 8 characters";
            public const string VendorRequired = "Vendor is required";
            public const string DocDateRequired = "Document date is required";
            public const string AtLeastOneLineItemRequired = "At least one line item is required";
            public const string InvalidBudgetPlanItemId = "Invalid budget plan item ID";
        }

        public static class Auth
        {
            public const string CurrentPasswordRequired = "Current password is required";
            public const string CompanySelectionRequired = "Company selection is required";
        }

        public static class User
        {
            public const string FullnameRequired = "Fullname is required";
            public const string WarehouseIdsMustHaveEntry = "WarehouseIds must contain at least one entry if provided";
            public const string WarehouseIdsNoDuplicates = "WarehouseIds must not contain duplicates";
            public const string PrimaryWarehouseIdMustBeInWarehouseIds = "PrimaryWarehouseId must be present in WarehouseIds";
        }

        public static class Company
        {
            public const string CodeRequired = "Company code is required";
            public const string CodeFormat = "Code must be uppercase alphanumeric with hyphens/underscores";
            public const string NameRequired = "Company name is required";
        }

        public static class RateCard
        {
            public const string AtLeastOneItemRequired = "At least one item is required";
            public const string InvalidCostTreatment = "Cost treatment must be either 'Dibiayakan' or 'TidakDibiayakan'.";
        }

        public static class TaxType
        {
            public const string CategoryRequired = "Tax category is required";
            public static string CategoryInvalid(string value) => $"Invalid tax category '{value}'. Must be 'Ppn' or 'Pph'.";
            public const string CodeRequired = "Code is required";
            public const string CodeTooLong = "Code must not exceed 20 characters";
            public const string NameRequired = "Name is required";
            public const string NameTooLong = "Name must not exceed 100 characters";
            public const string RateMustBeNonNegative = "Rate must be zero or greater";
            public const string RateMustNotExceed100 = "Rate must not exceed 100";
        }

        public static class FileUpload
        {
            public const string EntityTypeRequired = "Entity type is required";
            public const string EntityTypeTooLong = "Entity type is too long";
            public const string EntityTypeFormat = "Entity type must contain only lowercase letters, numbers, and hyphens";
            public const string EntityTypeInvalidPathCharacters = "Entity type contains invalid path characters";
            public const string EntityIdRequired = "Entity ID is required";
            public static string MaxAttachmentsExceeded(int max) => $"Cannot upload more than {max} files at once";
            public const string FileRequired = "File is required";
            public static string FileSizeExceeds(string maxSize) => $"File size exceeds the maximum allowed size of {maxSize}";
            public const string ContentTypeRequired = "File content type is required";
            public const string FileTypeNotAllowed = "File type is not allowed";
            public const string FileNameRequired = "File name is required";
        }

        public static class WorkflowTemplate
        {
            public const string NameRequired = "Name is required";
            public const string NameMustNotBeEmpty = "Name must not be empty";
            public const string NameMaxLength = "Name must not exceed 200 characters";
            public const string AtLeastOneStageRequired = "At least one stage is required";
            public const string StagesMustNotBeEmptyWhenProvided = "Stages must not be empty when provided";
            public const string StageOrderGreaterThanZero = "StageOrder must be greater than 0";
            public const string StageNameRequired = "StageName is required";
            public const string StageNameMaxLength = "StageName must not exceed 200 characters";
            public const string ApproverRolesRequired = "Each stage must have at least one approver role";
            public const string ApproverRoleNameRequired = "Approver role name must not be empty";
            public const string StageOrdersMustBeUnique = "Stage orders must be unique";
            public const string DocTypeRequired = "DocType is required";
            public static string DocTypeMustBeOneOf(string values) => $"DocType must be one of: {values}";
        }

        public static class AccountPayable
        {
            public const string RemarkMaxLength = "Remark must not exceed 500 characters";
        }

        public static class BudgetPlan
        {
            public const string BudgetTemplateRequired = "Budget template is required";
            public const string ItemRequired = "Item is required";
            public const string QuantityMustBeGreaterThanZero = "Quantity must be greater than zero";
            public const string UnitCostOverrideMustBePositive = "Unit cost override must be greater than zero";
            public const string SpkReferenceMustBeInBaseList = "Each cost item's SPK reference must be included in the base document list";
            public const string ActivityTypeRequired = "Activity type is required";
        }

        public static class Notification
        {
            public const string TypeRequired = "Type is required";
            public const string TitleRequired = "Title is required";
            public const string MessageRequired = "Message is required";
            public const string ReferenceTypeRequired = "ReferenceType is required";
            public const string ReferenceIdRequired = "ReferenceId is required";
        }

        public static class WorkOrder
        {
            public static string TemperatureRange(string field, decimal min, decimal max) =>
                $"{field} temperature must be between {min} and {max}";
            public static string DosageRange(string name, decimal max) =>
                $"{name} dosage must be between 0 and {max}";
        }
    }
}

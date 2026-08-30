namespace WAMS.Application.Common;

public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string SessionIdleTimeout = "SESSION_IDLE_TIMEOUT";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string InternalError = "INTERNAL_ERROR";
    public const string InternalErrorMessage = "An unexpected error occurred";
    public const string InvalidBudgetPlanId = "Invalid budget plan ID: '{0}'";
    public const string PurchaseOrderItemVendorMismatch = "PURCHASE_ORDER_ITEM_VENDOR_MISMATCH";
    public const string AuditLogNotFound = "Audit log {0} not found";
}

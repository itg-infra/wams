namespace WAMS.Infrastructure.Caching.Common;

/// <summary>
/// All HybridCache tag strings. Tags drive invalidation.
/// Cleared on: role-level change → RbacAllPerms/WarehouseShadows;
/// user-level override → RbacUser/WarehouseShadowsForUser; ERP sync → WarehouseShadows.
/// </summary>
internal static class CacheTags
{
    // RBAC
    internal const string RbacAllPerms = "rbac-all-perms";
    internal const string PermissionsCatalog = "permissions-catalog";

    internal static string RbacUser(long userId) => $"rbac-user:{userId}";

    // Reference data
    internal const string Uom = "uom";
    internal const string ActivityTypes = "activity-types";
    internal const string TaxTypes = "tax-types";

    internal static string WorkflowTemplates(long companyId) => $"workflow-templates:{companyId}";

    // ERP-synced shadows
    internal const string WarehouseShadows = "warehouse-shadows";
    internal static string WarehouseShadowsForUser(long userId) => $"warehouse-shadows:user:{userId}";

    // Lookup data
    internal const string RateCards = "rate-cards";
}

/// <summary>
/// All HybridCache key patterns. Build keys from these - never hardcode strings in decorators.
/// </summary>
internal static class CacheKeys
{
    // RBAC
    internal static string RbacPerm(long userId, string module, string resource, string action)
        => $"rbac:perm:{userId}:{module}.{resource}.{action}";

    internal static string RbacGlobal(long userId) => $"rbac:global:{userId}";
    internal const string PermissionsCatalog = "rbac:catalog";

    // UoM
    internal static string UomAll(bool activeOnly) => $"uom:all:{activeOnly}";
    internal static string UomById(long id) => $"uom:{id}";

    // ActivityType
    internal const string ActivityTypeAll = "activity-type:all";
    internal static string ActivityTypeById(long id) => $"activity-type:{id}";

    // TaxType
    internal static string TaxTypeAll(string? category, bool activeOnly)
        => $"tax-type:all:{category}:{activeOnly}";

    internal static string TaxTypeById(long id) => $"tax-type:{id}";

    // WorkflowTemplate
    internal static string WorkflowTemplateAll(
        long companyId,
        string? docType,
        string? search,
        string? sortBy,
        string sortOrder,
        int page,
        int limit
    )
        => $"workflow-template:all:{companyId}:dt{docType}:s{search}:sb{sortBy}:so{sortOrder}:p{page}:l{limit}";

    internal static string WorkflowTemplateById(long id, long companyId)
        => $"workflow-template:{id}:{companyId}";

    // WarehouseShadow
    internal static string WarehouseShadowAll(
        long userId,
        string? search,
        long? provinceId,
        string? sortBy,
        string sortOrder,
        int page,
        int limit
    )
        => $"warehouse-shadow:all:{userId}:s{search}:prov{provinceId}:sb{sortBy}:so{sortOrder}:p{page}:l{limit}";

    internal static string WarehouseShadowById(long id, long userId) => $"warehouse-shadow:{id}:{userId}";
    internal static string WarehouseShadowLocations(long userId) => $"warehouse-shadow:locations:{userId}";

    // RateCard
    internal static string RateCardById(long id) => $"rate-card:{id}";
}

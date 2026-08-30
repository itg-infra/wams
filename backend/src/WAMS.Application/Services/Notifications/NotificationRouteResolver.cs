namespace WAMS.Application.Services.Notifications;

internal static class NotificationRouteResolver
{
    public static string? Resolve(string referenceType, string referenceId) => referenceType switch
    {
        "budget_plan" => $"/budgeting/plan/{referenceId}",
        "budget_plan_batch" => "/budgeting/plan?status=InApproval",
        _ => null
    };
}

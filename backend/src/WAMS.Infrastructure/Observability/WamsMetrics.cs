using System.Diagnostics.Metrics;
using WAMS.Application.Interfaces.Common;

namespace WAMS.Infrastructure.Observability;

/// <summary>
/// Custom business metrics for WAMS.
/// Registered as singleton - Meter and instruments are thread-safe and long-lived.
/// </summary>
public sealed class WamsMetrics : IWamsMetrics, IDisposable
{
    public const string MeterName = "WAMS";

    private readonly Meter _meter;

    // Budget Plans
    private readonly Counter<long> _budgetPlansSubmitted;
    private readonly Counter<long> _budgetPlansApproved;
    private readonly Counter<long> _budgetPlansRejected;

    // Work Orders
    private readonly Counter<long> _workOrdersSubmitted;

    // Recap Work Orders
    private readonly Counter<long> _recapWorkOrdersApproved;
    private readonly Counter<long> _recapWorkOrdersRejected;

    // ERP Sync 
    private readonly Counter<long> _erpSyncRuns;
    private readonly Counter<long> _erpSyncItemsUpserted;
    private readonly Counter<long> _erpSyncFailures;
    private readonly Histogram<double> _erpSyncDurationMs;

    // Auth 
    private readonly Counter<long> _authLogins;
    private readonly Counter<long> _authLoginFailures;

    public WamsMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _budgetPlansSubmitted = _meter.CreateCounter<long>(
            "wams.budget_plans.submitted",
            unit: "{plan}",
            description: "Number of budget plans submitted for approval");

        _budgetPlansApproved = _meter.CreateCounter<long>(
            "wams.budget_plans.approved",
            unit: "{plan}",
            description: "Number of budget plans approved");

        _budgetPlansRejected = _meter.CreateCounter<long>(
            "wams.budget_plans.rejected",
            unit: "{plan}",
            description: "Number of budget plans rejected");

        _workOrdersSubmitted = _meter.CreateCounter<long>(
            "wams.work_orders.submitted",
            unit: "{order}",
            description: "Number of work orders submitted");

        _recapWorkOrdersApproved = _meter.CreateCounter<long>(
            "wams.recap_work_orders.approved",
            unit: "{recap}",
            description: "Number of recap work orders approved");

        _recapWorkOrdersRejected = _meter.CreateCounter<long>(
            "wams.recap_work_orders.rejected",
            unit: "{recap}",
            description: "Number of recap work orders rejected");

        _erpSyncRuns = _meter.CreateCounter<long>(
            "wams.erp_sync.runs",
            unit: "{run}",
            description: "Total ERP sync runs (success + failure)");

        _erpSyncItemsUpserted = _meter.CreateCounter<long>(
            "wams.erp_sync.items_upserted",
            unit: "{item}",
            description: "Total items added or updated from ERP sync");

        _erpSyncFailures = _meter.CreateCounter<long>(
            "wams.erp_sync.failures",
            unit: "{failure}",
            description: "ERP sync service-level failures");

        _erpSyncDurationMs = _meter.CreateHistogram<double>(
            "wams.erp_sync.duration",
            unit: "ms",
            description: "ERP sync run duration per service per company");

        _authLogins = _meter.CreateCounter<long>(
            "wams.auth.logins",
            unit: "{login}",
            description: "Successful login attempts");

        _authLoginFailures = _meter.CreateCounter<long>(
            "wams.auth.login_failures",
            unit: "{failure}",
            description: "Failed login attempts");
    }

    // Budget Plan
    public void RecordBudgetPlanSubmitted(long companyId) =>
        _budgetPlansSubmitted.Add(1, new KeyValuePair<string, object?>("company_id", companyId));

    public void RecordBudgetPlanApproved(long companyId, int stageOrder) =>
        _budgetPlansApproved.Add(1,
            new KeyValuePair<string, object?>("company_id", companyId),
            new KeyValuePair<string, object?>("stage_order", stageOrder));

    public void RecordBudgetPlanRejected(long companyId) =>
        _budgetPlansRejected.Add(1, new KeyValuePair<string, object?>("company_id", companyId));

    // Work Order
    public void RecordWorkOrderSubmitted(long companyId) =>
        _workOrdersSubmitted.Add(1, new KeyValuePair<string, object?>("company_id", companyId));

    // Recap Work Order 
    public void RecordRecapWorkOrderApproved(long companyId) =>
        _recapWorkOrdersApproved.Add(1, new KeyValuePair<string, object?>("company_id", companyId));

    public void RecordRecapWorkOrderRejected(long companyId) =>
        _recapWorkOrdersRejected.Add(1, new KeyValuePair<string, object?>("company_id", companyId));

    // ERP Sync
    public void RecordErpSyncRun(string serviceName, bool success) =>
        _erpSyncRuns.Add(1,
            new KeyValuePair<string, object?>("service", serviceName),
            new KeyValuePair<string, object?>("success", success));

    public void RecordErpSyncItemsUpserted(string serviceName, int added, int updated) =>
        _erpSyncItemsUpserted.Add(added + updated,
            new KeyValuePair<string, object?>("service", serviceName));

    public void RecordErpSyncFailure(string serviceName) =>
        _erpSyncFailures.Add(1, new KeyValuePair<string, object?>("service", serviceName));

    public void RecordErpSyncDuration(string serviceName, double milliseconds) =>
        _erpSyncDurationMs.Record(milliseconds,
            new KeyValuePair<string, object?>("service", serviceName));

    // Auth 
    public void RecordLogin(long companyId) =>
        _authLogins.Add(1, new KeyValuePair<string, object?>("company_id", companyId));

    public void RecordLoginFailure() =>
        _authLoginFailures.Add(1);

    public void Dispose() => _meter.Dispose();
}

namespace WAMS.Application.Interfaces.Common;

public interface IWamsMetrics
{
    void RecordBudgetPlanSubmitted(long companyId);
    void RecordBudgetPlanApproved(long companyId, int stageOrder);
    void RecordBudgetPlanRejected(long companyId);

    void RecordWorkOrderSubmitted(long companyId);

    void RecordRecapWorkOrderApproved(long companyId);
    void RecordRecapWorkOrderRejected(long companyId);

    void RecordErpSyncRun(string serviceName, bool success);
    void RecordErpSyncItemsUpserted(string serviceName, int added, int updated);
    void RecordErpSyncFailure(string serviceName);
    void RecordErpSyncDuration(string serviceName, double milliseconds);

    void RecordLogin(long companyId);
    void RecordLoginFailure();
}

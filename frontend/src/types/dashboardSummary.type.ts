export interface DashboardSummaryResponse {
  success: boolean;
  data: DashboardSummary;
  message: string;
  requestId: string;
}

export interface DashboardSummary {
  budgetAchievedPercent: number;
  totalBudgetValue: number;
  totalActualValue: number;
  activePoWithoutApCount: number;
  newPoWithoutApLast7DaysCount: number;
  openWorkOrderCount: number;
  activeWarehouseCount: number;
  pendingApprovalCount: number;
  overdueApprovalCount: number;
}

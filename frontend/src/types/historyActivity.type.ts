import type { HistoryActivityStatus } from "../screens/dashboardContent";

export interface HistoryActivity {
  budgetPlanId: number;
  budgetNo: string;
  vendorName: string;
  remark: string;
  anyRfba: boolean;
  location: string;
  date: string;
  status: string;
  statusDisplay: HistoryActivityStatus;
}

export interface HistoryActivityMeta {
  page: number;
  limit: number;
  total: number;
  totalPages: number;
}

export interface HistoryActivityResponse {
  success: boolean;
  data: HistoryActivity[];
  meta: HistoryActivityMeta;
  requestId: string;
}

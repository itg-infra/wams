export interface PurchaseOrderRef {
  id: number;
  code: string;
}

export interface BudgetPlanListItem {
  budgetPlanId: number;
  budgetPlanCode: string;
  remark: string;
  docDate: string;
  budgetPlanStatus: string;
  budgetPlanStatusDisplay: string;
  hasRfbaItems: boolean;
  vendorName: string;
  makerName: string;
  approvalName: string;
  purchaseOrders: PurchaseOrderRef[];
  location: string;
  totalBudgetPlan: number;
  budgetApproved: number;
  budgetVariance: number;
}

export interface PaginationMeta {
  page: number;
  limit: number;
  total: number;
  totalPages: number;
}

export interface FinanceReportListResponse {
  success: boolean;
  data: BudgetPlanListItem[];
  meta: PaginationMeta;
  requestId: string;
}

/** Query params yang didukung endpoint list (pagination + filter) */
/** Columns the API can order by — see FinanceReportRepository.SortColumns. */
export type FinanceReportSortBy =
  | "createdAt"
  | "sapId"
  | "warehouseCode"
  | "approvalDate";

export interface FinanceReportListParams {
  page?: number;
  limit?: number;
  activityTypeCode?: string;
  warehouseCode?: string;
  dateFrom?: string; // format: YYYY-MM-DD
  dateTo?: string; // format: YYYY-MM-DD
  sortBy?: FinanceReportSortBy;
  sortOrder?: "asc" | "desc";
}

// ---------------- Detail ----------------

export interface FinanceReportHeader {
  budgetPlanId: number;
  budgetNo: string;
  templateId: string;
  templateName: string;
  status: string;
  remark: string;
  docDate: string;
  warehouseCode: string;
  warehouseName: string;
  location: string;
}

export type PaymentStatus = "Unpaid" | "Paid" | (string & {});

export interface FinanceReportCostDetail {
  purchaseOrderItemId: number;
  workOrderId: string;
  blNumber: string | null;
  vessel: string | null;
  product: string;
  pic: string;
  isRfba: boolean;
  startDate: string | null;
  endDate: string | null;
  totalPrice: number;
  isPpnApplied: boolean;
  ppnRatePercent: number;
  totalPricePpn: number;
  isPphApplied: boolean;
  pphType: string | null;
  totalPricePph: number;
  grandTotal: number;
  paymentStatus: PaymentStatus;
}

export interface FinanceReportBudgetRecap {
  budgetPlan: number;
  budgetRealization: number;
  budgetVariance: number;
}

export interface FinanceReportDetail {
  header: FinanceReportHeader;
  costDetails: FinanceReportCostDetail[];
  dpp: number;
  totalPpn: number;
  totalPph: number;
  grandTotal: number;
  budgetRecap: FinanceReportBudgetRecap;
}

export interface FinanceReportDetailResponse {
  success: boolean;
  data: FinanceReportDetail;
  message: string;
  requestId: string;
}

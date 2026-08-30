export interface ApprovedPlanActivity {
  itemShadowId: number;
  itemCode: string;
  activityName: string;
  activityTypeCode: string;
  workOrderId: number | null;
  workOrderCode: string | null;
  workOrderStatus: "Draft" | "Submitted" | "Approved" | null;
}

export interface RealizationApprovedBpApiItem {
  budgetPlanId: number;
  budgetPlanCode: string;
  templateCode: string;
  activityTypeCode: string;
  activityTypeName: string;
  warehouseShadowId: number;
  warehouseCode: string;
  warehouseName: string;
  remark: string;
  isRfba: boolean;
  docDate: string;
  isLocked: boolean;
  makerName: string;
  vendorName: string;
  activities: ApprovedPlanActivity[];
  purchaseOrderId: number;
  purchaseOrderCode: string;
  purchaseOrderStatus: string;
  sapPoNumber: string;
}

export interface RealizationApprovedBpApiResponse {
  success: boolean;
  data: RealizationApprovedBpApiItem[];
  message: string;
  requestId: string;
}

export interface RealizationApprovedBpItem {
  budgetPlanId: number;
  budgetPlanCode: string;
  templateCode: string;
  activityTypeCode: string;
  activityTypeName: string;
  warehouseShadowId: number;
  warehouseCode: string;
  warehouseName: string;
  remark: string;
  isRfba: boolean;
  docDate: string; // formatted
  makerName: string;
  vendorName: string;
  isLocked: boolean;
  activities: ApprovedPlanActivity[];
  purchaseOrderId: number;
  purchaseOrderCode: string;
  purchaseOrderStatus: string;
  sapPoNumber: string;
}

export interface RealizationApprovedBpResponse {
  data: RealizationApprovedBpItem[];
  meta: {
    total: number;
    lastUpdated: string;
  };
}

/* ================= SORT & QUERY TYPES ================= */

export type RealizationApprovedBpSortValue =
  | "latest"
  | "oldest"
  | "name_asc"
  | "name_desc";

export interface RealizationApprovedBudgetPlansQueryParams {
  search?: string;
  sortBy?: RealizationApprovedBpSortValue;
  page?: number;
  limit?: number;
}

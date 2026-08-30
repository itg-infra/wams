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
  vendorShadowId: string;
  vendorCode: string;
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

export interface RecapAPDPListResponse {
  success: boolean;
  data: BudgetPlanListItem[];
  meta: PaginationMeta;
  requestId: string;
}

/** Query params yang didukung endpoint list (pagination + filter) */
export interface RecapAPDPListParams {
  page?: number;
  limit?: number;
  //   activityTypeCode?: string;
  //   warehouseCode?: string;
  //   dateFrom?: string; // format: YYYY-MM-DD
  //   dateTo?: string; // format: YYYY-MM-DD
}

// ---------------- Detail ----------------
export interface PurchaseOrderDetailApdpResponse {
  success: boolean;
  data: PurchaseOrderDetailApdp;
  message: string;
  requestId: string;
}

export interface PurchaseOrderDetailApdp {
  id: number;
  code: string;
  vendorName: string;
  status: string;
  remark: string;
  docDate: string;
  createdAt: string;
  createdByName: string;
  generatedAt: string;
  generatedByName: string;
  linkedBudgetPlans: LinkedBudgetPlan[];
  items: PurchaseOrderDetailApdpItem[];
  grandTotal: number;
  totalItems: number;
}

export interface LinkedBudgetPlan {
  id: number;
  code: string;
  purchaseOrders: LinkedPurchaseOrder[];
}

export interface LinkedPurchaseOrder {
  id: number;
  code: string;
}

export interface PurchaseOrderDetailApdpItem {
  id: number;
  budgetPlanItemId: number;
  itemShadowId: number;
  itemCode: string;
  itemName: string;
  coaCode: string;
  coaName: string;
  vendorShadowId: number;
  vendorCode: string;
  vendorName: string;
  uomMasterId: number;
  uomCode: string;
  uomName: string;
  isRfba: boolean;
  billOfLading: string | null;
  costValue: number;
  quantity: number;
  totalValue: number;
  sortOrder: number;
  ppnTaxTypeCode: string | null;
  ppnRate: number;
  pphTaxTypeCode: string | null;
  pphRate: number;
  ppnAmount: number;
  pphAmount: number;
  grandTotal: number;
  costTreatment: string | null;
}
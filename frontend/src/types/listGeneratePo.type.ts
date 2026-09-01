export interface PurchaseOrder {
  id: number;
  code: string;
  // Status menentukan apakah PO boleh dipakai kembali sebagai edit-flow.
  status: string;
}

export interface ApprovedBudgetPlan {
  budgetPlanId: number;
  budgetPlanCode: string;
  remark: string;
  docDate: string;
  budgetPlanStatus: string;
  budgetPlanStatusDisplay: string;
  hasRfbaItems: boolean;
  vendorShadowId: number;
  vendorCode: string;
  vendorName: string;
  makerName: string;
  approvalName: string;

  purchaseOrders: PurchaseOrder[];

  location: string;

  totalBudgetPlan: number;
  budgetApproved: number;
  budgetVariance: number;
  allGenerated?: boolean;
}
export interface ApprovedBudgetPlanResponse {
  success: boolean;
  data: ApprovedBudgetPlan[];
  message: string;
  requestId: string;
}

export type SortField = keyof ApprovedBudgetPlan;

export interface SortOption {
  field: SortField;
  label: string;
  direction: "asc" | "desc";
}

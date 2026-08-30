
export type BudgetPlanStatus = "Draft" | "Submitted" | "InApproval" | "Approved" | "Rejected";
export type BudgetPlanType = "External" | "Internal";
export type BudgetPlanPaymentType = "NoAdvance" | "Advance";

// API Response shape (raw from server)
export interface BudgetPlanApiItem {
    id: number;
    budgetNo: string;
    templateCode: string;
    remark: string | null;
    location: string | null;
    vendorName: string | null;
    makerName: string | null;
    docDate: string;
    status: BudgetPlanStatus;
    type: BudgetPlanType;
    isRfba: boolean;
    paymentType: BudgetPlanPaymentType;
}

export interface BudgetPlanApiMeta {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
}

export interface BudgetPlanApiResponse {
    success: boolean;
    data: BudgetPlanApiItem[];
    meta: BudgetPlanApiMeta;
    requestId: string | null;
}

// Mapped/UI shape
export interface BudgetPlanItem {
    id: string;
    budgetNo: string;
    templateCode: string;
    remark: string | null;
    location: string | null;
    vendorName: string | null;
    makerName: string | null;
    docDate: string;          // formatted
    status: BudgetPlanStatus;
    type: BudgetPlanType;
    isRfba: boolean;
    paymentType: BudgetPlanPaymentType;
}

export interface BudgetPlanMeta {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
    from: number;
    to: number;
    lastUpdated: string;
}

export interface BudgetPlanResponse {
    data: BudgetPlanItem[];
    meta: BudgetPlanMeta;
}

// Query params
export interface BudgetPlanQueryParams {
  search?: string;
  sortBy?: "budgetNo" | "templateCode" | "location" | "docDate";
  // Tentukan urutan yang diizinkan
  sortOrder?: "asc" | "desc";
  page?: number;
  limit?: number;
  status?: BudgetPlanStatus | "";
  type?: BudgetPlanType | "";
}
export type BudgetType = 'External' | 'Internal';
export type PaymentType = 'NoAdvance' | 'Advance';
export type BudgetStatus = 'Draft' | 'Submitted' | 'PartialApproved' | 'Approved' | 'Rejected';

export interface DetailBudgetTemplateItem {
    id: number;
    templateCode: string;
    activityTypeName: string;
    warehouseCode: string;
    warehouseName: string;
    location: string | null;
}

export interface DetailBudgetApiItem {
    id: number;
    itemShadowId: number;
    itemCode: string;
    itemName: string;
    acctCode: string;
    acctName: string;
    vendorShadowId: number;
    vendorCode: string;
    vendorName: string;
    uomMasterId: number;
    uomCode: string;
    uomName: string;
    costValue: number;
    quantity: number;
    totalValue: number;
    sortOrder: number;
}

export interface DetailBudgetItem {
    id: number;
    itemShadowId: number;
    itemCode: string;
    itemName: string;
    acctCode: string;
    acctName: string;
    vendorShadowId: number;
    vendorCode: string;
    vendorName: string;
    uomMasterId: number;
    uomCode: string;
    uomName: string;
    costValue: number;
    quantity: number;
    totalValue: number;
    sortOrder: number;
}

export interface BudgetApproval {
  totalStages: number;
  currentStageOrder: number;
  stages: ApprovalStage[];
}

export interface ApprovalStage {
  stageOrder: number;
  stageName: string;
  approverRoles: string[];
  status: ApprovalStatus;

  approvedAt: string | null;
  approvedByName: string | null;

  rejectedAt: string | null;
  rejectedByName: string | null;
  rejectionReason: string | null;
}

export type ApprovalStatus = "Pending" | "Approved" | "Rejected";

export interface BudgetPlanResponse {
    id: number;
    budgetNo: string;
    template: DetailBudgetTemplateItem;
    remark: string | null;
    docDate: string;
    type: BudgetType;
    isRfba: boolean;
    paymentType: PaymentType;
    status: BudgetStatus;
    items: DetailBudgetItem[];
    grandTotal: number;
    createdAt: string;
    createdByName: string;
    submittedAt: string | null;
    submittedByName: string | null;
    approval: BudgetApproval;
    rejectedAt: string | null;
    rejectedByName: string | null;
    rejectionReason: string | null;
}
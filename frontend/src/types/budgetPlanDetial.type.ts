import type { costTreament } from "../screens/formMasterPriceList";
import type { BudgetPlanStatus, BudgetPlanType, BudgetPlanPaymentType } from "./budgetPlan.type";
import type { pphTaxTypeItem, ppnTaxTypeItem } from "./budgetRealization.type";

export interface BudgetPlanDetailTemplate {
    id: number;
    templateCode: string;
    activityTypeName: string;
    warehouseCode: string;
    warehouseName: string;
    location: string;
}

// ← field spkItems dari response
export interface BudgetPlanDetailSpkItem {
    id: number;
    spkShadowId: number;
    type: string;
    docNo: string;
    baseDoc: string;
    baseDocNo: string;
    cardCode: string;
    cardName: string;
    itemCode: string;
    itemName: string;
    quantity: number;
    deliveryQty: number | null;
    uoM: string;
    packType: string;
    whsCode: string;
    whsName: string;
    docStatus: string;
    blNo: string;
    sortOrder: number;
}

export interface BudgetPlanDetailItem {
  id: number;
  // Metadata ini membedakan item seed dan item dari budget plan tambahan.
  budgetPlanId: number;
  budgetPlanCode: string;
  budgetPlanItemId: string;
  budgetPlanDocDate?: string;
  isSeedBudgetPlan?: boolean;
  warehouseShadowId?: number;
  warehouseCode?: string;
  warehouseName?: string;
  itemShadowId: number;
  activityTypeName: string;
  itemCode: string;
  activityTypeId: number | null;
  costDetail: string;
  costName: string;
  coa: string;
  remark: string;
  coaName: string;
  vendorShadowId: number;
  vendorCode: string;
  vendorName: string;
  ppnTaxType: ppnTaxTypeItem;
  pphTaxType: pphTaxTypeItem;
  costTreatment: costTreament;
  uomMasterId: number;
  uomCode: string;
  uomName: string;
  costValue: number;
  quantity: number;
  totalValue: number;
  sortOrder: number;
  // ← field tambahan yang ada di response
  type: BudgetPlanType;
  isRfba: boolean;
  paymentType: BudgetPlanPaymentType;
  docExternal: string;
  billOfLading: string;
  kemasan: string;
  description: string | null;
  isGenerated: boolean;
  takenByCode?: string | null;
  availabilityStatus?: string;
}

// Field tambahan yang hanya dijamin tersedia dari endpoint available-items.
export interface AvailableBudgetPlanDetailItem extends BudgetPlanDetailItem {
  budgetPlanDocDate: string;
  isSeedBudgetPlan: boolean;
  warehouseShadowId: number;
  warehouseCode: string;
  warehouseName: string;
  takenByCode: string | null;
  availabilityStatus: string;
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

export interface BudgetPlanDetailApiData {
    id: number;
    budgetNo: string;
    template: BudgetPlanDetailTemplate;
    warehouseCode: string;
    warehouseName: string;
    remark: string;
    docDate: string;
    type: BudgetPlanType;
    isRfba: boolean;
    paymentType: BudgetPlanPaymentType;
    status: BudgetPlanStatus;
    spkItems: BudgetPlanDetailSpkItem[];   // ← spkItems bukan spkShadowIds
    items: BudgetPlanDetailItem[];
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

export interface BudgetPlanDetailApiResponse {
    success: boolean;
    data: BudgetPlanDetailApiData;
    message: string;
    requestId: string;
}

export interface BudgetPlanDetail {
  id: string;
  budgetNo: string;
  templateId: string;
  templateName: string;
  warehouseCode: string;
  warehouseName: string;
  location: string;
  remark: string;
  docDate: string;
  type: BudgetPlanType;
  isRfba: boolean;
  paymentType: BudgetPlanPaymentType;
  status: BudgetPlanStatus;
  spkItems: BudgetPlanDetailSpkItem[];
  items: BudgetPlanDetailItem[];
  grandTotal: number;
  grandTotalFormatted: string;
  createdAt: string;
  createdByName: string;
  submittedAt: string | null;
  submittedByName: string | null;
  approval: BudgetApproval;
  rejectedAt: string | null;
  rejectedByName: string | null;
  rejectionReason: string | null;
}

export interface RejectBudgetPlanPayload {
    reason: string;
}

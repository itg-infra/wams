export interface RealizationRecaApprovedResponse {
  success: boolean;
  message: string;
}

export interface RealizationRecapDetail {
  id: number;
  budgetPlanId: number;
  recapStatus: string;
  reviewedBy: string;
  reviewedAt: string;
  rejectionReason: string | null;

  plan: BudgetPlanSection;
  realization: RealizationSection;
}

export interface RealizationRecapDetailResponse {
  success: boolean;
  data: RealizationRecapDetail;
  message: string;
  requestId: string;
}

export interface RealizationRecapDetail {
  id: number;
  budgetPlanId: number;
  recapStatus: string;
  reviewedBy: string;
  reviewedAt: string;
  rejectionReason: string | null;

  plan: BudgetPlanSection;
  realization: RealizationSection;
}

export interface SpkDocument {
  spkType: string;

  spkNo: string;

  documentNo: string;

  blNo: string;

  itemCode: string;
  itemName: string;

  quantity: number | null;
  deliveryQty: number | null;

  uoM: string;
}

export interface BudgetPlanSection {
  header: BudgetHeader;
  spkDocuments: SpkDocument[];
  costDetails: BudgetPlanCostDetail[];

  budgetPlanTotal: number;
  budgetRealization: number;
  budgetVariance: number;
}

export interface BudgetPlanCostDetail {
  type: string;
  vendorCode: string;
  vendorName: string;
  isRfba: boolean;
  docExternal: string | null;

  costName: string;
  coaCode: string;
  coaName: string;

  billOfLading: string | null;

  unitCost: number;
  unitCount: number;

  uomCode: string;

  description: string | null;

  totalValue: number;
}

export interface RealizationSection {
  header: BudgetHeader;

  workOrders: WorkOrderItem[];

  budgetPlanTotal: number;
  budgetRealization: number;
  budgetVariance: number;
  realizationPercent: number;
}

export interface WorkOrderItem {
  workOrderId: number;
  workOrderCode: string;

  blNumber: string | null;

  picName: string;

  isRfba: boolean;

  startDate: string;
  endDate: string;

  actualCost: number;

  workOrderStatus: string;

  product: string;

  vehicleNo: string;
}

export interface BudgetHeader {
  budgetNo: string;

  templateCode: string;
  templateName: string;

  budgetPlanStatus: string;

  remark: string;

  docDate: string;

  warehouseCode: string;
  warehouseName: string;

  location: string;
}

export type BudgetRevisionRecapItem = {
  id: number;
  budgetPlanId: number;
  recapWorkOrderId: number;
  originalTotal: number;
  revisedTotal: number;
  reason: string;
  status: string;

  submittedBy: string;
  createdAt: string;

  reviewedBy: string | null;
  reviewedAt: string | null;

  rejectionReason: string | null;
};

export type BudgetRevisionRecapResponse = {
  success: boolean;
  data: BudgetRevisionRecapItem[];
  message: string;
  requestId: string;
};
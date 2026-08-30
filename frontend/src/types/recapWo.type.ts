export interface PaginationMeta {
  page: number;
  limit: number;
  total: number;
  totalPages: number;
}

export interface RecapWorkOrderItem {
  id: number;
  budgetPlanId: number;
  budgetPlanCode: string;
  templateCode: string;
  remark: string | null;
  warehouseCode: string;
  warehouseName: string;
  blNumbers: string | null;
  activityTypes: string;
  picNames: string;
  isRfba: boolean;
  docDate: string;
  recapStatus: string;
  createdAt: string;
}

export interface RecapWorkOrderListResponse {
  success: boolean;
  data: RecapWorkOrderItem[];
  meta: PaginationMeta;
  requestId: string;
}

export interface CostDetail {
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
  vehicleNo: string | null;
}

export interface RecapHeader {
  budgetNo: string;
  templateCode: string;
  templateName: string;
  budgetPlanStatus: string;
  remark: string | null;
  docDate: string;
  warehouseCode: string;
  warehouseName: string;
  location: string;
}

export interface PlanSection {
  header: RecapHeader;
  spkDocuments: unknown[];
  costDetails: CostDetail[];
  budgetPlanTotal: number;
  budgetRealization: number;
  budgetVariance: number;
}

export interface RealizationSection {
  header: RecapHeader;
  workOrders: WorkOrderItem[];
  budgetPlanTotal: number;
  budgetRealization: number;
  budgetVariance: number;
  realizationPercent: number;
}

export interface RecapWorkOrderDetail {
  id: number;
  budgetPlanId: number;
  recapStatus: string;
  reviewedBy: string;
  reviewedAt: string;
  rejectionReason: string | null;
  plan: PlanSection;
  realization: RealizationSection;
}

export interface RecapWorkOrderDetailResponse {
  success: boolean;
  data: RecapWorkOrderDetail;
  message: string;
  requestId: string;
}

export interface RejectRecapPayload {
  reason: string;
}

export interface ApproveRecapResponse {
  success: boolean;
  message: string;
}

export interface RejectRecapResponse {
  success: boolean;
  message: string;
}

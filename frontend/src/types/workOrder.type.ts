// ======================================================
// src/types/workOrderType.ts
// ======================================================

export type WorkOrderStatus = "Draft" | "Submitted" | "Approved" | "Rejected";

export type WorkOrderActivityType =
  | "K.BONGKAR"
  | "K.MUAT"
  | "FUMIGASI"
  | "K.GUDANG"
  | "QC"
  | "UNBAGGING"
  | "REBAGGING";

export type WorkOrderQueryParams = {
  page?: number;
  limit?: number;
  search?: string;

  status?: WorkOrderStatus;

  budgetPlanId?: number;

  activityTypeCode?: WorkOrderActivityType;

  dateFrom?: string;
  dateTo?: string;

  sortBy?: "status" | "startDate" | "createdAt";

  sortOrder?: "asc" | "desc";
};

export type WorkOrderItem = {
  id: number;

  code: string;

  budgetPlanId: number;
  budgetPlanCode: string;

  activityTypeCode: string;

  itemShadowId: number;

  activityName: string;

  warehouseCode: string;
  warehouseName: string;

  picName: string;

  isRfba: boolean;

  startDate: string;
  endDate: string;

  status: string;

  createdAt: string;
  createdByName: string;

  blNumber: string;

  productName: string;

  vesselName: string;
};

export type WorkOrderListMeta = {
  page: number;
  limit: number;
  total: number;
  totalPages: number;
};

export type WorkOrderListResponse = {
  success: boolean;

  data: WorkOrderItem[];

  meta: WorkOrderListMeta;

  requestId: string;
};

// ======================================================
// DETAIL
// ======================================================

export type WorkOrderDetail = {
  id: number;

  code: string;

  budgetPlanId: number;
  budgetPlanCode: string;

  activityTypeCode: string;

  itemShadowId: number;

  activityName: string;

  warehouseShadowId: number;

  warehouseCode: string;
  warehouseName: string;

  templateCode: string;

  vendorName: string;

  codeBlock: string | null;

  picUserId: number;

  picName: string;

  startDate: string;
  endDate: string;

  isRfba: boolean;

  status: string;

  notes: string | null;

  gpsLocation: string | null;

  productName: string;

  quantity: number | null;

  uomCode: string;

  blNumber: string;

  vesselName: string;

  transportOrders: unknown;

  unloadingItems: unknown[];

  loadingItems: unknown;

  fumigation: unknown;

  storage: unknown;

  qc: unknown;

  heavyEquipment: unknown;

  unbagging: unknown;

  rebagging: unknown;

  createdAt: string;

  createdByName: string;

  submittedAt: string | null;

  submittedByName: string | null;
};

export type WorkOrderDetailResponse = {
  success: boolean;

  data: WorkOrderDetail;

  message: string;

  requestId: string;
};

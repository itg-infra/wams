import axiosProvider from "../../../providers/axiosProvider"; 

export type PurchaseOrderPayload = {
  vendorShadowId: number;
  remark: string;
  docDate: string;
  items: number[];
};

export type PurchaseOrderMutationResponse = {
  success: boolean;
  data?: unknown;
  message: string;
  requestId: string;
};

export type LinkedPurchaseOrder = {
  id: number;
  code: string;
};

export type LinkedBudgetPlan = {
  id: number;
  code: string;
  purchaseOrders: LinkedPurchaseOrder[];
};

export type PurchaseOrderDetailItem = {
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
};

export type PurchaseOrderDetail = {
  id: number;
  code: string;

  vendorShadowId: number;
  vendorCode: string;
  vendorName: string;

  status: string;

  docDate: string;
  remark: string;

  sapPoNumber: string | null;

  linkedBudgetPlans: LinkedBudgetPlan[];

  items: PurchaseOrderDetailItem[];

  grandTotal: number;

  createdAt: string;
  createdByName: string;

  generatedAt: string | null;
  generatedByName: string | null;
};

export type PurchaseOrderDetailResponse = {
  success: boolean;
  data: PurchaseOrderDetail;
  message: string;
  requestId: string;
};

export const detailPoService = {

  getPurchaseOrderDetail: async (
    id: number,
  ): Promise<PurchaseOrderDetailResponse> => {
    const response = await axiosProvider.get<PurchaseOrderDetailResponse>(
      `api/v1/purchase-orders/${id}`,
    );

    return response.data;
  },

  updatePurchaseOrder: async (
    id: number,
    payload: PurchaseOrderPayload,
  ): Promise<PurchaseOrderMutationResponse> => {
    const response = await axiosProvider.put<PurchaseOrderMutationResponse>(
      `api/v1/purchase-orders/${id}`,
      payload,
    );

    return response.data;
  },

  generatePurchaseOrder: async (
    id: number,
  ): Promise<PurchaseOrderMutationResponse> => {
    const response = await axiosProvider.post<PurchaseOrderMutationResponse>(
      `api/v1/purchase-orders/${id}/generate`,
    );

    return response.data;
  },
};

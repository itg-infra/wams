import axiosProvider from '../../../providers/axiosProvider'; // sesuaikan path
import type { ApprovedBudgetPlanResponse } from '../../../../types/listGeneratePo.type';

const BASE_URL = 'api/v1/purchase-orders/approved-budget-plans';

type LinkedPurchaseOrder = {
  id: number;
  code: string;
};

type LinkedBudgetPlan = {
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

export const approvedBudgetPlanService = {
  /**
   * Fetch all approved budget plans
   */
  getApprovedBudgetPlans: async (): Promise<ApprovedBudgetPlanResponse> => {
    const response = await axiosProvider.get<ApprovedBudgetPlanResponse>(
      BASE_URL,
      {
        withWarehouseId: true,
      },
    );
    return response.data;
  },

  getPurchaseOrderDetail: async (id: number): Promise<PurchaseOrderDetail> => {
    const response = await axiosProvider.get<PurchaseOrderDetailResponse>(
      `api/v1/purchase-orders/${id}`,
    );

    return response.data.data;
  },
};
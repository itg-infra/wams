export interface AccountPayableItem {
  recapWorkOrderId: number;

  budgetPlanId: number;

  budgetPlanCode: string;

  remark: string;

  docDate: string;

  createdAt: string;

  hasRfbaItems: boolean;

  vendorShadowId: number;

  vendorCode: string;

  vendorName: string;

  budgetPlanTotal: number;

  accountPayableId: number | null;

  accountPayableCode: string | null;

  accountPayableStatus: string | null;

  accountPayables: AccountPayables[];

  isAllGenerate: boolean;

  location: string;

  budgetApproved: number;

  budgetVariance: number;
}

export interface AccountPayables {
  id: number;
  code: string;
  status: string;
  sapApNumber: string;
  vendorCode: string;
}

export interface AccountPayableListResponse {
  success: boolean;

  data: AccountPayableItem[];

  message: string;

  requestId: string;
}

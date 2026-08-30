export interface AccountPayableItemDetail {
  id: number;

  budgetPlanItemId: number;

  budgetPlanId: number;

  vendorShadowId: number;

  vendorCode: string;

  vendorName: string;

  itemCode: string;

  itemName: string;

  coaCode: string;

  coaName: string;

  uomCode: string;

  uomName: string;

  isRfba: boolean;

  billOfLading: string | null;

  unitCost: number;

  unitCount: number;

  budgetPlanTotal: number;

  budgetRealization: number;

  budgetVariance: number;

  sortOrder: number;

  ppnTaxTypeCode?: string | null;
  ppnRate?: number;
  pphTaxTypeCode?: string | null;
  pphRate?: number;
  grandTotal?: number;
  costTreatment?: "Dibiayakan" | "TidakDibiayakan" | null;
}

export interface AccountPayableDetail {
  id: number;

  code: string;

  vendorShadowId: number;

  vendorCode: string;

  vendorName: string;

  status: string;

  docDate: string;

  remark: string;

  sapApNumber: string;

  linkedBudgetPlanCodes: string[];

  linkedBudgetPlans?: {
    id: number;
    code: string;
  }[];

  items: AccountPayableItemDetail[];

  grandTotal: number;

  createdAt: string;

  createdByName: string;

  generatedAt: string;

  generatedByName: string;
}

export interface AccountPayableDetailResponse {
  success: boolean;

  data: AccountPayableDetail;

  message: string;

  requestId: string;
}

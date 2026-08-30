export type BudgetTemplateStatus = "Submitted" | "Draft" | "Approved" | "Rejected";
export interface BudgetTemplateItem {
  id: string;
  templateId: string;
  templateName: string;
  location: string;
  provinceId: string;
  provinceName: string;
  provinceDisplay: string;
  date: string;
  status: BudgetTemplateStatus;
}

export interface BudgetTemplateQueryParams {
  search?: string;
  sortBy?: string | null;
  sortOrder?: "asc" | "desc";
  page?: number;
  limit?: number;
}

export interface BudgetTemplateResponse {
    data: BudgetTemplateItem[];
    meta: {
        page: number;
        limit: number;
        total: number;
        totalPages: number;
        from: number;
        to: number;
        lastUpdated: string;
    };
}

export interface BudgetTemplateApiItem {
    id: number;
    templateCode: string;
    activityTypeName: string;
    warehouseCode: string;
    warehouseName: string;
    location: string;
    provinceId: string;
    provinceName: string;
    provinceDisplay: string;
    date: string;
    status: BudgetTemplateStatus;
}

export interface BudgetTemplateApiResponse {
    success: boolean;
    data: BudgetTemplateApiItem[];
    meta: {
        page: number;
        limit: number;
        total: number;
        totalPages: number;
    };
    requestId: string | null;
}

export interface BudgetTemplateDetailCostItem {
  id: string;
  itemShadowId: number;
  activityTypeCode: string;
  activityTypeName: string;
  activityTypeId: number;
  costDetail: string;
  costName: string;
  coa: string;
  coaName: string;
  sortOrder: number;
}

export interface BudgetTemplateDetailItem {
  id: string;
  templateId: string;

  // templateName: string;
  location: string;
  provinceId: string;
  provinceName: string;
  provinceDisplay: string;
  templateNumericId: number;
  status: BudgetTemplateStatus;
  items: BudgetTemplateDetailCostItem[];
  createdAt: string;
  submittedAt: string | null;
  approvedAt: string | null;
}

export interface BudgetTemplateDetailApiResponse {
  success: boolean;
  data: {
    id: number;
    templateCode: string;
    // activityType: {
    //   id: number;
    //   code: string;
    //   name: string;
    //   isActive: boolean;
    // };
    location: string;
    provinceId: string;
    provinceName: string;
    provinceDisplay: string;
    status: BudgetTemplateStatus;
    items: Array<{
      id: number;
      itemShadowId: number;
      costDetail: string;
      costName: string;
      coa: string;
      coaName: string;
      sortOrder: number;
      activityTypeName: string
      activityTypeCode: string
      activityTypeId: number;
    }>;
    createdAt: string;
    submittedAt: string | null;
    approvedAt: string | null;
  };
  message: string;
  requestId: string | null;
}
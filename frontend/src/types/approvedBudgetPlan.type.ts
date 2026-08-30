export type ApprovedBudgetPlanSortValue =
    | "latest"
    | "oldest"
    | "name_asc"
    | "name_desc";

export interface ApprovedBudgetPlanItem {
    id: string;
    templateId: string;
    templateName: string;
    warehouseCode: string;
    warehouseName: string;
    location: string;
    date: string;
}

export interface ApprovedBudgetPlanQuery {
    search?: string;
    sort?: ApprovedBudgetPlanSortValue;
    page?: number;
    limit?: number;
}

export interface ApprovedBudgetPlanResponse {
    data: ApprovedBudgetPlanItem[];
    total: number;
    page: number;
    limit: number;
    totalPages: number;
    lastUpdated: string;
}
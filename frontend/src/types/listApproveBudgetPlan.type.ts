/* ================= ACTIVITY TYPE ================= */

export interface ApprovedPlanActivity {
    itemShadowId: number;
    itemCode: string;
    activityName: string;
    workOrderId: number | null;
    workOrderCode: string | null;
    workOrderStatus: "Draft" | "Submitted" | "Approved" | null;
}

/* ================= API RESPONSE TYPES ================= */

export interface ApprovedBudgetPlanApiItem {
    budgetPlanId: number;
    budgetPlanCode: string;
    templateCode: string;
    activityTypeCode: string;
    activityTypeName: string;
    warehouseShadowId: number;
    warehouseCode: string;
    warehouseName: string;
    remark: string;
    isRfba: boolean;
    docDate: string;
    makerName: string;
    vendorName: string;
    activities: ApprovedPlanActivity[];
}

export interface ApprovedBudgetPlansApiResponse {
    success: boolean;
    data: ApprovedBudgetPlanApiItem[];
    message: string;
    requestId: string;
}

/* ================= MAPPED RESPONSE TYPES ================= */

export interface ApprovedBudgetPlanItem {
    budgetPlanId: number;
    budgetPlanCode: string;
    templateCode: string;
    activityTypeCode: string;
    activityTypeName: string;
    warehouseShadowId: number;
    warehouseCode: string;
    warehouseName: string;
    remark: string;
    isRfba: boolean;
    docDate: string; // formatted
    makerName: string;
    vendorName: string;
    activities: ApprovedPlanActivity[];
}

export interface ApprovedBudgetPlansResponse {
    data: ApprovedBudgetPlanItem[];
    meta: {
        total: number;
        lastUpdated: string;
    };
}

/* ================= SORT & QUERY TYPES ================= */

export type ApprovedBudgetPlanSortValue = "latest" | "oldest" | "name_asc" | "name_desc";

export interface ApprovedBudgetPlansQueryParams {
    search?: string;
    sortBy?: ApprovedBudgetPlanSortValue;
    page?: number;
    limit?: number;
}
import axiosProvider from "../../../providers/axiosProvider";
import type {
    BudgetPlanDetail,
    BudgetPlanDetailApiResponse,
    RejectBudgetPlanPayload,
} from "../../../../types/budgetPlanDetial.type";

function formatDocDate(dateString: string): string {
    const date = new Date(dateString);
    if (Number.isNaN(date.getTime())) return "-";
    return new Intl.DateTimeFormat("id-ID", {
        day: "2-digit",
        month: "long",
        year: "numeric",
    }).format(date);
}

function formatCurrency(value: number): string {
    return new Intl.NumberFormat("id-ID", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    }).format(value);
}

function mapBudgetPlanDetail(data: BudgetPlanDetailApiResponse["data"]): BudgetPlanDetail {
    return {
        id: String(data.id),
        budgetNo: data.budgetNo,
        templateId: data.template.templateCode,
        templateName: data.template.activityTypeName,
        warehouseCode: data.warehouseCode,
        warehouseName: data.warehouseName,
        location: data.template.location,
        remark: data.remark,
        docDate: formatDocDate(data.docDate),
        type: data.type,
        isRfba: data.isRfba,
        paymentType: data.paymentType,
        status: data.status,
         spkItems: data.spkItems,
        items: data.items,
        grandTotal: data.grandTotal,
        grandTotalFormatted: formatCurrency(data.grandTotal),
        createdAt: data.createdAt,
        createdByName: data.createdByName,
        submittedAt: data.submittedAt,
        submittedByName: data.submittedByName,
        approval: data.approval,
        rejectedAt: data.rejectedAt,
        rejectedByName: data.rejectedByName,
        rejectionReason: data.rejectionReason,
    };
}

export const budgetPlanDetailService = {
    async getBudgetPlanDetail(id: string): Promise<BudgetPlanDetail> {
        const response = await axiosProvider.get<BudgetPlanDetailApiResponse>(
            `api/v1/budget-plans/${id}`
        );
        return mapBudgetPlanDetail(response.data.data);
    },

    async approveBudgetPlan(id: string): Promise<void> {
        await axiosProvider.post(`api/v1/budget-plans/${id}/approve`);
    },

    async rejectBudgetPlan(id: string, payload: RejectBudgetPlanPayload): Promise<void> {
        await axiosProvider.post(`api/v1/budget-plans/${id}/reject`, payload);
    },
};
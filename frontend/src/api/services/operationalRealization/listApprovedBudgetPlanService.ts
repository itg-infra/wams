import axiosProvider from "../../providers/axiosProvider";
import type {
    ApprovedBudgetPlansApiResponse,
    ApprovedBudgetPlanItem,
    ApprovedBudgetPlansResponse,
    ApprovedBudgetPlansQueryParams,
} from "../../../types/listApproveBudgetPlan.type";

/* ================= FORMAT HELPERS ================= */

function formatDocDate(dateString: string): string {
    const date = new Date(dateString);
    if (Number.isNaN(date.getTime())) return "-";
    return new Intl.DateTimeFormat("id-ID", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
    }).format(date);
}

function formatLastUpdated(dateString: string): string {
    const date = new Date(dateString);
    if (Number.isNaN(date.getTime())) return "-";

    const formattedDate = new Intl.DateTimeFormat("id-ID", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
    }).format(date);

    const formattedTime = new Intl.DateTimeFormat("id-ID", {
        hour: "2-digit",
        minute: "2-digit",
        hour12: false,
        timeZone: "Asia/Jakarta",
    }).format(date);

    return `${formattedDate}, ${formattedTime} WIB`;
}

/* ================= MAPPING ================= */

function mapApprovedBudgetPlanItem(item: ApprovedBudgetPlansApiResponse["data"][0]): ApprovedBudgetPlanItem {
    return {
        budgetPlanId: item.budgetPlanId,
        budgetPlanCode: item.budgetPlanCode,
        templateCode: item.templateCode,
        activityTypeCode: item.activityTypeCode,
        activityTypeName: item.activityTypeName,
        warehouseShadowId: item.warehouseShadowId,
        warehouseCode: item.warehouseCode,
        warehouseName: item.warehouseName,
        remark: item.remark,
        isRfba: item.isRfba,
        docDate: formatDocDate(item.docDate),
        makerName: item.makerName,
        vendorName: item.vendorName,
        activities: item.activities,
    };
}

function sortPlans(
    data: ApprovedBudgetPlanItem[],
    sortBy: ApprovedBudgetPlansQueryParams["sortBy"]
): ApprovedBudgetPlanItem[] {
    const cloned = [...data];

    switch (sortBy) {
        case "name_asc":
            return cloned.sort((a, b) => a.budgetPlanCode.localeCompare(b.budgetPlanCode));
        case "name_desc":
            return cloned.sort((a, b) => b.budgetPlanCode.localeCompare(a.budgetPlanCode));
        case "oldest":
            return cloned.reverse();
        case "latest":
        default:
            return cloned;
    }
}

export const approvedBudgetPlanService = {
    async getApprovedBudgetPlans(
        params: ApprovedBudgetPlansQueryParams = {}
    ): Promise<ApprovedBudgetPlansResponse> {
        const {
            search = "",
            sortBy = "latest",
            page = 1,
            limit = 20,
        } = params;

        const response = await axiosProvider.get<ApprovedBudgetPlansApiResponse>(
            "api/v1/work-orders/approved-plans",
            {
                params: { page, limit },
                withWarehouseId: true,
            }
        );

        const raw = response.data;
        const mappedData = raw.data.map(mapApprovedBudgetPlanItem);

        let filteredData = [...mappedData];

        // Filter by search
        if (search.trim()) {
            const keyword = search.toLowerCase();
            filteredData = filteredData.filter((item) =>
                [
                    item.budgetPlanCode,
                    item.templateCode,
                    item.activityTypeCode,
                    item.activityTypeName,
                    item.warehouseCode,
                    item.warehouseName,
                    item.remark,
                    item.vendorName,
                    item.makerName,
                ]
                    .join(" ")
                    .toLowerCase()
                    .includes(keyword)
            );
        }

        // Sort
        filteredData = sortPlans(filteredData, sortBy);

        // Pagination
        const total = filteredData.length;
        const totalPages = Math.max(1, Math.ceil(total / limit));
        const safePage = Math.min(page, totalPages);
        const startIndex = (safePage - 1) * limit;
        const endIndex = startIndex + limit;
        const paginated = filteredData.slice(startIndex, endIndex);

        const latestDate =
            raw.data.length > 0
                ? raw.data
                    .map((item) => item.docDate)
                    .sort((a, b) => new Date(b).getTime() - new Date(a).getTime())[0]
                : "";

        return {
            data: paginated,
            meta: {
                total,
                lastUpdated: latestDate ? formatLastUpdated(latestDate) : "-",
            },
        };
    },
};
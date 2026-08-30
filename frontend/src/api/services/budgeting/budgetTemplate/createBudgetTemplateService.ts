import axiosProvider from "../../../providers/axiosProvider";
import type {
  BudgetTemplateApiItem,
  BudgetTemplateApiResponse,
  BudgetTemplateDetailApiResponse,
  BudgetTemplateDetailItem,
  BudgetTemplateItem,
  BudgetTemplateQueryParams,
  BudgetTemplateResponse,
} from "../../../../types/budgetTemplate.type";

export interface CreateBudgetTemplateItemPayload {
  itemShadowId: number;
  activityTypeId: number;
}

export interface CreateBudgetTemplatePayload {
//   activityTypeId: number | null;
//   warehouseShadowId: number | null;
  items: CreateBudgetTemplateItemPayload[];
}

function formatDate(dateString: string): string {
    const date = new Date(dateString);

    if (Number.isNaN(date.getTime())) {
        return "-";
    }

    return new Intl.DateTimeFormat("id-ID", {
        day: "2-digit",
        month: "2-digit",
        year: "2-digit",
    }).format(date);
}

function sortTemplates(
    data: BudgetTemplateItem[],
    sortBy: BudgetTemplateQueryParams["sortBy"]
): BudgetTemplateItem[] {
    const cloned = [...data];

    switch (sortBy) {
        case "name_asc":
            return cloned.sort((a, b) => a.templateName.localeCompare(b.templateName));
        case "name_desc":
            return cloned.sort((a, b) => b.templateName.localeCompare(a.templateName));
        case "oldest":
            return cloned.reverse();
        case "latest":
        default:
            return cloned;
    }
}

function formatLastUpdated(dateString: string): string {
    const date = new Date(dateString);

    if (Number.isNaN(date.getTime())) {
        return "-";
    }

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

function mapBudgetTemplateDetail(raw: BudgetTemplateDetailApiResponse["data"]): BudgetTemplateDetailItem {
    return {
        id: String(raw.id),
        templateId: raw.templateCode,
        // templateName: raw.activityType.name,
        templateNumericId: raw.id,
        location: raw.location,
        provinceName: raw.provinceName,
        provinceDisplay: raw.provinceDisplay,
        status: raw.status,
        createdAt: raw.createdAt,
        provinceId: raw.provinceId,
        submittedAt: raw.submittedAt,
        approvedAt: raw.approvedAt,
        items: raw.items
            .sort((a, b) => a.sortOrder - b.sortOrder)
            .map((item) => ({
                id: String(item.id),
                activityTypeId: item.activityTypeId,
                activityTypeName: item.activityTypeName,
                activityTypeCode: item.activityTypeCode,
                itemShadowId: item.itemShadowId,
                costDetail: item.costDetail,
                costName: item.costName,
                coa: item.coa,
                coaName: item.coaName,
                sortOrder: item.sortOrder,
            })),
    };
}

function mapBudgetTemplateItem(item: BudgetTemplateApiItem): BudgetTemplateItem {
    return {
        id: String(item.id),
        templateId: item.templateCode,
        templateName: item.activityTypeName,
        location: item.location,
        provinceId: item.provinceId,
        provinceName: item.provinceName,
        provinceDisplay: item.provinceDisplay,
        date: formatDate(item.date),
        status: item.status,
    };
}

export const createBudgetTemplateService = {
    async getBudgetTemplates(
        params: BudgetTemplateQueryParams = {}
    ): Promise<BudgetTemplateResponse> {
        const {
            search = "",
            sortBy = "latest",
            page = 1,
            limit = 20,
        } = params;

        const response = await axiosProvider.get<BudgetTemplateApiResponse>(
            "api/v1/budget-templates",
            {
                params: {
                    page,
                    limit,
                },
            }
        );

        const raw = response.data;

        const mappedData = raw.data.map(mapBudgetTemplateItem);

        let filteredData = [...mappedData];

        if (search.trim()) {
            const keyword = search.toLowerCase();

            filteredData = filteredData.filter((item) =>
                [
                    item.templateId,
                    item.templateName,
                    item.location,
                    item.status,
                ]
                    .join(" ")
                    .toLowerCase()
                    .includes(keyword)
            );
        }

        filteredData = sortTemplates(filteredData, sortBy);

        const total = filteredData.length;
        const totalPages = Math.max(1, Math.ceil(total / limit));
        const safePage = Math.min(page, totalPages);
        const startIndex = (safePage - 1) * limit;
        const endIndex = startIndex + limit;
        const paginated = filteredData.slice(startIndex, endIndex);

        const latestDate =
            raw.data.length > 0
                ? raw.data
                    .map((item) => item.date)
                    .sort((a, b) => new Date(b).getTime() - new Date(a).getTime())[0]
                : "";

        return {
            data: paginated,
            meta: {
                page: safePage,
                limit,
                total,
                totalPages,
                from: total === 0 ? 0 : startIndex + 1,
                to: total === 0 ? 0 : Math.min(endIndex, total),
                lastUpdated: latestDate ? formatLastUpdated(latestDate) : "-",
            },
        };
    },
    async createSubmit(payload: CreateBudgetTemplatePayload) {
        const response = await axiosProvider.post(
            "api/v1/budget-templates/submit",
            payload
        );
        return response.data;
    },

    async createDraft(payload: CreateBudgetTemplatePayload) {
        const response = await axiosProvider.post(
            "api/v1/budget-templates",
            payload
        );
        return response.data;
    },

    async update(id: string, payload: CreateBudgetTemplatePayload) {
        const response = await axiosProvider.put(`api/v1/budget-templates/${id}`, payload);
        return response.data;
    },

    async getById(id: string): Promise<BudgetTemplateDetailItem> {
        const response = await axiosProvider.get<BudgetTemplateDetailApiResponse>(
            `api/v1/budget-templates/${id}`
        );

        return mapBudgetTemplateDetail(response.data.data);
    },

    async deleteById(id: string) {
        const response = await axiosProvider.delete(`api/v1/budget-templates/${id}`)

        return response.data;
    },

    async submitById(id: string) {
        const response = await axiosProvider.post(`api/v1/budget-templates/${id}/submit`);
        return response.data;
    },
};
import type {
  BudgetTemplateApiItem,
  BudgetTemplateApiResponse,
  BudgetTemplateDetailApiResponse,
  BudgetTemplateDetailItem,
  BudgetTemplateItem,
  BudgetTemplateQueryParams,
  BudgetTemplateResponse,
} from "../../../../types/budgetTemplate.type";
import axiosProvider from "../../../providers/axiosProvider";

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

function mapBudgetTemplateItem(
  item: BudgetTemplateApiItem,
): BudgetTemplateItem {
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

function mapBudgetTemplateDetail(
  raw: BudgetTemplateDetailApiResponse["data"],
): BudgetTemplateDetailItem {
  return {
    id: String(raw.id),
    templateId: raw.templateCode,
    // templateName: raw.activityType.name,
    templateNumericId: raw.id,
    location: raw.location,
    provinceId: raw.provinceId,
    provinceName: raw.provinceName,
    provinceDisplay: raw.provinceDisplay,
    status: raw.status,
    createdAt: raw.createdAt,
    submittedAt: raw.submittedAt,
    approvedAt: raw.approvedAt,
    items: raw.items
      .sort((a, b) => a.sortOrder - b.sortOrder)
      .map((item) => ({
        id: String(item.id),
        itemShadowId: item.itemShadowId,
        costDetail: item.costDetail,
        costName: item.costName,
        coa: item.coa,
        coaName: item.coaName,
        sortOrder: item.sortOrder,
        activityTypeName: item.activityTypeName,
        activityTypeCode: item.activityTypeCode,
        activityTypeId: item.activityTypeId,
      })),
  };
}

export const budgetTemplateService = {
  async getBudgetTemplates(
    params: BudgetTemplateQueryParams = {},
  ): Promise<BudgetTemplateResponse> {
    const {
      search = "",
      sortBy = "date",
      sortOrder = "desc",
      page = 1,
      limit = 20,
    } = params;

    // Opsional: Jika value dari dropdown ("templateName") berbeda dengan standar backend ("name")
    const apiSortBy = sortBy === "templateName" ? "name" : sortBy;

    const response = await axiosProvider.get<BudgetTemplateApiResponse>(
      "api/v1/budget-templates",
      {
        params: {
          page,
          limit,
          search,
          sortBy: apiSortBy,
          sortOrder, // Langsung diteruskan
        },
        withWarehouseId: true,
      },
    );

    const raw = response.data;
    const mappedData = raw.data.map(mapBudgetTemplateItem);
    const { page: apiPage, limit: apiLimit, total, totalPages } = raw.meta;

    const from = total === 0 ? 0 : (apiPage - 1) * apiLimit + 1;
    const to = total === 0 ? 0 : Math.min(apiPage * apiLimit, total);

    const latestDate =
      mappedData.length > 0
        ? mappedData
            .map((item) => item.date)
            .sort((a, b) => new Date(b).getTime() - new Date(a).getTime())[0]
        : "";

    return {
      data: mappedData,
      meta: {
        page: apiPage,
        limit: apiLimit,
        total,
        totalPages,
        from,
        to,
        lastUpdated: latestDate ? formatLastUpdated(latestDate) : "-",
      },
    };
  },

  async getBudgetTemplateDetail(id: string): Promise<BudgetTemplateDetailItem> {
    const response = await axiosProvider.get<BudgetTemplateDetailApiResponse>(
      `api/v1/budget-templates/${id}`,
    );
    return mapBudgetTemplateDetail(response.data.data);
  },

  async budgetTemplateApproved(id?: string) {
    const { data } = await axiosProvider.post(
      `api/v1/budget-templates/${id}/approve`,
    );
    return data;
  },
};

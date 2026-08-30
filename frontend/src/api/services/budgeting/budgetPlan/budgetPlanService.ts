import type {
  BudgetPlanApiItem,
  BudgetPlanApiResponse,
  BudgetPlanItem,
  BudgetPlanQueryParams,
  BudgetPlanResponse,
} from "../../../../types/budgetPlan.type";
import type {
  BudgetPlanDetailApiResponse,
  BudgetPlanDetailApiData,
  BudgetPlanDetailItem,
  BudgetPlanDetailSpkItem,
} from "../../../../types/budgetPlanDetial.type";
import axiosProvider from "../../../providers/axiosProvider";

/* ================= PAYLOAD TYPES ================= */

// Shape yang dipakai FormBudgetPlan untuk render field-field atas
export interface BudgetPlanFormDetail {
  id: string;
  budgetNo: string;
  templateId: string;
  templateName: string;
  warehouseCode: string;
  templateNumericId: number;
  provinceId?: string;
  warehouseName: string;
  location: string;
  status: string;
  remark: string;
  docDate: string;
  // spkItems dari response — dipakai untuk rebuild baseRows
  spkItems: BudgetPlanDetailSpkItem[];
  // items dari response — dipakai untuk rebuild cost rows
  items: BudgetPlanDetailItem[];
}

export interface CreateBudgetPlanPayload {
  budgetTemplateId: number;
  remark: string;
  docDate: string;
  warehouseShadowId: number;
  spkShadowIds: number[];
  items: {
    itemShadowId: number;
    vendorShadowId: number;
    quantity: number;
    type: string;
    isRfba: boolean;
    paymentType: string;
    docExternal: string;
    billOfLading: string;
    description: string | null;
  }[];
}

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

function mapBudgetPlanItem(item: BudgetPlanApiItem): BudgetPlanItem {
  return {
    id: String(item.id),
    budgetNo: item.budgetNo,
    templateCode: item.templateCode,
    remark: item.remark,
    location: item.location,
    vendorName: item.vendorName,
    makerName: item.makerName,
    docDate: formatDocDate(item.docDate),
    status: item.status,
    type: item.type,
    isRfba: item.isRfba,
    paymentType: item.paymentType,
  };
}

// Map raw API response → BudgetPlanFormDetail
function mapDetailToFormDetail(
  raw: BudgetPlanDetailApiData,
): BudgetPlanFormDetail {
  return {
    id: String(raw.id),
    budgetNo: raw.budgetNo,
    templateNumericId: raw.id,
    templateId: String(raw.template.id),
    templateName: raw.template.activityTypeName,
    warehouseCode: raw.template.warehouseCode,
    warehouseName: raw.template.warehouseName,
    location: raw.template.location,
    status: raw.status,
    remark: raw.remark ?? "",
    docDate: raw.docDate,
    spkItems: raw.spkItems ?? [], // ← dari response
    items: raw.items ?? [], // ← dari response
  };
}

/* ================= SERVICE ================= */

export const budgetPlanService = {
  /* ── LIST ─────────────────────────────────────────────────────────── */

  async getBudgetPlans(
    params: BudgetPlanQueryParams = {},
  ): Promise<BudgetPlanResponse> {
    const {
      search = "",
      sortBy = "docDate", // Menjadi default field jika tidak ada yang dikirim
      sortOrder = "desc", // Default sorting secara descending
      page = 1,
      limit = 20,
      status = "",
      type = "",
    } = params;

    const response = await axiosProvider.get<BudgetPlanApiResponse>(
      "api/v1/budget-plans",
      {
        params: {
          page,
          limit,
          search,
          sortBy,
          sortOrder,
          status,
          type,
        },
        withWarehouseId: true,
      },
    );

    const raw = response.data;
    const mappedData = raw.data.map(mapBudgetPlanItem);

    // Backend sudah handle search/filter/sort/pagination.
    // Jangan hitung ulang total/totalPages dari mappedData (itu cuma 1 halaman).
    const { page: apiPage, limit: apiLimit, total, totalPages } = raw.meta;

    const from = total === 0 ? 0 : (apiPage - 1) * apiLimit + 1;
    const to = total === 0 ? 0 : Math.min(apiPage * apiLimit, total);

    // Catatan sama seperti budget-template: lastUpdated di sini cuma
    // representatif untuk data di halaman aktif, bukan seluruh dataset,
    // karena backend tidak expose field ini di meta.
    const latestDate =
      mappedData.length > 0
        ? mappedData
            .map((item) => item.docDate)
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

  /* ── DETAIL ───────────────────────────────────────────────────────── */

  async getBudgetPlanById(id: string | number): Promise<BudgetPlanFormDetail> {
    const response = await axiosProvider.get<BudgetPlanDetailApiResponse>(
      `api/v1/budget-plans/${id}`,
      { withWarehouseId: true },
    );
    return mapDetailToFormDetail(response.data.data);
  },

  /* ── CREATE ───────────────────────────────────────────────────────── */

  async submitBudgetPlan(payload: CreateBudgetPlanPayload) {
    const response = await axiosProvider.post(
      "api/v1/budget-plans/submit",
      payload,
      { withWarehouseId: true },
    );
    return response.data;
  },

  async draftBudgetPlan(payload: CreateBudgetPlanPayload) {
    const response = await axiosProvider.post("api/v1/budget-plans", payload, {
      withWarehouseId: true,
    });
    return response.data;
  },

  /* ── UPDATE ───────────────────────────────────────────────────────── */

  async updateBudgetPlan(
    id: string | number,
    payload: CreateBudgetPlanPayload,
  ) {
    const response = await axiosProvider.put(
      `api/v1/budget-plans/${id}`,
      payload,
      { withWarehouseId: true },
    );
    return response.data;
  },

  async submitUpdateBudgetPlan(
    id: string | number,
    payload: CreateBudgetPlanPayload,
  ) {
    const response = await axiosProvider.put(
      `api/v1/budget-plans/${id}`,
      payload,
      { withWarehouseId: true },
    );
    return response.data;
  },

  async submitFromDraftBudgetPlan(id: string | number) {
    const response = await axiosProvider.post(
      `api/v1/budget-plans/${id}/submit`,
      {},
      { withWarehouseId: true },
    );
    return response.data;
  },

  async submitAndUpdateBudgetPlan(
    id: string | number,
    payload: CreateBudgetPlanPayload,
  ) {
    // Step 1: update data
    await axiosProvider.put(`api/v1/budget-plans/${id}`, payload, {
      withWarehouseId: true,
    });
    // Step 2: ubah status ke submitted
    const response = await axiosProvider.post(
      `api/v1/budget-plans/${id}/submit`,
      {},
      { withWarehouseId: true },
    );
    return response.data;
  },

  async deleteBudgetPlan(id: string | number) {
    const response = await axiosProvider.delete(`api/v1/budget-plans/${id}`, {
      withWarehouseId: true,
    });
    return response.data;
  },
};

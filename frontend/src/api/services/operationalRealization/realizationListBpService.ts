import type { costTreament } from "../../../screens/formMasterPriceList";
import type {
  AvailableBudgetPlanDetailItem,
} from "../../../types/budgetPlanDetial.type";
import type { pphTaxTypeItem, ppnTaxTypeItem } from "../../../types/budgetRealization.type";
import type {
  RealizationApprovedBpItem,
  RealizationApprovedBpApiResponse,
  RealizationApprovedBpResponse,
  RealizationApprovedBudgetPlansQueryParams,
} from "../../../types/realizationApprovedBp.type";
import axiosProvider from "../../providers/axiosProvider";

export type { AvailableBudgetPlanDetailItem } from "../../../types/budgetPlanDetial.type";

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

function mapRealizationApprovedBp(
  item: RealizationApprovedBpApiResponse["data"][0],
): RealizationApprovedBpItem {
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
    isLocked: item.isLocked,
    vendorName: item.vendorName,
    activities: item.activities,
    purchaseOrderId: item.purchaseOrderId,
    purchaseOrderCode: item.purchaseOrderCode,
    purchaseOrderStatus: item.purchaseOrderStatus,
    sapPoNumber: item.sapPoNumber,
  };
}

function sortPlans(
  data: RealizationApprovedBpItem[],
  sortBy: RealizationApprovedBudgetPlansQueryParams["sortBy"],
): RealizationApprovedBpItem[] {
  const cloned = [...data];

  switch (sortBy) {
    case "name_asc":
      return cloned.sort((a, b) =>
        a.budgetPlanCode.localeCompare(b.budgetPlanCode),
      );
    case "name_desc":
      return cloned.sort((a, b) =>
        b.budgetPlanCode.localeCompare(a.budgetPlanCode),
      );
    case "oldest":
      return cloned.reverse();
    case "latest":
    default:
      return cloned;
  }
}

type AvailableItem = {
  budgetPlanItemId: number;
  budgetPlanId: number;
  budgetPlanCode: string;
  budgetPlanRemark: string;
  budgetPlanDocDate: string;
  isSeedBudgetPlan: boolean;
  warehouseShadowId: number;
  warehouseCode: string;
  warehouseName: string;
  vendorShadowId: number;
  itemShadowId: number;
  itemCode: string;
  itemName: string;
  coaCode: string;
  coaName: string;
  vendorCode: string;
  vendorName: string;
  ppnTaxType: ppnTaxTypeItem;
  pphTaxType: pphTaxTypeItem;
  costTreatment: costTreament;
  isRfba: boolean;
  billOfLading: string;
  costValue: number;
  quantity: number;
  uomCode: string;
  uomName: string;
  isGenerated: boolean;
  takenByCode: string | null;
  availabilityStatus: string;
};

export type AvailableItemsApiResponse = {
  success: boolean;
  data: AvailableItem[];
  message: string;
  requestId: string;
};

function mapAvailableItem(item: AvailableItem): AvailableBudgetPlanDetailItem {
  return {
    id: item.budgetPlanItemId,
    budgetPlanId: item.budgetPlanId,
    budgetPlanCode: item.budgetPlanCode,
    budgetPlanItemId: String(item.budgetPlanItemId),
    budgetPlanDocDate: item.budgetPlanDocDate,
    isSeedBudgetPlan: item.isSeedBudgetPlan,
    warehouseShadowId: item.warehouseShadowId,
    warehouseCode: item.warehouseCode,
    warehouseName: item.warehouseName,
    isGenerated: item.isGenerated,
    takenByCode: item.takenByCode,
    availabilityStatus: item.availabilityStatus,
    itemShadowId: item.itemShadowId,
    itemCode: item.itemCode,
    activityTypeName: "",
    activityTypeId: null,
    costDetail: item.itemCode,
    costName: item.itemName,
    coa: item.coaCode,
    coaName: item.coaName,
    remark: item.budgetPlanRemark,
    vendorShadowId: item.vendorShadowId,
    vendorCode: item.vendorCode,
    vendorName: item.vendorName,
    pphTaxType: item.pphTaxType,
    ppnTaxType: item.ppnTaxType,
    costTreatment: item.costTreatment,
    uomMasterId: 0,
    uomCode: item.uomCode,
    uomName: item.uomName,
    costValue: item.costValue,
    quantity: item.quantity,
    totalValue: item.costValue * item.quantity,
    sortOrder: 0,

    isRfba: item.isRfba,
    paymentType: "Advance",
    type: "External",
    docExternal: "",
    billOfLading: item.billOfLading,
    kemasan: "",
    description: null,
  };
}

export type CreatePurchaseOrderPayload = {
  vendorShadowId: number;
  remark: string;
  docDate: string;
  items: number[];
};

type CreatePurchaseOrderResponse = {
  success: boolean;
  message: string;
  requestId: string;
};

export type WorkOrderItemPayload = {
  spkShadowId: number | null;
  blNumber: string;
  productName: string;
  quantity: number;
  uomCode: string;
  noVehicle: string;
  noContainer: string;
  noSeal: string;
  grossWeight: number;
  finalWeight: number;
  nettWeight: number;
  totalBag: number;
  unitWeight: number;
  isChecked: boolean;
  sortOrder: number;
};

export type CreateWorkOrderPayload = {
  budgetPlanId: number;
  picUserId: number;
  startDate: string;
  endDate: string;
  codeBlock: string;
  notes: string | null;

  unloadingItems?: WorkOrderItemPayload[];
  loadingItems?: WorkOrderItemPayload[];
};

export type CreateWorkOrderResponse = {
  success: boolean;
  message: string;
  data?: unknown;
};

export const realizationApprovedBpService = {
  async getRealizationApprovedBp(
    params: RealizationApprovedBudgetPlansQueryParams = {},
  ): Promise<RealizationApprovedBpResponse> {
    const { search = "", sortBy = "latest", page = 1, limit = 20 } = params;

    const response = await axiosProvider.get<RealizationApprovedBpApiResponse>(
      "api/v1/work-orders/approved-plans",
      {
        params: { page, limit },
        withWarehouseId: true,
      },
    );

    const raw = response.data;
    const mappedData = raw.data.map(mapRealizationApprovedBp);

    let filteredData = [...mappedData];

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
          .includes(keyword),
      );
    }
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

  async submitGeneratePO(
    payload: CreatePurchaseOrderPayload,
  ): Promise<CreatePurchaseOrderResponse> {
    const response = await axiosProvider.post<CreatePurchaseOrderResponse>(
      "api/v1/purchase-orders/generate",
      payload,
    );
    return response.data;
  },

  async draftGeneratePO(
    payload: CreatePurchaseOrderPayload,
  ): Promise<CreatePurchaseOrderResponse> {
    const response = await axiosProvider.post<CreatePurchaseOrderResponse>(
      "api/v1/purchase-orders",
      payload,
    );
    return response.data;
  },

  async createWorkOrder(
    payload: CreateWorkOrderPayload,
  ): Promise<CreateWorkOrderResponse> {
    const response = await axiosProvider.post<CreateWorkOrderResponse>(
      "api/v1/work-orders",
      payload,
      {
        withWarehouseId: true,
      },
    );

    return response.data;
  },

  async fetchAvailableItems(
    vendorShadowId: number,
    budgetPlanId: number,
    purchaseOrderId?: number,
  ): Promise<AvailableBudgetPlanDetailItem[]> {
    // Draft PO pakai ID di endpointnya. create PO baru pakai query vendor dan BP.
    const url = purchaseOrderId
      ? `api/v1/purchase-orders/${purchaseOrderId}/available-items`
      : "api/v1/purchase-orders/available-items";

    const response = await axiosProvider.get<AvailableItemsApiResponse>(
      url,
      {
        params: purchaseOrderId
          ? undefined
          : {
              vendorShadowId,
              budgetPlanId,
            },
      },
    );
    return (response.data.data ?? []).map(mapAvailableItem);
  },

  // async fetchAvailableItems(
  //   vendorShadowId: number,
  //   budgetPlanId: number,
  // ): Promise<AvailableItemsApiResponse> {
  //   const response = await axiosProvider.get<AvailableItemsApiResponse>(
  //     "api/v1/purchase-orders/available-items",
  //     {
  //       params: {
  //         vendorShadowId,
  //         budgetPlanIds: budgetPlanId,
  //       },
  //     },
  //   );
  //   return (response.data);
  // },
};

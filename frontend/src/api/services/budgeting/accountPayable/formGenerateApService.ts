import type { costTreament } from "../../../../screens/formMasterPriceList";
import type { BudgetPlanDetailItem } from "../../../../types/budgetPlanDetial.type";
import type { pphTaxTypeItem, ppnTaxTypeItem } from "../../../../types/budgetRealization.type";
import axiosProvider from "../../../providers/axiosProvider";

export type AvailableItem = {
  budgetPlanItemId: number;
  budgetPlanId: number;
  budgetPlanCode: string;
  budgetPlanRemark: string;
  itemShadowId: number;
  itemCode: string;
  itemName: string;
  coaCode: string;
  coaName: string;
  vendorCode: string;
  vendorName: string;
  pphTaxType: pphTaxTypeItem;
  ppnTaxType: ppnTaxTypeItem;
  costTreatment: costTreament;
  isRfba: boolean;
  billOfLading: string;
  costValue: number;
  quantity: number;
  uomCode: string;
  uomName: string;
};

type AvailableItemsApiResponse = {
  success: boolean;
  data: AvailableItem[];
  message: string;
  requestId: string;
};

// ── Mapper: AvailableItem → BudgetPlanDetailItem ─────────────────────────────
function mapAvailableItem(item: AvailableItem): BudgetPlanDetailItem {
  return {
    id: item.budgetPlanItemId,
    itemShadowId: item.itemShadowId,
    activityTypeName: "",
    budgetPlanItemId: String(item.budgetPlanItemId),
    activityTypeId: null,
    costDetail: item.itemCode,
    costName: item.itemName,
    coa: item.coaCode,
    coaName: item.coaName,
    vendorShadowId: 0,
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
    budgetPlanCode: item.budgetPlanCode,
    budgetPlanId: item.budgetPlanId,
    isGenerated: false,
    itemCode: item.itemCode,
    kemasan: "",
    remark: item.budgetPlanRemark,

    isRfba: item.isRfba,
    paymentType: "Advance",
    type: "External",
    docExternal: "",
    billOfLading: item.billOfLading,
    description: null,
  };
}

export type CreatePurchaseOrderPayload = {
  vendorShadowId: number;
  remark: string;
  docDate: string;
  items: number[];
};

export type UpdateAccountPayablePayload = {
  remark: string;
  docDate: string;
  items: number[];
};

type CreateAccountPayableResponse = {
  success: boolean;
  message: string;
  requestId: string;
};

type CreatePurchaseOrderResponse = CreateAccountPayableResponse;

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

export const generateApServices = {
  async submitGeneratePO(
    payload: CreatePurchaseOrderPayload,
  ): Promise<CreatePurchaseOrderResponse> {
    const response = await axiosProvider.post<CreatePurchaseOrderResponse>(
      "api/v1/account-payables/generate",
      payload,
    );
    return response.data;
  },

  async draftGeneratePO(
    payload: CreatePurchaseOrderPayload,
  ): Promise<CreatePurchaseOrderResponse> {
    const response = await axiosProvider.post<CreatePurchaseOrderResponse>(
      "api/v1/account-payables",
      payload,
    );
    return response.data;
  },

  async updateAccountPayable(
    id: number,
    payload: UpdateAccountPayablePayload,
  ): Promise<CreateAccountPayableResponse> {
    const response = await axiosProvider.put<CreateAccountPayableResponse>(
      `api/v1/account-payables/${id}`,
      payload,
    );
    return response.data;
  },

  async generateAccountPayable(
    id: number,
  ): Promise<CreateAccountPayableResponse> {
    const response = await axiosProvider.post<CreateAccountPayableResponse>(
      `api/v1/account-payables/${id}/generate`,
    );
    return response.data;
  },

  async createWorkOrder(
    payload: CreateWorkOrderPayload,
  ): Promise<CreateWorkOrderResponse> {
    const response = await axiosProvider.post<CreateWorkOrderResponse>(
      "api/v1/account-payables/generate",
      payload,
      {
        withWarehouseId: true,
      },
    );

    return response.data;
  },

  // async fetchAvailableItems(
  //   vendorShadowId: number,
  //   budgetPlanId: number,
  // ): Promise<BudgetPlanDetailItem[]> {
  //   const response = await axiosProvider.get<AvailableItemsApiResponse>(
  //     "api/v1/account-payables/available-items",
  //     {
  //       params: {
  //         vendorShadowId,
  //         budgetPlanIds: budgetPlanId,
  //       },
  //     },
  //   );
  //   return (response.data.data ?? []).map(mapAvailableItem);
  // },

  async fetchAvailableItems(
    vendorShadowId: number,
    budgetPlanId: number,
    accountPayableId?: number,
  ): Promise<BudgetPlanDetailItem[]> {
    const url = accountPayableId
      ? `api/v1/account-payables/${accountPayableId}/available-items`
      : "api/v1/account-payables/available-items";

    const response = await axiosProvider.get<AvailableItemsApiResponse>(
      url,
      {
        params: {
          vendorShadowId,
          budgetPlanIds: budgetPlanId,
        },
      },
    );
    return (response.data.data ?? []).map(mapAvailableItem);
  },
};

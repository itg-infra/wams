import type { PurchaseOrderDetailNonApdpResponse, RecapNonAPDPListParams, RecapNonAPDPListResponse } from "../../../types/recapNonApdp.type";
import axiosProvider from "../../providers/axiosProvider";

const BASE_URL = "/api/v1/purchase-orders/recap/non-apdp";

function cleanParams<T extends Record<string, unknown>>(params: T): Partial<T> {
  const result: Partial<T> = {};
  (Object.keys(params) as (keyof T)[]).forEach((key) => {
    const value = params[key];
    if (value !== undefined && value !== null && value !== "") {
      result[key] = value;
    }
  });
  return result;
}

export const RecapNonAPDPService = {
  getList: async (
    params: RecapNonAPDPListParams = {},
  ): Promise<RecapNonAPDPListResponse> => {
    const query = cleanParams({
      page: params.page ?? 1,
      limit: params.limit ?? 10,
    });

    const { data } = await axiosProvider.get<RecapNonAPDPListResponse>(
      BASE_URL,
      {
        params: query,
      },
    );
    return data;
  },

  getDetail: async (
    poId: number | string,
  ): Promise<PurchaseOrderDetailNonApdpResponse> => {
    const { data } =
      await axiosProvider.get<PurchaseOrderDetailNonApdpResponse>(
        `${BASE_URL}/${poId}`,
      );
    return data;
  },
};

export default RecapNonAPDPService;

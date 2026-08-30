import type { PurchaseOrderDetailApdpResponse, RecapAPDPListParams, RecapAPDPListResponse } from "../../../types/recapApdp.type";
import axiosProvider from "../../providers/axiosProvider";

const BASE_URL = "/api/v1/purchase-orders/recap/apdp";

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

export const RecapAPDPService = {
    getList: async(
        params: RecapAPDPListParams = {},
    ): Promise<RecapAPDPListResponse> => {
        const query = cleanParams({
            page: params.page ?? 1,
            limit: params.limit ?? 10,
        });

        const {data}  = await axiosProvider.get<RecapAPDPListResponse>(
            BASE_URL,{
                params: query,
            }
        );
        return data;
    },

    getDetail: async(
        poId: number | string,
    ): Promise<PurchaseOrderDetailApdpResponse> =>{
        const { data } =
          await axiosProvider.get<PurchaseOrderDetailApdpResponse>(
            `${BASE_URL}/${poId}`,
          );
          return data;
    }
}

export default RecapAPDPService;

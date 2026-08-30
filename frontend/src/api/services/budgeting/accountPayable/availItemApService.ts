 
import type { AvailableItemsResponse } from "../../../../types/avaiItemAp.type";
import axiosProvider from "../../../providers/axiosProvider";

export const availableItemService = {
  getAvailableItems: async (vendorShadowId: number) => {
    const { data } = await axiosProvider.get<AvailableItemsResponse>(
      "/api/v1/account-payables/available-items",
      {
        params: { vendorShadowId },
      },
    );
    return data;
  },
};

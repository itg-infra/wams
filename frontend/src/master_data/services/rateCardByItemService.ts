// api/services/rateCardService.ts

import axiosProvider from "../../api/providers/axiosProvider";
import type { RateCardByItemResponse, RateCardByItemVendor } from "../types/rateCardByItem.type";

export const rateCardService = {
    async getVendorsByItem(itemShadowId: number): Promise<RateCardByItemVendor[]> {
        const response = await axiosProvider.get<RateCardByItemResponse>(
            `api/v1/rate-cards/by-item/${itemShadowId}`
        );
        return response.data.data;
    },
};
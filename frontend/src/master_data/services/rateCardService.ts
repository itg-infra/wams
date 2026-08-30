import axiosProvider from "../../api/providers/axiosProvider";
import type { RateCardListParams, RateCardListResponse, RateCardPayload, RateCardResponse } from "../types/rateCard.type";
import type { RateCardDetailApiResponse } from "../types/rateCardDetail.type";

export const rateCardService = {

    async listRateCard(params: RateCardListParams = {}): Promise<RateCardListResponse> {
        const response = await axiosProvider.get<RateCardListResponse>(
            "api/v1/rate-cards",
            {
                params: {
                    page: params.page ?? 1,
                    limit: params.limit ?? 20,
                    search: params.search ?? "",
                    is_active: params.is_active ?? "",
                    // Paginated endpoint: ordering must be resolved by the API.
                    ...(params.sortBy
                        ? { sortBy: params.sortBy, sortOrder: params.sortOrder ?? "asc" }
                        : {}),
                },
            }
        );

        return response.data;
    },

    async getDetail(id: number | string): Promise<RateCardDetailApiResponse> {
        const response = await axiosProvider.get<RateCardDetailApiResponse>(
            `api/v1/rate-cards/${id}`
        );
        return response.data;
    },

    async update(id: number | string, payload: RateCardPayload): Promise<RateCardResponse> {
        const response = await axiosProvider.put<RateCardResponse>(
            `api/v1/rate-cards/${id}`,
            payload
        );
        return response.data;
    },

    async delete(id: number | string): Promise<RateCardResponse> {
        const response = await axiosProvider.delete<RateCardResponse>(
            `api/v1/rate-cards/${id}`
        );
        return response.data;
    },

    async saveDraft(payload: RateCardPayload): Promise<RateCardResponse> {
        const response = await axiosProvider.post<RateCardResponse>(
            "api/v1/rate-cards",
            payload
        );
        return response.data;
    },

    async submit(payload: RateCardPayload): Promise<RateCardResponse> {
        const response = await axiosProvider.post<RateCardResponse>(
            "api/v1/rate-cards/submit",
            payload
        );
        return response.data;
    },
};
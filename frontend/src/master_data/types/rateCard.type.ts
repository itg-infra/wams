import type { PaginationMeta } from "../../types/users.types";
import type { Vendor } from "./vendor.type";

export interface RateCardItem {
    itemShadowId: number;
    uomMasterId: number;
    costValue: number;
}

export interface RateCardPayload {
    vendorShadowId: number;
    location: string;
    items: RateCardItem[];
}

export interface RateCardResponse {
    success: boolean;
    message?: string;
    data?: unknown;
    requestId?: string | null;
}

export interface RateCardListResponse{
    success: boolean;
    data: RateCardItemResponse[];
    meta: PaginationMeta
    requestId?: string | null;

}

/** Columns the API can order by — see RateCardRepository.GetAllAsync. */
export type RateCardSortBy = "createdAt" | "status" | "submittedAt";

export interface RateCardListParams {
    page?: number;
    limit?: number;
    search?: string;
    is_active?: boolean | "";
    sortBy?: RateCardSortBy;
    sortOrder?: "asc" | "desc";
}

export interface RateCardItemResponse{
    id: number,
    vendor: Vendor,
    status: string,
    itemCount: number,
    createdAt: string,
}

export interface RateCardResponseState {
    rateCard: RateCardItemResponse[];
    meta: PaginationMeta | null;
    isLoading: boolean;
    error: string | null;
    params: RateCardListParams;

    // Actions
    fetchRateCard: (params?: RateCardListParams) => Promise<void>;
    setParams: (params: Partial<RateCardListParams>) => void;
    clearError: () => void;
}
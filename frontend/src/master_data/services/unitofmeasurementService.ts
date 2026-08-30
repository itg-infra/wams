import axiosProvider from "../../api/providers/axiosProvider";
import type {
    UomItem,
    UomListQueryParams,
    UomListResponse,
    UomDetailResponse,
    CreateUomPayload,
    UpdateUomPayload,
    DeleteUomResponse,
} from "../types/unitofmeasurement.type";

interface UomListApiResponse {
    success: boolean;
    data: UomItem[];
    meta?: {
        page: number;
        limit: number;
        total: number;
        totalPages: number;
    };
    message?: string;
    requestId?: string | null;
}

interface UomDetailApiResponse {
    success: boolean;
    data: UomItem;
    message?: string;
    requestId?: string | null;
}

interface UomDeleteApiResponse {
    success: boolean;
    data: null;
    message: string;
    requestId?: string | null;
}

export const uomService = {
    async getUoms(params: UomListQueryParams = {}): Promise<UomListResponse> {
        const { search = "", page = 1, limit = 10 } = params;

        const response = await axiosProvider.get<UomListApiResponse>("api/v1/uoms", {
            params: {
                search,
                page,
                limit,
            },
        });

        return {
            data: response.data.data ?? [],
            meta: {
                page: response.data.meta?.page ?? page,
                limit: response.data.meta?.limit ?? limit,
                total: response.data.meta?.total ?? response.data.data.length ?? 0,
                totalPages: response.data.meta?.totalPages ?? 1,
            },
        };
    },

    async getUomDetail(id: number | string): Promise<UomDetailResponse> {
        const response = await axiosProvider.get<UomDetailApiResponse>(`api/v1/uoms/${id}`);

        return {
            data: response.data.data,
        };
    },

    async createUom(payload: CreateUomPayload): Promise<UomItem> {
        const response = await axiosProvider.post<UomDetailApiResponse>("api/v1/uoms", payload);
        return response.data.data;
    },

    async updateUom(id: number | string, payload: UpdateUomPayload): Promise<UomItem> {
        const response = await axiosProvider.put<UomDetailApiResponse>(`api/v1/uoms/${id}`, payload);
        return response.data.data;
    },

    async deleteUom(id: number | string): Promise<DeleteUomResponse> {
        const response = await axiosProvider.delete<UomDeleteApiResponse>(`api/v1/uoms/${id}`);

        return {
            success: response.data.success,
            message: response.data.message,
        };
    },
};
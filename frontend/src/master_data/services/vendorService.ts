import axiosProvider from "../../api/providers/axiosProvider";
import type {
    Vendor,
    VendorDetailResponse,
    VendorListResponse,
    VendorQueryParams,
} from "../types/vendor.type";

interface VendorListApiResponse {
    success: boolean;
    data: Array<{
        id: number;
        cardCode: string;
        cardName: string;
    }>;
    meta: {
        page: number;
        limit: number;
        total: number;
        totalPages: number;
    };
    requestId?: string | null;
}

interface VendorDetailApiResponse {
    success: boolean;
    data: {
        id: number;
        cardCode: string;
        cardName: string;
    };
    message?: string;
    requestId?: string | null;
}

function mapVendor(vendor: VendorListApiResponse["data"][number]): Vendor {
    return {
        id: String(vendor.id),
        cardCode: vendor.cardCode,
        cardName: vendor.cardName,
    };
}

export const vendorService = {
    async getVendors(params: VendorQueryParams = {}): Promise<VendorListResponse> {
        const {
            search = "",
            page = 1,
            limit = 10,
        } = params;

        const response = await axiosProvider.get<VendorListApiResponse>("api/v1/vendors", {
            params: {
                search: search || undefined,
                page,
                limit,
            },
        });

        const { data, meta } = response.data;
        const mappedData = data.map(mapVendor);

        const from = meta.total === 0 ? 0 : (meta.page - 1) * meta.limit + 1;
        const to = meta.total === 0 ? 0 : Math.min(meta.page * meta.limit, meta.total);

        return {
            data: mappedData,
            meta: {
                page: meta.page,
                limit: meta.limit,
                total: meta.total,
                totalPages: meta.totalPages,
                from,
                to,
            },
        };
    },

    async getVendorDetail(id: string): Promise<VendorDetailResponse> {
        const response = await axiosProvider.get<VendorDetailApiResponse>(`api/v1/vendors/${id}`);

        return {
            data: mapVendor(response.data.data),
        };
    },
};
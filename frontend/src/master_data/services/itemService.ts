import axiosProvider from "../../api/providers/axiosProvider";
import type {
    Item,
    ItemDetailResponse,
    ItemListResponse,
    ItemQueryParams,
} from "../types/item.types";

interface ItemListApiResponse {
    success: boolean;
    data: Array<{
        id: number;
        itemCode: string;
        itemName: string;
        acctCode: string;
        acctName: string;
    }>;
    meta: {
        page: number;
        limit: number;
        total: number;
        totalPages: number;
    };
    requestId?: string | null;
}

interface ItemDetailApiResponse {
    success: boolean;
    data: {
        id: number;
        itemCode: string;
        itemName: string;
        acctCode: string;
        acctName: string;
    };
    message?: string;
    requestId?: string | null;
}

function mapItem(item: ItemListApiResponse["data"][number]): Item {
    return {
        id: String(item.id),
        itemCode: item.itemCode,
        itemName: item.itemName,
        acctCode: item.acctCode,
        acctName: item.acctName
    };
}

export const itemService = {
    async getItems(params: ItemQueryParams = {}): Promise<ItemListResponse> {
        const {
            search = "",
            page = 1,
            limit = 10,
        } = params;

        const response = await axiosProvider.get<ItemListApiResponse>("api/v1/items", {
            params: {
                search: search || undefined,
                page,
                limit,
            },
        });

        const { data, meta } = response.data;
        const mappedData = data.map(mapItem);

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

    async getItemDetail(id: string): Promise<ItemDetailResponse> {
        const response = await axiosProvider.get<ItemDetailApiResponse>(`api/v1/items/${id}`);

        return {
            data: mapItem(response.data.data),
        };
    },
};
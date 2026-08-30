import axiosProvider from "../../api/providers/axiosProvider";
import type {
    SpkTypes,
    SpkListApiResponse,
    SpkDetailApiResponse,
    SpkListResponse,
    SpkDetailResponse,
    SpkQueryParams,
} from "../types/spk.type";

// ─── Mapper ───────────────────────────────────────────────────────────────────

function mapItem(item: SpkListApiResponse["data"][number]): SpkTypes {
    return {
        id: item.id,
        type: item.type,
        docNo: item.docNo,
        baseDoc: item.baseDoc,
        baseDocNo: item.baseDocNo,
        cardCode: item.cardCode,
        cardName: item.cardName,
        itemCode: item.itemCode,
        itemName: item.itemName,
        quantity: item.quantity,
        deliveryQty: item.deliveryQty,
        uoM: item.uoM,
        packType: item.packType,
        whsCode: item.whsCode,
        whsName: item.whsName,
        docStatus: item.docStatus,
        blNo: item.blNo,
    };
}

// ─── Service ──────────────────────────────────────────────────────────────────

export const spkService = {
    async getSpkList(params: SpkQueryParams = {}): Promise<SpkListResponse> {
        const { search = "", page = 1, limit = 10 } = params;

        const response = await axiosProvider.get<SpkListApiResponse>("api/v1/spk", {
            params: {
                search: search || undefined,
                page,
                limit,
            },
        });

        const { data, meta } = response.data;
        const mappedData = data.map(mapItem);

        const from = meta.total === 0 ? 0 : (meta.page - 1) * meta.limit + 1;
        const to =
            meta.total === 0 ? 0 : Math.min(meta.page * meta.limit, meta.total);

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

    async getSpkDetail(id: string | number): Promise<SpkDetailResponse> {
        const response = await axiosProvider.get<SpkDetailApiResponse>(
            `api/v1/spk/${id}`
        );

        return {
            data: mapItem(response.data.data),
        };
    },
};
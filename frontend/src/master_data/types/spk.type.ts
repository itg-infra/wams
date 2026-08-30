// ─── Raw API shapes ───────────────────────────────────────────────────────────

export interface SpkRawItem {
    id: number;
    type: string;
    docNo: string;
    baseDoc: string;
    baseDocNo: string;
    cardCode: string;
    cardName: string;
    itemCode: string;
    itemName: string;
    quantity: number | null;
    deliveryQty: number | null;
    uoM: string;
    packType: string;
    whsCode: string;
    whsName: string;
    docStatus: string;
    blNo: string;
}

export interface SpkListApiResponse {
    success: boolean;
    data: SpkRawItem[];
    meta: {
        page: number;
        limit: number;
        total: number;
        totalPages: number;
    };
    requestId: string | null;
}

export interface SpkDetailApiResponse {
    success: boolean;
    data: SpkRawItem;
    message?: string;
    requestId: string | null;
}

// ─── Domain types (used across the app) ──────────────────────────────────────

export interface SpkTypes {
    id: number;
    type: string;
    docNo: string;
    baseDoc: string;
    baseDocNo: string;
    cardCode: string;
    cardName: string;
    itemCode: string;
    itemName: string;
    quantity: number | null;
    deliveryQty: number | null;
    uoM: string;
    packType: string;
    whsCode: string;
    whsName: string;
    docStatus: string;
    blNo: string;
}

export interface SpkMeta {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
    from: number;
    to: number;
}

export interface SpkListResponse {
    data: SpkTypes[];
    meta: SpkMeta;
}

export interface SpkDetailResponse {
    data: SpkTypes;
}

// ─── Query params ─────────────────────────────────────────────────────────────

export interface SpkQueryParams {
    search?: string;
    page?: number;
    limit?: number;
}
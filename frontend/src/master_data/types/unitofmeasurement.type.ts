export interface UomItem {
    id: number;
    code: string;
    name: string;
    isActive: boolean;
}

export interface UomListQueryParams {
    search?: string;
    page?: number;
    limit?: number;
}

export interface UomListResponse {
    data: UomItem[];
    meta: {
        page: number;
        limit: number;
        total: number;
        totalPages: number;
    };
}

export interface UomDetailResponse {
    data: UomItem;
}

export interface CreateUomPayload {
    code: string;
    name: string;
}

export interface UpdateUomPayload {
    name?: string;
    isActive?: boolean;
}

export interface DeleteUomResponse {
    success: boolean;
    message: string;
}
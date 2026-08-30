export interface Vendor {
    id: string;
    cardCode: string;
    cardName: string;
}

export interface VendorQueryParams {
    search?: string;
    page?: number;
    limit?: number;
}

export interface VendorListResponse {
    data: Vendor[];
    meta: {
        page: number;
        limit: number;
        total: number;
        totalPages: number;
        from: number;
        to: number;
    };
}

export interface VendorDetailResponse {
    data: Vendor;
}
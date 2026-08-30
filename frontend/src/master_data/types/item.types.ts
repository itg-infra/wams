export interface Item {
  id: string;
  itemCode: string;
  itemName: string;
  acctCode: string;
  acctName: string;
}

export interface ItemQueryParams {
    search?: string;
    page?: number;
    limit?: number;
}

export interface ItemListResponse {
    data: Item[];
    meta: {
        page: number;
        limit: number;
        total: number;
        totalPages: number;
        from: number;
        to: number;
    };
}

export interface ItemDetailResponse {
    data: Item;
}
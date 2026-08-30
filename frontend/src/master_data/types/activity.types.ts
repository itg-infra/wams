export interface Activity{
    id: number;
    code: string;
    name: string;
    isActive: boolean;
}

export interface ActivityListResponse{
    data: Activity[];
}

export interface ActivityDetailResponse{
    data: Activity;
}
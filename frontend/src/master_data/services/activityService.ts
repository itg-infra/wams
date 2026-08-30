import axiosProvider from "../../api/providers/axiosProvider";
import type { Activity, ActivityDetailResponse, ActivityListResponse } from "../types/activity.types";

interface ActivityListApiResponse{
    succss: boolean;
    data: Array<{
        id:number;
        code: string;
        name: string;
        isActive: boolean;
    }>;
    message: string;
    requestId?: string | null;
}

interface ActivityDetailApiResponse{
    success: boolean;
    data:{
        id: number;
        code: string;
        name: string;
        isActive: boolean;
    }
    message?: string;
    requestId?: string | null;
}

function mapItem(activity: ActivityListApiResponse['data'][number]): Activity{
    return {
        id: Number(activity.id),
        code: activity.code,
        name: activity.name,
        isActive: activity.isActive,
    }
}

export const activityService = {
    async getActivities():Promise<ActivityListResponse>{
        const response = await axiosProvider.get<ActivityListResponse>(`api/v1/activity-types`);

        const {data} = response.data;
        const mappedData = data.map(mapItem);

        return {
            data: mappedData,
        };
    },

    async getActivityDetail(id: string): Promise<ActivityDetailResponse>{
        const response = await axiosProvider.get<ActivityDetailApiResponse>(`api/v1/activity-types/${id}`);

        return {
            data: mapItem(response.data.data),
        }
    }
}


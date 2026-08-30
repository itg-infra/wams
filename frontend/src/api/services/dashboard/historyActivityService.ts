import axiosProvider from "../../providers/axiosProvider";
import type { HistoryActivityResponse } from "../../../types/historyActivity.type";

export interface HistoryActivityParams {
  page?: number;
  limit?: number;
}

export const getHistoryActivities = async (
  params: HistoryActivityParams,
): Promise<HistoryActivityResponse> => {
  const response = await axiosProvider.get<HistoryActivityResponse>(
    "/api/v1/dashboard/activities",
    {
      params,
    },
  );

  return response.data;
};

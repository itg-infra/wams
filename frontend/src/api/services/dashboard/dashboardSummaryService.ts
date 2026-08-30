import type { DashboardSummaryResponse } from "../../../types/dashboardSummary.type";
import axiosProvider from "../../providers/axiosProvider";

class DashboardSummaryService {
  async getSummary(): Promise<DashboardSummaryResponse> {
    const response = await axiosProvider.get<DashboardSummaryResponse>(
      "/api/v1/dashboard/summary",
    );

    return response.data;
  }
}

export default new DashboardSummaryService();

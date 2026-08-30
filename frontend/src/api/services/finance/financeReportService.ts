import axiosProvider from "../../providers/axiosProvider";
import type {
  FinanceReportListParams,
  FinanceReportListResponse,
  FinanceReportDetailResponse,
} from "../../../types/financeReport.type";

const BASE_URL = "/api/v1/finance-reports";

/** Buang key yang undefined/'' supaya tidak ikut terkirim sebagai query param */
function cleanParams<T extends Record<string, unknown>>(params: T): Partial<T> {
  const result: Partial<T> = {};
  (Object.keys(params) as (keyof T)[]).forEach((key) => {
    const value = params[key];
    if (value !== undefined && value !== null && value !== "") {
      result[key] = value;
    }
  });
  return result;
}

export const financeReportService = {

  getList: async (
    params: FinanceReportListParams = {},
  ): Promise<FinanceReportListResponse> => {
    const query = cleanParams({
      page: params.page ?? 1,
      limit: params.limit ?? 10,
      activityTypeCode: params.activityTypeCode,
      warehouseCode: params.warehouseCode,
      dateFrom: params.dateFrom,
      dateTo: params.dateTo,
      // Paginated endpoint: ordering has to be resolved by the API.
      sortBy: params.sortBy,
      sortOrder: params.sortBy ? (params.sortOrder ?? "asc") : undefined,
    });

    const { data } = await axiosProvider.get<FinanceReportListResponse>(
      BASE_URL,
      {
        params: query,
      },
    );
    return data;
  },
  
  getDetail: async (
    budgetPlanId: number | string,
  ): Promise<FinanceReportDetailResponse> => {
    const { data } = await axiosProvider.get<FinanceReportDetailResponse>(
      `${BASE_URL}/${budgetPlanId}`,
    );
    return data;
  },
};

export default financeReportService;

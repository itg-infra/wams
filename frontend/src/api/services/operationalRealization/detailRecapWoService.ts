import axiosProvider from "../../providers/axiosProvider";

import type {
  BudgetRevisionRecapItem,
  BudgetRevisionRecapResponse,
  RealizationRecaApprovedResponse,
  RealizationRecapDetail,
  RealizationRecapDetailResponse,
} from "../../../types/detailRecapWo";

export interface RejectRecapPayload {
  reason: string;
}

export interface RejectRevisionPayload {
  reason: string;
}

export interface CreateBudgetRevisionPayload {
  recapWorkOrderId: number;
  revisedTotal: number;
  reason: string;
}

export async function createBudgetRevision(
  payload: CreateBudgetRevisionPayload,
) {
  const response = await axiosProvider.post(
    "/api/v1/budget-revisions",
    payload,
  );

  return response.data;
}

class RealizationRecapDetailService {
  async getDetail(id: number): Promise<RealizationRecapDetail> {
    const response = await axiosProvider.get<RealizationRecapDetailResponse>(
      `/api/v1/recap-work-orders/${id}`,
    );

    return response.data.data;
  }

  async approvedRecap(id: number): Promise<RealizationRecaApprovedResponse> {
    const response = await axiosProvider.post<RealizationRecaApprovedResponse>(
      `/api/v1/recap-work-orders/${id}/approve`,
    );

    return response.data;
  }

  async rejectRecap(
    id: number,
    payload: RejectRecapPayload,
  ): Promise<RealizationRecaApprovedResponse> {
    const response = await axiosProvider.post<RealizationRecaApprovedResponse>(
      `/api/v1/recap-work-orders/${id}/reject`,
      payload,
    );

    return response.data;
  }

  async getRevisionbyRecap(id: number): Promise<BudgetRevisionRecapItem[]> {
    const response = await axiosProvider.get<BudgetRevisionRecapResponse>(
      `/api/v1/budget-revisions/by-recap/${id}`,
    );

    return response.data.data;
  }

  async approvedRevision(id: number): Promise<RealizationRecaApprovedResponse> {
    const response = await axiosProvider.post<RealizationRecaApprovedResponse>(
      `/api/v1/budget-revisions/${id}/approve`,
    );

    return response.data;
  }

  async rejectRevision(
    id: number,
    payload: RejectRevisionPayload,
  ): Promise<RealizationRecaApprovedResponse> {
    const response = await axiosProvider.post<RealizationRecaApprovedResponse>(
      `/api/v1/budget-revisions/${id}/reject`,
      payload,
    );

    return response.data;
  }
}

export const realizationRecapDetailService =
  new RealizationRecapDetailService();

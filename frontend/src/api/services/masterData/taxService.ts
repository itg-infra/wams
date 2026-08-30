import axiosProvider from "../../providers/axiosProvider";
import type {
  CreateTaxBody,
  TaxDetailResponse,
  TaxFilterParams,
  TaxListResponse,
  UpdateTaxBody,
} from "../../../types/tax.type";

export const getTaxListService = async (
  params?: TaxFilterParams,
): Promise<TaxListResponse> => {
  const response = await axiosProvider.get("/api/v1/tax-types", {
    params,
  });

  return response.data;
};

export const getTaxDetailService = async (
  id: number,
): Promise<TaxDetailResponse> => {
  const response = await axiosProvider.get(`/api/v1/tax-types/${id}`);

  return response.data;
};

export const createTaxService = async (
  body: CreateTaxBody,
): Promise<TaxDetailResponse> => {
  const response = await axiosProvider.post("/api/v1/tax-types", body);

  return response.data;
};

export const updateTaxService = async (
  id: number,
  body: UpdateTaxBody,
): Promise<TaxDetailResponse> => {
  const response = await axiosProvider.put(`/api/v1/tax-types/${id}`, body);

  return response.data;
};

export const deleteTaxService = async (id: number): Promise<void> => {
  await axiosProvider.delete(`/api/v1/tax-types/${id}`);
};

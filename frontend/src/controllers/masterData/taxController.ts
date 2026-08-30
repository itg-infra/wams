import {
  createTaxService,
  deleteTaxService,
  getTaxDetailService,
  getTaxListService,
  updateTaxService,
} from "../../api/services/masterData/taxService";

import { useTaxStore } from "../../store/taxStore";

import type {
  CreateTaxBody,
  TaxFilterParams,
  UpdateTaxBody,
} from "../../types/tax.type";

export const getTaxListController = async (params?: TaxFilterParams) => {
  const { setTaxes, setLoading, setError } = useTaxStore.getState();

  try {
    setLoading(true);
    setError(null);

    const response = await getTaxListService(params);

    setTaxes(response.data);

    return response;
  } catch (error: any) {
    setError(error?.response?.data?.message ?? "Failed to fetch tax list");
    throw error;
  } finally {
    setLoading(false);
  }
};

export const getTaxDetailController = async (id: number) => {
  const { setTaxDetail, setLoading, setError } = useTaxStore.getState();

  try {
    setLoading(true);
    setError(null);

    const response = await getTaxDetailService(id);

    setTaxDetail(response.data);

    return response;
  } catch (error: any) {
    setError(error?.response?.data?.message ?? "Failed to fetch tax detail");
    throw error;
  } finally {
    setLoading(false);
  }
};

export const createTaxController = async (body: CreateTaxBody) => {
  const { setSubmitLoading, setError } = useTaxStore.getState();

  try {
    setSubmitLoading(true);
    setError(null);

    const response = await createTaxService(body);

    return response;
  } catch (error: any) {
    setError(error?.response?.data?.message ?? "Failed to create tax");
    throw error;
  } finally {
    setSubmitLoading(false);
  }
};

export const updateTaxController = async (id: number, body: UpdateTaxBody) => {
  const { setSubmitLoading, setError } = useTaxStore.getState();

  try {
    setSubmitLoading(true);
    setError(null);

    const response = await updateTaxService(id, body);

    return response;
  } catch (error: any) {
    setError(error?.response?.data?.message ?? "Failed to update tax");
    throw error;
  } finally {
    setSubmitLoading(false);
  }
};

export const deleteTaxController = async (id: number) => {
  const { setSubmitLoading, setError } = useTaxStore.getState();

  try {
    setSubmitLoading(true);
    setError(null);

    await deleteTaxService(id);
  } catch (error: any) {
    setError(error?.response?.data?.message ?? "Failed to delete tax");
    throw error;
  } finally {
    setSubmitLoading(false);
  }
};

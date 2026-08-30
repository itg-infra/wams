import { create } from "zustand";
import type { TaxData } from "../types/tax.type";

interface TaxStore {
  taxes: TaxData[];
  taxDetail: TaxData | null;

  loading: boolean;
  submitLoading: boolean;

  error: string | null;

  setTaxes: (data: TaxData[]) => void;
  setTaxDetail: (data: TaxData | null) => void;

  setLoading: (loading: boolean) => void;
  setSubmitLoading: (loading: boolean) => void;

  setError: (error: string | null) => void;

  resetDetail: () => void;
}

export const useTaxStore = create<TaxStore>((set) => ({
  taxes: [],
  taxDetail: null,

  loading: false,
  submitLoading: false,

  error: null,

  setTaxes: (taxes) =>
    set({
      taxes,
    }),

  setTaxDetail: (taxDetail) =>
    set({
      taxDetail,
    }),

  setLoading: (loading) =>
    set({
      loading,
    }),

  setSubmitLoading: (submitLoading) =>
    set({
      submitLoading,
    }),

  setError: (error) =>
    set({
      error,
    }),

  resetDetail: () =>
    set({
      taxDetail: null,
    }),
}));

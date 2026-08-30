import { create } from "zustand";

import { realizationRecapDetailService } from "../api/services/operationalRealization/detailRecapWoService";

import type {
  BudgetRevisionRecapItem,
  RealizationRecapDetail,
} from "../types/detailRecapWo";

interface RealizationRecapDetailStore {
  detail: RealizationRecapDetail | null;

  isLoading: boolean;

  error: string | null;

  fetchDetail: (id: number) => Promise<void>;

  clear: () => void;

  revisionByRecap: BudgetRevisionRecapItem[] | null;

  fetchRevisionbyRecap: (id: number) => Promise<void>;
}

export const useRealizationRecapDetailStore =
  create<RealizationRecapDetailStore>((set) => ({
    detail: null,

    isLoading: false,

    error: null,

    revisionByRecap: null,

    fetchRevisionbyRecap: async (id) => {
      try {
        set({
          isLoading: true,
          error: null,
        });

        const result =
          await realizationRecapDetailService.getRevisionbyRecap(id);

        set({
          revisionByRecap: result,
          isLoading: false,
        });
      } catch (error) {
        console.error(error);

        set({
          isLoading: false,
          error: "Failed to load recap detail",
        });
      }
    },

    fetchDetail: async (id) => {
      try {
        set({
          isLoading: true,
          error: null,
        });

        const result = await realizationRecapDetailService.getDetail(id);

        set({
          detail: result,
          isLoading: false,
        });
      } catch (error) {
        console.error(error);

        set({
          isLoading: false,
          error: "Failed to load recap detail",
        });
      }
    },

    clear: () =>
      set({
        detail: null,
        error: null,
      }),
  }));

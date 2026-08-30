// stores/workOrder.store.ts

import { create } from "zustand";
import type { WOPIC } from "../types/woPic";

type WorkOrderStore = {
  isSubmitting: boolean;
  isDrafting: boolean;
  errorDrafting: string;
  errorSubmitting: string;

  setIsSubmitting: (value: boolean) => void;
  setIsDrafting: (value: boolean) => void;

  seterrorSubmitting: (value: string) => void;
  seterrorDrafting: (value: string) => void;

  pics: WOPIC[];
  isLoading: boolean;
  error: string | null;
  setPics: (pics: WOPIC[]) => void;
  setIsLoading: (isLoading: boolean) => void;
  setError: (error: string | null) => void;
  resetStore: () => void;
};



export const useWorkOrderStore = create<WorkOrderStore>((set) => ({
  isSubmitting: false,
  isDrafting: false,
  errorSubmitting: "",
  errorDrafting: "",

  pics: [],
  isLoading: false,
  error: null,

  setPics: (pics) => set({ pics }),
  setIsLoading: (isLoading) => set({ isLoading }),
  setError: (error) => set({ error }),

  // Berguna jika ingin mengosongkan state saat pindah/menutup form WO
  resetStore: () => set({ pics: [], isLoading: false, error: null }),

  setIsDrafting(value) {
    set({
      isDrafting: value,
    });
  },

  seterrorSubmitting(value) {
    set({
      errorSubmitting: value,
    });
  },

  seterrorDrafting(value) {
    set({
      errorDrafting: value,
    });
  },

  setIsSubmitting: (value) =>
    set({
      isSubmitting: value,
    }),
}));

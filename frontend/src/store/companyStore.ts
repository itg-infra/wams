import { create } from "zustand";
import { devtools } from "zustand/middleware";
import { companyService } from "../api/services/masterData/companyService";
import type { CompanyState } from "../types/company.types";

export const useCompanyStore = create<CompanyState>()(
  devtools(
    (set) => ({
      companies: [],
      isLoading: false,
      error: null,

      selectedCompanyId: null, // add

      setSelectedCompanyId: (
        id: number, // add
      ) => set({ selectedCompanyId: id }),

      fetchCompanies: async () => {
        set({ isLoading: true, error: null });
        try {
          const response = await companyService.getPublicList();

          if (!response.success || !response.data) {
            set({
              isLoading: false,
              error: response.message ?? "Failed to load companies.",
            });
            return;
          }

          set({
            companies: response.data,
            isLoading: false,
            error: null,
          });
        } catch {
          set({
            isLoading: false,
            error: "Failed to load companies. Please try again.",
          });
        }
      },

      clearError: () => set({ error: null }),
    }),
    { name: "CompanyStore" },
  ),
);
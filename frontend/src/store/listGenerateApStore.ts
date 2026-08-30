import { create } from "zustand";

import type { AccountPayableItem } from "../types/listGenerateAp.type";

import type { AccountPayableDetail } from "../types/DetailAp.type";

import { accountPayableService } from "../api/services/budgeting/accountPayable/listGenerateAp";

interface AccountPayableState {
  // ======================================================
  // LIST
  // ======================================================

  accountPayables: AccountPayableItem[];

  isLoading: boolean;

  error: string | null;

  // --- TAMBAHAN UNTUK SORTING ---
  sortBy: "status" | "docDate" | "createdAt";

  sortOrder: "asc" | "desc";

  setSortBy: (sortBy: "status" | "docDate" | "createdAt") => void;

  setSortOrder: (sortOrder: "asc" | "desc") => void;
  // ------------------------------

  fetchAccountPayables: () => Promise<void>;

  // ======================================================
  // DETAIL
  // ======================================================

  selectedAccountPayable: AccountPayableDetail | null;

  isDetailLoading: boolean;

  fetchAccountPayableDetail: (id: number) => Promise<void>;

  clearSelectedAccountPayable: () => void;

  // ======================================================
  // DELETE
  // ======================================================

  deleteLoadingId: number | null;

  setDeleteLoadingId: (id: number | null) => void;

  deleteAccountPayable: (id: number) => Promise<void>;

  // ======================================================
  // ERROR
  // ======================================================

  errorMessage: string | null;

  setErrorMessage: (message: string | null) => void;
}

// Tambahkan 'get' pada parameter create agar kita bisa membaca state saat memanggil API
export const useAccountPayableStore = create<AccountPayableState>(
  (set, get) => ({
    // ======================================================
    // LIST
    // ======================================================

    accountPayables: [],

    isLoading: false,

    error: null,

    // --- INISIALISASI SORTING ---
    sortBy: "createdAt", // Nilai default

    sortOrder: "desc", // Nilai default

    setSortBy: (sortBy) => {
      set({ sortBy });
      get().fetchAccountPayables(); // Langsung memanggil ulang API setelah nilai diubah
    },

    setSortOrder: (sortOrder) => {
      set({ sortOrder });
      get().fetchAccountPayables(); // Langsung memanggil ulang API setelah nilai diubah
    },
    // ----------------------------

    fetchAccountPayables: async () => {
      try {
        set({
          isLoading: true,
        });

        // Ambil parameter sortBy dan sortOrder yang sedang aktif di state
        const { sortBy, sortOrder } = get();

        // Kirim parameter tersebut ke service yang sudah kita ubah sebelumnya
        const response = await accountPayableService.getApprovedRecaps({
          sortBy,
          sortOrder,
        });

        set({
          accountPayables: response.data,
        });
      } catch (error) {
        set({
          error:
            error instanceof Error
              ? error.message
              : "Failed fetch account payables",
        });
      } finally {
        set({
          isLoading: false,
        });
      }
    },

    // ======================================================
    // DETAIL
    // ======================================================

    selectedAccountPayable: null,

    isDetailLoading: false,

    fetchAccountPayableDetail: async (id: number) => {
      try {
        set({
          isDetailLoading: true,
        });

        const response =
          await accountPayableService.getAccountPayableDetail(id);

        set({
          selectedAccountPayable: response.data,
        });
      } finally {
        set({
          isDetailLoading: false,
        });
      }
    },

    clearSelectedAccountPayable: () => {
      set({
        selectedAccountPayable: null,
      });
    },

    // ======================================================
    // DELETE
    // ======================================================

    deleteLoadingId: null,

    setDeleteLoadingId: (id: number | null) => {
      set({
        deleteLoadingId: id,
      });
    },

    deleteAccountPayable: async (id: number) => {
      await accountPayableService.deleteAccountPayable(id);
    },

    // ======================================================
    // ERROR
    // ======================================================

    errorMessage: null,

    setErrorMessage: (message: string | null) => {
      set({
        errorMessage: message,
      });
    },
  }),
);

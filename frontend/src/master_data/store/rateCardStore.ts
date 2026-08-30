import { create } from "zustand";
import { devtools } from "zustand/middleware";
import { rateCardService } from "../services/rateCardService";
import type {
    RateCardListParams,
    RateCardPayload,
    RateCardResponseState,
} from "../types/rateCard.type";
import type { RateCardDetail } from "../types/rateCardDetail.type";

interface RateCardStoreState extends RateCardResponseState {
    isSavingDraft: boolean;
    draftError: string | null;

    isSubmitting: boolean;
    submitError: string | null;

    isUpdating: boolean;
    updateError: string | null;

    isDeleting: boolean;
    deleteError: string | null;

    selectedRateCard: RateCardDetail | null;
    isDetailLoading: boolean;
    detailError: string | null;

    fetchRateCard: (params?: RateCardListParams) => Promise<void>;
    fetchDetail: (id: number | string) => Promise<void>;
    saveDraft: (payload: RateCardPayload) => Promise<boolean>;
    submit: (payload: RateCardPayload) => Promise<boolean>;
    update: (id: number | string, payload: RateCardPayload) => Promise<boolean>;
    delete: (id: number | string) => Promise<boolean>;

    clearSelectedRateCard: () => void;
    clearDraftError: () => void;
    clearSubmitError: () => void;
    clearUpdateError: () => void;
    clearDeleteError: () => void;
}

export const useRateCardStore = create<RateCardStoreState>()(
    devtools((set, get) => ({
        rateCard: [],
        meta: null,
        isLoading: false,
        error: null,
        params: { page: 1, limit: 20, search: "", is_active: "" },

        selectedRateCard: null,
        isDetailLoading: false,
        detailError: null,

        isSavingDraft: false,
        draftError: null,

        isSubmitting: false,
        submitError: null,

        isUpdating: false,
        updateError: null,

        isDeleting: false,
        deleteError: null,

        fetchRateCard: async (params?) => {
            const mergedParams = { ...get().params, ...params };
            set({ isLoading: true, error: null, params: mergedParams });
            try {
                const response = await rateCardService.listRateCard(mergedParams);
                if (!response.success) {
                    set({ isLoading: false, error: "Failed to fetch rate card." });
                    return;
                }
                set({ rateCard: response.data, meta: response.meta, isLoading: false });
            } catch (err: unknown) {
                const e = err as { response?: { data?: { message?: string } } };
                set({ isLoading: false, error: e?.response?.data?.message ?? "Failed to load rate cards." });
            }
        },

        fetchDetail: async (id) => {
            set({ isDetailLoading: true, detailError: null, selectedRateCard: null });
            try {
                const response = await rateCardService.getDetail(id);
                if (!response.success) {
                    set({ isDetailLoading: false, detailError: "Failed to fetch detail." });
                    return;
                }
                set({ selectedRateCard: response.data, isDetailLoading: false });
            } catch (err: unknown) {
                const e = err as { response?: { data?: { message?: string } } };
                set({ isDetailLoading: false, detailError: e?.response?.data?.message ?? "Failed to load detail." });
            }
        },

        saveDraft: async (payload) => {
            set({ isSavingDraft: true, draftError: null });
            try {
                await rateCardService.saveDraft(payload);
                set({ isSavingDraft: false });
                return true;
            } catch (error) {
                set({ isSavingDraft: false, draftError: error instanceof Error ? error.message : "Failed to save draft" });
                return false;
            }
        },

        submit: async (payload) => {
            set({ isSubmitting: true, submitError: null });
            try {
                await rateCardService.submit(payload);
                set({ isSubmitting: false });
                return true;
            } catch (error) {
                set({ isSubmitting: false, submitError: error instanceof Error ? error.message : "Failed to submit rate card" });
                return false;
            }
        },

        update: async (id, payload) => {
            set({ isUpdating: true, updateError: null });
            try {
                await rateCardService.update(id, payload);
                set({ isUpdating: false });
                return true;
            } catch (error) {
                set({ isUpdating: false, updateError: error instanceof Error ? error.message : "Failed to update rate card" });
                return false;
            }
        },

        delete: async (id) => {
            set({ isDeleting: true, deleteError: null });
            try {
                await rateCardService.delete(id);
                set({ isDeleting: false });
                return true;
            } catch (error) {
                set({ isDeleting: false, deleteError: error instanceof Error ? error.message : "Failed to delete rate card" });
                return false;
            }
        },

        setParams: (params) => set((s) => ({ params: { ...s.params, ...params } })),
        clearError: () => set({ error: null }),

        clearSelectedRateCard: () => set({ selectedRateCard: null, detailError: null }),
        clearDraftError: () => set({ draftError: null }),
        clearSubmitError: () => set({ submitError: null }),
        clearUpdateError: () => set({ updateError: null }),
        clearDeleteError: () => set({ deleteError: null }),
    }))
);
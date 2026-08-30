import { create } from "zustand";
import type { BudgetPlanDetail } from "../types/budgetPlanDetial.type";

interface BudgetPlanDetailState {
    // Data
    detail: BudgetPlanDetail | null;

    // UI State
    isLoading: boolean;
    isApproving: boolean;
    isRejecting: boolean;
    error: string | null;
    approveError: string | null;
    rejectError: string | null;

    // Actions
    setDetail: (detail: BudgetPlanDetail | null) => void;
    setLoading: (loading: boolean) => void;
    setApproving: (approving: boolean) => void;
    setRejecting: (rejecting: boolean) => void;
    setError: (error: string | null) => void;
    setApproveError: (error: string | null) => void;
    setRejectError: (error: string | null) => void;
    reset: () => void;
}

export const useBudgetPlanDetailStore = create<BudgetPlanDetailState>((set) => ({
    detail: null,
    isLoading: false,
    isApproving: false,
    isRejecting: false,
    error: null,
    approveError: null,
    rejectError: null,

    setDetail: (detail) => set({ detail }),
    setLoading: (isLoading) => set({ isLoading }),
    setApproving: (isApproving) => set({ isApproving }),
    setRejecting: (isRejecting) => set({ isRejecting }),
    setError: (error) => set({ error }),
    setApproveError: (approveError) => set({ approveError }),
    setRejectError: (rejectError) => set({ rejectError }),
    reset: () =>
        set({
            detail: null,
            isLoading: false,
            isApproving: false,
            isRejecting: false,
            error: null,
            approveError: null,
            rejectError: null,
        }),
}));
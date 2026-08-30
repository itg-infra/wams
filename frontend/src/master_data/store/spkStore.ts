import { create } from "zustand";
import type { SpkTypes, SpkMeta, SpkQueryParams } from "../types/spk.type";

// ─── State shape ──────────────────────────────────────────────────────────────

interface SpkState {
    // List
    spkList: SpkTypes[];
    meta: SpkMeta;
    queryParams: SpkQueryParams;
    isLoadingList: boolean;
    listError: string | null;

    // Detail
    selectedSpk: SpkTypes | null;
    isLoadingDetail: boolean;
    detailError: string | null;
}

// ─── Actions ──────────────────────────────────────────────────────────────────

interface SpkActions {
    // List
    setSpkList: (list: SpkTypes[]) => void;
    setMeta: (meta: SpkMeta) => void;
    setQueryParams: (params: Partial<SpkQueryParams>) => void;
    setIsLoadingList: (loading: boolean) => void;
    setListError: (error: string | null) => void;

    // Detail
    setSelectedSpk: (spk: SpkTypes | null) => void;
    setIsLoadingDetail: (loading: boolean) => void;
    setDetailError: (error: string | null) => void;

    // Reset
    resetList: () => void;
    resetDetail: () => void;
}

// ─── Initial values ───────────────────────────────────────────────────────────

const initialMeta: SpkMeta = {
    page: 1,
    limit: 10,
    total: 0,
    totalPages: 0,
    from: 0,
    to: 0,
};

const initialQueryParams: SpkQueryParams = {
    search: "",
    page: 1,
    limit: 10,
};

// ─── Store ────────────────────────────────────────────────────────────────────

export const useSpkStore = create<SpkState & SpkActions>((set) => ({
    // ── List state ────────────────────────────────────────────────────────────
    spkList: [],
    meta: initialMeta,
    queryParams: initialQueryParams,
    isLoadingList: false,
    listError: null,

    setSpkList: (list) => set({ spkList: list }),
    setMeta: (meta) => set({ meta }),
    setQueryParams: (params) =>
        set((state) => ({
            queryParams: { ...state.queryParams, ...params },
        })),
    setIsLoadingList: (loading) => set({ isLoadingList: loading }),
    setListError: (error) => set({ listError: error }),

    // ── Detail state ──────────────────────────────────────────────────────────
    selectedSpk: null,
    isLoadingDetail: false,
    detailError: null,

    setSelectedSpk: (spk) => set({ selectedSpk: spk }),
    setIsLoadingDetail: (loading) => set({ isLoadingDetail: loading }),
    setDetailError: (error) => set({ detailError: error }),

    // ── Reset ─────────────────────────────────────────────────────────────────
    resetList: () =>
        set({
            spkList: [],
            meta: initialMeta,
            queryParams: initialQueryParams,
            isLoadingList: false,
            listError: null,
        }),

    resetDetail: () =>
        set({
            selectedSpk: null,
            isLoadingDetail: false,
            detailError: null,
        }),
}));
import { create } from "zustand";
import { uomService } from "../services/unitofmeasurementService";
import type {
    UomItem,
    UomListQueryParams,
    CreateUomPayload,
    UpdateUomPayload,
} from "../types/unitofmeasurement.type";

interface UomStoreState {
    // List
    uoms: UomItem[];
    isLoading: boolean;
    error: string | null;

    // Detail
    selectedUom: UomItem | null;
    isDetailLoading: boolean;
    detailError: string | null;

    // Mutation
    isCreating: boolean;
    createError: string | null;

    isUpdating: boolean;
    updateError: string | null;

    isDeleting: boolean;
    deleteError: string | null;

    // Query
    search: string;
    page: number;
    limit: number;

    // Meta
    total: number;
    totalPages: number;

    // Actions
    fetchUoms: (params?: Partial<UomListQueryParams>) => Promise<void>;
    fetchUomDetail: (id: number | string) => Promise<void>;
    createUom: (payload: CreateUomPayload) => Promise<UomItem | null>;
    updateUom: (id: number | string, payload: UpdateUomPayload) => Promise<UomItem | null>;
    deleteUom: (id: number | string) => Promise<boolean>;

    setSearch: (value: string) => void;
    setPage: (value: number) => void;
    setLimit: (value: number) => void;
    clearSelectedUom: () => void;

    clearError: () => void;
    clearDetailError: () => void;
    clearCreateError: () => void;
    clearUpdateError: () => void;
    clearDeleteError: () => void;
}

export const useUomStore = create<UomStoreState>((set, get) => ({
    // List
    uoms: [],
    isLoading: false,
    error: null,

    // Detail
    selectedUom: null,
    isDetailLoading: false,
    detailError: null,

    // Mutation
    isCreating: false,
    createError: null,

    isUpdating: false,
    updateError: null,

    isDeleting: false,
    deleteError: null,

    // Query
    search: "",
    page: 1,
    limit: 10,

    // Meta
    total: 0,
    totalPages: 1,

    fetchUoms: async (params = {}) => {
        const current = get();

        const nextSearch = params.search ?? current.search;
        const nextPage = params.page ?? current.page;
        const nextLimit = params.limit ?? current.limit;

        set({
            isLoading: true,
            error: null,
            search: nextSearch,
            page: nextPage,
            limit: nextLimit,
        });

        try {
            const response = await uomService.getUoms({
                search: nextSearch,
                page: nextPage,
                limit: nextLimit,
            });

            set({
                uoms: response.data,
                total: response.meta.total,
                totalPages: response.meta.totalPages,
                page: response.meta.page,
                limit: response.meta.limit,
                isLoading: false,
            });
        } catch (error) {
            set({
                isLoading: false,
                error: error instanceof Error ? error.message : "Failed to fetch UOMs",
            });
        }
    },

    fetchUomDetail: async (id) => {
        set({
            isDetailLoading: true,
            detailError: null,
            selectedUom: null,
        });

        try {
            const response = await uomService.getUomDetail(id);

            set({
                selectedUom: response.data,
                isDetailLoading: false,
            });
        } catch (error) {
            set({
                isDetailLoading: false,
                detailError: error instanceof Error ? error.message : "Failed to fetch UOM detail",
            });
        }
    },

    createUom: async (payload) => {
        set({
            isCreating: true,
            createError: null,
        });

        try {
            const created = await uomService.createUom(payload);

            set({
                isCreating: false,
            });

            await get().fetchUoms({ page: 1 });

            return created;
        } catch (error) {
            set({
                isCreating: false,
                createError: error instanceof Error ? error.message : "Failed to create UOM",
            });
            return null;
        }
    },

    updateUom: async (id, payload) => {
        set({
            isUpdating: true,
            updateError: null,
        });

        try {
            const updated = await uomService.updateUom(id, payload);

            set((state) => ({
                isUpdating: false,
                selectedUom:
                    state.selectedUom?.id === updated.id ? updated : state.selectedUom,
                uoms: state.uoms.map((item) =>
                    item.id === updated.id ? updated : item
                ),
            }));

            return updated;
        } catch (error) {
            set({
                isUpdating: false,
                updateError: error instanceof Error ? error.message : "Failed to update UOM",
            });
            return null;
        }
    },

    deleteUom: async (id) => {
        set({
            isDeleting: true,
            deleteError: null,
        });

        try {
            await uomService.deleteUom(id);

            set((state) => ({
                isDeleting: false,
                uoms: state.uoms.filter((item) => item.id !== Number(id)),
                selectedUom:
                    state.selectedUom?.id === Number(id) ? null : state.selectedUom,
            }));

            await get().fetchUoms();

            return true;
        } catch (error) {
            set({
                isDeleting: false,
                deleteError: error instanceof Error ? error.message : "Failed to delete UOM",
            });
            return false;
        }
    },

    setSearch: (value) => set({ search: value, page: 1 }),
    setPage: (value) => set({ page: value }),
    setLimit: (value) => set({ limit: value, page: 1 }),
    clearSelectedUom: () => set({ selectedUom: null }),

    clearError: () => set({ error: null }),
    clearDetailError: () => set({ detailError: null }),
    clearCreateError: () => set({ createError: null }),
    clearUpdateError: () => set({ updateError: null }),
    clearDeleteError: () => set({ deleteError: null }),
}));
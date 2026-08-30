import { create } from "zustand";
import { itemService } from "../services/itemService";
import type { Item, ItemQueryParams } from "../types/item.types";

interface ItemStoreState {
    items: Item[];
    selectedItem: Item | null;
    isLoading: boolean;
    isDetailLoading: boolean;
    error: string | null;
    detailError: string | null;

    search: string;
    page: number;
    limit: number;
    total: number;
    totalPages: number;
    from: number;
    to: number;

    fetchItems: (params?: Partial<ItemQueryParams>) => Promise<void>;
    fetchItemDetail: (id: string) => Promise<void>;
    setSearch: (value: string) => void;
    setPage: (value: number) => void;
    resetFilters: () => void;
    clearSelectedItem: () => void;
}

export const useItemStore = create<ItemStoreState>((set, get) => ({
    items: [],
    selectedItem: null,
    isLoading: false,
    isDetailLoading: false,
    error: null,
    detailError: null,

    search: "",
    page: 1,
    limit: 10,
    total: 0,
    totalPages: 1,
    from: 0,
    to: 0,

    fetchItems: async (params = {}) => {
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
            const response = await itemService.getItems({
                search: nextSearch,
                page: nextPage,
                limit: nextLimit,
            });

            set({
                items: response.data,
                total: response.meta.total,
                totalPages: response.meta.totalPages,
                from: response.meta.from,
                to: response.meta.to,
                isLoading: false,
            });
        } catch (error) {
            set({
                isLoading: false,
                error: error instanceof Error ? error.message : "Failed to fetch items",
            });
        }
    },

    fetchItemDetail: async (id: string) => {
        set({
            isDetailLoading: true,
            detailError: null,
        });

        try {
            const response = await itemService.getItemDetail(id);

            set({
                selectedItem: response.data,
                isDetailLoading: false,
            });
        } catch (error) {
            set({
                isDetailLoading: false,
                detailError: error instanceof Error ? error.message : "Failed to fetch item detail",
            });
        }
    },

    setSearch: (value) => set({ search: value, page: 1 }),
    setPage: (value) => set({ page: value }),

    resetFilters: () =>
        set({
            search: "",
            page: 1,
        }),

    clearSelectedItem: () =>
        set({
            selectedItem: null,
            detailError: null,
        }),
}));
import { create } from "zustand";
import { devtools, persist } from "zustand/middleware";
import { warehouseService } from "../api/services/masterData/warehouseList";
import type { WarehouseState, FetchWarehouseParams } from "../types/warehouseList.type";

const deduplicateByCode = (list: ReturnType<typeof Array.prototype.filter>) => {
    const seen = new Set<string>();
    return list.filter((wh: { code: string }) => {
        if (seen.has(wh.code)) return false;
        seen.add(wh.code);
        return true;
    });
};

export const useWarehouseStore = create<WarehouseState>()(
    devtools(
        persist(
            (set, get) => ({
                warehouses: [],
                selectedWarehouse: null,
                isLoading: false,
                error: null,
                meta: null,

                page: 1,
                search: "",
                limit: 20,

                fetchWarehouses: async (params?: FetchWarehouseParams) => {
                    set({ isLoading: true, error: null });
                    try {
                        const currentPage = params?.page ?? get().page;
                        const currentSearch = params?.search ?? get().search;
                        const currentLimit = params?.limit ?? get().limit;

                        const response = await warehouseService.getList({
                            page: currentPage,
                            limit: currentLimit,
                            search: currentSearch,
                        });

                        if (!response.success || !response.data) {
                            set({ isLoading: false, error: "Failed to load warehouses." });
                            return;
                        }

                        const incoming = deduplicateByCode(response.data);
                        const isLoadMore = currentPage > 1 && currentSearch === get().search;

                        const merged = isLoadMore
                            ? deduplicateByCode([...get().warehouses, ...incoming])
                            : incoming;

                        set({
                            warehouses: merged,
                            meta: response.meta,
                            page: currentPage,
                            search: currentSearch,
                            limit: currentLimit,
                            isLoading: false,
                            error: null,
                        });

                        if (!get().selectedWarehouse && merged.length > 0) {
                            set({ selectedWarehouse: merged[0] });
                        }
                    } catch {
                        set({
                            isLoading: false,
                            error: "Failed to load warehouses. Please try again.",
                        });
                    }
                },

                setSelectedWarehouse: (warehouse) =>
                    set({ selectedWarehouse: warehouse }),

                getWarehouseId: () => get().selectedWarehouse?.id ?? null,

                setSearch: (search) => set({ search }),

                setPage: (page) => set({ page }),

                clearError: () => set({ error: null }),
            }),
            {
                name: "WarehouseStore",
                partialize: (state) => ({
                    selectedWarehouse: state.selectedWarehouse,
                }),
            }
        ),
        { name: "WarehouseStore" }
    )
);
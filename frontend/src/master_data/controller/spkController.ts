import { spkService } from "../services/spkService";
import { useSpkStore } from "../store/spkStore";
import type { SpkQueryParams } from "../types/spk.type";

// ─── Controller ───────────────────────────────────────────────────────────────

export const spkController = {
    /**
     * Fetch SPK list.
     * Optionally pass params to override current queryParams in the store.
     *
     * Usage:
     *   await spkController.fetchSpkList();
     *   await spkController.fetchSpkList({ page: 2, search: "260591" });
     */
    async fetchSpkList(params?: Partial<SpkQueryParams>): Promise<void> {
        const store = useSpkStore.getState();

        // Merge incoming params into the store first so pagination/search state
        // stays in sync before the request fires.
        if (params) {
            store.setQueryParams(params);
        }

        const mergedParams: SpkQueryParams = {
            ...store.queryParams,
            ...params,
        };

        store.setIsLoadingList(true);
        store.setListError(null);

        try {
            const result = await spkService.getSpkList(mergedParams);
            store.setSpkList(result.data);
            store.setMeta(result.meta);
        } catch (error) {
            const message =
                error instanceof Error ? error.message : "Failed to fetch SPK list.";
            store.setListError(message);
        } finally {
            store.setIsLoadingList(false);
        }
    },

    /**
     * Fetch SPK detail by id.
     *
     * Usage:
     *   await spkController.fetchSpkDetail(3234);
     *   await spkController.fetchSpkDetail("3234");
     */
    async fetchSpkDetail(id: string | number): Promise<void> {
        const store = useSpkStore.getState();

        store.setIsLoadingDetail(true);
        store.setDetailError(null);
        store.setSelectedSpk(null);

        try {
            const result = await spkService.getSpkDetail(id);
            store.setSelectedSpk(result.data);
        } catch (error) {
            const message =
                error instanceof Error
                    ? error.message
                    : "Failed to fetch SPK detail.";
            store.setDetailError(message);
        } finally {
            store.setIsLoadingDetail(false);
        }
    },

    /**
     * Update search query and reset to page 1, then re-fetch.
     *
     * Usage:
     *   await spkController.searchSpk("ARDIANSYAH");
     */
    async searchSpk(search: string): Promise<void> {
        await spkController.fetchSpkList({ search, page: 1 });
    },

    /**
     * Navigate to a specific page.
     *
     * Usage:
     *   await spkController.goToPage(3);
     */
    async goToPage(page: number): Promise<void> {
        await spkController.fetchSpkList({ page });
    },

    /**
     * Change items per page and reset to page 1.
     *
     * Usage:
     *   await spkController.changeLimit(25);
     */
    async changeLimit(limit: number): Promise<void> {
        await spkController.fetchSpkList({ limit, page: 1 });
    },

    /** Clear list state (e.g. on unmount). */
    resetList(): void {
        useSpkStore.getState().resetList();
    },

    /** Clear detail state (e.g. on modal close). */
    resetDetail(): void {
        useSpkStore.getState().resetDetail();
    },
};
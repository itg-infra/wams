import { create } from "zustand";
import { activityService } from "../services/activityService";
import type { Activity } from "../types/activity.types";

interface ActivityStoreState {
    activities: Activity[];
    selectedActivity: Activity | null;
    isLoading: boolean;
    isDetailLoading: boolean;
    error: string | null;
    detailError: string | null;

    search: string;

    fetchActivity: ()=> Promise<void>;
    fetchActivityDetail: (id: string) => Promise<void>;
    setSearch: (value: string) => void;
    resetFilters: () => void;
    clearSelectedActivity: () => void;

}

export const useActivityStore = create<ActivityStoreState>((set) => ({
    activities: [],
    selectedActivity: null,
    isLoading: false,
    isDetailLoading: false,
    error: null,
    detailError: null,

    search: "",

    fetchActivity: async () => {

        set({
            isLoading: true,
            error: null,
        });

        try {
            const response = await activityService.getActivities();

            set({
                activities: response.data,
                isLoading: false,
            });
        } catch (error) {
            set({
                isLoading: false,
                error: error instanceof Error ? error.message : "Failed to fetch activity",
            });
        }
    },

    fetchActivityDetail: async (id: string) => {
        set({ isDetailLoading: true, detailError: null, });

        try {
            const response = await activityService.getActivityDetail(id);

            set({
                selectedActivity: response.data,
                isDetailLoading: false,
            });
        } catch (error) {
            set({
                isDetailLoading: false,
                detailError: error instanceof Error ? error.message : "Failed to fetch activity detail",
            });
        }
    },

    setSearch: (value) => set({ search: value }),
    resetFilters: () => set({
        search: "",
    }),

    clearSelectedActivity: () =>
        set({
            selectedActivity: null,
            detailError: null,
        }),
}))
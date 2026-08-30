import { create } from "zustand";
import type { HistoryActivity, HistoryActivityMeta } from "../types/historyActivity.type";

interface HistoryActivityState {
  activities: HistoryActivity[];
  meta: HistoryActivityMeta | null;

  isLoadingHistory: boolean;
  error: string | null;

  page: number;
  limit: number;

  setActivities: (
    activities: HistoryActivity[],
    meta: HistoryActivityMeta,
  ) => void;

  setLoading: (loading: boolean) => void;

  setError: (error: string | null) => void;

  setPage: (page: number) => void;
}

export const useHistoryActivityStore = create<HistoryActivityState>((set) => ({
  activities: [],
  meta: null,

  isLoadingHistory: false,
  error: null,

  page: 1,
  limit: 20,

  setActivities: (activities, meta) =>
    set({
      activities,
      meta,
    }),

  setLoading: (loading) =>
    set({
      isLoadingHistory: loading,
    }),

  setError: (error) =>
    set({
      error,
    }),

  setPage: (page) =>
    set({
      page,
    }),
}));

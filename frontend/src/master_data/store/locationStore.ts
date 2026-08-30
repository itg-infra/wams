import { create } from "zustand";
import type { LocationItems } from "../types/location.type";


interface LocationStore {
  locations: LocationItems[];
  loading: boolean;
  error: string | null;

  setLocations: (locations: LocationItems[]) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
}

export const useLocationStore = create<LocationStore>((set) => ({
  locations: [],
  loading: false,
  error: null,

  setLocations: (locations) => set({ locations }),
  setLoading: (loading) => set({ loading }),
  setError: (error) => set({ error }),
}));

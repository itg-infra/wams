import { create } from "zustand";
import type { WorkOrderFile } from "../types/file.type";

interface FileState {
  files: WorkOrderFile[];
  isLoading: boolean;
  error: string | null;
  setFiles: (files: WorkOrderFile[]) => void;
  setLoading: (isLoading: boolean) => void;
  setError: (error: string | null) => void;
  reset: () => void;
}

export const useFileStore = create<FileState>((set) => ({
  files: [],
  isLoading: false,
  error: null,
  setFiles: (files) => set({ files }),
  setLoading: (isLoading) => set({ isLoading }),
  setError: (error) => set({ error }),
  reset: () => set({ files: [], isLoading: false, error: null }),
}));

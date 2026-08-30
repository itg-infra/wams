import { create } from "zustand";
import { fileUploadService } from "../api/services/file/fileUploadService";

interface FileUploadStore {
  isUploading: boolean;

  uploadFiles: (
    id: number,
    files: File[],
  ) => Promise<void>;
}

export const useFileUploadStore = create<FileUploadStore>((set) => ({
  isUploading: false,

  uploadFiles: async (id: number, files: File[]) => {
    try {
      set({ isUploading: true });

      await fileUploadService.uploadFiles(id, files);
    } finally {
      set({ isUploading: false });
    }
  },
}));

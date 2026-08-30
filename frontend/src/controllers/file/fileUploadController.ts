import { useFileUploadStore } from "../../store/fileUploadStore";

export const useFileUploadController = () => {
  const isUploading = useFileUploadStore((state) => state.isUploading);

  const uploadFiles = useFileUploadStore((state) => state.uploadFiles);

  return {
    isUploading,
    uploadFiles,
  };
};

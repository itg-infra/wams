import { useCallback } from "react";
import { useFileStore } from "../../store/fileStore";
import { fileUploadService } from "../../api/services/file/fileUploadService";

export const useFileController = () => {
  const { files, isLoading, error, setFiles, setLoading, setError } =
    useFileStore();

  const getWorkOrderFiles = useCallback(
    async (id: number) => {
      setLoading(true);
      setError(null);

      try {
        const response = await fileUploadService.getWorkOrderFiles(id);
        setFiles(response.data);
        return response;
      } catch (err) {
        const message =
          err instanceof Error ? err.message : "Failed to fetch files";
        setError(message);
        throw err;
      } finally {
        setLoading(false);
      }
    },
    [setFiles, setLoading, setError],
  );

  return {
    files,
    isLoading,
    error,
    getWorkOrderFiles,
  };
};

import type { GetWorkOrderFilesResponse } from "../../../types/file.type";
import axiosProvider from "../../providers/axiosProvider";

export interface UploadFileResponse {
  success: boolean;
  message: string;
  requestId: string;
}

export const fileUploadService = {
  uploadFiles: async (
    id: number,
    files: File[],
  ): Promise<UploadFileResponse> => {
    const formData = new FormData();

    files.forEach((file) => {
      formData.append("files", file, file.name);
    });

    for (const [key, value] of formData.entries()) {
      console.log(key, value);
    }

    const response = await axiosProvider.post<UploadFileResponse>(
      `/api/v1/files/work-orders/${id}`,

      formData,
      {
        headers: {
          "Content-Type": "multipart/form-data",
        },
      },
    );

    return response.data;
  },

  getWorkOrderFiles: async (id: number): Promise<GetWorkOrderFilesResponse> => {
    const token = localStorage.getItem("token");

    const response = await axiosProvider.get<GetWorkOrderFilesResponse>(
      `/api/v1/files/work-orders/${id}`,
      {
        params: {
          token,
        },
      },
    );

    return response.data;
  },
};

export interface WorkOrderFile {
  id: number;
  entityId: number;
  entityType: string;
  originalFileName: string;
  url: string;
  fileSize: string;
  fileSizeRaw: number;
  contentType: string;
  uploadedAt: string;
  uploadedByName: string;
  uploadedByUserId: number;
}

export interface GetWorkOrderFilesResponse {
  success: boolean;
  message: string;
  data: WorkOrderFile[];
  requestId: string;
}

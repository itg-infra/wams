export interface WorkflowTemplate {
  id: number;
  docType: string;
  name: string;
  isActive: boolean;
  stageCount?: number;
  companyId?: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface WorkflowStage {
  id?: number;
  stageOrder: number;
  stageName: string;
  approverRoles: string[];
}

export interface WorkflowTemplateDetail {
  id: number;
  docType: string;
  name: string;
  companyId: number;
  isActive: boolean;
  stages: WorkflowStage[];
  createdAt: string;
  updatedAt: string | null;
}

export interface WorkflowTemplateListParams {
  page?: number;
  limit?: number;
  docType?: string;
  search?: string;
  sortBy?: string;
  sortOrder?: "asc" | "desc";
}

export interface WorkflowTemplateListResponse {
  success: boolean;
  data: WorkflowTemplate[];
  meta: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
  };
  requestId: string;
}

export interface WorkflowTemplateDetailResponse {
  success: boolean;
  data: WorkflowTemplateDetail;
  message?: string;
  requestId: string;
}

export interface CreateWorkflowTemplatePayload {
  docType: string;
  name: string;
  isActive: boolean;
  stages: WorkflowStage[];
}

export interface UpdateWorkflowTemplatePayload {
  stages: WorkflowStage[];
}

export interface ToggleWorkflowTemplatePayload {
  isActive: boolean;
}

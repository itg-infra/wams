

import axiosProvider from "../../providers/axiosProvider";
import type {
  WorkflowTemplateListParams,
  WorkflowTemplateListResponse,
  WorkflowTemplateDetailResponse,
  CreateWorkflowTemplatePayload,
  UpdateWorkflowTemplatePayload,
  ToggleWorkflowTemplatePayload,
} from "../../../types/workflowTemplate.type";

export const workflowTemplateService = {
  getList: async (
    params: WorkflowTemplateListParams,
  ): Promise<WorkflowTemplateListResponse> => {
    const response = await axiosProvider.get("/api/v1/workflow-templates", {
      params,
    });

    return response.data;
  },

  getDetail: async (
    templateId: number,
  ): Promise<WorkflowTemplateDetailResponse> => {
    const response = await axiosProvider.get(`/api/v1/workflow-templates/${templateId}`);

    return response.data;
  },

  create: async (payload: CreateWorkflowTemplatePayload) => {
    const response = await axiosProvider.post(
      "/api/v1/workflow-templates",
      payload,
    );

    return response.data;
  },

  update: async (
    templateId: number,
    payload: UpdateWorkflowTemplatePayload,
  ) => {
    const response = await axiosProvider.put(
      `/api/v1/workflow-templates/${templateId}`,
      payload,
    );

    return response.data;
  },

  toggleActive: async (
    templateId: number,
    payload: ToggleWorkflowTemplatePayload,
  ) => {
    const response = await axiosProvider.put(
      `/api/v1/workflow-templates/${templateId}`,
      payload,
    );

    return response.data;
  },

  delete: async (templateId: number) => {
    const response = await axiosProvider.delete(
      `/api/v1/workflow-templates/${templateId}`,
    );

    return response.data;
  },
};

import { useState } from "react";

import { workflowTemplateService } from "../../api/services/masterData/workflowTemplateService";
import { useWorkflowTemplateStore } from "../../store/workflowTemplateStore";

import type {
  WorkflowTemplateListParams,
  CreateWorkflowTemplatePayload,
  UpdateWorkflowTemplatePayload,
} from "../../types/workflowTemplate.type";

export const useWorkflowTemplateController = () => {
  const {
    workflowTemplates,
    workflowTemplateDetail,
    setWorkflowTemplates,
    setWorkflowTemplateDetail,
  } = useWorkflowTemplateStore();

  const [isLoading, setIsLoading] = useState(false);

  const [pagination, setPagination] = useState({
    page: 1,
    limit: 10,
    total: 0,
    totalPages: 0,
  });

  const getWorkflowTemplates = async (params: WorkflowTemplateListParams) => {
    try {
      setIsLoading(true);

      const response = await workflowTemplateService.getList(params);

      setWorkflowTemplates(response.data);

      setPagination(response.meta);

      return response;
    } finally {
      setIsLoading(false);
    }
  };

  const getWorkflowTemplateDetail = async (templateId: number) => {
    try {
      setIsLoading(true);

      const response = await workflowTemplateService.getDetail(templateId);

      setWorkflowTemplateDetail(response.data);

      return response;
    } finally {
      setIsLoading(false);
    }
  };

  const createWorkflowTemplate = async (
    payload: CreateWorkflowTemplatePayload,
  ) => {
    try {
      setIsLoading(true);

      return await workflowTemplateService.create(payload);
    } finally {
      setIsLoading(false);
    }
  };

  const updateWorkflowTemplate = async (
    templateId: number,
    payload: UpdateWorkflowTemplatePayload,
  ) => {
    try {
      setIsLoading(true);

      return await workflowTemplateService.update(templateId, payload);
    } finally {
      setIsLoading(false);
    }
  };

  const toggleWorkflowTemplate = async (
    templateId: number,
    isActive: boolean,
  ) => {
    try {
      setIsLoading(true);

      return await workflowTemplateService.toggleActive(templateId, {
        isActive,
      });
    } finally {
      setIsLoading(false);
    }
  };

  const deleteWorkflowTemplate = async (templateId: number) => {
    try {
      setIsLoading(true);

      return await workflowTemplateService.delete(templateId);
    } finally {
      setIsLoading(false);
    }
  };

  return {
    isLoading,

    workflowTemplates,
    workflowTemplateDetail,

    pagination,

    getWorkflowTemplates,
    getWorkflowTemplateDetail,

    createWorkflowTemplate,
    updateWorkflowTemplate,

    toggleWorkflowTemplate,

    deleteWorkflowTemplate,
  };
};

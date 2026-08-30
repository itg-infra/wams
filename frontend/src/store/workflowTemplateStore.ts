import { create } from "zustand";

import type {
  WorkflowTemplate,
  WorkflowTemplateDetail,
} from "../types/workflowTemplate.type";

interface WorkflowTemplateStore {
  workflowTemplates: WorkflowTemplate[];
  workflowTemplateDetail: WorkflowTemplateDetail | null;

  setWorkflowTemplates: (workflowTemplates: WorkflowTemplate[]) => void;

  setWorkflowTemplateDetail: (detail: WorkflowTemplateDetail | null) => void;
}

export const useWorkflowTemplateStore = create<WorkflowTemplateStore>(
  (set) => ({
    workflowTemplates: [],
    workflowTemplateDetail: null,

    setWorkflowTemplates: (workflowTemplates) => set({ workflowTemplates }),

    setWorkflowTemplateDetail: (workflowTemplateDetail) =>
      set({ workflowTemplateDetail }),
  }),
);

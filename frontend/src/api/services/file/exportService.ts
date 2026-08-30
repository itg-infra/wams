import axiosProvider from "../../providers/axiosProvider";

export interface ExportBudgetTemplateParams {
  format: "Pdf" | "Xlsx" | "Csv";

  status?: "Draft" | "Submitted" | "Approved";

  dateFrom?: string;
  dateTo?: string;

  search?: string;

  sortBy?: string;
  sortOrder?: "asc" | "desc";
}

export interface ExportRCAParams {
  warehouseCode?: string;
  dateFrom?: string;
  dateTo?: string;
}

export interface ExportParams {
  format: "Pdf" | "Xlsx" | "Csv";

  status?: "Draft" | "Submitted" | "Approved";

  dateFrom?: string;
  dateTo?: string;

  search?: string;

  sortBy?: string;
  sortOrder?: "asc" | "desc";
}

export const exportFileServices = {
  exportBudgetPlans: async (
    params: ExportBudgetTemplateParams,
  ): Promise<Blob> => {
    const response = await axiosProvider.get("/api/v1/budget-plans/export", {
      params,
      responseType: "blob",
    });

    return response.data;
  },

  exportRCA: async (params: ExportRCAParams): Promise<Blob> => {
    const response = await axiosProvider.get("/api/v1/rca/export", {
      params,
      responseType: "blob",
    });

    return response.data;
  },

  exportBudgetTemplates: async (
    params: ExportBudgetTemplateParams,
  ): Promise<Blob> => {
    const response = await axiosProvider.get(
      "/api/v1/budget-templates/export",
      {
        params,
        responseType: "blob",
      },
    );

    return response.data;
  },

  exportPurchaseOrder: async (
    params: ExportBudgetTemplateParams,
  ): Promise<Blob> => {
    const response = await axiosProvider.get("/api/v1/purchase-orders/export", {
      params,
      responseType: "blob",
    });

    return response.data;
  },

  exportPurchaseOrderDetails: async (poId: number): Promise<Blob> => {
    const response = await axiosProvider.get(
      `/api/v1/purchase-orders/${poId}/pdf`,
      {
        responseType: "blob",
      },
    );

    return response.data;
  },

  exportWorkOrders: async (
    params: ExportBudgetTemplateParams,
  ): Promise<Blob> => {
    const response = await axiosProvider.get("/api/v1/work-orders/export", {
      params,
      responseType: "blob",
    });

    return response.data;
  },

  exportRecapWorkOrders: async (
    params: ExportBudgetTemplateParams,
  ): Promise<Blob> => {
    const response = await axiosProvider.get(
      "/api/v1/recap-work-orders/export",
      {
        params,
        responseType: "blob",
      },
    );

    return response.data;
  },

  exportAccountPayable: async (
    params: ExportBudgetTemplateParams,
  ): Promise<Blob> => {
    const response = await axiosProvider.get(
      "/api/v1/account-payables/export",
      {
        params,
        responseType: "blob",
      },
    );

    return response.data;
  },

  exportTransportOrders: async (
    params: ExportBudgetTemplateParams,
  ): Promise<Blob> => {
    const response = await axiosProvider.get(
      "/api/v1/transport-orders/export",
      {
        params,
        responseType: "blob",
      },
    );

    return response.data;
  },

  exportSPK: async (params: ExportBudgetTemplateParams): Promise<Blob> => {
    const response = await axiosProvider.get("/api/v1/spk/export", {
      params,
      responseType: "blob",
    });

    return response.data;
  },

  exportFinanceReports: async (
    params: ExportBudgetTemplateParams,
  ): Promise<Blob> => {
    const response = await axiosProvider.get("/api/v1/finance-reports/export", {
      params,
      responseType: "blob",
    });

    return response.data;
  },

  exportUsers: async (params: ExportBudgetTemplateParams): Promise<Blob> => {
    const response = await axiosProvider.get("/api/v1/users/export", {
      params,
      responseType: "blob",
    });

    return response.data;
  },

  exportRoles: async (params: ExportBudgetTemplateParams): Promise<Blob> => {
    const response = await axiosProvider.get("/api/v1/roles/export", {
      params,
      responseType: "blob",
    });

    return response.data;
  },

  exportCompanies: async (
    params: ExportBudgetTemplateParams,
  ): Promise<Blob> => {
    const response = await axiosProvider.get("/api/v1/companies/export", {
      params,
      responseType: "blob",
    });

    return response.data;
  },

  exportWarehouses: async (
    params: ExportBudgetTemplateParams,
  ): Promise<Blob> => {
    const response = await axiosProvider.get("/api/v1/warehouses/export", {
      params,
      responseType: "blob",
    });

    return response.data;
  },

  exportItems: async (params: ExportBudgetTemplateParams): Promise<Blob> => {
    const response = await axiosProvider.get("/api/v1/items/export", {
      params,
      responseType: "blob",
    });

    return response.data;
  },

  exportVendors: async (params: ExportBudgetTemplateParams): Promise<Blob> => {
    const response = await axiosProvider.get("/api/v1/vendors/export", {
      params,
      responseType: "blob",
    });

    return response.data;
  },

  exportRateCards: async (
    params: ExportBudgetTemplateParams,
  ): Promise<Blob> => {
    const response = await axiosProvider.get("/api/v1/rate-cards/export", {
      params,
      responseType: "blob",
    });

    return response.data;
  },

  exportRFBA: async (bpId: number): Promise<Blob> => {
    const response = await axiosProvider.get(
      `/api/v1/budget-plans/${bpId}/rfba-pdf`,
      {
        params: {
          bpId,
        },
        responseType: "blob",
      },
    );

    return response.data;
  },
};

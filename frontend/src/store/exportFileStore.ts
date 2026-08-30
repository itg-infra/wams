import { create } from "zustand";
import {
  exportFileServices,
  type ExportParams,
  type ExportRCAParams,
} from "../api/services/file/exportService";

interface ExportFileStore {
  isExporting: boolean;

  exportBudgetTemplates: (params: ExportParams) => Promise<void>;
  exportRCA: (params: ExportRCAParams) => Promise<void>;
  exportBudgetPlans: (params: ExportParams) => Promise<void>;
  exportPurchaseOrder: (params: ExportParams) => Promise<void>;
  exportPurchaseOrderDetails: (poId: number) => Promise<void>;
  exportRFBA: (bpId: number) => Promise<void>;
  exportWorkOrders: (params: ExportParams) => Promise<void>;
  exportRecapWorkOrders: (params: ExportParams) => Promise<void>;
  exportAccountPayable: (params: ExportParams) => Promise<void>;
  exportTransportOrders: (params: ExportParams) => Promise<void>;
  exportSPK: (params: ExportParams) => Promise<void>;
  exportFinanceReports: (params: ExportParams) => Promise<void>;
  exportUsers: (params: ExportParams) => Promise<void>;
  exportRoles: (params: ExportParams) => Promise<void>;
  exportCompanies: (params: ExportParams) => Promise<void>;
  exportWarehouses: (params: ExportParams) => Promise<void>;
  exportItems: (params: ExportParams) => Promise<void>;
  exportVendors: (params: ExportParams) => Promise<void>;
  exportRateCards: (params: ExportParams) => Promise<void>;
}

export const useExportFileStore = create<ExportFileStore>((set) => ({
  isExporting: false,

  exportRCA: async (params: ExportRCAParams) => {
    try {
      set({ isExporting: true });

      const file = await exportFileServices.exportRCA(params);

      const url = window.URL.createObjectURL(file);

      const link = document.createElement("a");

      link.href = url;
      link.download = `RCA${params.dateFrom}-${params.dateTo}.pdf`;

      document.body.appendChild(link);
      link.click();

      link.remove();
      window.URL.revokeObjectURL(url);
    } finally {
      set({ isExporting: false });
    }
  },

  exportBudgetTemplates: async (params: ExportParams) => {
    try {
      set({ isExporting: true });

      const file = await exportFileServices.exportBudgetTemplates(params);

      const url = window.URL.createObjectURL(file);

      const link = document.createElement("a");
      const extensionMap = {
        Pdf: "pdf",
        Xlsx: "xlsx",
        Csv: "csv",
      };

      link.href = url;
      link.download = `budget-templates.${extensionMap[params.format]}`;

      document.body.appendChild(link);
      link.click();

      link.remove();
      window.URL.revokeObjectURL(url);
    } finally {
      set({ isExporting: false });
    }
  },

  exportBudgetPlans: async (params: ExportParams) => {
    try {
      set({ isExporting: true });

      const file = await exportFileServices.exportBudgetPlans(params);

      const url = window.URL.createObjectURL(file);

      const link = document.createElement("a");

      const extensionMap = {
        Pdf: "pdf",
        Xlsx: "xlsx",
        Csv: "csv",
      };

      link.href = url;
      link.download = `budget-plans.${extensionMap[params.format]}`;

      document.body.appendChild(link);
      link.click();

      link.remove();
      window.URL.revokeObjectURL(url);
    } finally {
      set({ isExporting: false });
    }
  },

  exportPurchaseOrder: async (params: ExportParams) => {
    try {
      set({ isExporting: true });

      const file = await exportFileServices.exportPurchaseOrder(params);

      const url = window.URL.createObjectURL(file);

      const link = document.createElement("a");
      const extensionMap = {
        Pdf: "pdf",
        Xlsx: "xlsx",
        Csv: "csv",
      };

      link.href = url;
      link.download = `purchase-orders.${extensionMap[params.format]}`;

      document.body.appendChild(link);
      link.click();

      link.remove();
      window.URL.revokeObjectURL(url);
    } finally {
      set({ isExporting: false });
    }
  },

  exportPurchaseOrderDetails: async (poId: number) => {
    try {
      set({ isExporting: true });

      const file = await exportFileServices.exportPurchaseOrderDetails(poId);

      const url = window.URL.createObjectURL(file);

      const link = document.createElement("a");
      link.href = url;
      link.download = `purchase-order-${poId}.pdf`; // sesuaikan nama file

      document.body.appendChild(link);
      link.click();

      link.remove();
      window.URL.revokeObjectURL(url);
    } finally {
      set({ isExporting: false });
    }
  },

  exportRFBA: async (bpId: number) => {
    try {
      set({ isExporting: true });

      const file = await exportFileServices.exportRFBA(bpId);

      const url = window.URL.createObjectURL(file);

      const link = document.createElement("a");
      link.href = url;
      link.download = `RFBA-${bpId}.pdf`; // sesuaikan nama file

      document.body.appendChild(link);
      link.click();

      link.remove();
      window.URL.revokeObjectURL(url);
    } finally {
      set({ isExporting: false });
    }
  },

  exportWorkOrders: async (params: ExportParams) => {
    try {
      set({ isExporting: true });

      const file = await exportFileServices.exportWorkOrders(params);

      const url = window.URL.createObjectURL(file);

      const link = document.createElement("a");
      const extensionMap = {
        Pdf: "pdf",
        Xlsx: "xlsx",
        Csv: "csv",
      };

      link.href = url;
      link.download = `work-orders.${extensionMap[params.format]}`;

      document.body.appendChild(link);
      link.click();

      link.remove();
      window.URL.revokeObjectURL(url);
    } finally {
      set({ isExporting: false });
    }
  },

  exportRecapWorkOrders: async (params: ExportParams) => {
    try {
      set({ isExporting: true });

      const file = await exportFileServices.exportRecapWorkOrders(params);

      const url = window.URL.createObjectURL(file);

      const link = document.createElement("a");
      const extensionMap = {
        Pdf: "pdf",
        Xlsx: "xlsx",
        Csv: "csv",
      };

      link.href = url;
      link.download = `Recap-WO.${extensionMap[params.format]}`;

      document.body.appendChild(link);
      link.click();

      link.remove();
      window.URL.revokeObjectURL(url);
    } finally {
      set({ isExporting: false });
    }
  },

  exportAccountPayable: async (params: ExportParams) => {
    try {
      set({ isExporting: true });

      const file = await exportFileServices.exportAccountPayable(params);

      const url = window.URL.createObjectURL(file);

      const link = document.createElement("a");
      const extensionMap = {
        Pdf: "pdf",
        Xlsx: "xlsx",
        Csv: "csv",
      };

      link.href = url;
      link.download = `Account-Payables.${extensionMap[params.format]}`;

      document.body.appendChild(link);
      link.click();

      link.remove();
      window.URL.revokeObjectURL(url);
    } finally {
      set({ isExporting: false });
    }
  },

  exportTransportOrders: async (params: ExportParams) => {
    try {
      set({ isExporting: true });

      const file = await exportFileServices.exportTransportOrders(params);

      const url = window.URL.createObjectURL(file);

      const link = document.createElement("a");
      const extensionMap = {
        Pdf: "pdf",
        Xlsx: "xlsx",
        Csv: "csv",
      };

      link.href = url;
      link.download = `Transport-Orders.${extensionMap[params.format]}`;

      document.body.appendChild(link);
      link.click();

      link.remove();
      window.URL.revokeObjectURL(url);
    } finally {
      set({ isExporting: false });
    }
  },

  exportSPK: async (params: ExportParams) => {
    try {
      set({ isExporting: true });

      const file = await exportFileServices.exportSPK(params);

      const url = window.URL.createObjectURL(file);

      const link = document.createElement("a");
      const extensionMap = {
        Pdf: "pdf",
        Xlsx: "xlsx",
        Csv: "csv",
      };

      link.href = url;
      link.download = `Spk.${extensionMap[params.format]}`;

      document.body.appendChild(link);
      link.click();

      link.remove();
      window.URL.revokeObjectURL(url);
    } finally {
      set({ isExporting: false });
    }
  },

  exportFinanceReports: async (params: ExportParams) => {
    try {
      set({ isExporting: true });

      const file = await exportFileServices.exportFinanceReports(params);

      const url = window.URL.createObjectURL(file);

      const link = document.createElement("a");
      const extensionMap = {
        Pdf: "pdf",
        Xlsx: "xlsx",
        Csv: "csv",
      };

      link.href = url;
      link.download = `Finance-Reports.${extensionMap[params.format]}`;

      document.body.appendChild(link);
      link.click();

      link.remove();
      window.URL.revokeObjectURL(url);
    } finally {
      set({ isExporting: false });
    }
  },

  exportUsers: async (params: ExportParams) => {
    try {
      set({ isExporting: true });

      const file = await exportFileServices.exportUsers(params);

      const url = window.URL.createObjectURL(file);

      const link = document.createElement("a");
      const extensionMap = {
        Pdf: "pdf",
        Xlsx: "xlsx",
        Csv: "csv",
      };

      link.href = url;
      link.download = `Users.${extensionMap[params.format]}`;

      document.body.appendChild(link);
      link.click();

      link.remove();
      window.URL.revokeObjectURL(url);
    } finally {
      set({ isExporting: false });
    }
  },

  exportRoles: async (params: ExportParams) => {
    try {
      set({ isExporting: true });

      const file = await exportFileServices.exportRoles(params);

      const url = window.URL.createObjectURL(file);

      const link = document.createElement("a");
      const extensionMap = {
        Pdf: "pdf",
        Xlsx: "xlsx",
        Csv: "csv",
      };

      link.href = url;
      link.download = `Roles.${extensionMap[params.format]}`;

      document.body.appendChild(link);
      link.click();

      link.remove();
      window.URL.revokeObjectURL(url);
    } finally {
      set({ isExporting: false });
    }
  },

  exportCompanies: async (params: ExportParams) => {
    try {
      set({ isExporting: true });

      const file = await exportFileServices.exportCompanies(params);

      const url = window.URL.createObjectURL(file);

      const link = document.createElement("a");
      const extensionMap = {
        Pdf: "pdf",
        Xlsx: "xlsx",
        Csv: "csv",
      };

      link.href = url;
      link.download = `Companies.${extensionMap[params.format]}`;

      document.body.appendChild(link);
      link.click();

      link.remove();
      window.URL.revokeObjectURL(url);
    } finally {
      set({ isExporting: false });
    }
  },

  exportWarehouses: async (params: ExportParams) => {
    try {
      set({ isExporting: true });

      const file = await exportFileServices.exportWarehouses(params);

      const url = window.URL.createObjectURL(file);

      const link = document.createElement("a");
      const extensionMap = {
        Pdf: "pdf",
        Xlsx: "xlsx",
        Csv: "csv",
      };

      link.href = url;
      link.download = `Warehouses.${extensionMap[params.format]}`;

      document.body.appendChild(link);
      link.click();

      link.remove();
      window.URL.revokeObjectURL(url);
    } finally {
      set({ isExporting: false });
    }
  },

  exportItems: async (params: ExportParams) => {
    try {
      set({ isExporting: true });

      const file = await exportFileServices.exportItems(params);

      const url = window.URL.createObjectURL(file);

      const link = document.createElement("a");
      const extensionMap = {
        Pdf: "pdf",
        Xlsx: "xlsx",
        Csv: "csv",
      };

      link.href = url;
      link.download = `Items.${extensionMap[params.format]}`;

      document.body.appendChild(link);
      link.click();

      link.remove();
      window.URL.revokeObjectURL(url);
    } finally {
      set({ isExporting: false });
    }
  },

  exportVendors: async (params: ExportParams) => {
    try {
      set({ isExporting: true });

      const file = await exportFileServices.exportVendors(params);

      const url = window.URL.createObjectURL(file);

      const link = document.createElement("a");
      const extensionMap = {
        Pdf: "pdf",
        Xlsx: "xlsx",
        Csv: "csv",
      };

      link.href = url;
      link.download = `vendors.${extensionMap[params.format]}`;

      document.body.appendChild(link);
      link.click();

      link.remove();
      window.URL.revokeObjectURL(url);
    } finally {
      set({ isExporting: false });
    }
  },

  exportRateCards: async (params: ExportParams) => {
    try {
      set({ isExporting: true });

      const file = await exportFileServices.exportRateCards(params);

      const url = window.URL.createObjectURL(file);

      const link = document.createElement("a");
      const extensionMap = {
        Pdf: "pdf",
        Xlsx: "xlsx",
        Csv: "csv",
      };

      link.href = url;
      link.download = `Rate-Cards.${extensionMap[params.format]}`;

      document.body.appendChild(link);
      link.click();

      link.remove();
      window.URL.revokeObjectURL(url);
    } finally {
      set({ isExporting: false });
    }
  },
}));

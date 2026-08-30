import { useExportFileStore } from "../../store/exportFileStore";

export const useExportFileController = () => {
  const isExporting = useExportFileStore((state) => state.isExporting);

  const exportRca = useExportFileStore(
    (state) => state.exportRCA,
  );

  const exportBudgetTemplates = useExportFileStore(
    (state) => state.exportBudgetTemplates,
  );

  const exportBudgetPlans = useExportFileStore(
    (state) => state.exportBudgetPlans,
  );

  const exportPurchaseOrder = useExportFileStore(
    (state) => state.exportPurchaseOrder,
  );

  const exportPurchaseOrderDetails = useExportFileStore(
    (state) => state.exportPurchaseOrderDetails,
  );

  const exportRFBA = useExportFileStore(
    (state) => state.exportRFBA,
  );

  const exportWorkOrders = useExportFileStore(
    (state) => state.exportWorkOrders,
  );

  const exportRecapWorkOrders = useExportFileStore(
    (state) => state.exportRecapWorkOrders,
  );

  const exportAccountPayable = useExportFileStore(
    (state) => state.exportAccountPayable,
  );

  const exportTransportOrders = useExportFileStore(
    (state) => state.exportTransportOrders,
  );

  const exportSPK = useExportFileStore((state) => state.exportSPK);

  const exportFinanceReports = useExportFileStore(
    (state) => state.exportFinanceReports,
  );

  const exportUsers = useExportFileStore((state) => state.exportUsers);

  const exportRoles = useExportFileStore((state) => state.exportRoles);

  const exportCompanies = useExportFileStore((state) => state.exportCompanies);

  const exportWarehouses = useExportFileStore(
    (state) => state.exportWarehouses,
  );

  const exportItems = useExportFileStore((state) => state.exportItems);

  const exportVendors = useExportFileStore((state) => state.exportVendors);

  const exportRateCards = useExportFileStore((state) => state.exportRateCards);

  return {
    isExporting,

    exportRca,
    exportBudgetTemplates,
    exportBudgetPlans,
    exportPurchaseOrder,
    exportPurchaseOrderDetails,
    exportRFBA,
    exportWorkOrders,
    exportRecapWorkOrders,
    exportAccountPayable,
    exportTransportOrders,
    exportSPK,
    exportFinanceReports,
    exportUsers,
    exportRoles,
    exportCompanies,
    exportWarehouses,
    exportItems,
    exportVendors,
    exportRateCards,
  };
};

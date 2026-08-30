import { useEffect, useCallback, useState } from "react";
import { useApprovedBudgetPlanStore } from "../../store/listGeneratePoStore";
import type {
  ApprovedBudgetPlan,
  SortField,
} from "../../types/listGeneratePo.type";
import { useNavigate } from "react-router-dom";
import type { BudgetPlanResponse } from "../../types/detailBudgetPlan.type";
import { budgetPlanService } from "../../api/services/budgeting/budgetPlan/detailBudgetPlanService";
import { useWarehouseStore } from "../../store/warehouseStore";

export interface UseApprovedBudgetPlanController {
  budgetPlans: ApprovedBudgetPlan[];
  paginatedPlans: ApprovedBudgetPlan[];

  isLoading: boolean;
  error: string | null;
  lastUpdated: Date | null;
  lastUpdatedDisplay: string;

  searchQuery: string;
  sortField: SortField | null;
  sortDirection: "asc" | "desc";

  currentPage: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;

  handleSearch: (query: string) => void;
  handleSort: (field: SortField) => void;
  handleOrder: (direction: "asc" | "desc") => void;
  handlePageChange: (page: number) => void;
  handleRefresh: () => void;
  // handleExport: () => void;

  // getPoStatusLabel: (
  //   plan: ApprovedBudgetPlan,
  // ) => "Generate PO" | "Waiting SAP" | "Generated" | "-";

  formatDocDate: (dateStr: string) => string;

  handleNavigatePO: (item: ApprovedBudgetPlan) => void;

  detail: BudgetPlanResponse | null;
  fetchDetail: (id: string) => Promise<void>;
  clearDetail: () => void;
}

export const useApprovedBudgetPlanController =
  (): UseApprovedBudgetPlanController => {
    const {
      filteredPlans,
      isLoading,
      error,
      lastUpdated,
      searchQuery,
      sortField,
      sortDirection,
      currentPage,
      pageSize,
      totalItems,
      fetchBudgetPlans,
      setSearchQuery,
      setSortField,
      setSortDirection,
      setCurrentPage,
    } = useApprovedBudgetPlanStore();

    const selectedWarehouse = useWarehouseStore(
      (state) => state.selectedWarehouse,
    );

    const navigate = useNavigate();

    const [detail, setDetail] = useState<BudgetPlanResponse | null>(null);
    const [detailLoading, setDetailLoading] = useState(false);
    const [detailError, setDetailError] = useState<string | null>(null);


    useEffect(() => {
      fetchBudgetPlans();
    }, [selectedWarehouse?.id]);

    const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));

    const start = (currentPage - 1) * pageSize;

    const paginatedPlans = filteredPlans.slice(start, start + pageSize);

    const fetchDetail = useCallback(async (id: string) => {
      try {
        setDetailLoading(true);
        setDetailError(null);

        const response = await budgetPlanService.getBudgetPlanDetail(id);

        setDetail(response);
      } catch (err) {
        setDetailError(
          err instanceof Error
            ? err.message
            : "Failed to fetch budget plan detail",
        );
      } finally {
        setDetailLoading(false);
      }
    }, []);

    const clearDetail = useCallback(() => {
      setDetail(null);
      setDetailError(null);
    }, []);

    const lastUpdatedDisplay = lastUpdated
      ? lastUpdated.toLocaleString("id-ID", {
          day: "2-digit",
          month: "2-digit",
          year: "numeric",
          hour: "2-digit",
          minute: "2-digit",
        }) + " WIB"
      : "dd/mm/yyyy, hh:mm WIB";

    const handleSearch = useCallback(
      (query: string) => setSearchQuery(query),
      [setSearchQuery],
    );

    const handleNavigatePO = useCallback(
      (item: ApprovedBudgetPlan) => {
        const existingPurchaseOrderId = item.purchaseOrders?.[0]?.id;
        const query = existingPurchaseOrderId
          ? `?purchaseOrderId=${existingPurchaseOrderId}`
          : "";

        navigate(`/generate-po/create/${item.budgetPlanId}${query}`, {
          state: {
            budgetPlan: item,
            purchaseOrderId: existingPurchaseOrderId,
          },
        });
      },
      [navigate],
    );

    const handleSort = useCallback(
      (field: SortField) => setSortField(field),
      [setSortField],
    );

   const handleOrder = useCallback(
     (direction: "asc" | "desc") => setSortDirection(direction),
     [setSortDirection],
   );

    const handlePageChange = useCallback(
      (page: number) => setCurrentPage(page),
      [setCurrentPage],
    );

    const handleRefresh = useCallback(() => {
      fetchBudgetPlans();
    }, [fetchBudgetPlans]);

    const formatDocDate = (dateStr: string): string => {
      const d = new Date(dateStr);

      const dd = String(d.getDate()).padStart(2, "0");
      const mm = String(d.getMonth() + 1).padStart(2, "0");
      const yyyy = d.getFullYear();

      return `${dd}/${mm}/${yyyy}`;
    };

    // const getPoStatusLabel = (
    //   plan: ApprovedBudgetPlan,
    // ): "Generate PO" | "Waiting SAP" | "Generated" | "-" => {
    //   if (!plan.purchaseOrderId) return "Generate PO";
    //   if (plan.sapPoNumber) return "Generated";
    //   if (plan.purchaseOrderStatus === "Draft") return "Waiting SAP";

    //   return "-";
    // };

    // const handleExport = useCallback(() => {
    //   const headers = [
    //     "Budget No",
    //     "Vendor Name",
    //     "Remark",
    //     "Maker",
    //     "RFBA",
    //     "Approval",
    //     "Doc Date",
    //     "PO No",
    //     "Status",
    //   ];

    //   const rows = filteredPlans.map((p) => [
    //     p.budgetPlanCode,
    //     p.vendorName,
    //     p.remark,
    //     p.makerName,
    //     p.hasRfbaItems ? "Yes" : "-",
    //     p.approvalName || "-",
    //     formatDocDate(p.docDate),
    //     p.purchaseOrderCode || "-",
    //     getPoStatusLabel(p),
    //   ]);

    //   const csvContent = [headers, ...rows]
    //     .map((row) => row.map((cell) => `"${cell}"`).join(","))
    //     .join("\n");

    //   const blob = new Blob([csvContent], {
    //     type: "text/csv;charset=utf-8;",
    //   });

    //   const url = URL.createObjectURL(blob);

    //   const link = document.createElement("a");

    //   link.href = url;

    //   link.download = `approved-budget-plans-${new Date()
    //     .toISOString()
    //     .slice(0, 10)}.csv`;

    //   link.click();

    //   URL.revokeObjectURL(url);
    // }, [filteredPlans]);

    return {
      budgetPlans: filteredPlans,
      paginatedPlans,

      isLoading: isLoading || detailLoading,

      error: error || detailError,

      lastUpdated,
      lastUpdatedDisplay,

      searchQuery,
      sortField,
      sortDirection,

      currentPage,
      pageSize,
      totalItems,
      totalPages,

      handleSearch,
      handleSort,
      handleOrder,
      handlePageChange,
      handleRefresh,
      // handleExport,

      // getPoStatusLabel,
      formatDocDate,

      handleNavigatePO,

      detail,
      fetchDetail,
      clearDetail,
    };
  };

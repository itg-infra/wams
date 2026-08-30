import { useEffect, useState } from "react";
import { useBudgetTemplateStore } from "../../store/budgetTemplateStore";
import type { BudgetTemplateItem } from "../../types/budgetTemplate.type";
import { createBudgetTemplateService } from "../../api/services/budgeting/budgetTemplate/createBudgetTemplateService";
import { useWarehouseStore } from "../../store/warehouseStore";

export function useBudgetTemplateController(
  onNavigate?: (pageId: string, payload?: Record<string, string>) => void,
) {
  const {
    templates,
    isLoading,
    error,
    search,

    // 1. Ambil state sort yang baru (menggantikan sortBy)
    sortBy,
    sortOrder,

    page,
    totalPages,
    total,
    from,
    to,
    lastUpdated,

    fetchTemplates,
    setSearch,

    // 2. Ambil action sort yang baru
    setSortBy,
    setSortOrder,

    setPage,
  } = useBudgetTemplateStore();

  const [searchInput, setSearchInput] = useState(search);

  const selectedWarehouse = useWarehouseStore(
    (state) => state.selectedWarehouse,
  );

  // Initial fetch saat warehouse berubah
  useEffect(() => {
    void fetchTemplates({ page: 1 });
  }, [selectedWarehouse?.id]); // Disederhanakan agar tidak infinite loop

  // Debounce search
  useEffect(() => {
    const handler = setTimeout(() => {
      if (search !== searchInput) {
        // setSearch di Store sekarang sudah otomatis panggil fetchTemplates()
        setSearch(searchInput);
      }
    }, 400);

    return () => clearTimeout(handler);
  }, [searchInput, search, setSearch]);

  const handleSearchChange = (value: string) => {
    setSearchInput(value);
  };

  // 3. Pecah handleSortChange menjadi handleSort (Field) & handleOrder (Direction)
  const handleSort = (field: string) => setSortBy(field);
  const handleOrder = (order: "asc" | "desc") => setSortOrder(order);

  // 4. Hapus pemanggilan fetchTemplates manual dari pagination
  // karena setPage di Store sudah otomatis fetch API.
  const handlePrevPage = () => {
    if (page <= 1) return;
    setPage(page - 1);
  };

  const handleNextPage = () => {
    if (page >= totalPages) return;
    setPage(page + 1);
  };

  const handlePageClick = (targetPage: number) => {
    if (targetPage === page) return;
    setPage(targetPage);
  };

  const handleView = (item: BudgetTemplateItem) => {
    onNavigate?.("budgeting.template.detail", { id: item.id });
  };

  const handleEdit = (item: BudgetTemplateItem) => {
    console.log("Edit template:", item);
  };

  const handleDelete = async (item: BudgetTemplateItem) => {
    if (!confirm(`Hapus template ${item.templateId}?`)) return;
    try {
      await createBudgetTemplateService.deleteById(item.id);
      fetchTemplates();
    } catch (err) {
      console.error(err);
      alert("Hapus gagal ❌");
    }
  };

  // 5. sortLabel dihapus karena komponen UI Toolbar yang baru (`SortDropdown`)
  // sudah otomatis mencari label berdasarkan value "asc"/"desc" dari konstanta OPTIONS.

  return {
    templates,
    isLoading,
    error,
    searchInput,

    // Export state sort yang baru
    sortBy,
    sortOrder,

    page,
    totalPages,
    total,
    from,
    to,
    lastUpdated,

    fetchTemplates,
    handleSearchChange,

    // Export handler sort yang baru ke UI
    handleSort,
    handleOrder,

    handlePrevPage,
    handleNextPage,
    handlePageClick,
    handleView,
    handleEdit,
    handleDelete,
  };
}

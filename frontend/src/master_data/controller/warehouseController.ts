import { useWarehouseStore } from "../store/warehouseStore";
import { useEffect, useState } from "react";

export function useWarehouseController() {
  const {
    warehousesTypes,
    selectedItem,
    isLoading,
    isLoadingMore,
    isDetailLoading,
    error,
    detailError,
    search,
    page,
    limit,
    total,
    totalPages,
    from,
    to,
    hasMore,
    location,
    provinceId,
    fetchWarehouse,
    fetchWarehouseDetail,
    setSearch,
    setPage,
    clearSelectedWarehouse,
  } = useWarehouseStore();

  const [searchInput, setSearchInput] = useState(search);

  // Refetch dari page 1 setiap kali filter location/provinceId berubah
  useEffect(() => {
    fetchWarehouse({
      page: 1,
      location,
      provinceId,
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [fetchWarehouse, location, provinceId]); // FIX: provinceId ditambahkan

  useEffect(() => {
    const handler = setTimeout(() => {
      fetchWarehouse({
        search: searchInput,
        page: 1,
        location,
        provinceId,
      });
    }, 400);

    return () => clearTimeout(handler);
  }, [searchInput, fetchWarehouse, location, provinceId]);

  const handleSearchChange = (value: string) => {
    setSearchInput(value);
    setSearch(value);
  };

  // Pagination model "klasik" (mis. untuk tabel dengan tombol prev/next)
  const handlePrevPage = () => {
    if (page <= 1) return;
    const nextPage = page - 1;
    setPage(nextPage);
    fetchWarehouse({
      page: nextPage,
      search: searchInput,
      limit,
      location,
      provinceId,
    });
  };

  const handleNextPage = () => {
    if (page >= totalPages) return;
    const nextPage = page + 1;
    setPage(nextPage);
    fetchWarehouse({
      page: nextPage,
      search: searchInput,
      limit,
      location,
      provinceId,
    });
  };

  // NEW: pagination model "infinite scroll" (untuk Combobox)
  const handleLoadMore = () => {
    if (isLoading || isLoadingMore || !hasMore) return;

    const nextPage = page + 1;
    setPage(nextPage);

    fetchWarehouse({
      page: nextPage,
      search: searchInput,
      limit,
      location,
      provinceId,
      append: true, // NEW: tambahkan ke list, bukan replace
    });
  };

  const handleGetDetail = async (id: string) => {
    await fetchWarehouseDetail(id);
  };

  return {
    warehousesTypes,
    selectedItem,
    isLoading,
    isLoadingMore, // NEW
    isDetailLoading,
    error,
    detailError,
    searchInput,
    page,
    total,
    totalPages,
    from,
    to,
    hasMore, // NEW
    location,

    handleSearchChange,
    handlePrevPage,
    handleNextPage,
    handleLoadMore, // NEW
    handleGetDetail,
    clearSelectedWarehouse,
    fetchWarehouse,
  };
}

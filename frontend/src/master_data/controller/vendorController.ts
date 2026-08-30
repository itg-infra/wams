import { useEffect, useState } from "react";
import { useVendorStore } from "../store/vendorStore";

export function useVendorController() {
    const {
        vendors,
        selectedVendor,
        isLoading,
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
        fetchVendors,
        fetchVendorDetail,
        setSearch,
        setPage,
        clearSelectedVendor,
    } = useVendorStore();

    const [searchInput, setSearchInput] = useState(search);

    useEffect(() => {
        fetchVendors();
    }, [fetchVendors]);

    useEffect(() => {
        const handler = setTimeout(() => {
            fetchVendors({ search: searchInput, page: 1 });
        }, 400);

        return () => clearTimeout(handler);
    }, [searchInput, fetchVendors]);

    const handleSearchChange = (value: string) => {
        setSearchInput(value);
        setSearch(value);
    };

    const handlePrevPage = () => {
        if (page <= 1) return;
        const nextPage = page - 1;
        setPage(nextPage);
        fetchVendors({ page: nextPage, search: searchInput, limit });
    };

    const handleNextPage = () => {
        if (page >= totalPages) return;
        const nextPage = page + 1;
        setPage(nextPage);
        fetchVendors({ page: nextPage, search: searchInput, limit });
    };

    const handleGetDetail = async (id: string) => {
        await fetchVendorDetail(id);
    };

    return {
        vendors,
        selectedVendor,
        isLoading,
        isDetailLoading,
        error,
        detailError,
        searchInput,
        page,
        total,
        totalPages,
        from,
        to,

        handleSearchChange,
        handlePrevPage,
        handleNextPage,
        handleGetDetail,
        clearSelectedVendor,
    };
}
import { useEffect, useState } from "react";
import { useItemStore } from "../store/itemStore";

export function useItemController() {
    const {
        items,
        selectedItem,
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
        fetchItems,
        fetchItemDetail,
        setSearch,
        setPage,
        clearSelectedItem,
    } = useItemStore();

    const [searchInput, setSearchInput] = useState(search);

    useEffect(() => {
        fetchItems();
    }, [fetchItems]);

    useEffect(() => {
        const handler = setTimeout(() => {
            fetchItems({ search: searchInput, page: 1 });
        }, 400);

        return () => clearTimeout(handler);
    }, [searchInput, fetchItems]);

    const handleSearchChange = (value: string) => {
        setSearchInput(value);
        setSearch(value);
    };

    const handlePrevPage = () => {
        if (page <= 1) return;
        const nextPage = page - 1;
        setPage(nextPage);
        fetchItems({ page: nextPage, search: searchInput, limit });
    };

    const handleNextPage = () => {
        if (page >= totalPages) return;
        const nextPage = page + 1;
        setPage(nextPage);
        fetchItems({ page: nextPage, search: searchInput, limit });
    };

    const handleGetDetail = async (id: string) => {
        await fetchItemDetail(id);
    };

    return {
        items,
        selectedItem,
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
        clearSelectedItem,
    };
}
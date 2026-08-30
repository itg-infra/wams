import { useEffect, useMemo, useState } from "react";
import { useUomStore } from "../store/unitofmeasurementStore";
import type { CreateUomPayload, UpdateUomPayload, UomItem } from "../types/unitofmeasurement.type";

export function useUomController() {
    const {
        uoms,
        isLoading,
        error,

        selectedUom,
        isDetailLoading,
        detailError,

        isCreating,
        createError,

        isUpdating,
        updateError,

        isDeleting,
        deleteError,

        search,
        page,
        limit,
        total,
        totalPages,

        fetchUoms,
        fetchUomDetail,
        createUom,
        updateUom,
        deleteUom,

        setSearch,
        setPage,
        clearSelectedUom,

        clearError,
        clearDetailError,
        clearCreateError,
        clearUpdateError,
        clearDeleteError,
    } = useUomStore();

    const [searchInput, setSearchInput] = useState(search);

    useEffect(() => {
        fetchUoms();
    }, [fetchUoms]);

    useEffect(() => {
        const handler = setTimeout(() => {
            fetchUoms({ search: searchInput, page: 1 });
        }, 400);

        return () => clearTimeout(handler);
    }, [searchInput, fetchUoms]);

    const handleSearchChange = (value: string) => {
        setSearchInput(value);
        setSearch(value);
    };

    const handlePrevPage = () => {
        if (page <= 1) return;
        const nextPage = page - 1;
        setPage(nextPage);
        fetchUoms({ page: nextPage, search: searchInput, limit });
    };

    const handleNextPage = () => {
        if (page >= totalPages) return;
        const nextPage = page + 1;
        setPage(nextPage);
        fetchUoms({ page: nextPage, search: searchInput, limit });
    };

    const handlePageClick = (targetPage: number) => {
        if (targetPage === page) return;
        setPage(targetPage);
        fetchUoms({ page: targetPage, search: searchInput, limit });
    };

    const handleRefresh = () => {
        fetchUoms({ search: searchInput, page });
    };

    const handleView = async (item: UomItem) => {
        await fetchUomDetail(item.id);
    };

    const handleCreate = async (payload: CreateUomPayload) => {
        return await createUom(payload);
    };

    const handleUpdate = async (id: number | string, payload: UpdateUomPayload) => {
        return await updateUom(id, payload);
    };

    const handleDelete = async (id: number | string) => {
        return await deleteUom(id);
    };

    const from = useMemo(() => {
        if (total === 0) return 0;
        return (page - 1) * limit + 1;
    }, [page, limit, total]);

    const to = useMemo(() => {
        if (total === 0) return 0;
        return Math.min(page * limit, total);
    }, [page, limit, total]);

    return {
        // list
        uoms,
        isLoading,
        error,

        // detail
        selectedUom,
        isDetailLoading,
        detailError,

        // mutation
        isCreating,
        createError,
        isUpdating,
        updateError,
        isDeleting,
        deleteError,

        // pagination
        searchInput,
        page,
        limit,
        total,
        totalPages,
        from,
        to,

        // handlers
        handleSearchChange,
        handlePrevPage,
        handleNextPage,
        handlePageClick,
        handleRefresh,
        handleView,
        handleCreate,
        handleUpdate,
        handleDelete,

        // utils
        clearSelectedUom,
        clearError,
        clearDetailError,
        clearCreateError,
        clearUpdateError,
        clearDeleteError,
    };
}
import { useState, useEffect, useRef, useCallback } from "react";
import { useWarehouseStore } from "../../store/warehouseStore";
import type { Warehouse } from "../../types/warehouseList.type";

export function useWarehouseController() {
    const {
        warehouses,
        selectedWarehouse,
        isLoading,
        error,
        meta,
        search,
        page,
        fetchWarehouses,
        setSelectedWarehouse,
        setSearch,
        setPage,
        clearError,
    } = useWarehouseStore();

    const [isDropdownOpen, setIsDropdownOpen] = useState(false);
    const [localSearch, setLocalSearch] = useState("");
    const triggerRef = useRef<HTMLButtonElement>(null);      // ← ref ke trigger button
    const portalRef = useRef<HTMLDivElement>(null);          // ← ref ke portal dropdown
    const searchDebounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

    useEffect(() => {
        const handleClickOutside = (e: MouseEvent) => {
            const target = e.target as Node;
            const insideTrigger = triggerRef.current?.contains(target);
            const insidePortal = portalRef.current?.contains(target);

            if (!insideTrigger && !insidePortal) {
                setIsDropdownOpen(false);
                setLocalSearch("");
            }
        };
        document.addEventListener("mousedown", handleClickOutside);
        return () => document.removeEventListener("mousedown", handleClickOutside);
    }, []);

    useEffect(() => {
        return () => {
            if (searchDebounceRef.current) clearTimeout(searchDebounceRef.current);
        };
    }, []);

    useEffect(() => {
        fetchWarehouses({ page: 1, search: "" });
    }, [fetchWarehouses]);


    const handleSearchChange = (value: string) => {
        setLocalSearch(value);
        if (searchDebounceRef.current) clearTimeout(searchDebounceRef.current);
        searchDebounceRef.current = setTimeout(() => {
            setSearch(value);
            fetchWarehouses({ page: 1, search: value });
        }, 400);
    };

    const handleSelectWarehouse = (warehouse: Warehouse) => {
        setSelectedWarehouse(warehouse);
        setIsDropdownOpen(false);
        setLocalSearch("");
    };

    const handleToggleDropdown = () => {
        if (!isDropdownOpen) {
            fetchWarehouses({ page: 1, search: "" });
            setLocalSearch("");
        }
        setIsDropdownOpen((prev) => !prev);
    };

    const handleLoadMore = useCallback(() => {
        if (meta && page < meta.totalPages) {
            const nextPage = page + 1;
            setPage(nextPage);
            fetchWarehouses({ page: nextPage, search });
        }
    }, [meta, page, search, setPage, fetchWarehouses]);

    const hasMore = meta ? page < meta.totalPages : false;

    return {
        warehouses,
        selectedWarehouse,
        isLoading,
        error,
        isDropdownOpen,
        localSearch,
        hasMore,

        triggerRef,   
        portalRef,    

        handleToggleDropdown,
        handleSelectWarehouse,
        handleSearchChange,
        handleLoadMore,
        clearError,
    };
}
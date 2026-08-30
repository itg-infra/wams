import { useCallback, useEffect, useRef, useState } from "react";
import { useRoleStore } from "../../store/roleStore";
import type { RoleSortBy } from "../../types/role.type";

export function useRoleController() {
  const {
    roles,
    meta,
    isLoading,
    error,
    params,
    isDeleting,
    deleteError,
    isUpdating,
    updateError,
    selectedRole,
    fetchRoleDetail,
    isFetchingDetail,
    detailError,
    fetchRoles,
    setParams,
    deleteRole,
    clearDeleteError,
    updateRole,
    clearUpdateError,
    assignPermissionToRole,
    deletePermissionFromRole,
    isAssigning,
    isDeletingPermission,
  } = useRoleStore();

  const handleRefresh = useCallback(() => {
    fetchRoles();
  }, [fetchRoles]);

  // `setParams` alone only updates state — the list effect depends on the stable
  // `fetchRoles`, so it never re-runs and the typed keyword never reached the
  // API. Fetching on every keystroke fixes that but fires one request per
  // character, and the responses can land out of order, leaving the list showing
  // the result of a stale prefix. Debounce it, the way budgetPlanController does.
  const [searchInput, setSearchInput] = useState(params.search ?? "");
  const fetchRolesRef = useRef(fetchRoles);
  fetchRolesRef.current = fetchRoles;
  const didMount = useRef(false);

  useEffect(() => {
    if (!didMount.current) {
      didMount.current = true; // the list effect already did the first load
      return;
    }
    const handler = setTimeout(() => {
      void fetchRolesRef.current({ search: searchInput, page: 1 });
    }, 400);
    return () => clearTimeout(handler);
  }, [searchInput]);

  const handleSearch = useCallback(
    (value: string) => {
      setSearchInput(value);
      setParams({
        ...params,
        search: value,
        page: 1,
      });
    },
    [params, setParams],
  );

  /**
   * The list is paginated by the API, so ordering is sent to the server —
   * sorting the rows already fetched would only reorder the current page.
   */
  const handleSortChange = useCallback(
    (sortBy: RoleSortBy, sortOrder: "asc" | "desc") => {
      fetchRoles({ sortBy, sortOrder, page: 1 });
    },
    [fetchRoles],
  );

  const handleNextPage = useCallback(() => {
    if (!meta) return;
    if (meta.page < meta.totalPages) {
      const newPage = meta.page + 1;
      setParams({ ...params, page: newPage });
      fetchRoles({ ...params, page: newPage });
    }
  }, [meta, params, setParams, fetchRoles]);

  const handlePrevPage = useCallback(() => {
    if (!meta) return;
    if (meta.page > 1) {
      const newPage = meta.page - 1;
      setParams({ ...params, page: newPage });
      fetchRoles({ ...params, page: newPage });
    }
  }, [meta, params, setParams, fetchRoles]);

  const handleGoToPage = useCallback(
    (page: number) => {
      if (!meta) return;
      if (page < 1 || page > meta.totalPages) return;

      setParams({ ...params, page });
      fetchRoles({ ...params, page });
    },
    [meta, params, setParams, fetchRoles],
  );
  const hasNextPage = meta ? meta.page < meta.totalPages : false;
  const hasPrevPage = meta ? meta.page > 1 : false;
  const isEmpty = !isLoading && roles.length === 0;

  return {
    roles,
    meta,
    isLoading,
    error,
    params,

    isDeleting,
    deleteError,

    isUpdating,
    updateError,

    isEmpty,
    hasNextPage,
    hasPrevPage,

    selectedRole,
    fetchRoleDetail,
    isFetchingDetail,
    detailError,

    fetchRoles,
    deleteRole,
    updateRole,

    handleRefresh,
    handleSearch,
    handleSortChange,
    handleNextPage,
    handlePrevPage,
    handleGoToPage,

    clearDeleteError,
    clearUpdateError,

    assignPermissionToRole,
    deletePermissionFromRole,
    isAssigning,
    isDeletingPermission,
  };
}

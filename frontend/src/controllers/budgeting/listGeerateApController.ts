import { useCallback } from "react";

import { confirmationDialog } from "../../components/confirmationDelet";

import { useAccountPayableStore } from "../../store/listGenerateApStore";

export const useAccountPayableController = () => {
  const {
    // ======================================================
    // LIST
    // ======================================================

    accountPayables,

    isLoading,

    error,

    fetchAccountPayables,

    // --- TAMBAHAN SORTING ---
    sortBy,

    sortOrder,

    setSortBy,

    setSortOrder,

    // ======================================================
    // DETAIL
    // ======================================================

    selectedAccountPayable,

    isDetailLoading,

    fetchAccountPayableDetail,

    clearSelectedAccountPayable,

    // ======================================================
    // DELETE
    // ======================================================

    deleteLoadingId,

    setDeleteLoadingId,

    deleteAccountPayable,

    // ======================================================
    // ERROR
    // ======================================================

    errorMessage,

    setErrorMessage,
  } = useAccountPayableStore();

  // ======================================================
  // SORTING HANDLERS
  // ======================================================

  const handleSortChange = useCallback(
    (value: "status" | "docDate" | "createdAt") => {
      setSortBy(value);
    },
    [setSortBy],
  );

  const handleOrderChange = useCallback(
    (value: "asc" | "desc") => {
      setSortOrder(value);
    },
    [setSortOrder],
  );

  // ======================================================
  // DELETE
  // ======================================================

  const handleDeleteAccountPayable = useCallback(
    async (id: number) => {
      const confirmed = await confirmationDialog({
        title: "Delete Account Payable?",

        text: "This action cannot be undone.",

        confirmText: "Delete",
      });

      if (!confirmed) {
        return;
      }

      try {
        setErrorMessage(null);

        setDeleteLoadingId(id);

        await deleteAccountPayable(id);

        await fetchAccountPayables();
      } catch (error) {
        if (error instanceof Error) {
          setErrorMessage(error.message);
        } else {
          setErrorMessage("Failed delete account payable");
        }
      } finally {
        setDeleteLoadingId(null);
      }
    },
    [
      deleteAccountPayable,

      fetchAccountPayables,

      setDeleteLoadingId,

      setErrorMessage,
    ],
  );

  // ======================================================
  // DETAIL
  // ======================================================

  const handleGetDetail = useCallback(
    async (id: number) => {
      await fetchAccountPayableDetail(id);
    },
    [fetchAccountPayableDetail],
  );

  const clearError = () => {
    setErrorMessage("");
  };

  return {
    // ======================================================
    // LIST
    // ======================================================

    accountPayables,

    isLoading,

    error,

    fetchAccountPayables,

    // --- EXPORT SORTING ---
    sortBy,

    sortOrder,

    handleSortChange,

    handleOrderChange,

    // ======================================================
    // DETAIL
    // ======================================================

    selectedAccountPayable,

    isDetailLoading,

    fetchAccountPayableDetail,

    clearSelectedAccountPayable,

    handleGetDetail,

    // ======================================================
    // DELETE
    // ======================================================

    handleDeleteAccountPayable,

    deleteLoadingId,

    // ======================================================
    // ERROR
    // ======================================================

    errorMessage,

    clearError,
  };
};

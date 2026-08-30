import { useCallback, useEffect } from "react";
import { budgetPlanDetailService } from "../../api/services/budgeting/budgetPlan/budgetPlanDetailService";
import { useBudgetPlanDetailStore } from "../../store/budgetPlanDetailStore";

export function useBudgetPlanDetailController(id: string) {
    const {
        detail,
        isLoading,
        isApproving,
        isRejecting,
        error,
        approveError,
        rejectError,
        setDetail,
        setLoading,
        setApproving,
        setRejecting,
        setError,
        setApproveError,
        setRejectError,
        reset,
    } = useBudgetPlanDetailStore();

    const fetchDetail = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const result = await budgetPlanDetailService.getBudgetPlanDetail(id);
            setDetail(result);
        } catch (err) {
            const message =
                err instanceof Error ? err.message : "Gagal memuat detail budget plan.";
            setError(message);
        } finally {
            setLoading(false);
        }
    }, [id, setDetail, setError, setLoading]);

    useEffect(() => {
        fetchDetail();
        return () => {
            reset();
        };
    }, [id]); // eslint-disable-line react-hooks/exhaustive-deps

    const handleApprove = useCallback(async (): Promise<boolean> => {
        setApproving(true);
        setApproveError(null);
        try {
            await budgetPlanDetailService.approveBudgetPlan(id);
            await fetchDetail();
            return true;
        } catch (err) {
            const message =
                err instanceof Error ? err.message : "Gagal menyetujui budget plan.";
            setApproveError(message);
            return false;
        } finally {
            setApproving(false);
        }
    }, [id, fetchDetail, setApproving, setApproveError]);

    const handleReject = useCallback(
        async (notes: string): Promise<boolean> => {
            setRejecting(true);
            setRejectError(null);
            try {
                await budgetPlanDetailService.rejectBudgetPlan(id, { reason: notes });
                await fetchDetail();
                return true;
            } catch (err) {
                const message =
                    err instanceof Error ? err.message : "Gagal menolak budget plan.";
                setRejectError(message);
                return false;
            } finally {
                setRejecting(false);
            }
        },
        [id, fetchDetail, setRejecting, setRejectError]
    );

    return {
        detail,
        isLoading,
        isApproving,
        isRejecting,
        error,
        approveError,
        rejectError,
        fetchDetail,
        handleApprove,
        handleReject,
    };
}
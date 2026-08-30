import { useCallback } from "react";

import { useRealizationRecapDetailStore } from "../../store/detailRecapWoStore";
import { useNavigate } from "react-router-dom";
import {
  realizationRecapDetailService,
  type RejectRecapPayload,
} from "../../api/services/operationalRealization/detailRecapWoService";
import toast from "react-hot-toast";

export const useRealizationRecapDetailController = () => {
  const {
    detail,
    isLoading,
    error,
    fetchDetail,
    clear,
    fetchRevisionbyRecap,
    revisionByRecap,
  } = useRealizationRecapDetailStore();

  const navigate = useNavigate();

  const handleFetchRevisionByRecap = useCallback(
    async (id: number) => {
      await fetchRevisionbyRecap(id);
    },
    [fetchRevisionbyRecap],
  );

  const handleFetchDetail = useCallback(
    async (id: number) => {
      await fetchDetail(id);
    },
    [fetchDetail],
  );

  const handleApprove = useCallback(
    async (id: number) => {
      try {
        await realizationRecapDetailService.approvedRecap(id);

        toast.success("Success Approved Recap Wo");
        navigate(-1);

        return {
          success: true,
        };
      } catch (error: any) {
        console.error("Approve error:", error);
        console.error("Response:", error.response);

        const message =
          error.response?.data?.message ?? error.message ?? "Approve gagal";

        if (error.response?.status === 422) {
          toast.error(message);

          return {
            success: false,
            isThresholdError: true,
            message,
          };
        }

        toast.error(message);

        return {
          success: false,
          message,
        };
      }
    },
    [navigate],
  );

  const handleReject = useCallback(
    async (id: number, reason: RejectRecapPayload) => {
      try {
        await realizationRecapDetailService.rejectRecap(id, reason);
        toast.success("Success Reject Recap Wo");

        navigate(-1);
      } catch (error) {
        toast.error(String(error));

        console.error(error);
      }
    },
    [navigate],
  );

  const handleApprovedRevision = useCallback(
    async (id: number) => {
      try {
        await realizationRecapDetailService.approvedRevision(id);
        toast.success("Success Approved Revision Recap Wo");

        navigate(-1);
      } catch (error) {
        toast.error(String(error));

        console.error(error);
      }
    },
    [navigate],
  );

  const handleRejectRevision = useCallback(
    async (id: number, reason: RejectRecapPayload) => {
      try {
        await realizationRecapDetailService.rejectRevision(id, reason);
        toast.success("Success Reject Recap Wo");

        navigate(-1);
      } catch (error) {
        toast.error(String(error));

        console.error(error);
      }
    },
    [navigate],
  );

  return {
    detail,

    plan: detail?.plan ?? null,

    realization: detail?.realization ?? null,

    isLoading,

    error,

    fetchDetail: handleFetchDetail,

    fetchRevision: handleFetchRevisionByRecap,

    revisionByRecap,

    clear,

    approveRecap: handleApprove,

    rejectRecap: handleReject,

    rejectRevision: handleRejectRevision,

    approvedRevision: handleApprovedRevision,

    handleApprove,
  };
};

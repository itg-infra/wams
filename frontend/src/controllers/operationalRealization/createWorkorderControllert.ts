// controller/workOrder.controller.ts

// import { useNavigate } from "react-router-dom";

import { workOrderService } from "../../api/services/operationalRealization/workOrderService";

import type { CreateWorkOrderPayload } from "../../types/createWorkOrder.type";
import { useWorkOrderStore } from "../../store/createWorkOrderStore";
import type { EditWorkOrderPayload } from "../../types/editWo.type";
import { useCallback } from "react";

export const useWorkOrderController = () => {
  // const navigate = useNavigate();

  const { isDrafting, setIsDrafting } = useWorkOrderStore();

  const {
    isSubmitting,
    setIsSubmitting,
    errorDrafting,
    seterrorDrafting,
    errorSubmitting,
    seterrorSubmitting,

    pics,
    isLoading,
    error,
    setPics,
    setIsLoading,
    setError,
    resetStore,
  } = useWorkOrderStore();

const fetchWOPICs = useCallback(
  async (woID: string | number) => {
    if (!woID) return;

    setIsLoading(true);
    setError(null);

    try {
      const response = await workOrderService.woPic(woID);

      if (response.success) {
        setPics(response.data);
      } else {
        // Fallback jika API mengembalikan success: false (tergantung standar backend kamu)
        setError(response.message || "Failed to fetch PIC candidates.");
        setPics([]);
      }
    } catch (err: any) {
      const errorMessage =
        err?.response?.data?.message ||
        err?.message ||
        "An unexpected error occurred";
      setError(errorMessage);
      setPics([]);
    } finally {
      setIsLoading(false);
    }
  },
  [setPics, setIsLoading, setError],
);


  const submitWorkOrder = async (
    id: number,
    payload: CreateWorkOrderPayload,
  ) => {
    try {
      setIsSubmitting(true);
      seterrorSubmitting("");

      const response = await workOrderService.createWorkOrder(id, payload);

      return response; // ← return dulu, jangan navigate di sini
    } catch (error) {
      console.error("Failed submit work order:", error);
      seterrorSubmitting("Failed Submit WO");
      throw error;
    } finally {
      setIsSubmitting(false);
    }
  };

  const submitDraftWorkOrder = async (id: number) => {
    try {
      setIsSubmitting(true);
      seterrorDrafting("");

      const response = await workOrderService.submitDraftWorkOrder(id);

      return response;
    } catch (error) {
      console.error("Failed submit work order:", error);
      seterrorDrafting("Failed Submit Draft WO");
      throw error;
    } finally {
      setIsSubmitting(false);
    }
  };

  const draftWorkOrder = async (
    id: number,
    payload: CreateWorkOrderPayload,
  ) => {
    try {
      setIsDrafting(true);
      seterrorDrafting("");

      const response = await workOrderService.draftWorkOrder(id, payload);

      return response;
    } catch (error) {
      console.error("Failed submit work order:", error);
      seterrorDrafting("Failed Create Draft WO");

      throw error;
    } finally {
      setIsDrafting(false);
    }
  };

  const editWorkOrder = async (id: number, payload: EditWorkOrderPayload) => {
    try {
      setIsDrafting(true);
      seterrorDrafting("");

      const response = await workOrderService.editWorkOrder(id, payload);

      return response;
    } catch (error) {
      console.error("Failed submit work order:", error);
      seterrorDrafting("Failed Create Draft WO");

      throw error;
    } finally {
      setIsDrafting(false);
    }
  };

  return {
    isSubmitting,
    isDrafting,
    submitWorkOrder,
    editWorkOrder,
    draftWorkOrder,
    submitDraftWorkOrder,
    errorDrafting,
    errorSubmitting,

    pics,
    isLoading,
    error,
    // Actions
    fetchWOPICs,
    resetStore,
  };
};

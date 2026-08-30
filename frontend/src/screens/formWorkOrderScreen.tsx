import { useCallback, useEffect, useRef, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";

import { useTransportOrderController } from "../master_data/controller/transportController";

import type { RealizationApprovedBpApiItem } from "../types/realizationApprovedBp.type";
import type { TransportOrder } from "../master_data/types/transport.types";

import { useWorkOrderController } from "../controllers/operationalRealization/createWorkorderControllert";
import { ACTIVITY_CONFIG, type FieldConfig } from "../config/activityWoConfig";
import {
  createEmptyRow,
  type WorkOrderRow,
} from "../config/workOrderRowConfig";
import { PICDropdown } from "../components/picDropDown";
// import type { User } from "../types/users.types";
import { useFileUploadController } from "../controllers/file/fileUploadController";
import toast from "react-hot-toast";
// import { useWorkOrderStore } from "../store/detailWoStore";
import { mapWorkOrderDetailToRows } from "../types/workOrderMapper";
import { workOrderController } from "../controllers/operationalRealization/detailWoController";
import { useFileController } from "../controllers/file/fileController";
import { toInputDateFormat } from "../components/format/dateTimeFormat";
import { PageHeader } from "../components/ui/page-header";
import { Button } from "../components/ui/button";
import type { WOPIC } from "../types/woPic";

type LocationState = {
  budgetPlan: RealizationApprovedBpApiItem;
};

export default function FormWorkOrderScreen() {
  const location = useLocation();

  const token = localStorage.getItem("token");

  const state = location.state as LocationState;
  const budgetPlan = state?.budgetPlan;

  const LIMIT = 10;
  const [page, setPage] = useState(1);
  const [isFetchingMore, setIsFetchingMore] = useState(false);
  const scrollContainerRef = useRef<HTMLDivElement>(null);

  const {
    transportOrders,
    meta,
    isLoading: isLoadingTransportOrders,
    loadTransportOrders,
    reset,
  } = useTransportOrderController();

  // ================= MODAL =================
  const [isModalOpen, setIsModalOpen] = useState(false);

  // load halaman pertama saat modal dibuka
  useEffect(() => {
    if (!isModalOpen) return;

    reset();
    setPage(1);
    loadTransportOrders(
      { budgetPlanId: budgetPlan.budgetPlanId, page: 1, limit: LIMIT },
      false,
    );
  }, [isModalOpen]);

  const hasMore = meta ? page < meta.totalPages : false;

  const handleScroll = useCallback(() => {
    const el = scrollContainerRef.current;
    if (!el || isFetchingMore || isLoadingTransportOrders || !hasMore) return;

    const { scrollTop, scrollHeight, clientHeight } = el;
    const reachedBottom = scrollHeight - scrollTop - clientHeight < 100; // trigger 100px sebelum mentok

    if (reachedBottom) {
      const nextPage = page + 1;
      setIsFetchingMore(true);
      setPage(nextPage);
      loadTransportOrders(
        { budgetPlanId: budgetPlan.budgetPlanId, page: nextPage, limit: LIMIT },
        true, // append
      ).finally(() => setIsFetchingMore(false));
    }
  }, [page, hasMore, isFetchingMore, isLoadingTransportOrders]);

  const {
    submitWorkOrder,
    isSubmitting,
    isDrafting,
    draftWorkOrder,
    fetchWOPICs,
  } = useWorkOrderController();

  const [picUser, setPicUser] = useState<WOPIC | null>(null);

  // const { getDetail } = useWorkOrderStore();

  const { files: existingFiles, getWorkOrderFiles } = useFileController();

  const [previewImage, setPreviewImage] = useState<{
    src: string;
    alt: string;
  } | null>(null);

  const [selectedActivity, setSelectedActivity] = useState<
    RealizationApprovedBpApiItem["activities"][0] | null
  >(null);

  const workOrderCode = selectedActivity?.workOrderCode ?? null;

  const workOrderId = selectedActivity?.workOrderId;

  const [startDate, setStartDate] = useState<string>("");
  const [endDate, setEndDate] = useState<string>("");

  const [rows, setRows] = useState<WorkOrderRow[]>([]);
  const [notes, setNotes] = useState<string>("");

  useEffect(() => {
    if (workOrderId) {
      getWorkOrderFiles(workOrderId).then((res) => {
        console.log("existing files response:", res);
      });
    }
  }, [workOrderId, getWorkOrderFiles]);

  useEffect(() => {
    if (workOrderId) {
      fetchWOPICs(workOrderId);
    }
  }, [workOrderId, fetchWOPICs]);

  useEffect(() => {
    if (!selectedActivity) {
      if (budgetPlan.activities.length > 0) {
        setSelectedActivity(budgetPlan.activities[0]);
      }
      return;
    }

    const code = selectedActivity.activityTypeCode;
    const isTransport = code === "K.BONGKAR" || code === "K.MUAT";
    const woId = selectedActivity.workOrderId;

    if (woId === null) return;

    let cancelled = false;

    const fetchDetail = async () => {
      try {
        const res = await workOrderController.getDetail(Number(woId));
        if (cancelled) return;

        setPicUser({ id: res.data.picUserId, fullname: res.data.picName });
        setStartDate(res.data.startDate);
        setEndDate(res.data.endDate);
        setNotes(res.data.notes);

        const mappedRows = mapWorkOrderDetailToRows(res.data);

        if (isTransport) {
          setRows(mappedRows.length > 0 ? mappedRows : []);
        } else {
          setRows(
            mappedRows.length > 0
              ? mappedRows
              : [
                  {
                    ...createEmptyRow(++idCounter.current),
                    source: "budgetPlan",
                  },
                ],
          );
        }
      } catch (error) {
        console.error("Gagal mengambil detail work order:", error);
      }
    };

    fetchDetail();

    return () => {
      cancelled = true;
    };
  }, [selectedActivity, budgetPlan.activities]);

  // useEffect(() => {
  //   if (!selectedActivity) {
  //     if (budgetPlan.activities.length > 0) {
  //       setSelectedActivity(budgetPlan.activities[0]);
  //     }
  //     return;
  //   }

  //   const code = selectedActivity.activityTypeCode;

  //   const isTransport = code === "K.BONGKAR" || code === "K.MUAT";

  //   if (selectedActivity.workOrderId === null) return;

  //   const fetchDetail = async () => {
  //     try {
  //       const res = await workOrderController.getDetail(
  //         Number(selectedActivity.workOrderId),
  //       );

  //       setPicUser({
  //         id: res.data.picUserId,
  //         fullname: res.data.picName,
  //         // email: "",
  //         // employeeId: null,
  //         // isActive: true,
  //         // createdAt: "",
  //         // roles: [],
  //         // warehouses: [],
  //       });

  //       setStartDate(res.data.startDate);
  //       setEndDate(res.data.endDate);
  //       setNotes(res.data.notes);

  //       console.log(`pic user: ${picUser?.fullname}`);

  //       console.log(`start date: ${startDate} - end date: ${endDate}`);

  //       const mappedRows = mapWorkOrderDetailToRows(res.data);
  //       setRows(mappedRows);
  //     } catch (error) {
  //       console.error("Gagal mengambil detail work order:", error);
  //     }
  //   };

  //   fetchDetail();

  //   // transport activity tunggu pilih TO
  //   if (isTransport) {
  //     setRows([]);
  //     return;
  //   }

  //   // non transport auto generate 1 row
  //   setRows([
  //     {
  //       id: ++idCounter.current,

  //       source: "budgetPlan",

  //       blNumber: "",
  //       productName: "",
  //       quantity: 0,
  //       uomCode: "",

  //       noVehicle: "",
  //       noContainer: "",
  //       noSeal: "",

  //       grossWeight: 0,
  //       finalWeight: 0,
  //       nettWeight: 0,

  //       totalBag: 0,
  //       unitWeight: 0,

  //       fumiId: "",
  //       totalDuration: "",
  //       mvName: "",

  //       initialTemperature: 0,
  //       fumigationType: "",
  //       finalTemperature: 0,

  //       methylBromideDosage: 0,
  //       sulphurFluorideDosage: 0,
  //       phosphineDosage: 0,

  //       result: "",

  //       moisturePercent: 0,
  //       jamurPercent: 0,
  //       bauPercent: 0,
  //       qualityStatus: "",

  //       hasPindahStapel: false,
  //       hasPembersihan: false,
  //       hasPerapihan: false,

  //       volumeWeight: 0,
  //       workerOnDuty: 0,

  //       hasMask: false,
  //       hasSafetyGlasses: false,
  //       hasHandGloves: false,
  //       hasHelmet: false,
  //       hasSafetyShoes: false,
  //       hasSafetyVest: false,

  //       receiver: "",
  //       initialWeight: 0,
  //       totalWeight: 0,

  //       startTime: "",
  //       endTime: "",
  //       standbyDuration1: "",
  //       standbyDuration2: "",
  //       minimumDuration: "",
  //       costPerHour: 0,
  //       totalCost: 0,

  //       isChecked: false,
  //       sortOrder: 1,
  //     },
  //   ]);
  // }, [
  //   budgetPlan.activities,
  //   getDetail,
  //   getWorkOrderFiles,
  //   selectedActivity,
  // ]);

  const { uploadFiles, isUploading } = useFileUploadController();

  // ================= FILE =================
  const MAX_FILES = 5;

  const [docFiles, setDocFiles] = useState<File[]>([]);
  const [docPreviews, setDocPreviews] = useState<string[]>([]);

  const navigate = useNavigate();

  type WorkOrderStatus = "Draft" | "Submitted" | "Approved" | null | undefined;

  const getStatusStyle = (status: WorkOrderStatus) => {
    switch (status) {
      case "Draft":
        return "bg-blue-50 text-blue-700 border-blue-200";
      case "Submitted":
        return "bg-green-50 text-green-700 border-green-200";
      case "Approved":
        return "bg-amber-50 text-amber-700 border-amber-200";
      default:
        return "bg-gray-50 text-gray-700 border-gray-200";
    }
  };

  const handleCreateWo = async () => {
    try {
      const location = await getCurrentLocation();

      const payload = buildPayload({
        latitude: location.latitude,
        longitude: location.longitude,
        accuracy: location.accuracy,
        recordedAt: new Date().toISOString(),
      });

      console.log(workOrderId);

      console.log(payload);

      if (workOrderId) {
        const responseDraft = await draftWorkOrder(workOrderId, payload);

        const workOrderIdDraft = responseDraft.data.id;

        if (docFiles.length > 0) {
          await uploadFiles(workOrderIdDraft, docFiles);
        }

        const response = await submitWorkOrder(workOrderIdDraft, payload);

        toast.success(response.message);

        navigate(-1); // ← navigate setelah semua selesai
      }
    } catch (error) {
      console.error(error);
      toast.error("Failed Submit WO");
    }
  };

  const handleDraftWo = async () => {
    try {
      const location = await getCurrentLocation();

      const payload = buildPayload({
        latitude: location.latitude,
        longitude: location.longitude,
        accuracy: location.accuracy,
        recordedAt: new Date().toISOString(),
      });

      console.log(payload);

      if (workOrderId) {
        const response = await draftWorkOrder(workOrderId, payload);

        const workOrderIdDraft = response.data.id;

        if (docFiles.length > 0) {
          await uploadFiles(workOrderIdDraft, docFiles);
        }

        navigate(-1); // ← navigate setelah semua selesai
      }
    } catch (error) {
      console.error(error);
      toast.error("Failed Create Draft PO");
    }
  };

  const handleDocUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files || []);

    if (!files.length) return;

    const remaining = MAX_FILES - docFiles.length;

    const allowed = files.slice(0, remaining);

    setDocFiles((prev) => [...prev, ...allowed]);

    allowed.forEach((file) => {
      const reader = new FileReader();

      reader.onload = () => {
        setDocPreviews((prev) => [...prev, reader.result as string]);
      };

      reader.readAsDataURL(file);
    });
  };

  const removeDoc = (index: number) => {
    setDocFiles((prev) => prev.filter((_, i) => i !== index));

    setDocPreviews((prev) => prev.filter((_, i) => i !== index));
  };

  const toggleCheck = (id: number) => {
    setRows((prev) =>
      prev.map((row) =>
        row.id === id
          ? {
              ...row,
              isChecked: !row.isChecked,
            }
          : row,
      ),
    );
  };

  // ================= TO =================
  const [selectedTransportOrderIds, setSelectedTransportOrderIds] = useState<
    number[]
  >([]);

  const [tags, setTags] = useState<
    {
      id: number;
      label: string;
    }[]
  >([]);

  const [, setAllItems] = useState<TransportOrder[]>([]);

  // const [selectedRowIds, setSelectedRowIds] = useState<number[]>([]);

  const removeTag = (id: number) => {
    setTags((prev) => prev.filter((tag) => tag.id !== id));

    setSelectedTransportOrderIds((prev) =>
      prev.filter((itemId) => itemId !== id),
    );

    setAllItems((prev) => prev.filter((item) => item.id !== id));

    // setSelectedRowIds((prev) => prev.filter((rowId) => rowId !== id));

    setRows((prev) => prev.filter((row) => row.id !== id));
  };

  const handleRowChange = (
    id: number,
    field: keyof WorkOrderRow,
    value: string | number | boolean | null,
  ) => {
    setRows((prev) =>
      prev.map((row) =>
        row.id === id
          ? {
              ...row,
              [field]: value,
            }
          : row,
      ),
    );
  };

  const toggleTransportOrder = (transportOrder: TransportOrder) => {
    const exists = selectedTransportOrderIds.includes(transportOrder.id);

    // REMOVE
    if (exists) {
      setSelectedTransportOrderIds((prev) =>
        prev.filter((itemId) => itemId !== transportOrder.id),
      );

      setTags((prevTags) =>
        prevTags.filter((tag) => tag.id !== transportOrder.id),
      );

      setAllItems((prev) =>
        prev.filter((item) => item.id !== transportOrder.id),
      );

      return;
    }

    // ADD
    setSelectedTransportOrderIds((prev) => [...prev, transportOrder.id]);

    setTags((prevTags) => [
      ...prevTags,
      {
        id: transportOrder.id,
        label: transportOrder.docNo,
      },
    ]);

    setRows((prev) => {
      const exists = prev.some((x) => x.id === transportOrder.id);

      if (exists) return prev;

      return [
        ...prev,
        {
          ...createEmptyRow(transportOrder.id),

          blNumber: transportOrder.blNo || "",
          productName: transportOrder.itemName || "",
          quantity: transportOrder.quantity || 0,
          uomCode: transportOrder.uoM || "",

          noVehicle: transportOrder.vehicleNo || "",
          // noContainer: transportOrder.containerNo || "",
          // noSeal: transportOrder.sealNo || "",

          sortOrder: prev.length + 1,
        },
      ];
    });
  };

  useEffect(() => {
    console.log("Selected Transport Orders:", selectedTransportOrderIds);
  }, [selectedTransportOrderIds]);

  const idCounter = useRef(0);

  const hasFiles = docPreviews.length > 0 || existingFiles.length > 0;

  const handleStartDate = (value: string) => {
    const formatted = new Date(value).toISOString();
    setStartDate(formatted);
  };

  const handleEndDate = (value: string) => {
    const formatted = new Date(value).toISOString();
    setEndDate(formatted);
  };

  const getCurrentLocation = (): Promise<{
    latitude: number;
    longitude: number;
    accuracy: number;
  }> => {
    return new Promise((resolve, reject) => {
      if (!navigator.geolocation) {
        reject(new Error("Browser tidak mendukung Geolocation API."));
        return;
      }

      navigator.geolocation.getCurrentPosition(
        (position) => {
          console.log("GPS berhasil:", {
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
            accuracy: position.coords.accuracy,
          });

          resolve({
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
            accuracy: position.coords.accuracy,
          });
        },
        (error) => {
          console.error("GPS ERROR:", {
            code: error.code,
            message: error.message,
            PERMISSION_DENIED: error.PERMISSION_DENIED,
            POSITION_UNAVAILABLE: error.POSITION_UNAVAILABLE,
            TIMEOUT: error.TIMEOUT,
          });

          let message = "Gagal mendapatkan lokasi.";

          switch (error.code) {
            case error.PERMISSION_DENIED:
              message = "Izin lokasi ditolak oleh browser.";
              break;

            case error.POSITION_UNAVAILABLE:
              message =
                "Lokasi tidak tersedia. Pastikan GPS/location device aktif.";
              break;

            case error.TIMEOUT:
              message = "Gagal mendapatkan lokasi karena timeout.";
              break;
          }

          reject(new Error(message));
        },
        {
          enableHighAccuracy: true,
          timeout: 30000,
          maximumAge: 0,
        },
      );
    });
  };

  const buildPayload = (gpsLocation: {
    latitude: number;
    longitude: number;
    accuracy: number;
    recordedAt: string;
  }) => {
    const basePayload = {
      budgetPlanId: budgetPlan.budgetPlanId,
      budgetPlanItemId: selectedActivity?.budgetPlanItemId,
      picUserId: picUser?.id,

      startDate,
      endDate,

      codeBlock: "A3-01",
      notes: notes || null,
      // gpsLocation: {
      //   latitude: -6.1077,
      //   longitude: 106.8811,
      //   accuracy: 12.5,
      //   recordedAt: "2026-05-26T07:30:00Z",
      // },
      gpsLocation: gpsLocation,
    };

    const row = rows[0];

    // ================= BONGKAR =================
    if (selectedActivity?.activityTypeCode === "K.BONGKAR") {
      return {
        ...basePayload,

        unloadingItems: rows.map((row, index) => ({
          blNumber: row.blNumber,
          productName: row.productName,

          quantity: Number(row.quantity),
          uomCode: row.uomCode,

          noVehicle: row.noVehicle,
          noContainer: row.noContainer,
          noSeal: row.noSeal,

          grossWeight: Number(row.grossWeight),
          finalWeight: Number(row.finalWeight),
          nettWeight: Number(row.nettWeight),

          totalBag: Number(row.totalBag),
          unitWeight: Number(row.unitWeight),

          isChecked: row.isChecked,
          sortOrder: index + 1,
        })),
      };
    }

    // ================= MUAT =================
    if (selectedActivity?.activityTypeCode === "K.MUAT") {
      return {
        ...basePayload,

        loadingItems: rows.map((row, index) => ({
          // spkShadowId: null,

          blNumber: row.blNumber,
          productName: row.productName,

          quantity: Number(row.quantity),
          uomCode: row.uomCode,

          noVehicle: row.noVehicle,
          noContainer: row.noContainer,
          noSeal: row.noSeal,

          grossWeight: Number(row.grossWeight),
          finalWeight: Number(row.finalWeight),
          nettWeight: Number(row.nettWeight),

          totalBag: Number(row.totalBag),
          unitWeight: Number(row.unitWeight),

          isChecked: row.isChecked,
          sortOrder: index + 1,
        })),
      };
    }

    // ================= FUMIGASI =================
    if (selectedActivity?.activityTypeCode === "FUMIGASI") {
      return {
        ...basePayload,

        fumigation: {
          fumiId: row?.fumiId || "",
          totalDuration: row?.totalDuration || "",

          blNumber: row?.blNumber || "",

          mvName: row?.mvName || "",

          initialTemperature: Number(row?.initialTemperature || 0),
          finalTemperature: Number(row?.finalTemperature || 0),

          fumigationType: row?.fumigationType || "",

          methylBromideDosage: row?.methylBromideDosage ?? null,
          sulphurFluorideDosage: row?.sulphurFluorideDosage ?? null,
          phosphineDosage: Number(row?.phosphineDosage || 0),

          result: row?.result || "",
        },
      };
    }

    // ================= K.GUDANG =================
    if (selectedActivity?.activityTypeCode === "K.GUDANG") {
      return {
        ...basePayload,

        storage: {
          hasPindahStapel: row?.hasPindahStapel ?? false,
          hasPembersihan: row?.hasPembersihan ?? false,
          hasPerapihan: row?.hasPerapihan ?? false,

          volumeWeight: Number(row?.volumeWeight || 0),
          workerOnDuty: Number(row?.workerOnDuty || 0),

          hasMask: row?.hasMask ?? false,
          hasSafetyGlasses: row?.hasSafetyGlasses ?? false,
          hasHandGloves: row?.hasHandGloves ?? false,
          hasHelmet: row?.hasHelmet ?? false,
          hasSafetyShoes: row?.hasSafetyShoes ?? false,
          hasSafetyVest: row?.hasSafetyVest ?? false,
        },
      };
    }

    // ================= QC =================
    if (selectedActivity?.activityTypeCode === "QC") {
      return {
        ...basePayload,

        qc: {
          moisturePercent: Number(row?.moisturePercent || 0),
          jamurPercent: Number(row?.jamurPercent || 0),
          bauPercent: Number(row?.bauPercent || 0),

          qualityStatus: row?.qualityStatus || "",
        },
      };
    }

    // ================= ALAT BERAT =================
    if (selectedActivity?.activityTypeCode === "ALAT BERAT") {
      return {
        ...basePayload,

        heavyEquipment: {
          blNumber: row?.blNumber || "",

          startTime: row?.startTime || "",
          endTime: row?.endTime || "",

          standbyDuration1: row?.standbyDuration1 || "",
          standbyDuration2: row?.standbyDuration2 || "",

          minimumDuration: row?.minimumDuration || "",

          costPerHour: Number(row?.costPerHour || 0),
          totalCost: Number(row?.totalCost || 0),
        },
      };
    }

    // ================= UNBAGGING =================
    if (selectedActivity?.activityTypeCode === "UNBAGGING") {
      return {
        ...basePayload,

        unbagging: {
          noVehicle: row?.noVehicle || "",
          noContainer: row?.noContainer || "",
          noSeal: row?.noSeal || "",

          initialWeight: Number(row?.initialWeight || 0),

          totalBag: Number(row?.totalBag || 0),

          finalWeight: Number(row?.finalWeight || 0),

          unitWeight: Number(row?.unitWeight || 0),

          totalWeight: Number(row?.totalWeight || 0),
        },
      };
    }

    // ================= REBAGGING =================
    if (selectedActivity?.activityTypeCode === "REBAGGING") {
      return {
        ...basePayload,

        rebagging: {
          receiver: row?.receiver || "",

          noVehicle: row?.noVehicle || "",
          noContainer: row?.noContainer || "",
          noSeal: row?.noSeal || "",

          initialWeight: Number(row?.initialWeight || 0),

          finalWeight: Number(row?.finalWeight || 0),

          totalWeight: Number(row?.totalWeight || 0),
        },
      };
    }

    // ================= OTHERS =================
    if (selectedActivity?.activityTypeCode === "OTHERS") {
      return {
        ...basePayload,
        // Sesuaikan nama key 'others' di bawah ini dengan struktur JSON
        // yang diminta oleh API untuk endpoint POST/PUT Work Order
        others: {
          hasPindahStapel: row?.hasPindahStapel ?? false,
          hasPembersihan: row?.hasPembersihan ?? false,
          hasPerapihan: row?.hasPerapihan ?? false,

          volumeWeight: Number(row?.volumeWeight || 0),
          workerOnDuty: Number(row?.workerOnDuty || 0),

          hasMask: row?.hasMask ?? false,
          hasSafetyGlasses: row?.hasSafetyGlasses ?? false,
          hasHandGloves: row?.hasHandGloves ?? false,
          hasHelmet: row?.hasHelmet ?? false,
          hasSafetyShoes: row?.hasSafetyShoes ?? false,
          hasSafetyVest: row?.hasSafetyVest ?? false,
        },
      };
    }

    return basePayload;
  };

  const isTransportActivity =
    selectedActivity?.activityTypeCode === "K.BONGKAR" ||
    selectedActivity?.activityTypeCode === "K.MUAT";

  const currentConfig =
    ACTIVITY_CONFIG[selectedActivity?.activityTypeCode || ""];

  console.log(selectedActivity?.activityTypeCode);

  const currentFields = currentConfig?.fields || [];

  const renderDynamicField = (row: WorkOrderRow, field: FieldConfig) => {
    const value = row[field.key];

    // ================= COMMON CLASS =================

    const baseInputClass = `
  w-full
  min-w-0
  h-[50px]
  rounded-[4px]
  border
  border-[#ffff]
  bg-[#ffff]
  px-2
  text-[11px]
  md:text-[11px]
  text-[#2f2f2f]
  placeholder:text-[#b8b8b8]
  focus:outline-none
  focus:border-[#3f2b96]
  transition-all
`;

    // ================= BOOLEAN =================

    if (field.type === "boolean") {
      return (
        <label className="flex items-center gap-2 cursor-pointer min-w-0">
          <input
            type="checkbox"
            checked={Boolean(value)}
            onChange={(e) =>
              handleRowChange(row.id, field.key, e.target.checked)
            }
            className="
            w-4
            h-4
            rounded
            border-[#c5c5c5]
            text-[#3f2b96]
            focus:ring-0
            cursor-pointer
          "
          />

          <span
            className="
    text-[11px]
    text-[#2f2f2f]
    wrap-break-word
  "
          >
            {field.label}
          </span>
        </label>
      );
    }

    // ================= NUMBER =================

    if (field.type === "number") {
      return (
        <input
          type="number"
          value={
            typeof value === "number" || value === null ? (value ?? "") : ""
          }
          onChange={(e) =>
            handleRowChange(
              row.id,
              field.key,
              e.target.value === "" ? null : Number(e.target.value),
            )
          }
          placeholder={`Fill ${field.label}`}
          className={baseInputClass}
        />
      );
    }

    // ================= TEXT =================

    return (
      <input
        type="text"
        value={typeof value === "string" ? value : ""}
        onChange={(e) => handleRowChange(row.id, field.key, e.target.value)}
        placeholder={`Fill ${field.label}`}
        className={baseInputClass}
      />
    );
  };

  if (!budgetPlan) {
    return <div>Data not found</div>;
  }

  return (
    <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto flex flex-col gap-6">
      {/* HEADER */}
      <div className="w-full">
        <PageHeader
          title="Form Work Order"
          onBack={() => navigate(-1)}
          actions={
            <span
              className={`text-[13px] font-bold px-3 py-1 rounded-full border capitalize ${getStatusStyle(
                selectedActivity?.workOrderStatus,
              )}`}
            >
              {selectedActivity?.workOrderStatus}
            </span>
          }
        />

        {/* Tabs */}
        <div
          className="relative flex items-end px-2 md:px-4 overflow-x-auto scrollbar-hide"
          style={{
            WebkitOverflowScrolling: "touch",
          }}
        >
          {budgetPlan.activities.map((activity, index) => {
            // const isActive =
            //   selectedActivity?.activityTypeCode === activity.activityTypeCode;

            const isActive =
              selectedActivity?.budgetPlanItemId === activity.budgetPlanItemId;

            return (
              <button
                key={activity.budgetPlanItemId}
                // onClick={() => setSelectedActivity(activity.activityTypeCode)}
                onClick={() => setSelectedActivity(activity)}
                className={`
                  relative
                  h-13.5
                 min-w-35
                  md:min-w-41.25
                  px-4 md:px-8
                  text-[13px] md:text-[16px]
                  shrink-0
                  rounded-t-[24px]
                  border
                  border-b-0
                  font-medium
                  transition-all
                  duration-200
                  flex
                  items-center
                  justify-center
                  ${
                    isActive
                      ? "bg-[#EAF1FF] border-[#C8D6F0] text-[#1F2937] z-20"
                      : "bg-[#E7E7E7] border-[#D1D5DB] text-[#374151] z-10"
                  }
                  ${index !== 0 ? "-ml-5" : ""}
                `}
                style={{
                  boxShadow: "0px -2px 5px rgba(0,0,0,0.08)",
                }}
              >
                {activity.activityTypeDisplay}
              </button>
            );
          })}
        </div>

        {/* Card */}
        <div className="relative -mt-px rounded-[14px] border border-[#C8D6F0] bg-[#EAF1FF] px-4 md:px-6 py-6 md:py-10">
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-x-4 gap-y-6">
            {/* Work Order ID */}
            <div>
              <p className="text-[15px] font-semibold text-[#111827] mb-2">
                Work Order ID
              </p>
              <div className="h-10 rounded-[5px] border border-[#D1D5DB] bg-[#F5F5F5] px-3 flex items-center text-[14px] text-[#374151]">
                {workOrderCode}
              </div>
            </div>

            {/* Template ID */}
            <div>
              <p className="text-[15px] font-semibold text-[#111827] mb-2">
                Template ID
              </p>

              <div className="h-10 rounded-[5px] border border-[#D1D5DB] bg-[#F5F5F5] px-3 flex items-center text-[14px] text-[#374151]">
                {budgetPlan.templateCode}
              </div>
            </div>

            {/* Warehouse */}
            <div>
              <p className="text-[15px] font-semibold text-[#111827] mb-2">
                Warehouse
              </p>

              <div className="h-10 rounded-[5px] border border-[#D1D5DB] bg-[#F5F5F5] px-3 flex items-center text-[14px] text-[#374151]">
                {budgetPlan.warehouseName}
              </div>
            </div>
            {/* PIC */}
            <PICDropdown
              label="Pilih PIC"
              woID={workOrderId}
              value={picUser?.id ?? null}
              onChange={(user) => {
                console.log("Selected PIC:", user?.fullname);
                setPicUser(user);
              }}
            />

            {/* Start Date */}
            <div>
              <p className="text-[15px] font-semibold text-[#111827] mb-2">
                Start Date
              </p>

              <input
                type="date"
                value={toInputDateFormat(startDate) ?? ""}
                onChange={(e) => handleStartDate(e.target.value)}
                className="h-10 w-full rounded-[5px] border border-[#ffffff] bg-[#ffffff] px-3 text-[14px] text-[#374151]"
              />
            </div>

            {/* End Date */}
            <div>
              <p className="text-[15px] font-semibold text-[#111827] mb-2">
                End Date
              </p>

              <input
                type="date"
                value={toInputDateFormat(endDate) ?? ""}
                onChange={(e) => handleEndDate(e.target.value)}
                className="h-10 w-full rounded-[5px] border border-[#ffffff] bg-[#ffffff] px-3 text-[14px] text-[#374151]"
              />
            </div>
          </div>
        </div>
      </div>

      {/* TRANSPORT ORDER */}
      {isTransportActivity && (
        <div className="mb-4">
          <label className="block text-[12px] font-semibold text-gray-700 mb-2">
            Transport Order
          </label>

          <div className="flex flex-wrap items-center gap-2 overflow-hidden bg-white border border-gray-300 rounded-lg px-2.5 py-2 min-h-9">
            <button
              type="button"
              onClick={() => setIsModalOpen(true)}
              className="w-5 h-5 rounded-[3px] bg-[#4f46e5] hover:bg-indigo-700 flex items-center justify-center shrink-0 transition-colors"
            >
              <svg width="10" height="10" viewBox="0 0 10 10" fill="none">
                <path
                  d="M5 1V9M1 5H9"
                  stroke="white"
                  strokeWidth="1.8"
                  strokeLinecap="round"
                />
              </svg>
            </button>

            {tags.map((tag) => (
              <span
                key={tag.id}
                className="
inline-flex
max-w-full
items-center
gap-1
bg-[#e8eaf6]
text-[#3730a3]
text-[12px]
font-medium
px-2
py-0.75
rounded-[3px]
"
              >
                {tag.label}

                <button
                  onClick={() => removeTag(tag.id)}
                  className="text-[#3730a3] hover:text-red-500"
                >
                  ✕
                </button>
              </span>
            ))}
          </div>
        </div>
      )}

      {selectedActivity && currentConfig && (
        <div className="bg-[#d9dde3] rounded-[6px] p-3">
          {/* ================= TABLE LAYOUT ================= */}
          {currentConfig.layout === "table" && (
            <div className="overflow-x-auto">
              <table className="min-w-225 w-full border-separate border-spacing-y-2 text-[12px]">
                <thead>
                  <tr>
                    {currentFields.map((field) => (
                      <th
                        key={field.key}
                        className="text-left text-[12px] font-semibold text-[#2f2f2f] px-1 whitespace-nowrap"
                      >
                        {field.label}
                      </th>
                    ))}

                    <th />
                  </tr>
                </thead>

                <tbody>
                  {rows.map((row) => {
                    const checked = row.isChecked;

                    return (
                      <tr key={row.id}>
                        {currentFields.map((field) => (
                          <td key={field.key} className="px-0.5">
                            {renderDynamicField(row, field)}
                          </td>
                        ))}

                        <td className="pl-2">
                          <button
                            onClick={() => toggleCheck(row.id)}
                            className={`w-4.5 h-4.5 rounded-[3px] flex items-center justify-center border transition-all ${
                              checked
                                ? "bg-[#3f2b96] border-[#3f2b96]"
                                : "bg-[#efefef] border-[#9ca3af]"
                            }`}
                          >
                            {checked && (
                              <svg
                                width="10"
                                height="10"
                                viewBox="0 0 10 10"
                                fill="none"
                              >
                                <path
                                  d="M1.5 5L4 7.5L8.5 2.5"
                                  stroke="white"
                                  strokeWidth="1.8"
                                  strokeLinecap="round"
                                  strokeLinejoin="round"
                                />
                              </svg>
                            )}
                          </button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}

          {/* ================= GRID LAYOUT ================= */}
          {currentConfig.layout === "grid" && (
            <div className="space-y-3">
              {rows.map((row) => {
                return (
                  <div key={row.id} className="bg-[#cfd5dd] rounded-[6px] p-3">
                    {/* FORM GRID */}
                    <div
                      className={`grid gap-x-6 gap-y-2 ${
                        currentConfig.columns === 2
                          ? "grid-cols-1 md:grid-cols-2"
                          : "grid-cols-1"
                      }`}
                    >
                      {currentFields.map((field) => (
                        <div
                          key={field.key}
                          className={field.colSpan === 2 ? "md:col-span-2" : ""}
                        >
                          {/* LABEL */}
                          <label className="block text-[11px] font-semibold text-[#1f1f1f] mb-1">
                            {field.label}
                          </label>

                          {/* INPUT */}
                          <div className="relative">
                            {renderDynamicField(row, field)}
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                );
              })}
            </div>
          )}

          {/* ================= INLINE LAYOUT (QC) ================= */}
          {currentConfig.layout === "inline" && (
            <div className="space-y-4">
              {rows.map((row) => {
                const checked = row.isChecked;

                return (
                  <div
                    key={row.id}
                    className="bg-white rounded-[6px] p-3 space-y-4"
                  >
                    {/* TOP INLINE */}
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                      {currentFields
                        .filter((field) => field.colSpan !== 3)
                        .map((field) => (
                          <div key={field.key}>
                            <label className="block text-[12px] font-semibold text-[#2f2f2f] mb-1">
                              {field.label}
                            </label>

                            {renderDynamicField(row, field)}
                          </div>
                        ))}
                    </div>

                    {/* FULL WIDTH */}
                    {currentFields
                      .filter((field) => field.colSpan === 3)
                      .map((field) => (
                        <div key={field.key}>
                          <label className="block text-[12px] font-semibold text-[#2f2f2f] mb-1">
                            {field.label}
                          </label>

                          {renderDynamicField(row, field)}
                        </div>
                      ))}

                    <div className="flex justify-end">
                      <button
                        onClick={() => toggleCheck(row.id)}
                        className={`w-5 h-5 rounded-lg flex items-center justify-center border transition-all ${
                          checked
                            ? "bg-[#3f2b96] border-[#3f2b96]"
                            : "bg-[#efefef] border-[#9ca3af]"
                        }`}
                      >
                        {checked && (
                          <svg
                            width="10"
                            height="10"
                            viewBox="0 0 10 10"
                            fill="none"
                          >
                            <path
                              d="M1.5 5L4 7.5L8.5 2.5"
                              stroke="white"
                              strokeWidth="1.8"
                              strokeLinecap="round"
                              strokeLinejoin="round"
                            />
                          </svg>
                        )}
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>
          )}

          {/* ================= CHECKLIST LAYOUT ================= */}
          {currentConfig.layout === "checklist" && (
            <div className="space-y-4">
              {rows.map((row) => {
                const checked = row.isChecked;

                return (
                  <div
                    key={row.id}
                    className="bg-white rounded-[6px] p-3 space-y-4"
                  >
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                      {currentFields.map((field) => (
                        <div key={field.key}>
                          {field.type === "boolean" ? (
                            <div className="flex items-center gap-2 h-full pt-6">
                              {renderDynamicField(row, field)}

                              <label className="text-[12px] font-medium text-[#2f2f2f]">
                                {field.label}
                              </label>
                            </div>
                          ) : (
                            <>
                              <label className="block text-[12px] font-semibold text-[#2f2f2f] mb-1">
                                {field.label}
                              </label>

                              {renderDynamicField(row, field)}
                            </>
                          )}
                        </div>
                      ))}
                    </div>

                    <div className="flex justify-end">
                      <button
                        onClick={() => toggleCheck(row.id)}
                        className={`w-5 h-5 rounded-lg flex items-center justify-center border transition-all ${
                          checked
                            ? "bg-[#3f2b96] border-[#3f2b96]"
                            : "bg-[#efefef] border-[#9ca3af]"
                        }`}
                      >
                        {checked && (
                          <svg
                            width="10"
                            height="10"
                            viewBox="0 0 10 10"
                            fill="none"
                          >
                            <path
                              d="M1.5 5L4 7.5L8.5 2.5"
                              stroke="white"
                              strokeWidth="1.8"
                              strokeLinecap="round"
                              strokeLinejoin="round"
                            />
                          </svg>
                        )}
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}

      {/* DOCUMENTATION */}
      <div className="rounded-lg bg-[#DCE3E8] p-3 md:p-4">
        {/* Upload Section */}
        <div>
          <label className="mb-2 block text-[14px] font-semibold text-[#1F1F1F]">
            Documentation
          </label>

          <div className="flex flex-wrap gap-2">
            {/* Preview slot yang sudah terisi */}
            {hasFiles === false ? (
              // Container awal — full width, tidak berubah
              <label
                className="flex h-39.5 w-full cursor-pointer flex-col items-center justify-center
  rounded-[6px] border border-[#D6D6D6] bg-[#F7F7F7]"
              >
                <svg
                  width="18"
                  height="18"
                  viewBox="0 0 24 24"
                  fill="none"
                  className="mb-3"
                >
                  <path
                    d="M12 16V4"
                    stroke="#8B8B8B"
                    strokeWidth="1.8"
                    strokeLinecap="round"
                  />
                  <path
                    d="M8 8L12 4L16 8"
                    stroke="#8B8B8B"
                    strokeWidth="1.8"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  />
                  <path
                    d="M5 20H19"
                    stroke="#8B8B8B"
                    strokeWidth="1.8"
                    strokeLinecap="round"
                  />
                </svg>
                <p className="text-[14px] font-medium leading-4.5 text-[#8B8B8B]">
                  Upload File
                </p>
                <p className="text-[14px] leading-4.5 text-[#8B8B8B]">
                  (.pdf, .png, .jpg)
                </p>
                <input
                  type="file"
                  className="hidden"
                  accept=".pdf,.png,.jpg,.jpeg"
                  multiple
                  onChange={handleDocUpload}
                />
              </label>
            ) : (
              // Setelah ada gambar — preview + upload slot kecil di sebelahnya
              <div className="flex flex-wrap gap-2">
                {/* File yang sudah ada di server (hasil fetch GET) */}
                {existingFiles.map((file) => {
                  const fileName = file.originalFileName ?? "";
                  const isPdf =
                    file.contentType === "application/pdf" ||
                    fileName.toLowerCase().endsWith(".pdf");

                  return (
                    <div
                      key={`existing-${file.id}`}
                      className="
                    relative
                    h-40
                    w-full
                    sm:w-60
                    sm:h-50
                    overflow-hidden
                    rounded-[6px]
                    border
                    border-[#D6D6D6]
                    bg-[#F7F7F7]
                    "
                    >
                      {isPdf ? (
                        <div className="flex h-full flex-col items-center justify-center p-4">
                          <svg
                            width="64"
                            height="64"
                            viewBox="0 0 24 24"
                            fill="none"
                          >
                            <path
                              d="M14 2H6C4.89 2 4 2.89 4 4V20C4 21.11 4.89 22 6 22H18C19.11 22 20 21.11 20 20V8L14 2Z"
                              fill="#EF4444"
                            />
                          </svg>
                          <p className="mt-2 line-clamp-2 text-center text-sm font-medium">
                            {fileName || "Untitled file"}
                          </p>
                          <span className="mt-1 text-xs text-gray-500">
                            PDF Document
                          </span>
                        </div>
                      ) : (
                        <img
                          src={`${import.meta.env.VITE_API_URL}${file.url}?token=${token}`}
                          alt={file.originalFileName}
                          className="h-full w-full cursor-pointer object-cover transition hover:opacity-90"
                          onClick={() =>
                            setPreviewImage({
                              src: `${import.meta.env.VITE_API_URL}${file.url}?token=${token}`,
                              alt: file.originalFileName,
                            })
                          }
                        />
                      )}
                    </div>
                  );
                })}

                {docPreviews.map((preview, index) => {
                  const file = docFiles[index];
                  const isPdf = file?.type === "application/pdf";

                  return (
                    <div
                      key={index}
                      className="
                            relative
                            h-40
                            w-full
                            sm:w-60
                            sm:h-50
                            overflow-hidden
                            rounded-[6px]
                            border
                            border-[#D6D6D6]
                            bg-[#F7F7F7]
                            "
                    >
                      {isPdf ? (
                        <div className="flex h-full flex-col items-center justify-center p-4">
                          <svg
                            width="64"
                            height="64"
                            viewBox="0 0 24 24"
                            fill="none"
                          >
                            <path
                              d="M14 2H6C4.89 2 4 2.89 4 4V20C4 21.11 4.89 22 6 22H18C19.11 22 20 21.11 20 20V8L14 2Z"
                              fill="#EF4444"
                            />
                          </svg>

                          <p className="mt-2 line-clamp-2 text-center text-sm font-medium">
                            {file.name}
                          </p>

                          <span className="mt-1 text-xs text-gray-500">
                            PDF Document
                          </span>
                        </div>
                      ) : (
                        <img
                          src={preview}
                          alt={`doc-${index}`}
                          className="h-full w-full object-cover"
                        />
                      )}

                      <button
                        type="button"
                        onClick={() => removeDoc(index)}
                        className="absolute right-1 top-1 flex h-5 w-5 items-center justify-center rounded-full bg-red-500 text-[10px] text-white"
                      >
                        ✕
                      </button>
                    </div>
                  );
                })}

                {/* Upload slot kecil — hanya muncul jika belum mencapai MAX_FILES */}
                {docFiles.length < MAX_FILES && (
                  <label
                    className="
flex
h-50
w-full
sm:w-48
cursor-pointer
flex-col
items-center
justify-center
rounded-[6px]
border
border-[#D6D6D6]
bg-[#F7F7F7]
"
                  >
                    <svg
                      width="18"
                      height="18"
                      viewBox="0 0 24 24"
                      fill="none"
                      className="mb-3"
                    >
                      <path
                        d="M12 16V4"
                        stroke="#8B8B8B"
                        strokeWidth="1.8"
                        strokeLinecap="round"
                      />
                      <path
                        d="M8 8L12 4L16 8"
                        stroke="#8B8B8B"
                        strokeWidth="1.8"
                        strokeLinecap="round"
                        strokeLinejoin="round"
                      />
                      <path
                        d="M5 20H19"
                        stroke="#8B8B8B"
                        strokeWidth="1.8"
                        strokeLinecap="round"
                      />
                    </svg>
                    <p className="text-[14px] font-medium leading-4.5 text-[#8B8B8B]">
                      Upload File
                    </p>
                    <p className="text-[14px] leading-4.5 text-[#8B8B8B]">
                      (.pdf, .png, .jpg)
                    </p>
                    <input
                      type="file"
                      className="hidden"
                      accept=".pdf,.png,.jpg,.jpeg"
                      multiple
                      onChange={handleDocUpload}
                    />
                  </label>
                )}
              </div>
            )}
          </div>
        </div>

        {/* Notes */}
        <div className="mt-6">
          <label className="mb-2 block text-[14px] font-semibold text-[#1F1F1F]">
            Notes
          </label>

          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            className="
      h-26.25
      w-full
      resize-none
      rounded-[6px]
      border
      border-[#D6D6D6]
      bg-[#F7F7F7]
      p-4
      text-[14px]
      text-[#222]
      outline-none
    "
          />
        </div>
      </div>

      {/* Footer Buttons */}
      {selectedActivity?.workOrderStatus !== "Submitted" ? (
        <div className="mt-5 flex flex-col-reverse sm:flex-row justify-end gap-4">
          {/* Draft */}
          <Button
            variant="secondary"
            size="lg"
            onClick={handleDraftWo}
            disabled={isDrafting}
            type="button"
            className="w-full sm:w-auto min-w-28.5"
          >
            {isDrafting ? "Loading..." : "Draft"}
          </Button>

          {/* Submit */}
          <Button
            variant="primary"
            size="lg"
            onClick={handleCreateWo}
            disabled={isSubmitting || isUploading}
            className="w-full sm:w-auto"
          >
            {isSubmitting
              ? "Submitting..."
              : isUploading
                ? "Uploading..."
                : "Submit"}
          </Button>
        </div>
      ) : null}

      {/* MODAL */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="flex max-h-[90vh] w-full max-w-7xl flex-col overflow-hidden rounded-2xl bg-white shadow-2xl">
            {/* HEADER */}
            <div className="flex items-center justify-between border-b border-gray-200 px-6 py-4">
              <h2 className="text-lg font-semibold text-[#2B2469]">
                Select Transport Order
              </h2>

              <button
                type="button"
                onClick={() => setIsModalOpen(false)}
                className="flex h-8 w-8 items-center justify-center rounded-full transition hover:bg-red-50 hover:text-red-500"
              >
                ✕
              </button>
            </div>

            {/* BODY */}
            <div
              ref={scrollContainerRef}
              onScroll={handleScroll}
              className="flex-1 overflow-auto p-5"
            >
              <table className="w-max min-w-full border-separate border-spacing-y-2 text-[12px]">
                <thead className="sticky top-0 z-10 bg-white">
                  <tr>
                    {[
                      "Document No",
                      "Type",
                      "Card Code",
                      "Card Name",
                      "Vehicle No",
                      "Vehicle Type",
                      "BL No",
                      "Item Code",
                      "Item Name",
                      "Quantity",
                      "UoM",
                      "Whs Code",
                      "Warehouse Name",
                      "Doc Status",
                      "",
                    ].map((header) => (
                      <th
                        key={header}
                        className="whitespace-nowrap px-2 pb-2 text-left text-xs font-semibold text-gray-700"
                      >
                        {header}
                      </th>
                    ))}
                  </tr>
                </thead>

                <tbody>
                  {isLoadingTransportOrders && page === 1 ? (
                    <tr>
                      <td
                        colSpan={16}
                        className="py-8 text-center text-gray-500"
                      >
                        Loading...
                      </td>
                    </tr>
                  ) : (
                    <>
                      {transportOrders.map((item) => {
                        const checked = selectedTransportOrderIds.includes(
                          item.id,
                        );

                        return (
                          <tr key={item.id}>
                            <TableCell value={item.docNo} />
                            <TableCell value={item.type} />
                            <TableCell value={item.cardCode} />
                            <TableCell value={item.cardName} />
                            <TableCell value={item.vehicleNo} />
                            <TableCell value={item.vehicleType} />
                            <TableCell value={item.blNo} />
                            <TableCell value={item.itemCode} />
                            <TableCell value={item.itemName} />
                            <TableCell value={item.quantity} />
                            <TableCell value={item.uoM} />
                            <TableCell value={item.whsCode} />
                            <TableCell value={item.whsName} />
                            <TableCell value={item.docStatus} />

                            <td className="px-2">
                              <button
                                type="button"
                                onClick={() => toggleTransportOrder(item)}
                                className={`flex h-5 w-5 items-center justify-center rounded border text-xs font-bold transition ${
                                  checked
                                    ? "border-[#2B2469] bg-[#2B2469] text-white"
                                    : "border-gray-400 bg-white"
                                }`}
                              >
                                {checked && "✓"}
                              </button>
                            </td>
                          </tr>
                        );
                      })}

                      {isFetchingMore && (
                        <tr>
                          <td
                            colSpan={16}
                            className="py-3 text-center text-gray-400 text-xs"
                          >
                            Memuat data lainnya...
                          </td>
                        </tr>
                      )}

                      {!hasMore && transportOrders.length > 0 && (
                        <tr>
                          <td
                            colSpan={16}
                            className="py-3 text-center text-gray-300 text-xs"
                          >
                            — Semua data sudah dimuat —
                          </td>
                        </tr>
                      )}
                    </>
                  )}
                </tbody>
              </table>
            </div>

            {/* FOOTER */}
            <div className="flex justify-end gap-3 border-t border-gray-200 px-6 py-4">
              <Button
                variant="outline"
                type="button"
                onClick={() => setIsModalOpen(false)}
              >
                Cancel
              </Button>

              <Button
                variant="primary"
                type="button"
                onClick={() => setIsModalOpen(false)}
              >
                Apply
              </Button>
            </div>
          </div>
        </div>
      )}

      {previewImage && (
        <div
          className="fixed inset-0 z-999 flex items-center justify-center bg-black/80 p-6"
          onClick={() => setPreviewImage(null)}
        >
          <div
            className="relative max-h-[90vh] max-w-[90vw]"
            onClick={(e) => e.stopPropagation()}
          >
            <button
              type="button"
              onClick={() => setPreviewImage(null)}
              className="absolute -right-3 -top-3 flex h-9 w-9 items-center justify-center rounded-full bg-white shadow-lg hover:bg-gray-100"
            >
              ✕
            </button>

            <img
              src={previewImage.src}
              alt={previewImage.alt}
              className="max-h-[90vh] max-w-[90vw] rounded-lg object-contain shadow-2xl"
            />
          </div>
        </div>
      )}
    </div>
  );
}

const TableCell = ({ value }: { value: React.ReactNode }) => (
  <td className="px-1 py-1">
    <div className="flex h-8 min-w-max items-center rounded-lg border border-[#D6D6D6] bg-[#F5F5F5] px-3 text-[12px] text-[#374151]">
      {value ?? "-"}
    </div>
  </td>
);

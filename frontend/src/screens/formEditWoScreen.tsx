import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useWorkOrderStore } from "../store/detailWoStore";
import InfoField from "../components/infoField";
import { formatDate } from "../components/format/dateTimeFormat";
import { formatNumber } from "../components/format/formatCurrency";
import type { TransportOrder } from "../master_data/types/transport.types";
import {
  createEmptyRow,
  type WorkOrderRow,
} from "../config/workOrderRowConfig";
import { useTransportOrderController } from "../master_data/controller/transportController";
import {
  ACTIVITY_CONFIG_EDIT,
  type FieldConfig,
} from "../config/activityWoConfig";
import { useFileUploadController } from "../controllers/file/fileUploadController";
import { useWorkOrderController } from "../controllers/operationalRealization/createWorkorderControllert";
import toast from "react-hot-toast";
import { buildWorkOrderPayload } from "../utils/workOrderPayloads";
import { PageHeader } from "../components/ui/page-header";
import { Button } from "../components/ui/button";

export default function FormEditWoScreen() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { getDetail, data, isLoading, error } = useWorkOrderStore();

  const [activeTab, setActiveTab] = useState("Unloading");
  const [notes, setNotes] = useState<string>("");

  const [rows, setRows] = useState<WorkOrderRow[]>([]);

  const { uploadFiles, isUploading } = useFileUploadController();

  // ================= FILE =================
  const MAX_FILES = 5;

  const [docFiles, setDocFiles] = useState<File[]>([]);
  const [docPreviews, setDocPreviews] = useState<string[]>([]);

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

  const { submitDraftWorkOrder, isSubmitting, isDrafting, editWorkOrder } =
    useWorkOrderController();

  useEffect(() => {
    if (!data) return;

    setNotes(data.notes ?? "");

    switch (activeTab) {
      case "Unloading":
        setRows(
          (data.unloadingItems ?? []).map((item) => ({
            ...createEmptyRow(item.id),
            ...item,
          })),
        );
        break;

      case "Loading":
        setRows(
          (data.loadingItems ?? []).map((item) => ({
            ...createEmptyRow(item.id),
            ...item,
          })),
        );
        break;

      case "Fumigation":
        setRows(
          data.fumigation
            ? [
                {
                  ...createEmptyRow(1),
                  ...data.fumigation,
                },
              ]
            : [],
        );
        break;

      case "Storage":
        setRows(
          data.others
            ? [
                {
                  ...createEmptyRow(1),
                  ...data.others,
                },
              ]
            : [],
        );
        break;

      case "QC":
        setRows(
          data.qc
            ? [
                {
                  ...createEmptyRow(1),
                  ...data.qc,
                },
              ]
            : [],
        );
        break;

      case "Heavy Equipment":
        setRows(
          data.heavyEquipment
            ? [
                {
                  ...createEmptyRow(1),
                  ...data.heavyEquipment,
                },
              ]
            : [],
        );
        break;

      case "Unbagging":
        setRows(
          data.unbagging
            ? [
                {
                  ...createEmptyRow(1),
                  ...data.unbagging,
                },
              ]
            : [],
        );
        break;

      case "Rebagging":
        setRows(
          data.rebagging
            ? [
                {
                  ...createEmptyRow(1),
                  ...data.rebagging,
                },
              ]
            : [],
        );
        break;
    }
  }, [data, activeTab]);

  useEffect(() => {
    if (id) getDetail(Number(id));
  }, [id]);

  const handleDraft = async () => {
    try {
      const payload = buildWorkOrderPayload({
        data,
        rows,
        // itemShadowId,
        activeTab,
        notes,
      });
      const response = await editWorkOrder(Number(id), payload);

      const workOrderId = response.data.id;

      if (docFiles.length > 0) {
        await uploadFiles(workOrderId, docFiles);
      }

      navigate(-1);
    } catch (error) {
      console.error(error);
      toast.error("Failed to draft WO");
    }
  };

  const handleSubmitFromDraft = async () => {
    try {
      const payload = buildWorkOrderPayload({
        data,
        rows,
        activeTab,
        // itemShadowId,
        notes,
      });
      await editWorkOrder(Number(id), payload);
      const response = await submitDraftWorkOrder(Number(id));

      const workOrderId = response.data.id;

      if (docFiles.length > 0) {
        await uploadFiles(workOrderId, docFiles);
      }

      toast.success("Success Submit WO");

      navigate(-1); // ← navigate setelah semua selesai
    } catch (error) {
      console.error(error);
      toast.success("Failed to Submit WO");
    }
  };

  const removeDoc = (index: number) => {
    setDocFiles((prev) => prev.filter((_, i) => i !== index));

    setDocPreviews((prev) => prev.filter((_, i) => i !== index));
  };

  const {
    transportOrders,
    isLoading: isLoadingTransportOrders,
    loadTransportOrders,
  } = useTransportOrderController();

  useEffect(() => {
    loadTransportOrders();
  }, []);

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

  // const handleNumberChange = (
  //   id: number,
  //   field: keyof UnloadingItem,
  //   value: string,
  // ) => {
  //   setRows((prev) =>
  //     prev.map((row) =>
  //       row.id === id
  //         ? {
  //             ...row,
  //             [field]: Number(value),
  //           }
  //         : row,
  //     ),
  //   );
  // };

  // const handleChange = (
  //   id: number,
  //   field: keyof UnloadingItem,
  //   value: string,
  // ) => {
  //   console.log(id, field, value);

  //   setRows((prev) =>
  //     prev.map((row) =>
  //       row.id === id
  //         ? {
  //             ...row,
  //             [field]: value,
  //           }
  //         : row,
  //     ),
  //   );
  // };

  const currentConfig = ACTIVITY_CONFIG_EDIT[activeTab || ""];

  const currentFields = currentConfig?.fields || [];

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

  const renderDynamicField = (row: WorkOrderRow, field: FieldConfig) => {
    const value = row[field.key];

    // ================= COMMON CLASS =================

    const baseInputClass = `
      w-full
      h-[50px]
      rounded-[4px]
      border
      border-[#ffff]
      bg-[#ffff]
      px-2
      text-[11px]
      text-[#2f2f2f]
      placeholder:text-[#b8b8b8]
      focus:outline-none
      focus:border-[#3f2b96]
      transition-all
    `;

    // ================= BOOLEAN =================

    if (field.type === "boolean") {
      return (
        <label className="flex items-center gap-2 cursor-pointer">
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

          <span className="text-[11px] text-[#2f2f2f]">{field.label}</span>
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

  const isTransportActivity =
    activeTab === "Unloading" || activeTab === "Loading";

  const [isModalOpen, setIsModalOpen] = useState(false);

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

  const removeTag = (id: number) => {
    setTags((prev) => prev.filter((tag) => tag.id !== id));

    setSelectedTransportOrderIds((prev) =>
      prev.filter((itemId) => itemId !== id),
    );

    setAllItems((prev) => prev.filter((item) => item.id !== id));

    // setSelectedRowIds((prev) => prev.filter((rowId) => rowId !== id));

    setRows((prev) => prev.filter((row) => row.id !== id));
  };

  if (isLoading) return <div>Loading...</div>;
  if (error) return <div>{error}</div>;
  if (!data) return <div>Data not found</div>;

  const tabs = [
    {
      key: "Unloading",
      data: data.unloadingItems,
    },
    {
      key: "Loading",
      data: data.loadingItems,
    },
    {
      key: "Fumigation",
      data: data.fumigation,
    },
    {
      key: "Storage",
      data: data.others,
    },
    {
      key: "QC",
      data: data.qc,
    },
    {
      key: "Heavy Equipment",
      data: data.heavyEquipment,
    },
    {
      key: "Unbagging",
      data: data.unbagging,
    },
    {
      key: "Rebagging",
      data: data.rebagging,
    },
  ];

  //   const activeTabData = tabs.find((tab) => tab.key === activeTab)
  //     ?.data as UnloadingItem[];
  return (
    <div className="flex-1 space-y-4 md:space-y-6 p-4 sm:p-6 lg:p-8 overflow-y-auto">
      {/* Header */}
      <PageHeader
        breadcrumbs={[{ label: "Dashboard" }, { label: "Edit Work Order" }]}
        title="Edit Work Order"
        onBack={() => navigate(-1)}
      />

      <div>
        {/* Tab Header */}
        <div className="relative z-10 flex overflow-x-auto px-2 md:px-4 scrollbar-hide">
          {tabs.map((tab) => (
            <button
              key={tab.key}
              onClick={() => {
                console.log(
                  "clicked:",
                  tab.key,
                  "current activeTab:",
                  activeTab,
                  tab.data,
                );
                setActiveTab(tab.key);
              }}
              className={`min-w-fit whitespace-nowrap rounded-t-[24px] border border-b-0 px-4 md:px-8 py-3 text-xs md:text-sm font-medium transition-all ${
                activeTab === tab.key
                  ? "bg-[#D8DFEA] text-black"
                  : "bg-[#ECECEC] text-gray-600"
              }`}
            >
              {tab.key}
            </button>
          ))}
        </div>

        {/* Tab Content */}
        <div className="-mt-px rounded-4xl border border-[#B9C9DD] bg-[#D8DFEA] p-6">
          <div className="grid grid-cols-1 gap-5 md:grid-cols-2 lg:grid-cols-4">
            <InfoField label="Work Order ID" value={data.code} />

            <InfoField label="Activity Name" value={data.activityName} />

            <InfoField label="Warehouse" value={`${data.warehouseName}`} />

            <InfoField label="Warehouse Code" value={data.warehouseCode} />

            <InfoField label="PIC" value={data.picName} />

            <InfoField label="Start Date" value={formatDate(data.startDate)} />

            <InfoField label="Start Date" value={formatDate(data.endDate)} />

            <InfoField
              label="RFBA"
              value={`${data.isRfba === true ? "Yes" : "No"}`}
            />

            <InfoField label="Product Name" value={`${data.productName}`} />

            <InfoField label="Qty" value={`${formatNumber(data.quantity)}`} />

            <InfoField label="OuM" value={`${data.uomCode}`} />

            <InfoField label="Bill of Landing" value={`${data.blNumber}`} />
          </div>
        </div>

        {isTransportActivity && (
          <div className="my-7">
            <label className="block text-[12px] font-semibold text-gray-700 mb-2">
              Transport Order
            </label>

            <div className="flex flex-wrap items-start md:items-center gap-2 bg-white border border-gray-300 rounded-lg px-2.5 py-2 min-h-9">
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
                  className="inline-flex items-center gap-1 bg-[#e8eaf6] text-[#3730a3] text-[12px] font-medium px-2 py-0.75 rounded-[3px]"
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

        {activeTab && currentConfig && (
          <div className="bg-[#d9dde3] rounded-[6px] p-3">
            {/* ================= TABLE LAYOUT ================= */}
            {currentConfig.layout === "table" && (
              <div className="overflow-x-auto">
                <table className="min-w-175 w-full border-separate border-spacing-y-2 text-[12px]">
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
                    <div
                      key={row.id}
                      className="bg-[#cfd5dd] rounded-[6px] p-3"
                    >
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
                            className={
                              field.colSpan === 2 ? "md:col-span-2" : ""
                            }
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
                          //   onClick={() => toggleCheck(row.id)}
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
                          //   onClick={() => toggleCheck(row.id)}
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
      </div>

      {/* DOCUMENTATION */}
      <div className="rounded-lg bg-[#DCE3E8] p-4">
        {/* Upload Section */}
        <div>
          <label className="mb-2 block text-[14px] font-semibold text-[#1F1F1F]">
            Documentation
          </label>

          <div className="flex flex-wrap gap-2">
            {/* Preview slot yang sudah terisi */}
            {docPreviews.length === 0 ? (
              // Container awal — full width, tidak berubah
              <label
                className="flex h-32 md:h-39.5 w-full cursor-pointer flex-col items-center justify-center
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
                {docPreviews.map((preview, index) => {
                  const file = docFiles[index];
                  const isPdf = file?.type === "application/pdf";

                  return (
                    <div
                      key={index}
                      className="relative h-40 md:h-50 w-full sm:w-60 overflow-hidden rounded-[6px] border border-[#D6D6D6] bg-[#F7F7F7]"
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
                    className="flex h-32 md:h-39.5 w-full sm:w-48 cursor-pointer flex-col items-center justify-center
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

      <div className="mt-5 flex flex-col-reverse md:flex-row justify-end gap-3 md:gap-4">
        {/* Draft */}
        <Button
          variant="secondary"
          size="lg"
          onClick={handleDraft}
          disabled={isDrafting}
          type="button"
          className="w-full md:w-auto md:min-w-28.5"
        >
          {isDrafting ? "Loading..." : "Draft"}
        </Button>

        {/* Submit */}
        <Button
          variant="primary"
          size="lg"
          onClick={handleSubmitFromDraft}
          disabled={isSubmitting || isUploading}
          className="w-full md:w-auto"
        >
          {isSubmitting
            ? "Submitting..."
            : isUploading
              ? "Uploading..."
              : "Submit"}
        </Button>
      </div>

      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div
            className="
bg-white
w-[95vw]
md:w-225
max-h-[80vh]
overflow-hidden
rounded-xl
shadow-xl
"
          >
            {/* HEADER */}
            <div className="flex items-center justify-between px-5 py-4 border-b border-gray-200">
              <h2 className="text-[16px] font-semibold text-gray-800">
                Select Transport Order
              </h2>

              <button
                type="button"
                onClick={() => setIsModalOpen(false)}
                className="text-gray-500 hover:text-red-500 text-[18px]"
              >
                ✕
              </button>
            </div>

            {/* BODY */}
            <div className="p-3 md:p-5 overflow-auto max-h-[65vh]">
              <div className="overflow-x-auto">
                <table className="min-w-175 w-full border-separate border-spacing-y-2 text-[12px]">
                  <thead>
                    <tr>
                      {["Document No", "Vendor", "Warehouse", "Date", ""].map(
                        (header, index) => (
                          <th
                            key={index}
                            className="text-left text-[12px] font-semibold text-[#2f2f2f] px-1 whitespace-nowrap"
                          >
                            {header}
                          </th>
                        ),
                      )}
                    </tr>
                  </thead>

                  <tbody>
                    {isLoadingTransportOrders ? (
                      <tr>
                        <td
                          colSpan={5}
                          className="text-center py-5 text-gray-500"
                        >
                          Loading...
                        </td>
                      </tr>
                    ) : (
                      transportOrders.map((item: TransportOrder) => {
                        const checked = selectedTransportOrderIds.includes(
                          item.id,
                        );

                        return (
                          <tr key={item.id}>
                            <td className="px-0.5">
                              <div className="bg-[#efefef] border border-[#cfcfcf] rounded-lg h-7 px-2 flex items-center">
                                {item.docNo}
                              </div>
                            </td>

                            <td className="px-0.5">
                              <div className="bg-[#efefef] border border-[#cfcfcf] rounded-lg h-7 px-2 flex items-center">
                                {item.cardName || "-"}
                              </div>
                            </td>

                            <td className="px-0.5">
                              <div className="bg-[#efefef] border border-[#cfcfcf] rounded-lg h-7 px-2 flex items-center">
                                {item.whsName || "-"}
                              </div>
                            </td>

                            {/* <td className="px-0.5">
                              <div className="bg-[#efefef] border border-[#cfcfcf] rounded-lg h-7 px-2 flex items-center">
                                {item.docDate || "-"}
                              </div>
                            </td> */}

                            <td className="pl-2">
                              <button
                                type="button"
                                  onClick={() => toggleTransportOrder(item)}
                                className={`w-4.5 h-4.5 rounded-[3px] flex items-center justify-center border transition-all ${
                                  checked
                                    ? "bg-[#3f2b96] border-[#3f2b96]"
                                    : "bg-[#efefef] border-[#9ca3af]"
                                }`}
                              >
                                {checked && "✓"}
                              </button>
                            </td>
                          </tr>
                        );
                      })
                    )}
                  </tbody>
                </table>
              </div>
            </div>

            {/* FOOTER */}
            <div className="flex flex-col md:flex-row justify-end gap-3 px-4 md:px-5 py-4 border-t border-gray-200">
              <Button
                variant="outline"
                type="button"
                onClick={() => setIsModalOpen(false)}
                className="w-full md:w-auto"
              >
                Cancel
              </Button>

              <Button
                variant="primary"
                type="button"
                onClick={() => setIsModalOpen(false)}
                className="w-full md:w-auto"
              >
                Apply
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// function InputField({
//   label,
//   value,
//   onChange,
//   type = "text",
// }: {
//   label: string;
//   value: string | number;
//   onChange: React.ChangeEventHandler<HTMLInputElement>;
//   type?: string;
// }) {
//   return (
//     <div>
//       <label className="mb-1 block text-xs text-gray-500">{label}</label>

//       <input
//         type={type}
//         value={value}
//         onChange={onChange}
//         className="w-full rounded-md border border-gray-300 px-3 py-2"
//       />
//     </div>
//   );
// }

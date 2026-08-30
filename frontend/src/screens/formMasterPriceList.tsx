"use client";

import { useEffect, useRef, useState } from "react";
import { useVendorStore } from "../master_data/store/vendorStore";
import type { Item } from "../master_data/types/item.types";
import { useUomStore } from "../master_data/store/unitofmeasurementStore";
import { useRateCardStore } from "../master_data/store/rateCardStore";
import type { RateCardPayload } from "../master_data/types/rateCard.type";
import AlertWidget from "../components/alertWidget";
import { PageHeader } from "../components/ui/page-header";
import { Button } from "../components/ui/button";
import { useNavigate, useParams } from "react-router-dom";
import { fetchLocationsController } from "../master_data/controller/locationController";
import { useLocationStore } from "../master_data/store/locationStore";
import { useTaxStore } from "../store/taxStore";
import { getTaxListController } from "../controllers/masterData/taxController";
import { ItemSearchSelect } from "../components/itemSearch";
import { VendorCombobox } from "../components/vendorCombobox";

interface CostRow {
  id: string;
  costId: string;
  costName: string;
  uomId: string;
  uomCode: string;
  costValue: string;
  costLabel: string;
  ppnTaxType: TaxRow;
  pphTaxType: TaxRow;
}

interface TaxRow {
  id: number;
  code: string;
  rate: number;
}

function formatCurrency(value: string): string {
  const numeric = value.replace(/\D/g, "");
  if (!numeric) return "";
  return new Intl.NumberFormat("id-ID").format(Number(numeric));
}

function parseCurrency(value: string): number {
  return Number(value.replace(/\D/g, "")) || 0;
}

function createRow(): CostRow {
  return {
    id: crypto.randomUUID(),
    costId: "",
    costName: "",
    costLabel: "",
    uomId: "",
    uomCode: "",
    costValue: "",
    pphTaxType: {
      id: 0,
      code: "",
      rate: 0,
    },
    ppnTaxType: {
      id: 0,
      code: "",
      rate: 0,
    },
  };
}

function ChevronDown() {
  return (
    <svg width="12" height="12" viewBox="0 0 12 12" fill="none">
      <path
        d="M2.5 4.5L6 8L9.5 4.5"
        stroke="#6B7280"
        strokeWidth="1.3"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}
const GRID_TEMPLATE_COLUMNS = "2fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 40px";
export type costTreament = "Dibiayakan" | "TidakDibiayakan";

export default function FormCreatePriceList() {
  const { id } = useParams();

  const isEditMode = id !== undefined;

  const navigate = useNavigate();

  const [treatment, setTreatment] = useState<costTreament | "">("");

  const { vendors, fetchVendors } = useVendorStore();
  // const { items, fetchItems } = useItemStore();
  const { uoms, fetchUoms } = useUomStore();
  const { taxes } = useTaxStore();
  const {
    isSavingDraft,
    isSubmitting,
    isUpdating,
    draftError,
    submitError,
    updateError,
    selectedRateCard,
    isDetailLoading,
    detailError,
    saveDraft,
    submit,
    update,
    fetchDetail,
    clearDraftError,
    clearSubmitError,
    clearUpdateError,
    clearSelectedRateCard,
  } = useRateCardStore();

  const [vendorCode, setVendorCode] = useState("");
  const [rows, setRows] = useState<CostRow[]>(() => [createRow()]);
  const [validationError, setValidationError] = useState<string | null>(null);
  const hasPopulated = useRef(false);

  const { locations } = useLocationStore();
  const [selectedLocation, setSelectedLocation] = useState("");

  useEffect(() => {
    fetchLocationsController();
  }, []);

  // ── Fetch master data ──
  useEffect(() => {
    fetchVendors();
    // fetchItems();
    fetchUoms();
    getTaxListController();
  // }, [fetchVendors, fetchItems, fetchUoms]);
  }, [fetchVendors, fetchUoms]);


  // ── Fetch detail saat edit mode ──
  useEffect(() => {
    if (isEditMode) {
      fetchDetail(id);
    }
    return () => {
      clearSelectedRateCard();
    };
  }, [isEditMode, id]); // eslint-disable-line

  // ── Populate form via queueMicrotask agar tidak synchronous dalam effect ──
  useEffect(() => {
    if (!isEditMode || !selectedRateCard || hasPopulated.current) return;

    const newVendorCode = selectedRateCard.vendor.cardCode;
    const newRows = selectedRateCard.items.map((line) => ({
      id: crypto.randomUUID(),
      costId: String(line.item.id),
      costName: line.item.itemName,
      uomId: String(line.uom.id),
      uomCode: line.uom.code,
      costLabel: `${line.item.itemCode} - ${line.item.itemName}`,
      costValue: formatCurrency(String(line.costValue)),
      ppnTaxType: {
        id: line.ppnTaxType?.id ?? 0,
        code: line.ppnTaxType?.code ?? "",
        rate: line.ppnTaxType?.rate ?? 0,
      },
      pphTaxType: {
        id: line.pphTaxType?.id ?? 0,
        code: line.pphTaxType?.code ?? "",
        rate: line.pphTaxType?.rate ?? 0,
      },
    }));

    queueMicrotask(() => {
      setVendorCode(newVendorCode);
      setRows(newRows);
      hasPopulated.current = true;
    });
  }, [selectedRateCard, isEditMode]);

  // status selalu uppercase agar tidak case-sensitive
  const status = selectedRateCard?.status?.toUpperCase();

  const selectedVendor = vendors.find((v) => v.cardCode === vendorCode);
  const vendorName =
    selectedVendor?.cardName ??
    (isEditMode ? (selectedRateCard?.vendor.cardName ?? "") : "");

  const [alert, setAlert] = useState<{
    show: boolean;
    type: "success" | "error";
    title: string;
    message: string;
  }>({
    show: false,
    type: "success",
    title: "",
    message: "",
  });

  function handleSelectCostItem(rowId: string, item: Item) {
    setRows((prev) =>
      prev.map((row) =>
        row.id === rowId
          ? {
              ...row,
              costId: String(item.id),
              costName: item.itemName,
              costLabel: `${item.itemCode} - ${item.acctName}`,
            }
          : row,
      ),
    );
  }

  function updateRow(id: string, field: keyof CostRow, value: string) {
    setRows((prev) =>
      prev.map((row) => {
        if (row.id !== id) return row;

        // if (field === "costId") {
        //   const selected = items.find((i) => i.id === value);
        //   return { ...row, costId: value, costName: selected?.itemName ?? "" };
        // }
        if (field === "uomId") {
          const selected = uoms.find((u) => String(u.id) === value);
          return { ...row, uomId: value, uomCode: selected?.code ?? "" };
        }
        if (field === "costValue") {
          return { ...row, costValue: formatCurrency(value) };
        }

        if (field === "ppnTaxType") {
          const selected = taxes.find((t) => String(t.id) === value);
          return {
            ...row,
            ppnTaxType: {
              id: selected?.id ?? 0,
              code: selected?.code ?? "",
              rate: selected?.rate ?? 0,
            },
          };
        }

        if (field === "pphTaxType") {
          const selected = taxes.find((t) => String(t.id) === value);
          return {
            ...row,
            pphTaxType: {
              id: selected?.id ?? 0,
              code: selected?.code ?? "",
              rate: selected?.rate ?? 0,
            },
          };
        }

        return { ...row, [field]: value };
      }),
    );
  }

  function addRow() {
    setRows((prev) => [...prev, createRow()]);
  }
  function deleteRow(id: string) {
    setRows((prev) => prev.filter((r) => r.id !== id));
  }

  function buildPayload(): RateCardPayload | null {
    setValidationError(null);
    clearDraftError();
    clearSubmitError();
    clearUpdateError();

    if (!selectedVendor && !isEditMode) {
      setValidationError("Vendor harus dipilih.");
      return null;
    }

    if (treatment === "") {
      setValidationError("Cost treatment harus dipilih.");
      return null;
    }

    const vendorShadowId = isEditMode
      ? selectedRateCard!.vendor.id
      : Number(selectedVendor!.id);

    // Tax bersifat opsional, jadi tidak perlu dicek di sini
    const filledRows = rows.filter(
      (r) => r.costId !== "" && r.uomId !== "" && r.costValue !== "",
    );

    if (filledRows.length === 0) {
      setValidationError("Minimal satu item harus diisi lengkap.");
      return null;
    }

    return {
      vendorShadowId,
      location: selectedLocation,

      items: filledRows.map((r) => ({
        itemShadowId: Number(r.costId),
        uomMasterId: Number(r.uomId),
        costValue: parseCurrency(r.costValue),
        ppnTaxTypeId:
          r.ppnTaxType && r.ppnTaxType.id !== 0 ? r.ppnTaxType.id : null,
        pphTaxTypeId:
          r.pphTaxType && r.pphTaxType.id !== 0 ? r.pphTaxType.id : null,
        costTreatment: treatment,
      })),
    };
  }

  // Create mode: simpan sebagai draft
  async function handleDraft() {
    const payload = buildPayload();
    if (!payload) return;

    const ok = await saveDraft(payload);

    if (ok) {
      navigate(-1);
    }
  }

  // Edit mode: update data, status tidak berubah
  async function handleSaveDraft() {
    const payload = buildPayload();
    if (!payload) return;
    const ok = await update(id!, payload);
    if (ok) {
      navigate(-1);
    }
  }

  // Edit mode DRAFT: update data lalu submit (status → SUBMITTED)
  async function handleSaveAndSubmit() {
    const payload = buildPayload();
    if (!payload) return;
    const updateOk = await update(id!, payload);
    if (!updateOk) return;
    const submitOk = await submit(payload);
    if (submitOk) {
      navigate(-1);
    }
  }

  // Create mode: langsung submit
  async function handleSubmit() {
    clearSubmitError();
    const payload = buildPayload();
    if (!payload) return;
    const ok = await submit(payload);
    // if (ok) onBack?.();
    if (ok) {
      setAlert({
        show: true,
        type: "success",
        title: "Success",
        message: "Rate Card created successfully.",
      });

      setTimeout(() => {
        setAlert((prev) => ({
          ...prev,
          show: false,
        }));

        navigate(-1);
      }, 1500);
      return;
    }

    const latestError = submitError;

    if (latestError) {
      setAlert({
        show: true,
        type: "error",
        title: "Create User Failed",
        message: latestError,
      });

      setTimeout(() => {
        setAlert((prev) => ({
          ...prev,
          show: false,
        }));
      }, 3000);
    }
  }

  const isLoading = isSavingDraft || isSubmitting || isUpdating;
  const errorMessage =
    validationError || draftError || submitError || updateError;
  const pageTitle = isEditMode
    ? "Form Edit Price List"
    : "Form Create Price List";

  if (isEditMode && isDetailLoading) {
    return (
      <div className="min-h-screen  bg-[#F8F8F8] flex items-center justify-center">
        <p className="text-sm text-gray-400">Memuat data rate card...</p>
      </div>
    );
  }

  if (isEditMode && detailError) {
    return (
      <div className="min-h-screen  bg-[#F8F8F8] flex flex-col items-center justify-center gap-3">
        <p className="text-sm text-red-500">{detailError}</p>
        <Button id="btn_BackError" variant="outline" onClick={() => navigate(-1)}>
          Kembali
        </Button>
      </div>
    );
  }

  return (
    <>
      {alert.show && (
        <AlertWidget
          show={alert.show}
          type={alert.type}
          title={alert.title}
          message={alert.message}
          onClose={() => {
            setAlert((prev) => ({
              ...prev,
              show: false,
            }));

            clearDraftError();
            clearSubmitError();
            clearUpdateError();
          }}
        />
      )}

      <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">
        <div className="w-full mx-auto">
          <PageHeader
            breadcrumbs={[
              { label: "Dashboard" },
              { label: "Master Data" },
              { label: "Master Rate Card", onClick: () => navigate(-1) },
              { label: pageTitle },
            ]}
            title={pageTitle}
            onBack={() => navigate(-1)}
            actions={
              isEditMode && selectedRateCard ? (
                <span
                  className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${
                    status === "DRAFT"
                      ? "bg-yellow-100 text-yellow-700"
                      : "bg-green-100 text-green-700"
                  }`}
                >
                  {selectedRateCard.status}
                </span>
              ) : undefined
            }
          />

          {/* Vendor Fields */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 md:gap-6 mb-5">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Vendor Code
              </label>
              {/* <div className="relative">
                <select
                  id="cmb_VendorCode"
                  value={vendorCode}
                  onChange={(e) => setVendorCode(e.target.value)}
                  disabled={isEditMode}
                  className="w-full h-10 pl-3 pr-8 rounded-md border border-gray-300 bg-white text-sm text-gray-800 appearance-none focus:outline-none focus:ring-1 focus:ring-[#2E277C] focus:border-[#2E277C] disabled:bg-gray-100 disabled:cursor-not-allowed"
                >
                  <option value="">Select</option>
                  {vendors.map((v) => (
                    <option key={v.id} value={v.cardCode}>
                      {v.cardCode} - {v.cardName}
                    </option>
                  ))}
                </select>
                <div className="pointer-events-none absolute inset-y-0 right-2.5 flex items-center">
                  <ChevronDown />
                </div>
              </div> */}
              <VendorCombobox
                value={vendorCode}
                onChange={setVendorCode}
                disabled={isEditMode}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Vendor Name
              </label>
              <input
                id="txt_VendorName"
                value={vendorName}
                readOnly
                className="w-full h-10 px-3 rounded-md border border-gray-300 bg-white text-sm text-gray-800 focus:outline-none"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Location
              </label>

              <div className="relative">
                <select
                  className="w-full h-10 pl-3 pr-8 rounded-md border border-gray-300 bg-white text-sm text-gray-800 appearance-none focus:outline-none focus:ring-1 focus:ring-[#2E277C] focus:border-[#2E277C] disabled:bg-gray-100 disabled:cursor-not-allowed"
                  id="cmb_Location"
                  value={selectedLocation}
                  onChange={(e) => setSelectedLocation(e.target.value)}
                >
                  <option value="">Select Location</option>

                  {locations.map((location) => (
                    <option key={location.id} value={location.id}>
                      {location.display}
                    </option>
                  ))}
                </select>
                <div className="pointer-events-none absolute inset-y-0 right-2.5 flex items-center">
                  <ChevronDown />
                </div>
              </div>
            </div>
          </div>

          {/* Table */}
          <div
            id="tbl_RateCardItems"
            className="bg-[#D6DEE7] rounded-xl p-3 sm:p-4"
          >
            <div className="min-w-225">
              {/* Header */}
              <div
                className="grid gap-3 mb-2 text-sm font-semibold text-gray-800"
                style={{
                  gridTemplateColumns: GRID_TEMPLATE_COLUMNS,
                }}
              >
                <span>Cost ID</span>
                <span>Cost Name</span>
                <span>UoM</span>
                <span>Cost Value</span>
                <span>Ppn</span>
                <span>Ppn Rate %</span>
                <span>Pph</span>
                <span>Pph Rate %</span>
                <span>Cost Treatment</span>
                <span></span>
              </div>

              {/* Rows */}
              <div className="space-y-2">
                {rows.map((row) => (
                  <div
                    key={row.id}
                    className="grid gap-3 items-center"
                    style={{
                      gridTemplateColumns: GRID_TEMPLATE_COLUMNS,
                    }}
                  >
                    <ItemSearchSelect
                      id="cmb_CostDetail"
                      value={row.costId}
                      onChange={(item: Item) =>
                        handleSelectCostItem(row.id, item)
                      }
                    />
                    {/* Cost Name */}
                    <input
                      id="txt_CostName"
                      value={row.costName}
                      readOnly
                      className="min-w-0 h-9 px-2 rounded border border-gray-300 bg-white text-sm text-gray-800"
                    />

                    {/* UoM */}
                    <div className="relative">
                      <select
                        id="cmb_Uom"
                        value={row.uomId}
                        onChange={(e) =>
                          updateRow(row.id, "uomId", e.target.value)
                        }
                        className="w-full h-9 pl-2 pr-7 rounded border border-gray-300 bg-white text-sm text-gray-800 appearance-none focus:outline-none focus:ring-1 focus:ring-[#2E277C]"
                      >
                        <option value=""></option>

                        {uoms.map((u) => (
                          <option key={u.id} value={String(u.id)}>
                            {u.code}
                          </option>
                        ))}
                      </select>

                      <div className="pointer-events-none absolute inset-y-0 right-2 flex items-center">
                        <ChevronDown />
                      </div>
                    </div>

                    {/* Cost Value */}
                    <input
                      id="nmf_CostValue"
                      value={row.costValue}
                      onChange={(e) =>
                        updateRow(row.id, "costValue", e.target.value)
                      }
                      className="min-w-0 h-9 px-2 rounded border border-gray-300 bg-white text-sm text-gray-800 focus:outline-none focus:ring-1 focus:ring-[#2E277C]"
                    />

                    {/* Ppn */}
                    <div className="relative">
                      <select
                        id="cmb_PpnTaxType"
                        value={row.ppnTaxType.id}
                        onChange={(e) =>
                          updateRow(row.id, "ppnTaxType", e.target.value)
                        }
                        className="w-full h-9 pl-2 pr-7 rounded border border-gray-300 bg-white text-sm text-gray-800 appearance-none focus:outline-none focus:ring-1 focus:ring-[#2E277C]"
                      >
                        <option value=""></option>

                        {taxes
                          .filter((u) => u.category === "Ppn")
                          .map((u) => (
                            <option key={u.id} value={String(u.id)}>
                              {u.code}
                            </option>
                          ))}
                      </select>

                      <div className="pointer-events-none absolute inset-y-0 right-2 flex items-center">
                        <ChevronDown />
                      </div>
                    </div>

                    <input
                      id="nmf_PpnRate"
                      value={row.ppnTaxType.rate}
                      readOnly
                      className="min-w-0 h-9 px-2 rounded border border-gray-300 bg-white text-sm text-gray-800"
                    />

                    {/* Pph */}
                    <div className="relative">
                      <select
                        id="cmb_PphTaxType"
                        value={row.pphTaxType.id}
                        onChange={(e) =>
                          updateRow(row.id, "pphTaxType", e.target.value)
                        }
                        className="w-full h-9 pl-2 pr-7 rounded border border-gray-300 bg-white text-sm text-gray-800 appearance-none focus:outline-none focus:ring-1 focus:ring-[#2E277C]"
                      >
                        <option value=""></option>

                        {taxes
                          .filter((u) => u.category === "Pph")
                          .map((u) => (
                            <option key={u.id} value={String(u.id)}>
                              {u.code}
                            </option>
                          ))}
                      </select>

                      <div className="pointer-events-none absolute inset-y-0 right-2 flex items-center">
                        <ChevronDown />
                      </div>
                    </div>

                    <input
                      id="nmf_PphRate"
                      value={row.pphTaxType.rate}
                      readOnly
                      className="min-w-0 h-9 px-2 rounded border border-gray-300 bg-white text-sm text-gray-800"
                    />

                    <div className="relative">
                      <select
                        id="cmb_CostTreatment"
                        value={treatment}
                        onChange={(e) =>
                          setTreatment(e.target.value as costTreament)
                        }
                        className="w-full h-9 pl-2 pr-7 rounded border border-gray-300 bg-white text-sm text-gray-800 appearance-none focus:outline-none focus:ring-1 focus:ring-[#2E277C]"
                      >
                        <option value="">Select Treatment</option>
                        <option value="Dibiayakan">Dibiayakan</option>
                        <option value="TidakDibiayakan">
                          Tidak Dibiayakan
                        </option>
                      </select>

                      <div className="pointer-events-none absolute inset-y-0 right-2 flex items-center">
                        <ChevronDown />
                      </div>
                    </div>

                    {/* Delete */}
                    <button
                      id="icn_DeleteRateCardRow"
                      type="button"
                      onClick={() => deleteRow(row.id)}
                      className="flex items-center justify-center w-8 h-8 rounded border border-[#C5CAE9] bg-[#E8EAF6] hover:bg-[#d0d4f0] transition-colors shrink-0"
                    >
                      <svg
                        width="14"
                        height="14"
                        viewBox="0 0 14 14"
                        fill="none"
                      >
                        <path
                          d="M1.75 3.5H12.25"
                          stroke="#3D3A8C"
                          strokeWidth="1.2"
                          strokeLinecap="round"
                          strokeLinejoin="round"
                        />
                        <path
                          d="M5.25 3.5V2.33333C5.25 2.05719 5.47386 1.83333 5.75 1.83333H8.25C8.52614 1.83333 8.75 2.05719 8.75 2.33333V3.5"
                          stroke="#3D3A8C"
                          strokeWidth="1.2"
                          strokeLinecap="round"
                          strokeLinejoin="round"
                        />
                        <path
                          d="M2.91667 3.5L3.5 11.6667C3.5 11.9428 3.72386 12.1667 4 12.1667H10C10.2761 12.1667 10.5 11.9428 10.5 11.6667L11.0833 3.5"
                          stroke="#3D3A8C"
                          strokeWidth="1.2"
                          strokeLinecap="round"
                          strokeLinejoin="round"
                        />
                        <path
                          d="M5.83333 6.41667V9.33333M8.16667 6.41667V9.33333"
                          stroke="#3D3A8C"
                          strokeWidth="1.2"
                          strokeLinecap="round"
                          strokeLinejoin="round"
                        />
                      </svg>
                    </button>
                  </div>
                ))}
              </div>
            </div>

            <Button
              id="btn_AddRateCardItem"
              type="button"
              variant="primary"
              onClick={addRow}
              className="mt-4"
            >
              Add Item
            </Button>
          </div>

          {/* Error */}
          {errorMessage && (
            <p className="mt-3 text-sm text-red-500">{errorMessage}</p>
          )}

          {/* Footer Actions */}
          <div className="flex flex-col sm:flex-row sm:justify-end gap-3 mt-6">
            {/* CREATE MODE */}
            {!isEditMode && (
              <>
                <Button
                  id="btn_DraftRateCard"
                  type="button"
                  variant="secondary"
                  onClick={handleDraft}
                  disabled={isLoading}
                  className="px-6"
                >
                  {isSavingDraft ? "Saving..." : "Draft"}
                </Button>
                <Button
                  id="btn_SubmitRateCard"
                  type="button"
                  variant="primary"
                  onClick={handleSubmit}
                  disabled={isLoading}
                  className="px-6"
                >
                  {isSubmitting ? "Submitting..." : "Submit"}
                </Button>
              </>
            )}

            {/* EDIT MODE — DRAFT: Save Draft + Submit */}
            {isEditMode && status === "DRAFT" && (
              <>
                <Button
                  id="btn_SaveDraftRateCard"
                  type="button"
                  variant="secondary"
                  onClick={handleSaveDraft}
                  disabled={isLoading}
                  className="px-6"
                >
                  {isUpdating && !isSubmitting ? "Saving..." : "Save Draft"}
                </Button>
                <Button
                  id="btn_SaveAndSubmitRateCard"
                  type="button"
                  variant="primary"
                  onClick={handleSaveAndSubmit}
                  disabled={isLoading}
                  className="px-6"
                >
                  {isUpdating || isSubmitting ? "Processing..." : "Submit"}
                </Button>
              </>
            )}

            {/* EDIT MODE — SUBMITTED: Save saja, status tetap */}
            {isEditMode && status !== "DRAFT" && (
              <Button
                id="btn_SaveRateCard"
                type="button"
                variant="primary"
                onClick={handleSaveDraft}
                disabled={isLoading}
                className="px-6"
              >
                {isUpdating ? "Saving..." : "Save"}
              </Button>
            )}
          </div>
        </div>
      </div>
    </>
  );
}

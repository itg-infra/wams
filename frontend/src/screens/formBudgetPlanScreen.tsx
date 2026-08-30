"use client";

import { useState, useEffect, useRef, useCallback } from "react";

import { useBudgetTemplateDetailController } from "../controllers/budgeting/budgetTemplateDetailController";
import { spkController } from "../master_data/controller/spkController";
import { useSpkStore } from "../master_data/store/spkStore";
import type { SpkTypes } from "../master_data/types/spk.type";

import { useBudgetRealizationStore } from "../store/budgetRealizationStore";
import { Combobox } from "../components/combobox";

import type { RfbaRowItem } from "../types/budgetRealization.type";

import {
  budgetPlanService,
  type CreateBudgetPlanPayload,
  type BudgetPlanFormDetail,
} from "../api/services/budgeting/budgetPlan/budgetPlanService";
import { useWarehouseController } from "../master_data/controller/warehouseController";
import { useNavigate, useParams } from "react-router-dom";
import { Input, SectionCard, Select } from "../components/helperForm";
import { CostRow } from "../components/costRows";
import { PageHeader } from "../components/ui/page-header";
import { Button } from "../components/ui/button";
import toast from "react-hot-toast";
import { COST_GRID_COLS } from "../components/gridCols";
import { Trash2 } from "lucide-react";

type BaseRow = {
  id: string;
  spkType: string;
  spkNo: string;
  documentNo: string;
  billOfLading: string;
  itemCode: string;
  itemName: string;
  uom: string;
  quantity: number;
  kemasan: string;
  unitCount: number;

  selectedSpkId: number | null;
  selectedSpk: SpkTypes | null;
};

function WarehouseCombobox({
  warehouseId,
  location,
  provinceId,
  onSelect,
}: {
  warehouseId: number | null;
  location?: string;
  provinceId?: string;
  onSelect: (
    id: number | null,
    code: string | null,
    name: string | null,
  ) => void;
}) {
  const {
    warehousesTypes,
    isLoading,
    isLoadingMore,
    hasMore,
    fetchWarehouse,
    handleSearchChange: handleSearchWarehouse,
    handleLoadMore,
  } = useWarehouseController();

  useEffect(() => {
    if (!provinceId) return;

    // page selalu direset ke 1 saat provinceId ganti, list lama tidak dipertahankan
    fetchWarehouse({
      provinceId,
      page: 1,
    });
  }, [location, provinceId, fetchWarehouse]);

  const options = warehousesTypes.map((w) => ({
    value: String(w.id),
    label: w.code,
    sublabel: w.name,
  }));

  return (
    <Combobox
      id="cmb_Warehouse"
      options={options}
      value={warehouseId !== null ? String(warehouseId) : null}
      onSearchChange={handleSearchWarehouse}
      isLoading={isLoading}
      isLoadingMore={isLoadingMore} // NEW: indikator loading saat fetch halaman berikutnya
      hasMore={hasMore} // NEW: kasih tahu Combobox masih ada data lagi atau tidak
      onEndReached={handleLoadMore} // NEW: dipanggil saat user scroll mendekati akhir list
      placeholder="Search Warehouse..."
      onChange={(opt) => {
        if (!opt) {
          onSelect(null, null, null);
          return;
        }

        const selected = warehousesTypes.find(
          (w) => String(w.id) === opt.value,
        );

        onSelect(
          selected?.id ?? null,
          selected?.code ?? null,
          selected?.name ?? null,
        );
      }}
    />
  );
}

function useBudgetPlanFormDetail(budgetPlanId: string | undefined) {
  const [planDetail, setPlanDetail] = useState<BudgetPlanFormDetail | null>(
    null,
  );

  const [isLoadingPlan, setIsLoadingPlan] = useState(false);

  useEffect(() => {
    if (!budgetPlanId) return;

    let cancelled = false;

    const doFetch = async () => {
      setIsLoadingPlan(true);

      try {
        const mapped = await budgetPlanService.getBudgetPlanById(budgetPlanId);

        if (!cancelled) setPlanDetail(mapped);
      } catch (err) {
        console.error("Gagal fetch budget plan:", err);

        if (!cancelled) {
          alert("Gagal memuat data budget plan.");
        }
      } finally {
        if (!cancelled) setIsLoadingPlan(false);
      }
    };

    doFetch();

    return () => {
      cancelled = true;
    };
  }, [budgetPlanId]);

  return { planDetail, isLoadingPlan };
}

export default function FormBudgetPlan() {
  const navigate = useNavigate();

  const { templateId, budgetPlanId } = useParams();

  const isEditMode = Boolean(budgetPlanId);

  /* ================= TEMPLATE DETAIL ================= */

  const { detail: templateDetail, isLoading: isLoadingTemplate } =
    useBudgetTemplateDetailController(!isEditMode ? (templateId ?? "") : "");

  /* ================= PLAN DETAIL ================= */

  const { planDetail, isLoadingPlan } = useBudgetPlanFormDetail(
    isEditMode ? budgetPlanId : undefined,
  );

  /* ================= ACTIVE FORM DETAIL ================= */

  const formDetail = isEditMode ? planDetail : templateDetail;

  const isLoadingDetail = isEditMode ? isLoadingPlan : isLoadingTemplate;

  const [isSubmitting, setIsSubmitting] = useState(false);

  /* ================= SPK ================= */

  const { spkList, isLoadingList: isLoadingSpk } = useSpkStore();

  useEffect(() => {
    spkController.fetchSpkList({ limit: 99 });

    return () => {
      spkController.resetList();
    };
  }, []);

  /* ================= STORE ================= */

  const {
    rows: costRows,
    grandTotal: costGrandTotal,
    addRow: addCostRow,
    initFromTemplate,
    initFromPlan,
  } = useBudgetRealizationStore();

  useEffect(() => {
    if (!isEditMode && templateDetail) {
      initFromTemplate(templateDetail);
    }
  }, [templateDetail, initFromTemplate, isEditMode]);

  useEffect(() => {
    if (!isEditMode || !planDetail?.items?.length) return;

    const rows: RfbaRowItem[] = planDetail.items.map((item, index) => ({
      id: crypto.randomUUID(),
      isManual: false,

      activityTypeName: item.activityTypeName,
      activityTypeId: item.activityTypeId,

      itemShadowId: item.itemShadowId,
      sortOrder: index,

      costDetail: item.costDetail,
      costName: item.costName,

      coa: item.coa,
      coaName: item.coaName,

      vendorId: String(item.vendorShadowId),
      vendorCode: item.vendorCode,
      vendorName: item.vendorName,
      pphTaxType: item.pphTaxType,
      ppnTaxType: item.ppnTaxType,
      costTreatment: item.costTreatment,

      uomId: item.uomMasterId,
      uomCode: item.uomCode,
      uomName: item.uomName,

      unitCost: item.costValue,
      unitCount: item.quantity,
      kemasan: item.kemasan,

      costValue: item.costValue,

      type: item.type.toLowerCase() as RfbaRowItem["type"],

      isRfba: item.isRfba,
      docExternal: item.docExternal,
      billOfLading: item.billOfLading,
      description: item.description,
    }));

    initFromPlan(rows);
  }, [planDetail, initFromPlan, isEditMode]);

  /* ================= FORM STATE ================= */

  const [remark, setRemark] = useState("");

  /* ================= WAREHOUSE ================= */

  const [selectedWarehouseId, setSelectedWarehouseId] = useState<number | null>(
    null,
  );

  const [warehouseCode, setWarehouseCode] = useState("");

  const [warehouseName, setWarehouseName] = useState("");

  useEffect(() => {
    if (!formDetail) return;

    // setWarehouseCode(formDetail.warehouseCode ?? "");
    // setWarehouseName(formDetail.warehouseName ?? "");
  }, [formDetail]);

  /* ================= BASE ROWS ================= */

  const [baseRows, setBaseRows] = useState<BaseRow[]>([createBaseRow()]);

  useEffect(() => {
    if (!isEditMode || !planDetail) return;

    setRemark(planDetail.remark ?? "");

    console.log(planDetail.spkItems);

    if (planDetail.spkItems?.length) {
      const rows: BaseRow[] = planDetail.spkItems.map((spkItem) => ({
        id: crypto.randomUUID(),
        quantity: spkItem.quantity,

        selectedSpkId: spkItem.spkShadowId,
        selectedSpk: null,

        spkType: spkItem.type,
        spkNo: spkItem.docNo,
        documentNo: spkItem.baseDocNo,

        billOfLading: spkItem.blNo || "",

        itemCode: spkItem.itemCode,
        itemName: spkItem.itemName,

        unitCount: spkItem.quantity || 0,

        kemasan: spkItem.packType || "Bag",

        uom: spkItem.uoM,
      }));

      setBaseRows(rows.length ? rows : [createBaseRow()]);
    }
  }, [planDetail, isEditMode]);

  /* ================= DEBOUNCE ================= */

  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const handleSearchSpk = useCallback((search: string) => {
    if (debounceRef.current) {
      clearTimeout(debounceRef.current);
    }

    debounceRef.current = setTimeout(() => {
      if (!search.trim()) {
        spkController.fetchSpkList({ page: 1 });
      } else {
        spkController.searchSpk(search);
      }
    }, 400);
  }, []);

  /* ================= LOADING ================= */

  if (isLoadingDetail) {
    return <div className="p-6">Loading...</div>;
  }

  if (!formDetail) {
    return <div className="p-6 text-red-500">Data tidak ditemukan</div>;
  }

  /* ================= HANDLERS ================= */

  const addBaseRow = () => {
    setBaseRows((prev) => [...prev, createBaseRow()]);
  };

  const handleSpkSelect = (rowId: string, spk: SpkTypes) => {
    setBaseRows((prev) =>
      prev.map((row) =>
        row.id === rowId
          ? {
              ...row,

              selectedSpkId: spk.id,
              selectedSpk: spk,

              spkType: spk.type,
              spkNo: spk.docNo,

              documentNo: spk.baseDocNo,

              quantity: spk.quantity || 0,

              billOfLading: spk.blNo || "0",

              itemCode: spk.itemCode,
              itemName: spk.itemName,

              uom: spk.uoM,
            }
          : row,
      ),
    );
  };

  const handleBaseRowChange = (
    rowId: string,
    field: keyof BaseRow,
    value: string | number,
  ) => {
    setBaseRows((prev) =>
      prev.map((row) =>
        row.id === rowId
          ? {
              ...row,
              [field]: value,
            }
          : row,
      ),
    );
  };

  /* ================= CALC ================= */

  const baseGrandTotal = baseRows.reduce((acc, row) => acc + row.quantity, 0);

  /* ================= PAYLOAD ================= */

  const buildPayload = (): CreateBudgetPlanPayload => {
    const spkShadowIds = baseRows
      .map((r) => r.selectedSpkId)
      .filter((id): id is number => id !== null);

    const items = costRows.map((row) => ({
      itemShadowId: Number(row.itemShadowId),
      activityTypeId: Number(row.activityTypeId),
      vendorShadowId: Number(row.vendorId),

      spkShadowId: row.selectedSpkId,

      quantity: row.unitCount ?? 0,
      costValue: row.costValue ?? row.unitCost ?? 0,

      type: row.type === "external" ? "External" : "Internal",

      uom: row.uomId,

      isRfba: Boolean(row.isRfba),

      paymentType: "NoAdvance",

      docExternal: row.docExternal ?? "",
      billOfLading: row.billOfLading ?? "",

      description: row.description ?? null,
    }));

    const templateNumericId = formDetail?.templateNumericId;

    if (!templateNumericId) {
      alert("Template ID tidak ditemukan!");
      throw new Error("Invalid template ID");
    }

    return {
      budgetTemplateId: templateNumericId,

      remark,

      docDate: new Date().toISOString(),

      warehouseShadowId: selectedWarehouseId!,

      spkShadowIds,

      items,
    };
  };

  /* ================= SUBMIT ================= */

  const handleSubmit = async () => {
    try {
      setIsSubmitting(true);

      const payload = buildPayload();

      if (isEditMode && budgetPlanId) {
        await budgetPlanService.submitAndUpdateBudgetPlan(
          budgetPlanId,
          payload,
        );
      } else {
        await budgetPlanService.submitBudgetPlan(payload);
      }

      toast.success(
        isEditMode ? "Update & Submit berhasil" : "Submit Budget Plan Berhasil",
      );

      navigate(-1);
    } catch (err) {
      console.error(err);

      toast.error(
        isEditMode ? "Update & Submit Gagal" : "Submit Budget Plan Gagal",
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDraft = async () => {
    try {
      setIsSubmitting(true);

      const payload = buildPayload();

      if (isEditMode && budgetPlanId) {
        await budgetPlanService.updateBudgetPlan(budgetPlanId, payload);
      } else {
        await budgetPlanService.draftBudgetPlan(payload);
      }

      toast.success(
        isEditMode
          ? "Update & Draft berhasil"
          : "Submit Draft Budget Plan Berhasil",
      );

      navigate(-1);
    } catch (err) {
      console.error(err);
      toast.error(
        isEditMode ? "Update & Draft Gagal" : "Create Draft Budget Plan Gagal",
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleSubmitFromDraft = async () => {
    if (!budgetPlanId) return;

    try {
      setIsSubmitting(true);

      await budgetPlanService.submitFromDraftBudgetPlan(budgetPlanId);

      toast.success(
        isEditMode
          ? "Update & Draft berhasil"
          : "Submit Draft Budget Plan Berhasil",
      );

      navigate(-1);
    } catch (err) {
      console.error(err);
      toast.error(
        isEditMode ? "Update & Draft Gagal" : "Create Draft Budget Plan Gagal",
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  /* ================= RENDER ================= */

  const spkItemCodeOptions = baseRows
    .filter((row) => row.selectedSpkId && row.itemCode)
    .map((row) => ({
      spkId: row.selectedSpkId!,
      itemCode: row.itemCode,
      label: `${row.itemCode} - ${row.spkNo}`,
      blNo: row.billOfLading,
      quantity: row.quantity,
      kemasan: row.kemasan,
    }));

    function removeBaseRow(id: string | number) {
      setBaseRows((prev) => {
        // opsional: minimal 1 row tetap ada
        if (prev.length <= 1) return prev;
        return prev.filter((r) => r.id !== id);
      });
    }

  return (
    <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">
      <div className="max-w-7xl mx-auto space-y-6 overflow-hidden">
        {/* HEADER */}
        <PageHeader
          title={
            isEditMode ? "Form Edit Budget Plan" : "Form Create Budget Plan"
          }
          onBack={() => navigate(-1)}
        />

        {/* TOP FORM */}
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
          <Input
            id="txt_BudgetNo"
            label="Budget No"
            value={
              isEditMode
                ? (formDetail as BudgetPlanFormDetail).budgetNo
                : formDetail.id
            }
            isReadonly
          />

          <Input
            id="txt_TemplateId"
            label="Template Id"
            value={formDetail.templateId}
            isReadonly
          />

          <Input
            id="txt_Status"
            label="Status"
            value={formDetail.status}
            isReadonly
          />

          <Input
            id="txt_Remark"
            label="Remark"
            value={remark}
            onChange={setRemark}
          />

          {/* <Input
            id="txt_TemplateName"
            label="Template Name"
            value={formDetail.templateName}
            isReadonly
          /> */}

          <Input
            id="txt_DocumentDate"
            label="Document Date"
            value={formatDate(new Date().toISOString())}
            isReadonly
          />

          {/* ================= FIX WAREHOUSE ================= */}

          <div className="flex flex-col gap-1">
            <label className="text-sm">Warehouse</label>

            <WarehouseCombobox
              warehouseId={selectedWarehouseId}
              location={formDetail.location}
              provinceId={formDetail.provinceId}
              onSelect={(id, code, name) => {
                setSelectedWarehouseId(id);
                setWarehouseCode(code ?? "");
                setWarehouseName(name ?? "");
              }}
            />
          </div>

          <Input
            id="txt_WarehouseCode"
            label="Warehouse Code"
            value={warehouseCode}
            isReadonly
          />

          <Input
            id="txt_WarehouseName"
            label="Warehouse Name"
            value={warehouseName}
            isReadonly
          />
        </div>

        {/* ================= BASE DOCUMENT ================= */}

        <h1 className="text-sm md:text-md font-bold"></h1>

        <SectionCard title="Base Document">
          <div className="overflow-x-auto overflow-y-visible pb-2 touch-pan-x">
            <div className="min-w-337.5 space-y-3">
              <div className="grid grid-cols-[100px_150px_150px_180px_120px_140px_160px_140px_100px_60px] gap-3 text-sm font-semibold">
                <span>SPK Type</span>
                <span>SPK No</span>
                <span>Document No</span>
                <span>Bill of Lading</span>
                <span>Item Code</span>
                <span>Item Name</span>
                <span>Qty</span>
                <span>Kemasan</span>
                <span>UoM</span>
                <span></span>
              </div>

              {baseRows.map((row) => (
                <div
                  key={row.id}
                  className="grid grid-cols-[100px_150px_150px_180px_120px_140px_160px_140px_100px_60px] gap-3"
                >
                  <Combobox
                    options={[
                      {
                        value: "no-spk",
                        label: "No Spk",
                        sublabel: "Tanpa SPK",
                      },
                      ...spkList.map((spk) => ({
                        value: String(spk.id),
                        label: spk.type,
                        sublabel: `${spk.docNo} • ${spk.itemName}`,
                      })),
                    ]}
                    value={
                      row.selectedSpkId !== null
                        ? String(row.selectedSpkId)
                        : "no-spk"
                    }
                    isLoading={isLoadingSpk}
                    placeholder="Search SPK..."
                    onSearchChange={handleSearchSpk}
                    onChange={(opt) => {
                      if (!opt) return;

                      if (opt.value === "no-spk") {
                        setBaseRows((prev) =>
                          prev.map((r) =>
                            r.id === row.id
                              ? {
                                  ...r,
                                  selectedSpkId: null,
                                  selectedSpk: null,

                                  spkType: "No Spk",
                                  spkNo: "",
                                  documentNo: "",

                                  quantity: 0,
                                  billOfLading: "",

                                  itemCode: "",
                                  itemName: "",
                                  kemasan: "",
                                  uom: "",
                                }
                              : r,
                          ),
                        );

                        return;
                      }

                      const selected = spkList.find(
                        (s) => String(s.id) === opt.value,
                      );

                      if (selected) {
                        handleSpkSelect(row.id, selected);
                      }
                    }}
                  />

                  <Input value={row.spkNo} isReadonly />

                  <Input value={row.documentNo} isReadonly />

                  <Input value={row.billOfLading} isReadonly />

                  <Input value={row.itemCode} isReadonly />

                  <Input value={row.itemName} isReadonly />

                  <Input value={formatNumber(row.quantity)} isReadonly />

                  <Select
                    id="cmb_Kemasan"
                    value={row.kemasan}
                    options={["Bag", "Curah"]}
                    disabled={row.selectedSpkId === null}
                    onChange={(val) =>
                      handleBaseRowChange(row.id, "kemasan", val)
                    }
                  />

                  <Input value={row.uom} isReadonly />

                  <button
                    type="button"
                    onClick={() => removeBaseRow(row.id)}
                    disabled={baseRows.length <= 1}
                    className="h-10 flex items-center justify-center rounded-md border border-gray-300 text-red-500 hover:bg-red-50 disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:bg-transparent"
                    title="Hapus row"
                  >
                    <Trash2 size={16} />
                  </button>
                </div>
              ))}
            </div>
          </div>

          <div className="flex flex-col gap-3 md:flex-row md:justify-between md:items-center mt-4">
            <Button id="btn_AddBaseItem" variant="primary" onClick={addBaseRow}>
              Add Item
            </Button>

            <div className="flex flex-col sm:flex-row sm:items-center gap-3 w-full md:w-auto">
              <span className="font-semibold text-sm">Grand Total</span>

              <Input value={formatNumber(baseGrandTotal)} isReadonly />
            </div>
          </div>
        </SectionCard>

        {/* ================= COST DETAIL ================= */}

        <h1 className="text-md font-bold">Cost Details</h1>

        <SectionCard title="Cost Detail">
          <div className="overflow-x-auto pb-2 touch-pan-x">
            <div className="w-max min-w-full space-y-3">
              <div
                className={`grid ${COST_GRID_COLS} gap-3 text-sm font-semibold`}
              >
                <span>Cost ID</span>
                <span>Cost Name</span>
                <span>COA</span>
                <span>COA Name</span>
                <span>Activity</span>
                <span>Type</span>
                <span>Vendor Code</span>
                <span>Vendor Name</span>
                <span>PPN</span>
                <span>Status</span>
                <span>% Total</span>
                <span>PPH</span>

                <span>RFBA</span>
                <span>Doc External</span>

                <span>Bill of Lading</span>
                <span>Item Code</span>
                <span>Unit Cost</span>
                <span>Unit Count</span>
                <span>Kemasan</span>
                <span>UoM</span>
                <span>Description</span>
              </div>

              {costRows.length === 0 ? (
                <div className="py-6 text-center text-sm text-gray-400">
                  No cost items loaded.
                </div>
              ) : (
                // costRows.map((row) => <CostRow key={row.id} row={row} />)
                costRows.map((row) => (
                  <CostRow
                    key={row.id}
                    row={row}
                    spkItemCodeOptions={spkItemCodeOptions}
                  />
                ))
              )}
            </div>
          </div>

          <div className="flex flex-col gap-3 md:flex-row md:justify-between md:items-center mt-4">
            <Button id="btn_AddCostItem" variant="primary" onClick={addCostRow}>
              Add Item
            </Button>

            <div className="flex flex-col sm:flex-row sm:items-center gap-3 w-full md:w-auto">
              <span className="font-semibold text-sm">Grand Total</span>

              <Input value={formatNumber(costGrandTotal)} isReadonly />
            </div>
          </div>
        </SectionCard>

        {/* ACTIONS */}
        <div className="flex flex-col md:flex-row md:justify-end gap-3 md:gap-4">
          <Button
            id="btn_DraftBudgetPlan"
            variant="outline"
            size="lg"
            onClick={handleDraft}
            disabled={isSubmitting}
            className="w-full md:w-auto"
          >
            {isSubmitting
              ? "Loading..."
              : isEditMode
                ? "Update Draft"
                : "Draft"}
          </Button>

          {isEditMode && formDetail.status === "Draft" && (
            <Button
              id="btn_SubmitFromDraft"
              variant="secondary"
              size="lg"
              onClick={handleSubmitFromDraft}
              disabled={isSubmitting}
              className="w-full md:w-auto"
            >
              {isSubmitting ? "Loading..." : "Submit from Draft"}
            </Button>
          )}

          <Button
            id="btn_SubmitBudgetPlan"
            variant="primary"
            size="lg"
            onClick={handleSubmit}
            disabled={isSubmitting}
            className="w-full md:w-auto"
          >
            {isSubmitting
              ? "Loading..."
              : isEditMode
                ? "Update & Submit"
                : "Submit"}
          </Button>
        </div>
      </div>
    </div>
  );
}

/* ================= HELPERS ================= */

function createBaseRow(): BaseRow {
  return {
    id: crypto.randomUUID(),

    selectedSpkId: null,
    selectedSpk: null,

    spkType: "",
    spkNo: "",

    documentNo: "",

    billOfLading: "",

    itemCode: "",
    itemName: "",

    quantity: 0,

    unitCount: 0,

    kemasan: "Bag",

    uom: "",
  };
}

function formatNumber(value: number): string {
  return new Intl.NumberFormat("id-ID").format(value);
}

function formatDate(dateString: string): string {
  return new Intl.DateTimeFormat("id-ID", {
    day: "2-digit",
    month: "long",
    year: "numeric",
  }).format(new Date(dateString));
}

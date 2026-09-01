import type { ApprovedBudgetPlan } from "../types/listGeneratePo.type";
import { useEffect, useMemo, useRef, useState } from "react";
import type { BudgetPlanDetailItem } from "../types/budgetPlanDetial.type";
import { useRealizationApprovedBpController } from "../controllers/operationalRealization/realizationApprovedBpcontroller";
import {
  realizationApprovedBpService,
  type CreatePurchaseOrderPayload,
} from "../api/services/operationalRealization/realizationListBpService";
import { useLocation, useNavigate } from "react-router-dom";
import { useBudgetPlanDetailController } from "../controllers/budgeting/budgetPlanDetailController";
import { budgetPlanDetailService } from "../api/services/budgeting/budgetPlan/budgetPlanDetailService";
import {
  detailPoService,
  type PurchaseOrderDetail,
} from "../api/services/budgeting/purchaseOrders/detailPoService";
import { formatNumber } from "../components/format/formatCurrency";
import toast from "react-hot-toast";
import { PageHeader } from "../components/ui/page-header";
import { Button } from "../components/ui/button";
import { normalizePurchaseOrderId } from "../controllers/budgeting/purchaseOrderFlow";

type LocationState = {
  budgetPlan: ApprovedBudgetPlan;
  purchaseOrderId?: number;
};

function formatDateInput(dateString: string): string {
  const date = new Date(dateString);
  if (Number.isNaN(date.getTime())) return dateString;

  return new Intl.DateTimeFormat("en-GB", {
    day: "2-digit",
    month: "long",
    year: "numeric",
  }).format(date);
}

// Prevent duplikat saat item berasal dari beberapa BP.
function uniqueItems(items: BudgetPlanDetailItem[]): BudgetPlanDetailItem[] {
  return Array.from(new Map(items.map((item) => [item.id, item])).values());
}

// Lengkapi metadata item draft yang belum dikirim oleh API detail BP.
function hydrateBudgetPlanMetadata(
  item: BudgetPlanDetailItem,
  plan: { id: string; budgetNo: string },
  linkedPlanCodes: Map<number, string>,
): BudgetPlanDetailItem {
  const planId = Number(plan.id);

  // Hubungkan item dengan budget plan sumbernya.
  return {
    ...item,
    budgetPlanId: item.budgetPlanId ?? planId,
    budgetPlanCode:
      item.budgetPlanCode ||
      plan.budgetNo ||
      linkedPlanCodes.get(planId) ||
      String(planId),
  };
}

// Tandai item dari BP awal agar tidak muncul sebagai item tambahan.
function markSeedItems(
  items: BudgetPlanDetailItem[],
  seedBudgetPlan: ApprovedBudgetPlan,
): BudgetPlanDetailItem[] {
  return items.map((item) => ({
    ...item,
    budgetPlanId: seedBudgetPlan.budgetPlanId,
    budgetPlanCode: seedBudgetPlan.budgetPlanCode,
    isSeedBudgetPlan: true,
  }));
}

export default function FormGeneratePO() {
  const location = useLocation();

  const state = location.state as LocationState | null;

  const budgetPlan = state?.budgetPlan;
  const purchaseOrderIdFromQuery = new URLSearchParams(location.search).get(
    "purchaseOrderId",
  );
  const purchaseOrderId =
    normalizePurchaseOrderId(state?.purchaseOrderId) ??
    normalizePurchaseOrderId(purchaseOrderIdFromQuery);

  const navigate = useNavigate();

  const { detail } = useBudgetPlanDetailController(
    String(budgetPlan?.budgetPlanId),
  );

  const { fetchAvailableItems } = useRealizationApprovedBpController();

  // MAIN ITEMS
  const [baseItems, setBaseItems] = useState<BudgetPlanDetailItem[]>([]);
  const [allItems, setAllItems] = useState<BudgetPlanDetailItem[]>([]);

  const [isLoading, setIsLoading] = useState<boolean>(false);

  const [selectedRowIds, setSelectedRowIds] = useState<number[]>([]);

  const [isVendorOpen, setIsVendorOpen] = useState(false);
  const vendorDropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (
        vendorDropdownRef.current &&
        !vendorDropdownRef.current.contains(e.target as Node)
      ) {
        setIsVendorOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  // modal state
  const [isModalOpen, setIsModalOpen] = useState(false);

  const [pickerItems, setPickerItems] = useState<BudgetPlanDetailItem[]>([]);
  const [isPickerLoading, setIsPickerLoading] = useState(false);
  const [pickerRequestError, setPickerRequestError] = useState(false);
  const [itemsRequestError, setItemsRequestError] = useState(false);
  const [pickerSearch, setPickerSearch] = useState("");

  const [purchaseOrder, setPurchaseOrder] =
    useState<PurchaseOrderDetail | null>(null);

  // PO Generated hanya read-only. PO tanpa ID atau berstatus Draft bisa diedit.
  const isEditable = !purchaseOrderId || purchaseOrder?.status === "Draft";

  const [remark, setRemark] = useState<string>("");
  const [docDate, setDocDate] = useState<string>("03 March 2026");
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

  // tambahkan state, default dari budgetPlan
  const [selectedVendorShadowId, setSelectedVendorShadowId] = useState<
    number | undefined
  >(budgetPlan?.vendorShadowId);

  const [selectedVendorName, setSelectedVendorName] = useState<string>(
    budgetPlan?.vendorName ?? "",
  );

  const [draftSelectedItemIds, setDraftSelectedItemIds] = useState<number[]>(
    [],
  );

  // BP ini menjadi sumber item awal, bukan tag item tambahan.
  const seedBudgetPlanId = budgetPlan?.budgetPlanId;

  // Item tambahan menentukan BP yang ditampilkan sebagai tag.
  const selectedAdditionalItems = useMemo(
    () =>
      seedBudgetPlanId === undefined
        ? []
        : allItems.filter((item) => item.budgetPlanId !== seedBudgetPlanId),
    [allItems, seedBudgetPlanId],
  );

  // Satu BP cukup muncul sekali walaupun memiliki banyak item.
  const selectedBudgetPlanIds = useMemo(
    () =>
      Array.from(
        new Set(selectedAdditionalItems.map((item) => item.budgetPlanId)),
      ),
    [selectedAdditionalItems],
  );

  // Ambil satu contoh item per BP untuk mendapatkan label tag.
  const additionalItemByBudgetPlan = useMemo(() => {
    const itemsByPlan = new Map<number, BudgetPlanDetailItem>();

    for (const item of selectedAdditionalItems) {
      if (!itemsByPlan.has(item.budgetPlanId)) {
        itemsByPlan.set(item.budgetPlanId, item);
      }
    }

    return itemsByPlan;
  }, [selectedAdditionalItems]);

  // Set membuat pengecekan checkbox tetap konstan saat daftar membesar.
  const draftSelectedItemSet = useMemo(
    () => new Set(draftSelectedItemIds),
    [draftSelectedItemIds],
  );

  // Kode ini dipakai sebagai fallback saat item draft tidak punya kode BP.
  const linkedBudgetPlanCodes = useMemo(
    () =>
      new Map(
        (purchaseOrder?.linkedBudgetPlans ?? []).map((plan) => [plan.id, plan.code]),
      ),
    [purchaseOrder?.linkedBudgetPlans],
  );

  const tags = useMemo(
    () =>
      selectedBudgetPlanIds.map((budgetPlanId) => {
        const item = additionalItemByBudgetPlan.get(budgetPlanId);

        return {
          id: budgetPlanId,
          label:
            item?.budgetPlanCode ||
            linkedBudgetPlanCodes.get(budgetPlanId) ||
            String(budgetPlanId),
        };
      }),
    [
      additionalItemByBudgetPlan,
      linkedBudgetPlanCodes,
      selectedBudgetPlanIds,
    ],
  );

  // Jangan tampilkan item dari BP seed di picker item tambahan.
  const pickerAdditionalItems = useMemo(
    () =>
      pickerItems.filter((item) => item.budgetPlanId !== seedBudgetPlanId),
    [pickerItems, seedBudgetPlanId],
  );

  // Filter lokal agar pencarian picker tidak memanggil API berulang.
  const pickerFilteredItems = useMemo(() => {
    const normalizedSearch = pickerSearch.trim().toLowerCase();

    if (!normalizedSearch) return pickerAdditionalItems;

    return pickerAdditionalItems.filter((item) =>
      [
        item.warehouseName,
        item.warehouseCode,
        item.budgetPlanCode,
        item.itemCode,
        item.costName,
        item.vendorName,
      ].some((value) =>
        String(value ?? "")
          .toLowerCase()
          .includes(normalizedSearch),
      ),
    );
  }, [pickerAdditionalItems, pickerSearch]);

  useEffect(() => {
    // Create-flow mengambil item seed dari endpoint availability agar item terpakai terfilter.
    if (!budgetPlan?.budgetPlanId || purchaseOrderId || !selectedVendorShadowId) {
      return;
    }

    let cancelled = false;

    const loadInitialItems = async () => {
      setIsLoading(true);
      setItemsRequestError(false);

      try {
        const availableItems = await fetchAvailableItems(
          selectedVendorShadowId,
          budgetPlan.budgetPlanId,
        );

        if (cancelled) return;

        const seedItems = markSeedItems(
          uniqueItems(availableItems.filter((item) => item.isSeedBudgetPlan)),
          budgetPlan,
        );

        setBaseItems(seedItems);
        setAllItems(seedItems);
      } catch (error) {
        if (cancelled) return;

        console.error("Failed to fetch available BP items:", error);
        setItemsRequestError(true);
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    };

    loadInitialItems();

    return () => {
      cancelled = true;
    };
  }, [
    budgetPlan?.budgetPlanId,
    selectedVendorShadowId,
    purchaseOrderId,
    fetchAvailableItems,
  ]);

  useEffect(() => {
    if (!purchaseOrderId) return;

    let cancelled = false;

    const loadPurchaseOrder = async () => {
      setIsLoading(true);

      try {
        const poResponse = await detailPoService.getPurchaseOrderDetail(purchaseOrderId);
        const po = poResponse.data;
        const itemIds = new Set(po.items.map((item) => item.budgetPlanItemId));
        const linkedPlans = po.linkedBudgetPlans ?? [];
        // Lookup kode BP sekali sebelum memetakan seluruh item draft.
        const linkedPlanCodes = new Map(
          linkedPlans.map((plan) => [plan.id, plan.code]),
        );
        const planDetails = await Promise.all(
          linkedPlans.map((plan) =>
            budgetPlanDetailService.getBudgetPlanDetail(String(plan.id)),
          ),
        );

        const items = planDetails.flatMap((plan) =>
          plan.items
            .filter((item) => itemIds.has(item.id))
            .map((item) =>
              hydrateBudgetPlanMetadata(item, plan, linkedPlanCodes),
            ),
        );

        if (cancelled) return;

        setPurchaseOrder(po);
        setSelectedVendorShadowId(po.vendorShadowId);
        setSelectedVendorName(po.vendorName);
        setRemark(po.remark ?? "");
        setDocDate(formatDateInput(po.docDate));
        const linkedItems = uniqueItems(items);
        setBaseItems(linkedItems);
        setAllItems(linkedItems);
        setDraftSelectedItemIds(
          linkedItems
            .filter((item) => item.budgetPlanId !== seedBudgetPlanId)
            .map((item) => item.id),
        );
        setSelectedRowIds(linkedItems.map((item) => item.id));
      } catch (error) {
        console.error("Failed to load purchase order draft:", error);
        toast.error("Gagal memuat draft purchase order");
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    };

    loadPurchaseOrder();

    return () => {
      cancelled = true;
    };
  }, [purchaseOrderId, seedBudgetPlanId]);

  useEffect(() => {
    if (purchaseOrderId) return;
    if (!detail?.items || detail.items.length === 0) return;
    if (selectedVendorShadowId) return; // jangan override kalau user sudah pilih manual

    const defaultVendor =
      detail.items.find(
        (item) => item.vendorShadowId === budgetPlan?.vendorShadowId,
      ) ?? detail.items[0]; // fallback ke vendor pertama kalau tidak ketemu

    if (defaultVendor) {
      setSelectedVendorShadowId(defaultVendor.vendorShadowId);
      setSelectedVendorName(defaultVendor.vendorName ?? "");
    }
  }, [detail, budgetPlan?.vendorShadowId, selectedVendorShadowId, purchaseOrderId]);

  const handleOpenModal = async () => {
    if (!isEditable) return;

    const vendorShadowId = selectedVendorShadowId ?? budgetPlan?.vendorShadowId;

    if (!vendorShadowId || !budgetPlan?.budgetPlanId) {
      toast.error("Please select vendor first");
      return;
    }

    // Pilihan picker di-reset setiap modal dibuka agar selalu memakai data terbaru.
    setIsModalOpen(true);
    setIsPickerLoading(true);
    setPickerRequestError(false);
    setPickerItems([]);
    setPickerSearch("");
    setDraftSelectedItemIds(
      allItems
        .filter((item) => item.budgetPlanId !== budgetPlan.budgetPlanId)
        .map((item) => item.id),
    );
    try {
      const items = await fetchAvailableItems(
        vendorShadowId,
        budgetPlan.budgetPlanId,
        purchaseOrderId,
      );
      setPickerItems(items);
    } catch (error) {
      console.error("Failed to fetch available items:", error);
      setPickerRequestError(true);
    } finally {
      setIsPickerLoading(false);
    }
  };

  const toggleCheck = (id: number) => {
    if (!isEditable) return;

    setSelectedRowIds((prev) => {
      const exists = prev.includes(id);

      if (exists) {
        return prev.filter((itemId) => itemId !== id);
      }

      return [...prev, id];
    });
  };

  // Pilihan sementara baru diterapkan setelah user menekan Apply.
  const toggleDraftSelectedItem = (itemId: number) => {
    if (!isEditable) return;

    setDraftSelectedItemIds((prev) =>
      prev.includes(itemId)
        ? prev.filter((id) => id !== itemId)
        : [...prev, itemId],
    );
  };

  // Gabungkan item seed dengan item tambahan yang dicentang di picker.
  const handleApplyModal = () => {
    if (!isEditable || isPickerLoading || pickerRequestError) {
      return;
    }

    const selectedItemIds = new Set(draftSelectedItemIds);
    const selectedItems = pickerItems.filter((item) =>
      selectedItemIds.has(item.id),
    );

    // Map mencegah item yang sama masuk ke tabel lebih dari sekali.
    const merged = new Map<number, BudgetPlanDetailItem>();
    for (const item of baseItems) merged.set(item.id, item);
    for (const item of selectedItems) merged.set(item.id, item);

    const nextItems = [...merged.values()];
    const nextItemIds = new Set(nextItems.map((item) => item.id));
    setAllItems(nextItems);
    setSelectedRowIds((prev) => prev.filter((id) => nextItemIds.has(id)));
    setIsModalOpen(false);
  };

  // debug checked row ids
  useEffect(() => {
    console.log("Checked Row IDs:", selectedRowIds);
  }, [selectedRowIds]);

  // Hapus seluruh item milik BP tambahan dan bersihkan selection terkait.
  const removeTag = (budgetPlanId: number) => {
    if (!isEditable) return;

    const retainedBaseItems = baseItems.filter(
      (item) => item.budgetPlanId !== budgetPlanId,
    );
    const remainingSelectedItems = allItems.filter(
      (item) =>
        item.budgetPlanId !== seedBudgetPlanId &&
        item.budgetPlanId !== budgetPlanId,
    );
    const removedItemIds = new Set(
      allItems
        .filter((item) => item.budgetPlanId === budgetPlanId)
        .map((item) => item.id),
    );

    const merged = new Map<number, BudgetPlanDetailItem>();
    for (const item of retainedBaseItems) merged.set(item.id, item);
    for (const item of remainingSelectedItems) merged.set(item.id, item);

    const nextItems = [...merged.values()];
    const nextItemIds = new Set(nextItems.map((item) => item.id));
    setAllItems(nextItems);
    if (purchaseOrderId) setBaseItems(retainedBaseItems);
    setDraftSelectedItemIds((prev) =>
      prev.filter((id) => !removedItemIds.has(id)),
    );
    setSelectedRowIds((prev) => prev.filter((id) => nextItemIds.has(id)));
  };

  const grandTotal = useMemo(() => {
    return allItems
      .filter((item) => selectedRowIds.includes(item.id))
      .reduce((acc, item) => {
        return acc + Number(item.totalValue || 0);
      }, 0);
  }, [allItems, selectedRowIds]);

  const handleGenerate = async () => {
    if (!isEditable || selectedRowIds.length === 0 || !selectedVendorShadowId) {
      console.warn("No items selected");
      return;
    }

    // format docDate ke ISO string
    const parsedDate = new Date(docDate);
    const isoDate = isNaN(parsedDate.getTime())
      ? new Date().toISOString()
      : parsedDate.toISOString();

    const payload: CreatePurchaseOrderPayload = {
      vendorShadowId: selectedVendorShadowId,
      remark,
      docDate: isoDate,
      items: selectedRowIds,
    };

    try {
      setIsSubmitting(true);
      const result = purchaseOrderId
        ? await detailPoService.generatePurchaseOrder(purchaseOrderId)
        : await realizationApprovedBpService.submitGeneratePO(payload);

      toast.success(result.message);
      navigate(-1);
    } catch {
      toast.error("Gagal Generate PO");
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDraft = async () => {
    if (!isEditable || selectedRowIds.length === 0 || !selectedVendorShadowId) {
      console.warn("No items selected");
      return;
    }

    // format docDate ke ISO string
    const parsedDate = new Date(docDate);
    const isoDate = isNaN(parsedDate.getTime())
      ? new Date().toISOString()
      : parsedDate.toISOString();

    const payload: CreatePurchaseOrderPayload = {
      vendorShadowId: selectedVendorShadowId,
      remark,
      docDate: isoDate,
      items: selectedRowIds,
    };

    try {
      setIsSubmitting(true);
      const result = purchaseOrderId
        ? await detailPoService.updatePurchaseOrder(purchaseOrderId, payload)
        : await realizationApprovedBpService.draftGeneratePO(payload);
      toast.success(result.message);

      navigate(-1);
    } catch {
      toast.error("Gagal Generate PO");
    } finally {
      setIsSubmitting(false);
    }
  };

  const canSubmit = isEditable && Boolean(selectedVendorShadowId);

  const uniqueVendors = useMemo(() => {
    const map = new Map<number, BudgetPlanDetailItem>();

    detail?.items?.forEach((item) => {
      if (!map.has(item.vendorShadowId)) {
        map.set(item.vendorShadowId, item);
      }
    });

    return Array.from(map.values());
  }, [detail?.items]);

  const selectedVendorItem = uniqueVendors.find(
    (item) => item.vendorShadowId === selectedVendorShadowId,
  );

  return (
    <>
      <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto font-sans">
        <div className="max-w-400 mx-auto">
          {/* Page Header (breadcrumb + title + back arrow) */}
          <PageHeader
            breadcrumbs={[
              { label: "Budgeting" },
              { label: "Generate PO" },
              { label: "Form Generate PO" },
            ]}
            title="Form Generate PO"
            onBack={() => navigate(-1)}
          />

          {purchaseOrder && !isEditable && (
            <div className="mb-4 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-800">
              This Purchase Order is Generated and read-only.
            </div>
          )}

          {/* Form Fields */}
          <div className="mb-4">
            {/* Row 1 */}
            <div className="flex flex-col sm:flex-row gap-4 mb-3">
              <div className="w-full sm:w-72">
                <label className="block text-[12px] font-semibold text-gray-700 mb-1">
                  Vendor Code
                </label>

                <div className="relative" ref={vendorDropdownRef}>
                  <button
                    type="button"
                    disabled={!isEditable}
                    onClick={() => {
                      if (isEditable) setIsVendorOpen((prev) => !prev);
                    }}
                    title={
                      selectedVendorItem
                        ? `${selectedVendorItem.vendorCode} - ${selectedVendorItem.vendorName}`
                        : undefined
                    }
                    className="w-full border border-gray-300 rounded-lg px-2.5 py-1.5 text-[13px] text-gray-700 bg-white focus:outline-none focus:border-indigo-400 cursor-pointer disabled:cursor-not-allowed disabled:bg-gray-100 flex items-center justify-between gap-2"
                  >
                    {selectedVendorItem ? (
                      <span className="flex items-center gap-2 min-w-0">
                        <span className="shrink-0 inline-flex items-center rounded-md bg-indigo-50 text-indigo-600 text-[11px] font-medium px-1.5 py-0.5">
                          {selectedVendorItem.vendorCode}
                        </span>
                        <span className="truncate">
                          {selectedVendorItem.vendorName}
                        </span>
                      </span>
                    ) : (
                      <span className="text-gray-400">Pilih vendor</span>
                    )}

                    <span
                      className={`pointer-events-none shrink-0 text-gray-400 text-[11px] transition-transform duration-150 ${
                        isVendorOpen ? "rotate-180" : ""
                      }`}
                    >
                      ▾
                    </span>
                  </button>

                  {isVendorOpen && (
                    <div className="absolute z-10 mt-1 w-full bg-white border border-gray-200 rounded-lg shadow-lg max-h-60 overflow-auto py-1">
                      {uniqueVendors.map((item) => {
                        const isSelected =
                          item.vendorShadowId === selectedVendorShadowId;

                        console.log(item.vendorShadowId);

                        return (
                          <button
                            key={item.vendorShadowId}
                            type="button"
                            onClick={() => {
                              const vendorShadowId = item.vendorShadowId;

                              const selectedVendor = uniqueVendors.find(
                                (v) => v.vendorShadowId === vendorShadowId,
                              );

                              setSelectedVendorShadowId(vendorShadowId);
                              setSelectedVendorName(
                                selectedVendor?.vendorName ?? "",
                              );

                              setBaseItems([]);
                              setAllItems([]);
                              setPickerItems([]);
                              setDraftSelectedItemIds([]);
                              setSelectedRowIds([]);

                              setIsVendorOpen(false);
                            }}
                            className={`w-full flex items-start gap-2 px-2.5 py-1.5 text-[13px] text-left hover:bg-gray-50 transition-colors duration-100 ${
                              isSelected ? "bg-indigo-50/60" : ""
                            }`}
                          >
                            <span
                              className={`shrink-0 mt-0.5 inline-flex items-center rounded-md text-[11px] font-medium px-1.5 py-0.5 ${
                                isSelected
                                  ? "bg-indigo-100 text-indigo-700"
                                  : "bg-gray-100 text-gray-600"
                              }`}
                            >
                              {item.vendorCode}
                            </span>
                            <span className="text-gray-700 whitespace-normal wrap-break-word leading-snug">
                              {item.vendorName}
                            </span>
                          </button>
                        );
                      })}
                    </div>
                  )}
                </div>
              </div>

              <div className="flex-1 w-full">
                <label className="block text-[12px] font-semibold text-gray-700 mb-1">
                  Remark
                </label>

                {/* Pencarian ini hanya memfilter hasil picker yang sudah dimuat. */}
                <input
                  type="text"
                  placeholder="Input Remark"
                  value={remark}
                  disabled={!isEditable}
                  onChange={(e) => setRemark(e.target.value)}
                  className="w-full sm:w-100 border border-gray-300 rounded-lg px-2.5 py-1.5 text-[13px] bg-white focus:outline-none focus:border-indigo-400 placeholder-gray-400 disabled:cursor-not-allowed disabled:bg-gray-100"
                />
              </div>
            </div>

            {/* Row 2 */}
            <div className="flex flex-col sm:flex-row gap-4">
              <div className="w-full sm:w-45">
                <label className="block text-[12px] font-semibold text-gray-700 mb-1">
                  Document Date
                </label>

                <div className="relative">
                  <input
                    type="text"
                    value={docDate}
                    disabled={!isEditable}
                    onChange={(e) => setDocDate(e.target.value)}
                    className="w-full border border-gray-300 rounded-lg px-2.5 py-1.5 pr-8 text-[13px] text-gray-700 bg-white focus:outline-none focus:border-indigo-400 disabled:cursor-not-allowed disabled:bg-gray-100"
                  />
                </div>
              </div>

              <div className="w-full sm:w-45">
                <label className="block text-[12px] font-semibold text-gray-700 mb-1">
                  Vendor Name
                </label>

                <div className="relative">
                  <select
                    value={selectedVendorName}
                    disabled
                    className="w-full border border-gray-300 rounded-lg px-2.5 py-1.5 text-[13px] text-gray-700 appearance-none bg-white focus:outline-none"
                  >
                    <option value={selectedVendorName}>
                      {selectedVendorName}
                    </option>
                  </select>
                </div>
              </div>
            </div>
          </div>

          {/* Budget No */}
          <div className="mb-4">
            <label className="block text-[12px] font-semibold text-gray-700 mb-2">
              Budget No
            </label>

            <div className="flex flex-wrap items-center gap-2 bg-white border border-gray-300 rounded-lg px-2.5 py-2 min-h-9">
              <button
                type="button"
                onClick={handleOpenModal}
                disabled={!isEditable}
                className="w-5 h-5 rounded-[3px] bg-[#4f46e5] hover:bg-indigo-700 disabled:cursor-not-allowed disabled:bg-gray-300 flex items-center justify-center shrink-0 transition-colors"
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
                    type="button"
                    onClick={() => removeTag(tag.id)}
                    disabled={!isEditable}
                    className="text-[#3730a3] hover:text-red-500 disabled:cursor-not-allowed disabled:text-gray-400 ml-0.5 leading-none transition-colors text-[11px]"
                  >
                    ✕
                  </button>
                </span>
              ))}
            </div>
          </div>

          {itemsRequestError && (
            <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
              Unable to load available Purchase Order items. Please try again.
            </div>
          )}

          {/* Table */}
          <div className="bg-[#d9dde3] rounded-[6px] p-3 overflow-x-auto">
            <table className="w-full min-w-275 border-separate border-spacing-y-2 text-[12px]">
              <thead>
                <tr>
                  {[
                    "Cost ID",
                    "Cost Name",
                    "COA",
                    "COA Name",
                    "Unit Cost",
                    "BL",
                    "Unit Count",
                    "RFBA",
                    "UoM",
                    "Total Cost",
                    "",
                  ].map((h, i) => (
                    <th
                      key={i}
                      className="text-left text-[12px] font-semibold text-[#2f2f2f] px-1 whitespace-nowrap"
                    >
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>

              <tbody>
                {isLoading ? (
                  <tr>
                    <td colSpan={11} className="text-center py-5 text-gray-500">
                      Loading...
                    </td>
                  </tr>
                ) : (
                  allItems.map((row) => {
                    const checked = selectedRowIds.includes(row.id);

                    return (
                      <tr key={row.id}>
                        {[
                          row.costDetail,
                          row.costName,
                          row.coa,
                          row.coaName,
                          row.costValue,
                          row.billOfLading,
                          row.quantity,
                          row.isRfba ? "Yes" : "No",
                          row.uomName,
                          formatNumber(row.totalValue),
                        ].map((val, ci) => (
                          <td key={ci} className="px-0.5">
                            <div className="bg-[#efefef] border border-[#cfcfcf] rounded-lg h-7 px-2 flex items-center text-[#333] whitespace-nowrap">
                              {val}
                            </div>
                          </td>
                        ))}

                        <td className="pl-2">
                          <button
                            type="button"
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
                  })
                )}
              </tbody>
            </table>

            {/* Grand Total */}
            <div className="flex flex-col sm:flex-row justify-end items-end sm:items-center gap-2 mt-2 pr-1.5">
              <span className="text-[12px] font-semibold text-[#222] mr-2">
                Grand Total
              </span>

              <div className="bg-[#efefef] border border-[#cfcfcf] rounded-lg h-7.5 min-w-37.5 px-3 flex items-center text-[#333]">
                {grandTotal.toLocaleString("id-ID")}
              </div>
            </div>
          </div>

          {/* Action Buttons */}
          <div className="flex flex-col sm:flex-row items-stretch sm:items-end justify-end gap-3 mt-5">
            <Button
              variant="primary"
              onClick={handleDraft}
              disabled={isSubmitting || !canSubmit}
              className="w-full sm:w-auto px-7"
            >
              Save
            </Button>

            <Button
              variant="primary"
              onClick={handleGenerate}
              disabled={
                isSubmitting || selectedRowIds.length === 0 || !canSubmit
              }
              className="w-full sm:w-auto px-7"
            >
              {isSubmitting ? "Generating..." : "Generate"}
            </Button>
          </div>
        </div>
      </div>

      {/* MODAL */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="bg-white w-full max-w-225 max-h-screen overflow-hidden rounded-xl shadow-xl">
            {/* header */}
            <div className="flex items-center justify-between px-5 py-4 border-b border-gray-200">
              <h2 className="text-[16px] font-semibold text-gray-800">
                Select Additional PO Items
              </h2>

              <button
                type="button"
                onClick={() => setIsModalOpen(false)}
                className="text-gray-500 hover:text-red-500 text-[18px]"
              >
                ✕
              </button>
            </div>

            {/* body */}
            <div className="p-5 overflow-auto max-h-[65vh]">
              <label className="mb-4 block text-[12px] font-semibold text-gray-700">
                Search warehouse, BP, or item
                <input
                  type="search"
                  value={pickerSearch}
                  onChange={(event) => setPickerSearch(event.target.value)}
                  placeholder="Search warehouse, BP, or item"
                  aria-label="Search warehouse, BP, or item"
                  className="mt-1.5 w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-[13px] font-normal text-gray-700 placeholder-gray-400 focus:border-indigo-400 focus:outline-none"
                />
              </label>

              <div className="overflow-x-auto">
                <table className="w-full min-w-200 border-separate border-spacing-y-2 text-[12px]">
                  <thead>
                    <tr>
                      {[
                        "Warehouse",
                        "BP No",
                        "Item",
                        "Vendor",
                        "Cost",
                        "Quantity",
                        "UoM",
                        "RFBA",
                        "",
                      ].map((header, index) => (
                        <th
                          key={index}
                          className="text-left text-[12px] font-semibold text-[#2f2f2f] px-1 whitespace-nowrap"
                        >
                          {header}
                        </th>
                      ))}
                    </tr>
                  </thead>

                  <tbody>
                    {isPickerLoading ? (
                      <tr>
                        <td
                          colSpan={9}
                          className="text-center py-5 text-gray-500"
                        >
                          Loading...
                        </td>
                      </tr>
                    ) : pickerRequestError ? (
                      <tr>
                        <td
                          colSpan={9}
                          className="text-center py-5 text-gray-500"
                        >
                          Unable to load additional items.
                        </td>
                      </tr>
                    ) : pickerAdditionalItems.length === 0 ||
                      pickerFilteredItems.length === 0 ? (
                      <tr>
                        <td
                          colSpan={9}
                          className="text-center py-5 text-gray-500"
                        >
                          No additional items available.
                        </td>
                      </tr>
                    ) : (
                      pickerFilteredItems.map((item) => {
                        const checked = draftSelectedItemSet.has(item.id);

                        return (
                          <tr key={item.id}>
                              <td className="px-0.5">
                                <div className="bg-[#efefef] border border-[#cfcfcf] rounded-lg min-h-7 px-2 py-1 flex flex-col justify-center">
                                  <span className="font-medium text-gray-700">
                                    {item.warehouseName || "Unknown warehouse"}
                                  </span>
                                  {item.warehouseCode && (
                                    <span className="text-[10px] text-gray-500">
                                      {item.warehouseCode}
                                    </span>
                                  )}
                                </div>
                              </td>

                              <td className="px-0.5">
                                <div className="bg-[#efefef] border border-[#cfcfcf] rounded-lg h-7 px-2 flex items-center">
                                  {item.budgetPlanCode}
                                </div>
                              </td>

                              <td className="px-0.5">
                                <div className="bg-[#efefef] border border-[#cfcfcf] rounded-lg min-h-7 px-2 py-1 flex flex-col justify-center">
                                  <span className="font-medium text-gray-700">
                                    {item.itemCode}
                                  </span>
                                  <span className="text-[10px] text-gray-500">
                                    {item.costName || "-"}
                                  </span>
                                </div>
                              </td>

                              <td className="px-0.5">
                                <div className="bg-[#efefef] border border-[#cfcfcf] rounded-lg min-h-7 px-2 py-1 flex flex-col justify-center">
                                  <span className="font-medium text-gray-700">
                                    {item.vendorName}
                                  </span>
                                  <span className="text-[10px] text-gray-500">
                                    {item.vendorCode}
                                  </span>
                                </div>
                              </td>

                              <td className="px-0.5">
                                <div className="bg-[#efefef] border border-[#cfcfcf] rounded-lg h-7 px-2 flex items-center">
                                  {formatNumber(item.costValue)}
                                </div>
                              </td>

                              <td className="px-0.5">
                                <div className="bg-[#efefef] border border-[#cfcfcf] rounded-lg h-7 px-2 flex items-center">
                                  {formatNumber(item.quantity)}
                                </div>
                              </td>

                              <td className="px-0.5">
                                <div className="bg-[#efefef] border border-[#cfcfcf] rounded-lg h-7 px-2 flex items-center">
                                  {item.uomName}
                                </div>
                              </td>

                              <td className="px-0.5">
                                <div className="bg-[#efefef] border border-[#cfcfcf] rounded-lg h-7 px-2 flex items-center">
                                  {item.isRfba ? "Yes" : "No"}
                                </div>
                              </td>

                              <td className="pl-2">
                                <button
                                  type="button"
                                  disabled={!isEditable || item.isGenerated}
                                  onClick={() => {
                                    if (item.isGenerated) return;

                                    toggleDraftSelectedItem(item.id);
                                  }}
                                  className={`flex h-4.5 w-4.5 items-center justify-center rounded-[3px] border transition-all ${
                                    item.isGenerated
                                      ? "cursor-not-allowed border-gray-300 bg-gray-200 opacity-50"
                                      : checked
                                        ? "border-[#3f2b96] bg-[#3f2b96]"
                                        : "border-[#9ca3af] bg-[#efefef] hover:border-[#3f2b96]"
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
                      })
                    )}
                  </tbody>
                </table>
              </div>
            </div>

            {/* footer */}
            {/* footer */}
            <div className="flex flex-col sm:flex-row justify-end gap-3 px-5 py-4 border-t border-gray-200">
              <Button
                variant="outline"
                type="button"
                onClick={() => setIsModalOpen(false)} // Cancel biarkan seperti ini (draft akan dibuang)
                className="w-full sm:w-auto"
              >
                Cancel
              </Button>

              <Button
                variant="primary"
                type="button"
                onClick={handleApplyModal} // UBAH INI
                className="w-full sm:w-auto"
              >
                Apply
              </Button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

import type { ApprovedBudgetPlan } from "../types/listGeneratePo.type";
import { useEffect, useMemo, useRef, useState } from "react";
import { useGenerateApController } from "../controllers/budgeting/formGenerateApController";
import {
  generateApServices,
  type CreatePurchaseOrderPayload,
} from "../api/services/budgeting/accountPayable/formGenerateApService";
import type { AvailableItem } from "../types/avaiItemAp.type";
import type { AccountPayableItemDetail } from "../types/DetailAp.type";
import { useLocation, useNavigate } from "react-router-dom";
import { useBudgetPlanDetailController } from "../controllers/budgeting/budgetPlanDetailController";
import { useAccountPayableController } from "../controllers/budgeting/listGeerateApController";
import { formatDate } from "../components/format/dateTimeFormat";
import { formatNumber } from "../components/format/formatCurrency";
import { PageHeader } from "../components/ui/page-header";
import { Button } from "../components/ui/button";
import toast from "react-hot-toast";
import { useAvailableItemController } from "../controllers/budgeting/availItemApController";
import { accountPayableService } from "../api/services/budgeting/accountPayable/listGenerateAp";
// import { aborted } from "util";

type LocationState = {
  budgetPlan: ApprovedBudgetPlan;
  accountPayableId?: number;
};

// Cache ringan untuk menghapus item tanpa menyimpan data item duplikat.
type ItemReference = { id: number };

// Ubah item AP ke bentuk item yang dipakai oleh tabel available items.
function mapAccountPayableItem(
  item: AccountPayableItemDetail,
  budgetPlanCode: string,
): AvailableItem {
  return {
    budgetPlanItemId: item.budgetPlanItemId,
    budgetPlanId: item.budgetPlanId,
    budgetPlanCode,
    budgetPlanRemark: "",
    vendorShadowId: item.vendorShadowId,
    vendorCode: item.vendorCode,
    vendorName: item.vendorName,
    itemCode: item.itemCode,
    itemName: item.itemName,
    coaCode: item.coaCode,
    coaName: item.coaName,
    uomCode: item.uomCode,
    uomName: item.uomName,
    isRfba: item.isRfba,
    billOfLading: item.billOfLading ?? "",
    unitCost: item.unitCost,
    unitCount: item.unitCount,
    budgetPlanTotal: item.budgetPlanTotal,
    isGenerated: false,
    takenByCode: null,
    availabilityStatus: "Available",
  };
}

export default function FormGenerateAP() {
  const location = useLocation();
  const state = location.state as LocationState;
  const budgetPlan = state?.budgetPlan;
  const accountPayableIdFromQuery = new URLSearchParams(location.search).get(
    "accountPayableId",
  );
  const accountPayableId =
    state?.accountPayableId ??
    (accountPayableIdFromQuery ? Number(accountPayableIdFromQuery) : undefined);
  const navigate = useNavigate();

  const { detail } = useBudgetPlanDetailController(
    String(budgetPlan?.budgetPlanId ?? ""),
  );

  const [selectedVendorShadowId, setSelectedVendorShadowId] = useState<number>(
    budgetPlan?.vendorShadowId,
  );

  const { items, setItems } = useAvailableItemController(
    selectedVendorShadowId,
    accountPayableId,
  );

  // Dipakai saat form dibuka dari draft AP.
  const [accountPayable, setAccountPayable] = useState<
    Awaited<ReturnType<typeof accountPayableService.getAccountPayableDetail>>["data"] | null
  >(null);

  const [isVendorOpen, setIsVendorOpen] = useState(false);
  const vendorDropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (
        vendorDropdownRef.current &&
        !vendorDropdownRef.current.contains(event.target as Node)
      ) {
        setIsVendorOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  // Draft AP bisa tidak punya detail BP, vendor diambil dari detail AP.
  const vendorItems = useMemo(() => {
    if (detail?.items?.length) return detail.items;
    if (!accountPayable) return [];

    return [
      {
        id: accountPayable.id,
        vendorShadowId: accountPayable.vendorShadowId,
        vendorCode: accountPayable.vendorCode,
        vendorName: accountPayable.vendorName,
      },
    ];
  }, [accountPayable, detail?.items]);
  const selectedVendorItem = vendorItems.find(
    (item) => item.vendorShadowId === selectedVendorShadowId,
  );

  useEffect(() => {
    const firstVendorId = detail?.items?.[0]?.vendorShadowId;

    if (!accountPayableId && !selectedVendorShadowId && firstVendorId !== undefined) {
      setSelectedVendorShadowId(firstVendorId);
    }
  }, [accountPayableId, detail, selectedVendorShadowId]);

  const {
    submitGeneratePO,
    draftGeneratePO,
    submitLoading,
    draftLoading,
    successMessage,
    clearError,
    clearSuccessMessage,
  } = useGenerateApController();

  const { accountPayables, fetchAccountPayables } =
    useAccountPayableController();

  useEffect(() => {
    void fetchAccountPayables();
  }, [fetchAccountPayables]);

  const { fetchAvailableItems, availableItemsLoading } =
    useGenerateApController();

  // Hanya ID yang diperlukan untuk ambil item per budget plan.
  const [, setAllItems] = useState<ItemReference[]>([]);

  // Tambah local loading state
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [selectedRowIds, setSelectedRowIds] = useState<number[]>([]);
  const [tags, setTags] = useState<{ id: number; label: string }[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  // Search hanya berlaku pada daftar budget plan di modal.
  const [budgetPlanSearch, setBudgetPlanSearch] = useState("");
  const [selectedBudgetPlanIds, setSelectedBudgetPlanIds] = useState<number[]>(
    [],
  );
  const [budgetPlanItemsMap, setBudgetPlanItemsMap] = useState<
    Record<number, ItemReference[]>
  >({});
  const [remark, setRemark] = useState<string>("");
  const [docDate, setDocDate] = useState<string>("03 March 2026");
  // Cari berdasarkan BP, vendor, lokasi, atau tanggal dokumen.
  const filteredAccountPayables = useMemo(() => {
    const normalizedSearch = budgetPlanSearch.trim().toLowerCase();

    return accountPayables.filter((item) => {
      if (item.budgetPlanId === budgetPlan?.budgetPlanId) return false;
      if (!normalizedSearch) return true;

      return [
        item.budgetPlanCode,
        item.vendorCode,
        item.vendorName,
        item.location,
        item.docDate,
        formatDate(item.docDate),
      ].some((value) =>
        String(value ?? "")
          .toLowerCase()
          .includes(normalizedSearch),
      );
    });
  }, [accountPayables, budgetPlan?.budgetPlanId, budgetPlanSearch]);

  const formatDateInput = (dateString: string) => {
    const date = new Date(dateString);
    if (Number.isNaN(date.getTime())) return dateString;

    return new Intl.DateTimeFormat("en-GB", {
      day: "2-digit",
      month: "long",
      year: "numeric",
    }).format(date);
  };

  // ======================================================
  // INITIAL FETCH
  // ✅ Konsumsi return value langsung, tidak pakai useEffect kedua
  // ======================================================

  useEffect(() => {
    if (accountPayableId || !budgetPlan?.budgetPlanId || !budgetPlan?.vendorShadowId) return;

    const loadInitialItems = async () => {
      setIsLoading(true);
      try {
        const items = await fetchAvailableItems(
          selectedVendorShadowId,
          budgetPlan.budgetPlanId,
        );

        setBudgetPlanItemsMap((prev) => ({
          ...prev,
          [budgetPlan.budgetPlanId]: items,
        }));

        setAllItems(items);
      } catch (error) {
        console.error("Failed to fetch initial BP items:", error);
      } finally {
        setIsLoading(false);
      }
    };

    loadInitialItems();
  }, [
    accountPayableId,
    budgetPlan?.budgetPlanId,
    budgetPlan?.vendorShadowId,
    fetchAvailableItems,
    selectedVendorShadowId,
  ]);

  useEffect(() => {
    if (!accountPayableId) return;

    let cancelled = false;

    const loadAccountPayable = async () => {
      setIsLoading(true);

      try {
        const response = await accountPayableService.getAccountPayableDetail(
          accountPayableId,
        );
        const ap = response.data;
        const linkedPlans = ap.linkedBudgetPlans ?? [
          {
            id: budgetPlan?.budgetPlanId,
            code: budgetPlan?.budgetPlanCode,
          },
        ].filter(
          (plan): plan is { id: number; code: string } =>
            typeof plan.id === "number" && typeof plan.code === "string",
        );
        const linkedPlanCodes = new Map(
          linkedPlans.map((plan) => [plan.id, plan.code]),
        );
        const mappedItems = ap.items.map((item) =>
          mapAccountPayableItem(
            item,
            linkedPlanCodes.get(item.budgetPlanId) ??
              budgetPlan?.budgetPlanCode ??
              "",
          ),
        );
        // Kelompokkan ID item agar tag BP dapat dihapus tanpa memuat ulang data.
        const itemReferencesByBudgetPlan = mappedItems.reduce<
          Record<number, ItemReference[]>
        >((map, item) => {
          const budgetPlanItems = map[item.budgetPlanId] ?? [];
          budgetPlanItems.push({ id: item.budgetPlanItemId });
          map[item.budgetPlanId] = budgetPlanItems;
          return map;
        }, {});

        if (cancelled) return;

        setAccountPayable(ap);
        setSelectedVendorShadowId(ap.vendorShadowId);
        setRemark(ap.remark ?? "");
        setDocDate(formatDateInput(ap.docDate));
        setSelectedBudgetPlanIds(linkedPlans.map((plan) => plan.id));
        setTags(linkedPlans.map((plan) => ({ id: plan.id, label: plan.code })));
        setBudgetPlanItemsMap(itemReferencesByBudgetPlan);
        setAllItems(mappedItems.map((item) => ({ id: item.budgetPlanItemId })));
        setItems(mappedItems, mappedItems.map((item) => item.budgetPlanItemId));
        setSelectedRowIds(mappedItems.map((item) => item.budgetPlanItemId));
      } catch (error) {
        console.error("Failed to load account payable draft:", error);
        toast.error("Gagal memuat draft account payable");
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    };

    void loadAccountPayable();

    return () => {
      cancelled = true;
    };
  }, [accountPayableId, budgetPlan?.budgetPlanCode, budgetPlan?.budgetPlanId, setItems]);

  // ✅ useEffect "STORE INITIAL ITEMS" dihapus total — tidak diperlukan lagi

  // ======================================================
  // TOGGLE ROW
  // ======================================================

  const toggleCheck = (id: number) => {
    setSelectedRowIds((prev) =>
      prev.includes(id)
        ? prev.filter((itemId) => itemId !== id)
        : [...prev, id],
    );
  };

  useEffect(() => {
    console.log("Checked Row IDs:", selectedRowIds);
  }, [selectedRowIds]);

  const toggleBudgetPlan = async (id: number, label: string) => {
    const exists = selectedBudgetPlanIds.includes(id);

    // REMOVE BP
    if (exists) {
      setSelectedBudgetPlanIds((prev) =>
        prev.filter((itemId) => itemId !== id),
      );

      setTags((prevTags) => prevTags.filter((tag) => tag.id !== id));

      const removedItems = budgetPlanItemsMap[id] || [];

      setAllItems((prev) =>
        prev.filter(
          (item) => !removedItems.some((removed) => removed.id === item.id),
        ),
      );

      setSelectedRowIds((prev) =>
        prev.filter(
          (rowId) => !removedItems.some((removed) => removed.id === rowId),
        ),
      );

      return;
    }

    // ADD BP
    setSelectedBudgetPlanIds((prev) => [...prev, id]);

    setTags((prevTags) => [
      ...prevTags,
      {
        id,
        label,
      },
    ]);

    // already cached — restore from cache
    if (budgetPlanItemsMap[id]) {
      setAllItems((prev) => {
        const merged = [...prev, ...budgetPlanItemsMap[id]];

        const unique = merged.filter(
          (item, index, self) =>
            index === self.findIndex((x) => x.id === item.id),
        );

        return unique;
      });

      return;
    }

    // fetch from available-items API using vendorShadowId from initial BP
    try {
      const items = await fetchAvailableItems(
        selectedVendorShadowId,
        id,
        accountPayableId,
      );

      // cache items for this BP
      setBudgetPlanItemsMap((prev) => ({
        ...prev,
        [id]: items,
      }));

      setAllItems((prev) => {
        const merged = [...prev, ...items];

        const unique = merged.filter(
          (item, index, self) =>
            index === self.findIndex((x) => x.id === item.id),
        );

        return unique;
      });
    } catch (error) {
      console.error("Failed to fetch available items for BP", id, error);

      // rollback tag & selected id on error
      setTags((prevTags) => prevTags.filter((tag) => tag.id !== id));
      setSelectedBudgetPlanIds((prev) =>
        prev.filter((itemId) => itemId !== id),
      );
    }
  };

  // ======================================================
  // REMOVE TAG
  // ======================================================

  const removeTag = (id: number) => {
    setTags((prev) => prev.filter((tag) => tag.id !== id));
    setSelectedBudgetPlanIds((prev) => prev.filter((itemId) => itemId !== id));

    const removedItems = budgetPlanItemsMap[id] || [];
    setAllItems((prev: ItemReference[]) =>
      prev.filter(
        (item) =>
          !removedItems.some((r) => r.id === item.id),
      ),
    );
    setSelectedRowIds((prev: number[]) =>
      prev.filter(
        (rowId) =>
          !removedItems.some((r) => r.id === rowId),
      ),
    );
    setBudgetPlanItemsMap((prev: Record<number, ItemReference[]>) => {
      const updated = { ...prev };
      delete updated[id];
      return updated;
    });
  };

  // ======================================================
  // GRAND TOTAL
  // ======================================================

  const grandTotal = useMemo(() => {
    return items
      .filter((item) => selectedRowIds.includes(item.budgetPlanItemId))
      .reduce((acc, item) => acc + Number(item.budgetPlanTotal || 0), 0);
  }, [items, selectedRowIds]);

  // ======================================================
  // GENERATE
  // ======================================================

  // const handleGenerate = async () => {
  //   if (selectedRowIds.length === 0) return;

  //   const parsedDate = new Date(docDate);
  //   const isoDate = isNaN(parsedDate.getTime())
  //     ? new Date().toISOString()
  //     : parsedDate.toISOString();

  //   const payload: CreatePurchaseOrderPayload = {
  //     vendorShadowId: selectedVendorShadowId,
  //     remark,
  //     docDate: isoDate,
  //     items: selectedRowIds,
  //   };

  //   try {
  //     clearError();
  //     clearSuccessMessage();

  //     const success = await submitGeneratePO(payload);

  //     if (!success) {
  //       toast.success(successMessage);
  //     }

  //     navigate(-1);
  //   } catch {
  //     toast.error("Generate AP Gagal");
  //   }
  // };

  const handleGenerate = async () => {
    if (!selectedVendorShadowId) {
      toast.error("Silakan pilih Vendor Code terlebih dahulu");
      return;
    }

    if (selectedRowIds.length === 0) {
      toast.error("Silakan pilih minimal 1 item");
      return;
    }

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
      clearError();
      clearSuccessMessage();

      const success = accountPayableId
        ? await generateApServices.generateAccountPayable(accountPayableId)
        : await submitGeneratePO(payload);

      if (!success) {
        toast.success(successMessage);
      }

      navigate(-1);
    } catch {
      toast.error("Generate AP Gagal");
    }
  };

  // ======================================================
  // DRAFT
  // ======================================================

  const handleDraft = async () => {
    if (selectedRowIds.length === 0) return;

    const parsedDate = new Date(docDate);
    const isoDate = isNaN(parsedDate.getTime())
      ? new Date().toISOString()
      : parsedDate.toISOString();

    const payload: CreatePurchaseOrderPayload = {
      vendorShadowId: selectedVendorShadowId ?? budgetPlan?.vendorShadowId ?? 0,
      remark,
      docDate: isoDate,
      items: selectedRowIds,
    };

    try {
      clearError();
      clearSuccessMessage();

      const success = accountPayableId
        ? await generateApServices.updateAccountPayable(accountPayableId, {
            remark,
            docDate: isoDate,
            items: selectedRowIds,
          })
        : await draftGeneratePO(payload);

      if (!success) {
        toast.success(successMessage);
      }

      navigate(-1);
    } catch {
      toast.error("Generate AP Gagal");
    }
  };

  const canSubmit =
    !accountPayableId || accountPayable?.status === "Draft";

  return (
    <>
      <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto font-sans">
        <div className="max-w-400 mx-auto">
          <PageHeader
            breadcrumbs={[
              { label: "Budgeting" },
              { label: "Generate PO" },
              { label: "Form Generate AP" },
            ]}
            title="Form Generate AP"
            onBack={() => navigate(-1)}
          />

          {/* Form Fields */}
          <div className="mb-4">
            {/* Row 1 */}
            <div className="flex flex-col sm:flex-row gap-4 mb-3">
              <div className="w-full sm:w-45">
                <label className="block text-[12px] font-semibold text-gray-700 mb-1">
                  Purchase Order
                </label>
                <div className="relative">
                  <select className="w-full border border-gray-300 rounded-lg px-2.5 py-1.5 text-[13px] text-gray-700 appearance-none bg-white focus:outline-none focus:border-indigo-400 cursor-pointer">
                    <option value="">Select Purchase Order</option>

                    {budgetPlan?.purchaseOrders?.map((po) => (
                      <option key={po.id} value={po.id}>
                        {po.code}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="w-full sm:w-60">
                <label className="block text-[12px] font-semibold text-gray-700 mb-1">
                  Vendor Code
                </label>
                <div className="relative" ref={vendorDropdownRef}>
                  <button
                    type="button"
                    onClick={() => setIsVendorOpen((prev) => !prev)}
                    title={
                      selectedVendorItem
                        ? `${selectedVendorItem.vendorCode} - ${selectedVendorItem.vendorName}`
                        : undefined
                    }
                    className="w-full border border-gray-300 rounded-lg px-2.5 py-1.5 text-[13px] text-gray-700 bg-white focus:outline-none focus:border-indigo-400 cursor-pointer flex items-center justify-between gap-2"
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
                      {vendorItems.map((item) => {
                        const isSelected =
                          item.vendorShadowId === selectedVendorShadowId;

                        return (
                          <button
                            key={item.id}
                            type="button"
                            onClick={() => {
                              setSelectedVendorShadowId(item.vendorShadowId);
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

              <div className="w-full flex-1 sm:max-w-xs">
                <label className="block text-[12px] font-semibold text-gray-700 mb-1">
                  Remark
                </label>
                {/* Filter daftar budget plan tanpa request tambahan. */}
                <input
                  type="text"
                  placeholder="Input Remark"
                  value={remark}
                  onChange={(e) => setRemark(e.target.value)}
                  className="w-full border border-gray-300 rounded-lg px-2.5 py-1.5 text-[13px] bg-white focus:outline-none focus:border-indigo-400 placeholder-gray-400"
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
                    onChange={(e) => setDocDate(e.target.value)}
                    className="w-full border border-gray-300 rounded-lg px-2.5 py-1.5 pr-8 text-[13px] text-gray-700 bg-white focus:outline-none focus:border-indigo-400"
                  />
                </div>
              </div>

              <div className="w-full sm:w-45">
                <label className="block text-[12px] font-semibold text-gray-700 mb-1">
                  Vendor Name
                </label>
                <input
                  type="text"
                  value={accountPayable?.vendorName ?? budgetPlan?.vendorName ?? ""}
                  disabled={true}
                  className="w-full border border-gray-300 rounded-lg px-2.5 py-1.5 text-[13px] text-gray-700 bg-white focus:outline-none focus:border-indigo-400"
                />
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
                    className="text-[#3730a3] hover:text-red-500 ml-0.5 leading-none transition-colors text-[11px]"
                  >
                    ✕
                  </button>
                </span>
              ))}
            </div>
          </div>

          {/* Table */}
          <div className="bg-[#d9dde3] rounded-[6px] p-3">
            <div className="overflow-x-auto -mx-1 px-1">
              <table className="w-full min-w-225 border-separate border-spacing-y-2 text-[12px]">
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
                      <td
                        colSpan={11}
                        className="text-center py-5 text-gray-500"
                      >
                        Loading...
                      </td>
                    </tr>
                  ) : (
                    items.map((row) => {
                      const checked = selectedRowIds.includes(
                        row.budgetPlanItemId,
                      );

                      return (
                        <tr key={row.budgetPlanItemId}>
                          {[
                            row.itemCode,
                            row.itemName,
                            row.coaCode,
                            row.coaName,
                            row.unitCost,
                            row.billOfLading,
                            row.unitCount,
                            row.isRfba ? "Yes" : "No",
                            row.uomName,
                            row.budgetPlanTotal,
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
                              onClick={() => toggleCheck(row.budgetPlanItemId)}
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
            </div>

            {/* Grand Total */}
            <div className="flex justify-end items-center mt-2 pr-1.5">
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
              variant="secondary"
              onClick={handleDraft}
              disabled={draftLoading || !canSubmit}
              className="px-7 text-[13px]"
            >
              {draftLoading ? "Saving..." : "Save"}
            </Button>

            <Button
              variant="primary"
              onClick={handleGenerate}
              disabled={
                submitLoading || draftLoading || selectedRowIds.length === 0 || !canSubmit
              }
              className="px-7 text-[13px] w-full sm:w-auto"
            >
              {submitLoading ? "Generating..." : "Generate"}
            </Button>
          </div>
        </div>
      </div>

      {/* MODAL */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white w-225 max-h-[80vh] overflow-hidden rounded-xl shadow-xl">
            {/* header */}
            <div className="flex items-center justify-between px-5 py-4 border-b border-gray-200">
              <h2 className="text-[16px] font-semibold text-gray-800">
                Select Budget Plan
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
                Search BP, vendor, or warehouse
                <input
                  type="search"
                  value={budgetPlanSearch}
                  onChange={(event) => setBudgetPlanSearch(event.target.value)}
                  placeholder="Search BP, vendor, or warehouse"
                  aria-label="Search BP, vendor, or warehouse"
                  className="mt-1.5 w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-[13px] font-normal text-gray-700 placeholder-gray-400 focus:border-indigo-400 focus:outline-none"
                />
              </label>

              <table className="w-full border-separate border-spacing-y-2 text-[12px]">
                <thead>
                  <tr>
                    {["Budget No", "Vendor", "Budget Approve", "Date", ""].map(
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
                  {availableItemsLoading ? (
                    <tr>
                      <td
                        colSpan={5}
                        className="text-center py-5 text-gray-500"
                      >
                        Loading...
                      </td>
                    </tr>
                  ) : filteredAccountPayables.length === 0 ? (
                    <tr>
                      <td
                        colSpan={5}
                        className="text-center py-5 text-gray-500"
                      >
                        No budget plans found.
                      </td>
                    </tr>
                  ) : (
                    filteredAccountPayables.map((item) => {
                        const checked = selectedBudgetPlanIds.includes(
                          item.budgetPlanId,
                        );
                        return (
                          <tr key={item.budgetPlanId}>
                            <td className="px-0.5">
                              <div className="bg-[#efefef] border border-[#cfcfcf] rounded-lg h-7 px-2 flex items-center">
                                {item.budgetPlanCode}
                              </div>
                            </td>

                            <td className="px-0.5">
                              <div className="bg-[#efefef] border border-[#cfcfcf] rounded-lg h-7 px-2 flex items-center">
                                {item.vendorName}
                              </div>
                            </td>

                            <td className="px-0.5">
                              <div className="bg-[#efefef] border border-[#cfcfcf] rounded-lg h-7 px-2 flex items-center">
                                {formatNumber(item.budgetApproved)}
                              </div>
                            </td>

                            <td className="px-0.5">
                              <div className="bg-[#efefef] border border-[#cfcfcf] rounded-lg h-7 px-2 flex items-center">
                                {formatDate(item.docDate)}
                              </div>
                            </td>

                            <td className="pl-2">
                              <button
                                type="button"
                                onClick={() =>
                                  toggleBudgetPlan(
                                    item.budgetPlanId,
                                    item.budgetPlanCode,
                                  )
                                }
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
            </div>

            {/* footer */}
            <div className="flex justify-end gap-3 px-5 py-4 border-t border-gray-200">
              <Button
                variant="outline"
                type="button"
                onClick={() => setIsModalOpen(false)}
                className="px-5 text-[13px]"
              >
                Cancel
              </Button>
              <Button
                variant="primary"
                type="button"
                onClick={() => setIsModalOpen(false)}
                className="px-5 text-[13px]"
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

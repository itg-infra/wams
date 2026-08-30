import { useState, useEffect } from "react";
import { useItemController } from "../master_data/controller/itemController";
import { useWarehouseController } from "../master_data/controller/warehouseController";
import { useActivityController } from "../master_data/controller/activityController";
import { createBudgetTemplateService } from "../api/services/budgeting/budgetTemplate/createBudgetTemplateService";
// import type { WarehouseTypes } from "../master_data/types/warehouse.type";
import { Trash2, ChevronDown } from "lucide-react";
import { useLocationStore } from "../master_data/store/locationStore";
import { fetchLocationsController } from "../master_data/controller/locationController";
import toast from "react-hot-toast";
import { useNavigate, useParams } from "react-router-dom";
import { PageHeader } from "../components/ui/page-header";
import { Button } from "../components/ui/button";
import { ItemSearchSelect } from "../components/itemSearch";
import type { Item } from "../master_data/types/item.types";

type CostRow = {
  itemId: string;
  itemCode: string;
  itemName: string;
  coa: string;
  coaName: string;
  activityTypeId: number;
};

export default function CreateBudgetTemplateScreen() {
  const {id} = useParams();
  const navigate = useNavigate();

  const { items } = useItemController();
  const { warehousesTypes } = useWarehouseController();
  const { activities } = useActivityController();
  const [templateCode, setTemplateCode] = useState<string>("T.0001");
  // const [, setSelectedActivityId] = useState<number | null>(
  //   null,
  // );
  const [rows, setRows] = useState<CostRow[]>([
    {
      itemId: "",
      itemCode: "",
      itemName: "",
      coa: "",
      coaName: "",
      activityTypeId: 0,
    },
  ]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const {locations} = useLocationStore();
  const [selectedLocation, setSelectedLocation] = useState("");

  useEffect(() => {
    fetchLocationsController();
  }, []);

  const isEditMode = Boolean(id);
  useEffect(() => {
    if (!id) return;

    const fetchDetail = async () => {
      setIsLoading(true);
      try {
        const detail = await createBudgetTemplateService.getById(id);

        setTemplateCode(detail.templateId);

        setSelectedLocation(detail.provinceId)

        // const matchedActivity = activities.find(
        //   (a) => a.name === detail.templateName,
        // );
        // if (matchedActivity) setSelectedActivityId(Number(matchedActivity.id));

        setRows(
          detail.items.map((item) => ({
            itemId: String(item.itemShadowId),
            itemCode: item.costDetail,
            itemName: item.costName,
            coa: item.coa,
            coaName: item.coaName,
            activityTypeId: item.activityTypeId,
          })),
        );
      } catch (error) {
        console.error(error);
        alert("Gagal memuat data template ❌");
      } finally {
        setIsLoading(false);
      }
    };

    fetchDetail();
  }, [id, activities, warehousesTypes]);
  const handleSelectActivity = (id: number, index: number) => {
    setRows((prev) =>
      prev.map((row, i) =>
        i === index
          ? {
              ...row,
              activityTypeId: id,
            }
          : row,
      ),
    );
  };

  // const handleSelectActivityFormUp = (id: string) => {
  //   setSelectedActivityId(Number(id));
  // };
  const handleSelectItem = (itemId: string, index: number) => {
    const selected = items.find((i) => i.id === itemId);
    if (!selected) return;

    const updated = [...rows];
    updated[index] = {
      itemId: selected.id,
      itemCode: selected.itemCode,
      itemName: selected.itemName,
      coa: selected.acctCode,
      coaName: selected.itemName,
      activityTypeId: 0, // need to check
    };

    setRows(updated);
  };
  const handleAddRow = () => {
    setRows([
      ...rows,
      {
        itemId: "",
        itemCode: "",
        itemName: "",
        coa: "",
        coaName: "",
        activityTypeId: 0,
      },
    ]);
  };

  const handleDeleteRow = (index: number) => {
    if (rows.length === 1) return;
    setRows(rows.filter((_, i) => i !== index));
  };

  type FormErrors = {
    template?: string;
    location?: string;
    rows?: {
      itemId?: string;
      activityTypeId?: string;
    }[];
  };

  const [, setErrors] = useState<FormErrors>({});

  const validateForm = () => {
   const newErrors: FormErrors = {
     rows: [],
   };

    let isValid = true;

    // Template validation
    // if (!selectedActivityId) {
    //   newErrors.template = "Template name is required";
    //   isValid = false;
    // }

    // Location validation
    if (!selectedLocation) {
      newErrors.location = "Location is required";
      isValid = false;
    }

    // Row validation
    rows.forEach((row, index) => {
      const rowError: {
        itemId?: string;
        activityTypeId?: string;
      } = {};

      if (!row.itemId) {
        rowError.itemId = "Cost detail is required";
        isValid = false;
      }

      if (!row.activityTypeId) {
        rowError.activityTypeId = "Template name is required";
        isValid = false;
      }

      newErrors.rows![index] = rowError;
    });

    setErrors(newErrors);

    return isValid;
  };

  const buildPayload = () => ({
    provinceId: selectedLocation,

    items: rows.map((r) => ({
      itemShadowId: Number(r.itemId),
      activityTypeId: Number(r.activityTypeId),
    })),
  });
  const handleSubmit = async () => {

      const isValid = validateForm();

      if (!isValid) {
        toast.error("Please complete all required fields");
        return;
      }
    try {
      const payload = buildPayload();
      if (isEditMode && id) {
        await createBudgetTemplateService.update(id, payload); // update data dulu
        await createBudgetTemplateService.submitById(id); // lalu submit
      } else {
        await createBudgetTemplateService.createSubmit(payload); // create + submit sekaligus
      }
            toast.success("Budget Template created successfully");

      navigate(-1);
    } catch (error) {
      console.error(error);
                  toast.success("Failed to create budget template");

    }
  };

  const handleDraft = async () => {
    try {
      const payload = buildPayload();
      if (isEditMode && id) {
        await createBudgetTemplateService.update(id, payload); // update data saja
      } else {
        await createBudgetTemplateService.createDraft(payload); // create as draft
      }
      toast.success("Draft created successfully");
      navigate(-1);
    } catch (error) {
      console.error(error);
            toast.success("Create Draft Failed");

    }
  };

  if (isLoading) {
    return (
      <div className="flex-1 overflow-y-auto  bg-[#F8F8F8] p-6">
        <div className="max-w-5xl">
          <div className="bg-white rounded-2xl border border-[#D7DEE8] p-6 animate-pulse space-y-4">
            <div className="h-6 bg-gray-100 rounded w-48" />
            <div className="grid grid-cols-2 gap-4">
              <div className="h-12 bg-gray-100 rounded-xl" />
              <div className="h-12 bg-gray-100 rounded-xl" />
            </div>
            <div className="grid grid-cols-3 gap-4">
              <div className="h-12 bg-gray-100 rounded-xl" />
              <div className="h-12 bg-gray-100 rounded-xl" />
              <div className="h-12 bg-gray-100 rounded-xl" />
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <>
      <div className="flex-1 overflow-y-auto  bg-[#F8F8F8]">
        <div className="px-4 sm:px-6 pt-5 sm:pt-7 pb-6 w-full">
          <PageHeader
            breadcrumbs={[
              { label: "Dashboard" },
              { label: "Budgeting" },
              { label: "Budget Template" },
              { label: isEditMode ? "Edit Template" : "Form Budget Template" },
            ]}
            title={
              isEditMode
                ? "Form Edit Budget Plan"
                : "Form Create Budget Template"
            }
            onBack={() => navigate(-1)}
          />

          {/* Card */}
          <div className="bg-white rounded-2xl border border-[#D7DEE8] p-4 sm:p-6">
            {/* TEMPLATE ROW */}
            <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4 mb-5">
              {/* Template ID */}
              <div className="flex flex-col gap-1.5">
                <label className="text-[14px] font-medium text-[#222222]">
                  Template ID
                </label>
                <input
                  id="txt_TemplateId"
                  className="h-11 w-full border border-[#D8DCE5] rounded-xl px-4 text-[15px] text-[#222222]  bg-[#F8F8F8] outline-none cursor-not-allowed"
                  value={templateCode}
                  disabled
                />
              </div>

              <div className="flex flex-col gap-1.5">
                <label className="text-[14px] font-medium text-[#222222]">
                  Location
                </label>

                <select
                  id="cmb_Location"
                  className="h-11 w-full border border-[#D8DCE5] rounded-xl px-4 text-[15px] text-[#222222] bg-white outline-none"
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
              </div>
            </div>

            {/* COST DETAIL TABLE */}
            <div className="bg-[#EEF0F6] rounded-2xl p-3 sm:p-4">
              {/* Rows */}
              <div className="space-y-3">
                {rows.map((row, index) => (
                  <div
                    key={index}
                    className="grid grid-cols-1 xl:grid-cols-[1fr_1fr_1fr_1fr_1fr_44px] gap-3 items-end"
                  >
                    {/* Cost Detail dropdown (searchable + paginated) */}
                    <div className="flex flex-col gap-1.5 min-w-0">
                      <label className="text-[14px] font-medium text-[#222222]">
                        Cost Detail
                      </label>

                      <ItemSearchSelect
                        id="cmb_CostDetail"
                        value={row.itemId}
                        onChange={(item: Item) =>
                          handleSelectItem(item.id, index)
                        }
                      />
                    </div>

                    {/* Item Name */}
                    <div className="flex flex-col gap-1.5 min-w-0">
                      <label className="text-[14px] font-medium text-[#222222]">
                        Item Name
                      </label>

                      <input
                        className="h-11 w-full border border-[#D8DCE5] rounded-xl px-3 text-[14px] text-[#222222] bg-white outline-none cursor-not-allowed"
                        id="txt_ItemName"
                        value={row.itemName}
                        disabled
                      />
                    </div>

                    {/* COA */}
                    <div className="flex flex-col gap-1.5 min-w-0">
                      <label className="text-[14px] font-medium text-[#222222]">
                        COA
                      </label>

                      <input
                        className="h-11 w-full border border-[#D8DCE5] rounded-xl px-3 text-[14px] text-[#222222] bg-white outline-none cursor-not-allowed"
                        id="txt_Coa"
                        value={row.coa}
                        disabled
                      />
                    </div>

                    {/* COA Name */}
                    <div className="flex flex-col gap-1.5 min-w-0">
                      <label className="text-[14px] font-medium text-[#222222]">
                        COA Name
                      </label>

                      <input
                        className="h-11 w-full border border-[#D8DCE5] rounded-xl px-3 text-[14px] text-[#222222] bg-white outline-none cursor-not-allowed"
                        id="txt_CoaName"
                        value={row.coaName}
                        disabled
                      />
                    </div>

                    {/* Template Name */}
                    <div className="flex flex-col gap-1.5 min-w-0">
                      <label className="text-[14px] font-medium text-[#222222]">
                        Template Name
                      </label>

                      <div className="relative">
                        <select
                          className="h-11 w-full border border-[#D8DCE5] rounded-xl px-4 pr-10 text-[15px] text-[#222222] bg-white outline-none appearance-none focus:ring-2 focus:ring-indigo-200 focus:border-[#2E277C] transition cursor-pointer"
                          id="cmb_RowTemplateName"
                          value={row.activityTypeId ?? ""}
                          onChange={(e) =>
                            handleSelectActivity(Number(e.target.value), index)
                          }
                        >
                          <option value="">Select</option>

                          {activities.map((a) => (
                            <option key={a.id} value={a.id}>
                              {a.name}
                            </option>
                          ))}
                        </select>

                        <ChevronDown className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[#7A7A7A] pointer-events-none" />
                      </div>
                    </div>

                    {/* Delete button */}
                    <button
                      id="icn_DeleteTemplateRow"
                      onClick={() => handleDeleteRow(index)}
                      className="w-full xl:w-11 h-11 bg-[#2E277C] hover:bg-[#241F63] text-white rounded-xl flex items-center justify-center transition shrink-0"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                ))}
              </div>

              {/* Add Item */}
              <Button
                id="btn_AddTemplateItem"
                variant="primary"
                onClick={handleAddRow}
                className="mt-4 w-full sm:w-auto"
              >
                Add Item
              </Button>
            </div>

            {/* ACTION BUTTONS */}
            <div className="flex flex-col sm:flex-row justify-end gap-3 mt-6">
              <Button
                id="btn_DraftBudgetTemplate"
                variant="secondary"
                size="lg"
                onClick={handleDraft}
                className="w-full sm:w-auto"
              >
                Draft
              </Button>
              <Button
                id="btn_SubmitBudgetTemplate"
                variant="primary"
                size="lg"
                onClick={handleSubmit}
                className="w-full sm:w-auto"
              >
                Submit
              </Button>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}

import type {
    BudgetTemplateFormResponse,
    BudgetTemplateCostItem,
    WarehouseOption,
} from "../../../../types/budgetTemplateForm.type";

const TEMPLATE_NAME_OPTIONS = [
    { label: "Kegiatan Bongkar", value: "Kegiatan Bongkar" },
    { label: "Kegiatan Muat", value: "Kegiatan Muat" },
    { label: "Fumigasi", value: "Fumigasi" },
    { label: "Opname", value: "Opname" },
];

const WAREHOUSE_OPTIONS: WarehouseOption[] = [
    {
        code: "WHLPG01",
        name: "MNP Blok A",
        location: "Lampung",
    },
    {
        code: "WHLPG02",
        name: "MNP Blok B",
        location: "Lampung",
    },
    {
        code: "WHJKT01",
        name: "JKT Blok A",
        location: "Jakarta",
    },
];

const COST_DETAIL_OPTIONS = [
    { code: "Z.GEN001", label: "Z.GEN001" },
    { code: "Z.GEN002", label: "Z.GEN002" },
    { code: "Z.GEN003", label: "Z.GEN003" },
];

const COA_OPTIONS = [
    { code: "5010101001", name: "B.Timbang" },
    { code: "5010101002", name: "B.Bongkar" },
    { code: "5010101003", name: "B.Muat" },
];

const INITIAL_ITEMS: BudgetTemplateCostItem[] = [
    {
        id: "1",
        costDetail: "Z.GEN001",
        costName: "B.Timbang",
        coa: "5010101001",
        coaName: "B.Timbang",
    },
    {
        id: "2",
        costDetail: "",
        costName: "",
        coa: "",
        coaName: "",
    },
];

export const budgetTemplateFormService = {
    async getCreateBudgetTemplateForm(): Promise<BudgetTemplateFormResponse> {
        await new Promise((resolve) => setTimeout(resolve, 250));

        return {
            form: {
                templateId: "T.0001",
                templateName: "Kegiatan Bongkar",
                warehouseCode: "WHLPG01",
                warehouseName: "MNP Blok A",
                location: "Lampung",
                items: INITIAL_ITEMS,
            },
            meta: {
                templateNameOptions: TEMPLATE_NAME_OPTIONS,
                warehouseOptions: WAREHOUSE_OPTIONS,
                costDetailOptions: COST_DETAIL_OPTIONS,
                coaOptions: COA_OPTIONS,
            },
        };
    },

    async submitBudgetTemplateForm(payload: unknown): Promise<{ success: boolean }> {
        console.log("Submit Budget Template:", payload);
        await new Promise((resolve) => setTimeout(resolve, 500));
        return { success: true };
    },

    async draftBudgetTemplateForm(payload: unknown): Promise<{ success: boolean }> {
        console.log("Draft Budget Template:", payload);
        await new Promise((resolve) => setTimeout(resolve, 500));
        return { success: true };
    },
};
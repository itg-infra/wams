import { create } from "zustand";
import { budgetTemplateFormService } from "../api/services/budgeting/budgetTemplate/budgetTemplateFormService";
import type {
    BudgetTemplateCostItem,
    BudgetTemplateFormData,
    CostDetailOption,
    CoaOption,
    TemplateNameOption,
    WarehouseOption,
} from "../types/budgetTemplateForm.type";

interface BudgetTemplateFormStoreState {
    form: BudgetTemplateFormData;
    templateNameOptions: TemplateNameOption[];
    warehouseOptions: WarehouseOption[];
    costDetailOptions: CostDetailOption[];
    coaOptions: CoaOption[];

    isLoading: boolean;
    isSubmitting: boolean;
    error: string | null;

    fetchForm: () => Promise<void>;
    updateField: <K extends keyof BudgetTemplateFormData>(
        key: K,
        value: BudgetTemplateFormData[K]
    ) => void;
    updateItemField: (
        itemId: string,
        field: keyof BudgetTemplateCostItem,
        value: string
    ) => void;
    addItem: () => void;
    removeItem: (itemId: string) => void;
    applyWarehouse: (warehouseCode: string) => void;
    applyCoaAutoFill: (itemId: string, coaCode: string) => void;
    submitForm: () => Promise<void>;
    draftForm: () => Promise<void>;
}

const INITIAL_FORM: BudgetTemplateFormData = {
    templateId: "",
    templateName: "",
    warehouseCode: "",
    warehouseName: "",
    location: "",
    items: [],
};

export const useBudgetTemplateFormStore = create<BudgetTemplateFormStoreState>((set, get) => ({
    form: INITIAL_FORM,
    templateNameOptions: [],
    warehouseOptions: [],
    costDetailOptions: [],
    coaOptions: [],

    isLoading: false,
    isSubmitting: false,
    error: null,

    fetchForm: async () => {
        set({ isLoading: true, error: null });

        try {
            const response = await budgetTemplateFormService.getCreateBudgetTemplateForm();

            set({
                form: response.form,
                templateNameOptions: response.meta.templateNameOptions,
                warehouseOptions: response.meta.warehouseOptions,
                costDetailOptions: response.meta.costDetailOptions,
                coaOptions: response.meta.coaOptions,
                isLoading: false,
            });
        } catch (error) {
            set({
                isLoading: false,
                error: error instanceof Error ? error.message : "Failed to load form",
            });
        }
    },

    updateField: (key, value) => {
        set((state) => ({
            form: {
                ...state.form,
                [key]: value,
            },
        }));
    },

    updateItemField: (itemId, field, value) => {
        set((state) => ({
            form: {
                ...state.form,
                items: state.form.items.map((item) =>
                    item.id === itemId ? { ...item, [field]: value } : item
                ),
            },
        }));
    },

    addItem: () => {
        const currentItems = get().form.items;
        const nextId = `${Date.now()}-${currentItems.length + 1}`;

        set((state) => ({
            form: {
                ...state.form,
                items: [
                    ...state.form.items,
                    {
                        id: nextId,
                        costDetail: "",
                        costName: "",
                        coa: "",
                        coaName: "",
                    },
                ],
            },
        }));
    },

    removeItem: (itemId) => {
        set((state) => ({
            form: {
                ...state.form,
                items: state.form.items.filter((item) => item.id !== itemId),
            },
        }));
    },

    applyWarehouse: (warehouseCode) => {
        const warehouse = get().warehouseOptions.find((item) => item.code === warehouseCode);
        if (!warehouse) return;

        set((state) => ({
            form: {
                ...state.form,
                warehouseCode: warehouse.code,
                warehouseName: warehouse.name,
                location: warehouse.location,
            },
        }));
    },

    applyCoaAutoFill: (itemId, coaCode) => {
        const coa = get().coaOptions.find((item) => item.code === coaCode);
        if (!coa) return;

        set((state) => ({
            form: {
                ...state.form,
                items: state.form.items.map((item) =>
                    item.id === itemId
                        ? {
                            ...item,
                            coa: coa.code,
                            coaName: coa.name,
                            costName: coa.name,
                        }
                        : item
                ),
            },
        }));
    },

    submitForm: async () => {
        const payload = get().form;
        set({ isSubmitting: true });

        try {
            await budgetTemplateFormService.submitBudgetTemplateForm(payload);
            set({ isSubmitting: false });
        } catch (error) {
            set({
                isSubmitting: false,
                error: error instanceof Error ? error.message : "Failed to submit form",
            });
        }
    },

    draftForm: async () => {
        const payload = get().form;
        set({ isSubmitting: true });

        try {
            await budgetTemplateFormService.draftBudgetTemplateForm(payload);
            set({ isSubmitting: false });
        } catch (error) {
            set({
                isSubmitting: false,
                error: error instanceof Error ? error.message : "Failed to save draft",
            });
        }
    },
}));
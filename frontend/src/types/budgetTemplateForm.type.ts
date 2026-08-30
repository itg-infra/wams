export interface TemplateNameOption {
    label: string;
    value: string;
}

export interface WarehouseOption {
    code: string;
    name: string;
    location: string;
}

export interface CostDetailOption {
    code: string;
    label: string;
}

export interface CoaOption {
    code: string;
    name: string;
}

export interface BudgetTemplateCostItem {
    id: string;
    costDetail: string;
    costName: string;
    coa: string;
    coaName: string;
}

export interface BudgetTemplateFormData {
    templateId: string;
    templateName: string;
    warehouseCode: string;
    warehouseName: string;
    location: string;
    items: BudgetTemplateCostItem[];
}

export interface BudgetTemplateFormMeta {
    templateNameOptions: TemplateNameOption[];
    warehouseOptions: WarehouseOption[];
    costDetailOptions: CostDetailOption[];
    coaOptions: CoaOption[];
}

export interface BudgetTemplateFormResponse {
    form: BudgetTemplateFormData;
    meta: BudgetTemplateFormMeta;
}
import { Trash2, ChevronDown } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { useBudgetTemplateFormController } from "../controllers/budgeting/budgetTemplateFormController";
import { PageHeader } from "../components/ui/page-header";
import { Button } from "../components/ui/button";

function FieldLabel({ children }: { children: React.ReactNode }) {
    return (
        <label className="block text-[18px] font-semibold text-black mb-2">
            {children}
        </label>
    );
}

function TextInput({
    value,
    readOnly = false,
    placeholder = "",
}: {
    value: string;
    readOnly?: boolean;
    placeholder?: string;
}) {
    return (
        <input
            type="text"
            value={value}
            readOnly={readOnly}
            placeholder={placeholder}
            className="w-full h-13 rounded-xl border border-[#D8D8D8] bg-white px-4 text-[16px] text-[#2C2C2C] outline-none"
        />
    );
}

function SelectInput({
    value,
    options,
    onChange,
}: {
    value: string;
    options: { label: string; value: string }[];
    onChange: (value: string) => void;
}) {
    return (
        <div className="relative">
            <select
                value={value}
                onChange={(e) => onChange(e.target.value)}
                className="w-full h-13 rounded-xl border border-[#D8D8D8] bg-white px-4 pr-10 text-[16px] text-[#2C2C2C] outline-none appearance-none"
            >
                {options.map((option) => (
                    <option key={option.value} value={option.value}>
                        {option.label}
                    </option>
                ))}
            </select>
            <ChevronDown className="w-4 h-4 text-[#2C2C2C] absolute right-4 top-1/2 -translate-y-1/2 pointer-events-none" />
        </div>
    );
}

function SelectCodeInput({
    value,
    options,
    onChange,
}: {
    value: string;
    options: { label: string; value: string }[];
    onChange: (value: string) => void;
}) {
    return (
        <div className="relative">
            <select
                value={value}
                onChange={(e) => onChange(e.target.value)}
                className="w-full h-12.5 rounded-[10px] border border-[#CFCFCF] bg-white px-4 pr-10 text-[15px] text-[#2C2C2C] outline-none appearance-none"
            >
                <option value="">Select</option>
                {options.map((option) => (
                    <option key={option.value} value={option.value}>
                        {option.label}
                    </option>
                ))}
            </select>
            <ChevronDown className="w-4 h-4 text-[#2C2C2C] absolute right-4 top-1/2 -translate-y-1/2 pointer-events-none" />
        </div>
    );
}

function LoadingSkeleton() {
    return (
        <div className="px-6 pt-7 pb-6 max-w-7xl animate-pulse">
            <div className="h-6 w-80 bg-gray-200 rounded mb-6" />
            <div className="h-10 w-72 bg-gray-200 rounded mb-8" />

            <div className="grid grid-cols-2 gap-8 mb-6">
                <div className="h-20 bg-gray-200 rounded-xl" />
                <div className="h-20 bg-gray-200 rounded-xl" />
            </div>

            <div className="grid grid-cols-3 gap-6 mb-6">
                <div className="h-20 bg-gray-200 rounded-xl" />
                <div className="h-20 bg-gray-200 rounded-xl" />
                <div className="h-20 bg-gray-200 rounded-xl" />
            </div>

            <div className="h-72 bg-gray-200 rounded-2xl" />
        </div>
    );
}

export default function BudgetTemplateFormScreen() {
    const navigate = useNavigate();
    const {
        form,
        templateNameOptions,
        warehouseOptions,
        costDetailOptions,
        coaOptions,
        isLoading,
        isSubmitting,
        error,
        handleTemplateNameChange,
        handleWarehouseCodeChange,
        handleCostDetailChange,
        handleCostNameChange,
        handleCoaChange,
        handleCoaNameChange,
        handleAddItem,
        handleRemoveItem,
        handleSubmit,
        handleDraft,
    } = useBudgetTemplateFormController();

    if (isLoading) {
        return (
            <div className="flex-1 overflow-y-auto  bg-[#F8F8F8]">
                <LoadingSkeleton />
            </div>
        );
    }

    return (
        <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">
            <div className="max-w-7xl">
                {/* Page Header (breadcrumb + title + back arrow) */}
                <PageHeader
                    breadcrumbs={[
                        { label: "Dashboard" },
                        { label: "Budgeting" },
                        { label: "Budget Template" },
                        { label: "Create" },
                    ]}
                    title="Form Budget Template"
                    onBack={() => navigate(-1)}
                />

                {/* Error */}
                {error && (
                    <div className="mb-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-500">
                        {error}
                    </div>
                )}

                {/* Top Form */}
                <div className="grid grid-cols-1 xl:grid-cols-2 gap-x-8 gap-y-6 mb-6">
                    <div>
                        <FieldLabel>Template ID</FieldLabel>
                        <TextInput value={form.templateId} readOnly />
                    </div>

                    <div>
                        <FieldLabel>Template Name</FieldLabel>
                        <SelectInput
                            value={form.templateName}
                            options={templateNameOptions}
                            onChange={handleTemplateNameChange}
                        />
                    </div>
                </div>

                <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-7">
                    <div>
                        <FieldLabel>Warehouse Code</FieldLabel>
                        <SelectInput
                            value={form.warehouseCode}
                            options={warehouseOptions.map((item) => ({
                                label: item.code,
                                value: item.code,
                            }))}
                            onChange={handleWarehouseCodeChange}
                        />
                    </div>

                    <div>
                        <FieldLabel>Warehouse Name</FieldLabel>
                        <TextInput value={form.warehouseName} readOnly />
                    </div>

                    <div>
                        <FieldLabel>Location</FieldLabel>
                        <TextInput value={form.location} readOnly />
                    </div>
                </div>

                {/* Cost Detail Panel */}
                <div className="bg-[#D6DEE7] rounded-2xl px-6 pt-6 pb-6">
                    {/* Header */}
                    <div className="grid grid-cols-[1.1fr_1.1fr_1.1fr_1.1fr_56px] gap-2 md:gap-3 mb-4 min-w-250">
                        <h3 className="text-[18px] font-semibold text-black">Cost Detail</h3>
                        <h3 className="text-[18px] font-semibold text-black">Cost Name</h3>
                        <h3 className="text-[18px] font-semibold text-black">COA</h3>
                        <h3 className="text-[18px] font-semibold text-black">COA Name</h3>
                        <div />
                    </div>

                    <div className="overflow-x-auto">
                        <div className="min-w-250">
                            <div className="space-y-4">
                                {form.items.map((item) => (
                                    <div
                                        key={item.id}
                                        className="grid grid-cols-[1.1fr_1.1fr_1.1fr_1.1fr_56px] gap-2 md:gap-3"
                                    >
                                        <SelectCodeInput
                                            value={item.costDetail}
                                            options={costDetailOptions.map((option) => ({
                                                label: option.label,
                                                value: option.code,
                                            }))}
                                            onChange={(value) => handleCostDetailChange(item.id, value)}
                                        />

                                        <input
                                            type="text"
                                            value={item.costName}
                                            onChange={(e) => handleCostNameChange(item.id, e.target.value)}
                                            className="w-full h-12.5 rounded-[10px] border border-[#CFCFCF] bg-white px-4 text-[15px] text-[#2C2C2C] outline-none"
                                        />

                                        <SelectCodeInput
                                            value={item.coa}
                                            options={coaOptions.map((option) => ({
                                                label: option.code,
                                                value: option.code,
                                            }))}
                                            onChange={(value) => handleCoaChange(item.id, value)}
                                        />

                                        <input
                                            type="text"
                                            value={item.coaName}
                                            onChange={(e) => handleCoaNameChange(item.id, e.target.value)}
                                            className="w-full h-12.5 rounded-[10px] border border-[#CFCFCF] bg-white px-4 text-[15px] text-[#2C2C2C] outline-none"
                                        />

                                        <button
                                            type="button"
                                            onClick={() => handleRemoveItem(item.id)}
                                            className="w-10.5 h-10.5 mt-1 rounded-[10px] border border-[#4A3F97] text-[#2E277C] flex items-center justify-center hover:bg-indigo-50 transition"
                                        >
                                            <Trash2 className="w-5 h-5" />
                                        </button>
                                    </div>
                                ))}
                            </div>
                        </div>
                    </div>

                    {/* Add Button */}
                    <Button
                        variant="primary"
                        type="button"
                        onClick={handleAddItem}
                        className="mt-5 h-10.5 px-5 rounded-[10px] text-[15px]"
                    >
                        Add Item
                    </Button>
                </div>

                {/* Footer Buttons */}
                <div className="mt-7 flex justify-end gap-6 flex-wrap">
                    <Button
                        variant="secondary"
                        type="button"
                        onClick={handleDraft}
                        disabled={isSubmitting}
                        className="w-37 h-13 rounded-xl border-2 text-[16px]"
                    >
                        Draft
                    </Button>

                    <Button
                        variant="primary"
                        type="button"
                        onClick={handleSubmit}
                        disabled={isSubmitting}
                        className="w-37 h-13 rounded-xl text-[16px]"
                    >
                        {isSubmitting ? "Submitting..." : "Submit"}
                    </Button>
                </div>
            </div>
        </div>
    );
}
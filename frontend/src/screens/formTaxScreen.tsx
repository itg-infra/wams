import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import {
  createTaxController,
  getTaxDetailController,
  updateTaxController,
} from "../controllers/masterData/taxController";

import { useTaxStore } from "../store/taxStore";

import type { TaxCategory } from "../types/tax.type";
import toast from "react-hot-toast";

import { PageHeader } from "../components/ui/page-header";
import { Button } from "../components/ui/button";

export default function FormTaxScreen() {
  const { id } = useParams();
  const navigate = useNavigate();

  const editMode = Boolean(id);

  const { taxDetail, loading, submitLoading, error, resetDetail } =
    useTaxStore();

  const [category, setCategory] = useState<TaxCategory | "">("");
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [rate, setRate] = useState("");

  useEffect(() => {
    if (editMode) {
      getTaxDetailController(Number(id));
    }

    return () => {
      resetDetail();
    };
  }, []);

  useEffect(() => {
    if (taxDetail) {
      setCategory(taxDetail.category);
      setCode(taxDetail.code);
      setName(taxDetail.name);
      setRate(String(taxDetail.rate));
    }
  }, [taxDetail]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      if (editMode) {
        await updateTaxController(Number(id), {
          code,
          name,
        });

        toast.success("Success update tax data");
      } else {
        if (!category) {
          alert("Category is required");
          return;
        }

        await createTaxController({
          category,
          code,
          name,
          rate: Number(rate),
        });

        toast.success("Success create tax data");
      }

      navigate(-1);
    } catch (err) {
      console.error(err);
      toast.error(`Error: ${err}`);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[50vh] px-4">
        <div className="text-center">
          <div className="h-10 w-10 mx-auto rounded-full border-4 border-slate-300 border-t-slate-700 animate-spin" />
          <p className="mt-4 text-slate-500">Loading tax data...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">
      <PageHeader
        title={editMode ? "Edit Tax" : "Create Tax"}
        onBack={() => navigate(-1)}
      />

      <div className="max-w-full mx-auto bg-white border border-slate-200 rounded-2xl shadow-sm">
        {/* Body */}
        <div className="p-6">
          {error && (
            <div className="mb-6 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-6">
            {/* Category */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-2">
                Category
              </label>

              <select
                id="cmb_TaxCategory"
                disabled={editMode}
                value={category}
                onChange={(e) => setCategory(e.target.value as TaxCategory)}
                className="w-full rounded-xl border border-slate-300 bg-white px-4 py-3 text-slate-700 outline-none transition focus:border-slate-500 disabled:bg-slate-100 disabled:text-slate-500 disabled:cursor-not-allowed"
              >
                <option value="">Select Category</option>
                <option value="Ppn">Ppn</option>
                <option value="Pph">Pph</option>
              </select>

              {editMode && (
                <p className="mt-2 text-xs text-slate-500">
                  Category cannot be changed.
                </p>
              )}
            </div>

            {/* Code */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-2">
                Tax Code
              </label>

              <input
                id="txt_TaxCode"
                type="text"
                value={code}
                onChange={(e) => setCode(e.target.value)}
                placeholder="Enter tax code"
                className="w-full rounded-xl border border-slate-300 px-4 py-3 outline-none transition focus:border-slate-500"
              />
            </div>

            {/* Name */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-2">
                Tax Name
              </label>

              <input
                id="txt_TaxName"
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Enter tax name"
                className="w-full rounded-xl border border-slate-300 px-4 py-3 outline-none transition focus:border-slate-500"
              />
            </div>

            {/* Rate */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-2">
                Rate (%)
              </label>

              <input
                id="nmf_TaxRate"
                type="number"
                min={0}
                max={100}
                disabled={editMode}
                value={rate}
                onChange={(e) => setRate(e.target.value)}
                placeholder="0 - 100"
                className="w-full rounded-xl border border-slate-300 px-4 py-3 outline-none transition focus:border-slate-500 disabled:bg-slate-100 disabled:text-slate-500 disabled:cursor-not-allowed"
              />

              {editMode && (
                <p className="mt-2 text-xs text-slate-500">
                  Rate cannot be changed.
                </p>
              )}
            </div>

            {/* Footer */}
            <div className="flex flex-col-reverse gap-3 pt-4 border-t border-slate-200 sm:flex-row sm:justify-end">
              <Button
                id="btn_CancelTax"
                type="button"
                variant="outline"
                size="lg"
                onClick={() => navigate(-1)}
                className="w-full sm:w-auto"
              >
                Cancel
              </Button>

              <Button
                id="btn_SaveTax"
                type="submit"
                variant="primary"
                size="lg"
                disabled={submitLoading}
                className="w-full sm:w-auto"
              >
                {submitLoading
                  ? "Saving..."
                  : editMode
                    ? "Update Tax"
                    : "Create Tax"}
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

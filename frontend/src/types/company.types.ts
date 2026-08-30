import type { ApiResponse } from "./auth.types";

export interface Company {
    id: number;
    code: string;
    name: string;
}

export type CompanyListResponse = ApiResponse<Company[]>;

// ─── Company Store State ──────────────────────────────────────────────────────
export interface CompanyState {
  companies: Company[];
  isLoading: boolean;
  error: string | null;
  selectedCompanyId: number | null;

  fetchCompanies: () => Promise<void>;
  clearError: () => void;
  setSelectedCompanyId: (id: number) => void;
}
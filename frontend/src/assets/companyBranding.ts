import type { Company } from "../types/company.types";

export interface CompanyBranding {
  code: string;
  logoUrl: string | null;
}

export function resolveCompanyBranding(
  companies: Company[],
  companyId: number | null,
  apiBaseUrl = "",
): CompanyBranding {
  const company = companies.find((candidate) => candidate.id === companyId);

  if (!company) {
    return { code: "Company", logoUrl: null };
  }

  const logoUrl = company.hasLogo ? company.logoUrl : null;

  return {
    code: company.code,
    logoUrl:
      logoUrl && apiBaseUrl ? new URL(logoUrl, apiBaseUrl).toString() : logoUrl,
  };
}

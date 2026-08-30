import axiosProvider from "../../providers/axiosProvider";
import type { CompanyListResponse } from "../../../types/company.types";

const COMPANY_ENDPOINTS = {
    publicList: "api/v1/companies/public",
} as const;

export const companyService = {
    getPublicList: async (): Promise<CompanyListResponse> => {
        const { data } = await axiosProvider.get<CompanyListResponse>(
            COMPANY_ENDPOINTS.publicList
        );
        return data;
    },
};
import axiosProvider from "../../../providers/axiosProvider";
import type { AccountPayableListResponse } from "../../../../types/listGenerateAp.type";
import type { AccountPayableDetailResponse } from "../../../../types/DetailAp.type";

const BASE_URL = "/api/v1/account-payables";

// 1. Definisikan tipe untuk Query Params (idealnya ini Anda pindahkan ke file types Anda)
export interface AccountPayableQueryParams {
  sortBy?: "status" | "docDate" | "createdAt";
  sortOrder?: "asc" | "desc";
  // Anda bisa menambahkan 'search', 'page', 'limit' di sini jika backend mendukungnya nanti
}

export const accountPayableService = {
  // ======================================================
  // LIST APPROVED RECAPS
  // ======================================================

  // 2. Tambahkan parameter dengan default object kosong
  async getApprovedRecaps(params: AccountPayableQueryParams = {}) {
    // 3. Beri nilai default agar jika dipanggil tanpa parameter tidak error
    const {
      sortBy = "createdAt", // Default mengurutkan dari yang terbaru dibuat
      sortOrder = "desc",
    } = params;

    const response = await axiosProvider.get<AccountPayableListResponse>(
      `${BASE_URL}/approved-recaps`,
      {
        // 4. Masukkan parameter ke config axios
        params: {
          sortBy,
          sortOrder,
        },
      },
    );

    return response.data;
  },

  // ======================================================
  // DETAIL
  // ======================================================

  async getAccountPayableDetail(id: number) {
    const response = await axiosProvider.get<AccountPayableDetailResponse>(
      `${BASE_URL}/${id}`,
    );

    return response.data;
  },

  // ======================================================
  // DELETE
  // ======================================================

  async deleteAccountPayable(id: number) {
    const response = await axiosProvider.delete(`${BASE_URL}/${id}`);

    return response.data;
  },
};

export type TaxCategory = "Ppn" | "Pph";

export interface TaxData {
  id: number;
  category: TaxCategory;
  code: string;
  name: string;
  rate: number;
  isActive: boolean;
}

export type TaxListData = TaxData[];

export interface TaxListResponse {
  success: boolean;
  data: TaxListData;
  message: string;
  requestId: string;
}

export interface TaxDetailResponse {
  success: boolean;
  data: TaxData;
  message: string;
  requestId: string;
}

export interface TaxFilterParams {
  category?: string;
  activeOnly?: boolean;
}

export interface CreateTaxBody {
  /**
   * required
   * Must be "Ppn" or "Pph"
   */
  category: TaxCategory;

  /**
   * required
   * unique
   */
  code: string;

  /**
   * required
   */
  name: string;

  /**
   * required
   * decimal 0-100
   */
  rate: number;
}

export interface UpdateTaxBody {
  /**
   * required
   * unique
   */
  code: string;

  /**
   * required
   */
  name: string;
}

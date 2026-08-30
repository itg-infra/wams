export interface WOPIC {
  id: number;
  fullname: string;
}

export interface WOPICResponse {
  success: boolean;
  data: WOPIC[];
  message: string;
  requestId: string;
}

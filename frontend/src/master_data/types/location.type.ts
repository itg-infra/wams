export interface LocationsResponse {
  success: boolean;
  data: LocationsData;
  message: string;
  requestId: string;
}

export interface LocationsData {
  locations: LocationItems[];
}

export interface LocationItems {
  id: number;
  name: string;
  display: string;
}

import axiosProvider from "../../api/providers/axiosProvider";
import type { LocationsResponse } from "../types/location.type";

export async function getLocationsService(): Promise<LocationsResponse> {
  const response = await axiosProvider.get<LocationsResponse>(
    "api/v1/warehouses/locations",
  );

  return response.data;
}

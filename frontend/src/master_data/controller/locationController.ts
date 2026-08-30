// controllers/locationController.ts

import axios from "axios";
import { getLocationsService } from "../services/locationService";
import { useLocationStore } from "../store/locationStore";

export async function fetchLocationsController() {
  const { setLocations, setLoading, setError } = useLocationStore.getState();

  try {
    setLoading(true);
    setError(null);

    const response = await getLocationsService();

    setLocations(response.data.locations);

    return response.data.locations;
  } catch (error: unknown) {
    let message = "Failed to fetch locations";

    if (axios.isAxiosError(error)) {
      message = error.response?.data?.message || message;
    }

    setError(message);

    return [];
  } finally {
    setLoading(false);
  }
}

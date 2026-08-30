import axios from "axios";
import type {
  AxiosInstance,
  AxiosRequestConfig,
  AxiosResponse,
  AxiosError,
  InternalAxiosRequestConfig,
} from "axios";
import { useWarehouseStore } from "../../store/warehouseStore";
import * as Sentry from "@sentry/react";

// ─── Extend AxiosRequestConfig untuk custom flag ──────────────────────────────
declare module "axios" {
  interface AxiosRequestConfig {
    withWarehouseId?: boolean;
  }
}

const axiosProvider: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  timeout: 10000,
  headers: {
    "Content-Type": "application/json",
    Accept: "application/json",
    "ngrok-skip-browser-warning": "true",
  },
});

axiosProvider.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem("token");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error: AxiosError) => Promise.reject(error),
);

// ─── Warehouse ID (opt-in) ────────────────────────────────────────────────────
axiosProvider.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    if (config.withWarehouseId) {
      const warehouseId = useWarehouseStore.getState().selectedWarehouse?.id;
      if (warehouseId) {
        config.headers["X-Warehouse-ID"] = String(warehouseId);
        config.headers["Cache-Control"] = "no-cache";
        config.headers["Pragma"] = "no-cache";
      }
    }
    return config;
  },
  (error: AxiosError) => Promise.reject(error),
);

axiosProvider.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    console.log(
      `%c[API REQUEST] ${config.method?.toUpperCase()} ${config.url}`,
      "color: #6366f1; font-weight: bold;",
    );
    if (config.data) {
      try {
        console.log("Payload:", JSON.parse(config.data));
      } catch {
        console.log("Payload:", config.data);
      }
    }

    console.log("Headers:", config.headers);

    // ─── Sentry: breadcrumb request ─────────────────────────────────────────
    Sentry.addBreadcrumb({
      category: "axios.request",
      message: `${config.method?.toUpperCase()} ${config.url}`,
      level: "info",
      data: {
        params: config.params,
      },
    });

    return config;
  },
  (error: AxiosError) => Promise.reject(error),
);

let refreshPromise: Promise<string> | null = null;

function refreshAccessToken(): Promise<string> {
  if (!refreshPromise) {
    refreshPromise = axios
      .post(
        `${import.meta.env.VITE_API_URL}api/v1/auth/refresh`,
        { refreshToken: localStorage.getItem("refreshToken") },
        {
          headers: {
            "Content-Type": "application/json",
            "ngrok-skip-browser-warning": "true",
          },
        },
      )
      .then(({ data }) => {
        if (!data?.data?.accessToken) {
          throw new Error("Refresh token response invalid");
        }

        localStorage.setItem("token", data.data.accessToken);
        localStorage.setItem("refreshToken", data.data.refreshToken);

        return data.data.accessToken as string;
      })
      .finally(() => {
        refreshPromise = null;
      });
  }
  return refreshPromise;
}

axiosProvider.interceptors.response.use(
  (response: AxiosResponse) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as AxiosRequestConfig & {
      _retry?: boolean;
    };

    // 401 → coba refresh token
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const accessToken = await refreshAccessToken();

        if (originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${accessToken}`;
        }

        return axiosProvider(originalRequest);
      } catch (refreshError) {
        // Refresh gagal → tandai session expired lalu redirect
        localStorage.removeItem("token");
        localStorage.removeItem("refreshToken");
        localStorage.removeItem("auth-storage");
        sessionStorage.setItem("session_expired", "true"); // ← flag untuk login screen

        // ─── Sentry: catat kegagalan refresh token ────────────────────────
        Sentry.captureException(refreshError, {
          tags: { phase: "auth.refresh_token" },
          extra: { originalUrl: originalRequest.url },
        });

        window.location.href = "/login";
        return Promise.reject(error);
      }
    }

    if (error.response?.status === 403) {
      window.location.href = "/forbidden";
    }

    if (error.response?.status === 500) {
      console.error("Server error, please try again later.");
    }

    // ─── Sentry: catat error API (selain 401 yang sudah ditangani di atas) ──
    if (error.response?.status !== 401) {
      Sentry.captureException(error, {
        contexts: {
          http: {
            method: originalRequest?.method,
            url: originalRequest?.url,
            status_code: error.response?.status,
          },
        },
        extra: {
          responseData: error.response?.data,
        },
        tags: {
          api_error: true,
        },
      });
    }

    return Promise.reject(error);
  },
);

// ─── Response Logger ──────────────────────────────────────────────────────────
axiosProvider.interceptors.response.use(
  (response: AxiosResponse) => {
    console.log(
      `%c[API RESPONSE] ${response.status} ${response.config.url}`,
      "color: #22c55e; font-weight: bold;",
    );
    console.log("Data:", response.data);
    return response;
  },
  (error: AxiosError) => {
    console.log(
      `%c[API ERROR] ${error.response?.status} ${error.config?.url}`,
      "color: #ef4444; font-weight: bold;",
    );
    console.log("Error Response:", error.response?.data);
    return Promise.reject(error);
  },
);

export default axiosProvider;

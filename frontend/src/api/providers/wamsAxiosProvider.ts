import axios from 'axios';
import type {
    AxiosInstance,
    AxiosRequestConfig,
    AxiosResponse,
    AxiosError,
    InternalAxiosRequestConfig,
} from 'axios';

const wamsAxiosProvider: AxiosInstance = axios.create({
    baseURL: import.meta.env.VITE_WAMS_API_URL, // e.g. https://wams.gerbangcahayautama.com:8020
    timeout: 15000,
    headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
    },
});

// ─── Request Interceptor: Attach Bearer Token ─────────────────────────────────
wamsAxiosProvider.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
        const token = localStorage.getItem('token');
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    (error: AxiosError) => Promise.reject(error)
);

// ─── Request Logger ───────────────────────────────────────────────────────────
wamsAxiosProvider.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
        console.log(
            `%c[WAMS REQUEST] ${config.method?.toUpperCase()} ${config.url}`,
            'color: #f59e0b; font-weight: bold;'
        );
        if (config.params) {
            console.log('Params:', config.params);
        }
        return config;
    },
    (error: AxiosError) => Promise.reject(error)
);

// ─── Response Interceptor ─────────────────────────────────────────────────────
wamsAxiosProvider.interceptors.response.use(
    (response: AxiosResponse) => {
        console.log(
            `%c[WAMS RESPONSE] ${response.status} ${response.config.url}`,
            'color: #22c55e; font-weight: bold;'
        );
        console.log('Data:', response.data);
        return response;
    },
    async (error: AxiosError) => {
        const originalRequest = error.config as AxiosRequestConfig & { _retry?: boolean };

        if (error.response?.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true;

            try {
                const refreshToken = localStorage.getItem('refreshToken');

                const { data } = await axios.post(
                    `${import.meta.env.VITE_API_URL}api/v1/auth/refresh`,
                    { refreshToken },
                    {
                        headers: { 'Content-Type': 'application/json' },
                    }
                );

                if (data?.data?.accessToken) {
                    localStorage.setItem('token', data.data.accessToken);
                    localStorage.setItem('refreshToken', data.data.refreshToken);

                    if (originalRequest.headers) {
                        originalRequest.headers.Authorization = `Bearer ${data.data.accessToken}`;
                    }

                    return wamsAxiosProvider(originalRequest);
                }

                throw new Error('Refresh token response invalid');
            } catch {
                localStorage.removeItem('token');
                localStorage.removeItem('refreshToken');
                localStorage.removeItem('auth-storage');
                sessionStorage.setItem('session_expired', 'true');
                window.location.href = '/login';
                return Promise.reject(error);
            }
        }

        if (error.response?.status === 403) {
            window.location.href = '/forbidden';
        }

        console.error(
            `%c[WAMS ERROR] ${error.response?.status} ${error.config?.url}`,
            'color: #ef4444; font-weight: bold;'
        );
        console.error('Error Response:', error.response?.data);

        return Promise.reject(error);
    }
);

export default wamsAxiosProvider;
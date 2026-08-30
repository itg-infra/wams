import type {
  ListNotification,
  ListNotificationParams,
  ListNotificationResponse,
} from "../../../types/listNotifications";
import type { NotificationStreamResponse } from "../../../types/notificationStream";
import axiosProvider from "../../providers/axiosProvider";

import { EventSourcePolyfill } from "event-source-polyfill";

export interface NotificationSSEPayload {
  event: string;
  data: ListNotification;
}

export const getNotifications = async (
  params: ListNotificationParams,
): Promise<ListNotificationResponse> => {
  const response = await axiosProvider.get("api/v1/notifications", {
    params,
  });

  return response.data;
};

export const markNotificationAsRead = async (notificationId: number) => {
  const response = await axiosProvider.post(
    `/api/v1/notifications/${notificationId}/read`,
  );

  return response.data;
};

export const markAllNotificationsAsRead = async (): Promise<{
  updatedCount: number;
}> => {
  const response = await axiosProvider.post("/api/v1/notifications/read-all");

  return response.data;
};

export const createNotificationStream = (
  onMessage: (notification: NotificationStreamResponse) => void,
  onError?: (error: Event) => void,
) => {
  const token = localStorage.getItem("token");

  const eventSource = new EventSourcePolyfill(
    `${import.meta.env.VITE_API_URL_TEST}api/v1/notifications/stream`,
    {
      withCredentials: true,

      headers: {
        Authorization: `Bearer ${token}`,
      },
    },
  );

  eventSource.addEventListener("connected", (event) => {
    console.log("SSE connected", event);
  });

  eventSource.addEventListener("heartbeat", () => {});

  eventSource.addEventListener("notification", (event) => {
    try {
      const messageEvent = event as MessageEvent;
      console.log(messageEvent.data);
      onMessage(JSON.parse(messageEvent.data));
    } catch (error) {
      console.error("Failed to parse notification:", error);
    }
  });

  eventSource.onerror = (error) => {
    onError?.(error.target);
  };

  return eventSource;
};

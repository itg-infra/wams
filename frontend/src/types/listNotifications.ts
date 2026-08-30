export interface ListNotification {
  id: number;
  type: string;
  title: string;
  message: string;
  referenceType: string;
  referenceId: string;
  status: "read" | "unread";
  createdAt: string;
  readAt: string | null;
  recipientUserId: number;
  actorUserId: number | null;
  route: string;
}

export interface ListNotificationMeta {
  page: number;
  limit: number;
  total: number;
  totalPages: number;
}

export interface ListNotificationResponse {
  success: boolean;
  data: ListNotification[];
  meta: ListNotificationMeta;
  requestId: string;
}

export interface ListNotificationParams {
  unreadOnly?: boolean;
  page?: number;
  limit?: number;
}

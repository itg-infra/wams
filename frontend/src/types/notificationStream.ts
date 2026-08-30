export interface NotificationStreamResponse {
  Id: number;
  Type: string;
  Title: string;
  Message: string;
  ReferenceType: string;
  ReferenceId: string;
  Status: "unread" | "read";
  CreatedAt: string;
  ReadAt: string | null;
  RecipientUserId: number;
  ActorUserId: number;
}

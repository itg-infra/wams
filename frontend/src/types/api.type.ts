export type ApiErrorResponse = {
  success: false;

  message: string;

  error?: unknown;

  requestId?: string;
};

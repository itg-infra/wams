export type Permission = {
    id: number;
    module: string;
    resource: string;
    action: string;
    description: string;
};

export type GetPermissionsResponse = {
    success: boolean;
    data: Permission[];
    message: string;
    requestId: string;
};
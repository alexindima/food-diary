export type AdminContentReport = {
    id: string;
    reporterId: string;
    targetType: string;
    targetId: string;
    reason: string;
    status: string;
    adminNote?: string | null;
    createdAtUtc: string;
    reviewedAtUtc?: string | null;
};

export type AdminReportAction = {
    adminNote?: string | null;
};

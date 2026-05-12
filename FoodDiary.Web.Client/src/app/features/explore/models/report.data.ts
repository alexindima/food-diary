export type ContentReport = {
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

export type CreateReportDto = {
    targetType: 'Recipe' | 'Comment';
    targetId: string;
    reason: string;
};

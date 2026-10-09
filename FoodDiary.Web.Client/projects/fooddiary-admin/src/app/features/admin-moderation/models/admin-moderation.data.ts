import type { AdminId, AdminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
export type AdminContentReport = {
    id: AdminId<'content-report'>;
    reporterId: AdminId<'user'>;
    targetType: string;
    targetId: string;
    reason: string;
    status: string;
    adminNote?: string | null;
    createdAtUtc: AdminUtcInstant;
    reviewedAtUtc?: AdminUtcInstant | null;
    reviewedByUserId?: AdminId<'user'> | null;
    targetTitle?: string | null;
    targetExcerpt?: string | null;
};

export type AdminReportAction = {
    adminNote?: string | null;
};

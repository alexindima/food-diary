import type { AdminId, AdminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
export type AdminBugReport = {
    id: AdminId<'bug-report'>;
    sourceMessageId: AdminId<'mail-inbox-message'>;
    receivedAtUtc: AdminUtcInstant;
    subject: string;
    status: string;
    attempt: number;
    summary: string | null;
    mergeRequestUrl: string | null;
    contentExpired: boolean;
};

export type AdminBugReportPage = {
    items: AdminBugReport[];
    totalItems: number;
    isConfigured: boolean;
};

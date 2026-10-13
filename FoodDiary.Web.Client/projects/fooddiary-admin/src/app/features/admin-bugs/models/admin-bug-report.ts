import type { AdminId, AdminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
export type AdminBugReport = {
    id: AdminId<'bug-report'>;
    sourceMessageId: AdminId<'mail-inbox-message'> | null;
    source: 'email' | 'logs';
    auditRunId: AdminId<'audit-run'> | null;
    auditFromUtc: AdminUtcInstant | null;
    auditToUtc: AdminUtcInstant | null;
    receivedAtUtc: AdminUtcInstant;
    subject: string;
    title: string | null;
    shortSummary: string | null;
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
    latestAudit: AdminAuditRunSummary | null;
};

export type AdminAuditRunSummary = {
    id: AdminId<'audit-run'>;
    fromUtc: AdminUtcInstant;
    toUtc: AdminUtcInstant;
    startedAtUtc: AdminUtcInstant;
    completedAtUtc: AdminUtcInstant | null;
    status: string;
    summary: string | null;
    findingCount: number;
    contentExpired: boolean;
    isStale: boolean;
    lastSuccessfulAtUtc: AdminUtcInstant | null;
};

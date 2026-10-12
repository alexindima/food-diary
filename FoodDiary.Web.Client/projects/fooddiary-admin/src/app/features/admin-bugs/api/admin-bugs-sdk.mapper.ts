import type { AdminAuditRunSummaryHttpResponse } from '../../../shared/api/sdk/generated/model/admin-audit-run-summary-http-response';
import type { AdminBugJournalEntryHttpResponse } from '../../../shared/api/sdk/generated/model/admin-bug-journal-entry-http-response';
import type { AdminBugJournalPageHttpResponse } from '../../../shared/api/sdk/generated/model/admin-bug-journal-page-http-response';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import type { AdminAuditRunSummary, AdminBugReport, AdminBugReportPage } from '../models/admin-bug-report';

export function adminBugReportPageFromSdk(response: AdminBugJournalPageHttpResponse): AdminBugReportPage {
    const value = requireSdkFields(response, ['items', 'totalItems', 'isConfigured']);
    return {
        ...value,
        items: value.items.map(item => adminBugReportFromSdk(item)),
        latestAudit: value.latestAudit !== undefined && value.latestAudit !== null ? adminAuditRunFromSdk(value.latestAudit) : null,
    };
}

export function adminBugReportFromSdk(response: AdminBugJournalEntryHttpResponse): AdminBugReport {
    const value = requireSdkFields(response, ['id', 'receivedAtUtc', 'subject', 'status', 'attempt', 'contentExpired']);
    return {
        ...value,
        ...reportSourceFromSdk(value),
        summary: value.summary ?? null,
        mergeRequestUrl: value.mergeRequestUrl ?? null,
        id: adminId<'bug-report'>(value.id),
        receivedAtUtc: adminUtcInstant(value.receivedAtUtc),
    };
}

function reportSourceFromSdk(
    response: AdminBugJournalEntryHttpResponse,
): Pick<AdminBugReport, 'source' | 'sourceMessageId' | 'auditRunId' | 'auditFromUtc' | 'auditToUtc'> {
    const source = response.source ?? 'email';
    if (source === 'logs') {
        const audit = requireSdkFields(response, ['auditRunId', 'auditFromUtc', 'auditToUtc']);
        return {
            source,
            sourceMessageId: null,
            auditRunId: adminId<'audit-run'>(audit.auditRunId),
            auditFromUtc: adminUtcInstant(audit.auditFromUtc),
            auditToUtc: adminUtcInstant(audit.auditToUtc),
        };
    }
    if (source !== 'email') {
        throw new Error('Unknown bug report source');
    }
    const email = requireSdkFields(response, ['sourceMessageId']);
    return {
        source,
        sourceMessageId: adminId<'mail-inbox-message'>(email.sourceMessageId),
        auditRunId: null,
        auditFromUtc: null,
        auditToUtc: null,
    };
}

export function adminAuditRunFromSdk(response: AdminAuditRunSummaryHttpResponse): AdminAuditRunSummary {
    const value = requireSdkFields(response, [
        'id',
        'fromUtc',
        'toUtc',
        'startedAtUtc',
        'status',
        'findingCount',
        'contentExpired',
        'isStale',
    ]);
    return {
        ...value,
        id: adminId<'audit-run'>(value.id),
        fromUtc: adminUtcInstant(value.fromUtc),
        toUtc: adminUtcInstant(value.toUtc),
        startedAtUtc: adminUtcInstant(value.startedAtUtc),
        completedAtUtc: value.completedAtUtc !== undefined && value.completedAtUtc !== null ? adminUtcInstant(value.completedAtUtc) : null,
        lastSuccessfulAtUtc:
            value.lastSuccessfulAtUtc !== undefined && value.lastSuccessfulAtUtc !== null
                ? adminUtcInstant(value.lastSuccessfulAtUtc)
                : null,
        summary: value.summary ?? null,
    };
}

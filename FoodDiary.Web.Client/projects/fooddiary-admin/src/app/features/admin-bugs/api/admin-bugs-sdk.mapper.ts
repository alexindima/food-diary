import type { AdminBugReportHttpResponse } from '../../../shared/api/sdk/generated/model/admin-bug-report-http-response';
import type { AdminBugReportPageHttpResponse } from '../../../shared/api/sdk/generated/model/admin-bug-report-page-http-response';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import type { AdminBugReport, AdminBugReportPage } from '../models/admin-bug-report';

export function adminBugReportPageFromSdk(response: AdminBugReportPageHttpResponse): AdminBugReportPage {
    const value = requireSdkFields(response, ['items', 'totalItems', 'isConfigured']);
    return { ...value, items: value.items.map(item => adminBugReportFromSdk(item)) };
}

export function adminBugReportFromSdk(response: AdminBugReportHttpResponse): AdminBugReport {
    const value = requireSdkFields(response, ['id', 'sourceMessageId', 'receivedAtUtc', 'subject', 'status', 'attempt', 'contentExpired']);
    return {
        ...value,
        summary: value.summary ?? null,
        mergeRequestUrl: value.mergeRequestUrl ?? null,
        id: adminId<'bug-report'>(value.id),
        sourceMessageId: adminId<'mail-inbox-message'>(value.sourceMessageId),
        receivedAtUtc: adminUtcInstant(value.receivedAtUtc),
    };
}

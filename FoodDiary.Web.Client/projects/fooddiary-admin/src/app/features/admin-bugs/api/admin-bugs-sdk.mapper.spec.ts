import { describe, expect, it } from 'vitest';

import type { AdminBugJournalEntryHttpResponse } from '../../../shared/api/sdk/generated/model/admin-bug-journal-entry-http-response';
import { adminAuditRunFromSdk, adminBugReportFromSdk, adminBugReportPageFromSdk } from './admin-bugs-sdk.mapper';

const entry: AdminBugJournalEntryHttpResponse = {
    id: 'report-id',
    receivedAtUtc: '2026-10-12T05:00:00Z',
    subject: 'Navigation failure',
    status: 'needs_information',
    attempt: 1,
    contentExpired: false,
};

describe('Admin bug journal decoder', () => {
    it('keeps older email responses compatible and requires their real message ID', () => {
        const legacy = adminBugReportFromSdk({ ...entry, sourceMessageId: 'mail-id' });
        expect(legacy.source).toBe('email');
        expect(legacy.title).toBeNull();
        expect(legacy.shortSummary).toBeNull();
        expect(() => adminBugReportFromSdk(entry)).toThrow();
        expect(adminBugReportPageFromSdk({ items: [], totalItems: 0, isConfigured: true }).latestAudit).toBeNull();
    });

    it('keeps normalized presentation fields without replacing original evidence', () => {
        const report = adminBugReportFromSdk({
            ...entry,
            sourceMessageId: 'mail-id',
            title: 'Clear title',
            shortSummary: 'Concise result',
        });
        expect(report.title).toBe('Clear title');
        expect(report.subject).toBe(entry.subject);
        expect(report.shortSummary).toBe('Concise result');
    });

    it('decodes log findings with an audit ID and interval without inventing a mail ID', () => {
        const finding = adminBugReportFromSdk({
            ...entry,
            source: 'logs',
            auditRunId: 'audit-id',
            auditFromUtc: '2026-10-11T05:00:00Z',
            auditToUtc: '2026-10-12T05:00:00Z',
        });
        expect(finding.sourceMessageId).toBeNull();
        expect(finding.auditRunId).toBe('audit-id');
        expect(finding.auditFromUtc).toBe('2026-10-11T05:00:00Z');
        expect(() => adminBugReportFromSdk({ ...entry, source: 'logs' })).toThrow();
        expect(() => adminBugReportFromSdk({ ...entry, source: 'unknown' })).toThrow();
    });

    it('requires audit coverage and staleness metadata instead of defaulting to a healthy result', () => {
        expect(() => adminAuditRunFromSdk({ status: 'clean' })).toThrow();
    });
});

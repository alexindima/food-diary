import { describe, expect, it } from 'vitest';

import { adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import type { AdminMailInboxDmarcReport } from '../models/admin-mail-inbox.data';
import { type AdminDmarcSourcedReport, summarizeAdminDmarcReports } from './admin-dmarc-summary';

const selectedPeriod = {
    startUtc: adminUtcInstant('2026-11-18T00:00:00Z'),
    endExclusiveUtc: adminUtcInstant('2026-11-25T00:00:00Z'),
};

function verifiedReport(overrides: Partial<AdminMailInboxDmarcReport> = {}): AdminDmarcSourcedReport {
    return {
        provenance: 'verified',
        report: {
            organizationName: 'google.com',
            domain: 'fooddiary.club',
            reportId: 'id-1',
            dateRangeStartUtc: adminUtcInstant('2026-11-20T00:00:00Z'),
            dateRangeEndUtc: adminUtcInstant('2026-11-20T23:59:59Z'),
            records: [{ sourceIp: '192.0.2.1', count: 1, dkim: 'pass', spf: 'fail' }],
            ...overrides,
        },
    };
}

describe('summarizeAdminDmarcReports outcomes', () => {
    it('uses aligned SPF and DKIM for results and leaves missing outcomes out of the failure rate', () => {
        const report = verifiedReport({
            records: [
                { sourceIp: '192.0.2.1', count: 2, dkim: 'pass', spf: 'fail' },
                { sourceIp: '192.0.2.1', count: 5, dkim: ' FAIL ', spf: 'FAIL', disposition: 'none', dkimResult: 'pass' },
                { sourceIp: '2001:DB8::1', count: 3, spf: ' PASS ' },
                { sourceIp: '2001:db8::1', count: 8, dkim: 'fail', spfResult: 'pass' },
                { sourceIp: '192.0.2.2', count: 9, dkimResult: 'pass', spfResult: 'pass' },
                { sourceIp: '192.0.2.3', count: -10, dkim: 'pass' },
            ],
        });

        const affectedMessages = 5;
        const evaluatedMessages = 7;
        const summary = summarizeAdminDmarcReports([report], selectedPeriod);

        expect(summary).toMatchObject({
            countedReports: 1,
            invalidRecords: 1,
            totals: { messages: 27, passed: 5, failed: 5, unknown: 17, failureRate: 0.5 },
            sources: [
                { sourceIp: '192.0.2.1', messages: 7, passed: 2, failed: 5, unknown: 0, failureRate: affectedMessages / evaluatedMessages },
                { sourceIp: '2001:db8::1', messages: 11, passed: 3, failed: 0, unknown: 8, failureRate: 0 },
                { sourceIp: '192.0.2.2', messages: 9, passed: 0, failed: 0, unknown: 9, failureRate: null },
            ],
        });
    });

    it('uses the earliest verified reporting period for each observed source', () => {
        const earliestVerified = verifiedReport({
            reportId: 'earliest-verified',
            dateRangeStartUtc: adminUtcInstant('2026-11-19T00:00:00Z'),
        });
        const unverified: AdminDmarcSourcedReport = {
            provenance: 'unverified',
            report: { ...earliestVerified.report, dateRangeStartUtc: adminUtcInstant('2026-11-18T00:00:00Z') },
        };

        const summary = summarizeAdminDmarcReports([verifiedReport(), unverified, earliestVerified], selectedPeriod);

        expect(summary.sources).toMatchObject([{ sourceIp: '192.0.2.1', firstReportedAtUtc: '2026-11-19T00:00:00.000Z' }]);
    });
});

describe('summarizeAdminDmarcReports reports', () => {
    it('counts matching copies once and excludes all conflicting copies of a report', () => {
        const records = [
            { sourceIp: '192.0.2.1', count: 4, dkim: 'pass' },
            { sourceIp: '192.0.2.2', count: 1, dkim: 'fail', spf: 'fail' },
        ];
        const original = verifiedReport({ records });
        const duplicate = verifiedReport({
            organizationName: '  GOOGLE.COM.  ',
            domain: 'FoodDiary.Club',
            reportId: ' <id-1> ',
            records: [...records].reverse(),
        });
        const anotherProvider = verifiedReport({ organizationName: 'comcast.net' });
        const conflictingOne = verifiedReport({ reportId: 'id-conflict' });
        const conflictingTwo = verifiedReport({ reportId: 'id-conflict', records });
        const summary = summarizeAdminDmarcReports(
            [conflictingOne, original, anotherProvider, duplicate, conflictingOne, conflictingTwo],
            selectedPeriod,
        );

        expect(summary).toMatchObject({
            countedReports: 2,
            duplicateReports: 1,
            conflictingReports: 3,
            totals: { messages: 6, passed: 5, failed: 1 },
        });
    });

    it('includes only whole periods within the selected days and reports missing metadata', () => {
        const fromBoundary = verifiedReport({ reportId: 'first', dateRangeStartUtc: adminUtcInstant('2026-11-18T00:00:00Z') });
        const toBoundary = verifiedReport({ reportId: 'last', dateRangeEndUtc: adminUtcInstant('2026-11-25T00:00:00Z') });
        const beforeBoundary = verifiedReport({ reportId: 'early', dateRangeStartUtc: adminUtcInstant('2026-11-17T23:59:59Z') });
        const afterBoundary = verifiedReport({ reportId: 'late', dateRangeEndUtc: adminUtcInstant('2026-11-25T00:00:01Z') });
        const nextDay = verifiedReport({
            reportId: 'next',
            dateRangeStartUtc: adminUtcInstant('2026-11-25T00:00:00Z'),
            dateRangeEndUtc: adminUtcInstant('2026-11-25T00:00:00Z'),
        });
        const invalid = verifiedReport({ dateRangeEndUtc: null });
        const noId = verifiedReport({ reportId: '' });

        expect(
            summarizeAdminDmarcReports([fromBoundary, toBoundary, beforeBoundary, afterBoundary, nextDay, invalid, noId], selectedPeriod),
        ).toMatchObject({ countedReports: 2, outOfPeriodReports: 3, invalidReports: 2, totals: { messages: 2 } });
    });

    it('keeps unverified results separate and never lets them override a verified copy', () => {
        const verified = verifiedReport();
        const unverified: AdminDmarcSourcedReport = {
            provenance: 'unverified',
            report: { ...verified.report, records: [{ sourceIp: '192.0.2.1', count: 50, dkim: 'fail', spf: 'fail' }] },
        };

        expect(summarizeAdminDmarcReports([verified, unverified], selectedPeriod)).toMatchObject({
            countedReports: 1,
            unverifiedReports: 1,
            conflictingReports: 0,
            totals: { messages: 1, passed: 1, failed: 0, failureRate: 0 },
        });
        expect(summarizeAdminDmarcReports([unverified], selectedPeriod)).toMatchObject({
            countedReports: 0,
            unverifiedReports: 1,
            totals: { messages: 0, failureRate: null },
        });
    });
});

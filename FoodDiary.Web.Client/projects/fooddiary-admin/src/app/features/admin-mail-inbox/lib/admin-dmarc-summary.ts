import { type AdminUtcInstant, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import type { AdminMailInboxDmarcRecord, AdminMailInboxDmarcReport } from '../models/admin-mail-inbox.data';

export type AdminDmarcReportProvenance = 'verified' | 'unverified';

export type AdminDmarcSourcedReport = {
    report: AdminMailInboxDmarcReport;
    provenance: AdminDmarcReportProvenance;
};

export type AdminDmarcSummaryWindow = {
    startUtc: AdminUtcInstant;
    endExclusiveUtc: AdminUtcInstant;
};

type DmarcCounts = {
    passed: number;
    failed: number;
    unknown: number;
};

type DmarcOutcome = keyof DmarcCounts;

export type AdminDmarcCounts = DmarcCounts & {
    messages: number;
    failureRate: number | null;
};

type AdminDmarcSourceSummary = AdminDmarcCounts & { sourceIp: string; firstReportedAtUtc: AdminUtcInstant };

export type AdminDmarcSummary = {
    totals: AdminDmarcCounts;
    sources: AdminDmarcSourceSummary[];
    countedReports: number;
    unverifiedReports: number;
    outOfPeriodReports: number;
    invalidReports: number;
    duplicateReports: number;
    conflictingReports: number;
    invalidRecords: number;
};

type ReportGroup = {
    records: readonly AdminMailInboxDmarcRecord[];
    start: number;
    fingerprint: string;
    copies: number;
    conflicting: boolean;
};

function normalize(value: string | null | undefined): string {
    return (value ?? '').trim().toLowerCase();
}

function normalizeDomain(value: string | null | undefined): string {
    return normalize(value).replace(/\.$/u, '');
}

function normalizeReportId(value: string | null | undefined): string {
    const trimmed = (value ?? '').trim();
    return trimmed.startsWith('<') && trimmed.endsWith('>') ? trimmed.slice(1, -1).trim() : trimmed;
}

function parseUtc(value: AdminUtcInstant | null | undefined): number {
    return value !== null && value !== undefined && /(?:Z|[+-]\d{2}:\d{2})$/iu.test(value) ? Date.parse(value) : Number.NaN;
}

function reportKey(report: AdminMailInboxDmarcReport): string | null {
    const reporter = normalizeDomain(report.organizationName);
    const domain = normalizeDomain(report.domain);
    const reportId = normalizeReportId(report.reportId);
    return reporter !== '' && domain !== '' && reportId !== '' ? JSON.stringify([reporter, domain, reportId]) : null;
}

function fingerprintReport(report: AdminMailInboxDmarcReport, start: number, end: number): string {
    const recordFingerprints = report.records
        .map(record =>
            JSON.stringify([
                normalize(record.sourceIp),
                record.count,
                normalize(record.disposition),
                normalize(record.dkim),
                normalize(record.spf),
                normalizeDomain(record.headerFrom),
                normalizeDomain(record.envelopeFrom),
                normalizeDomain(record.dkimDomain),
                normalize(record.dkimResult),
                normalizeDomain(record.spfDomain),
                normalize(record.spfResult),
            ]),
        )
        .sort();
    return JSON.stringify([start, end, recordFingerprints]);
}

function addReport(
    groups: Map<string, ReportGroup>,
    key: string,
    report: AdminMailInboxDmarcReport,
    { start, end }: { start: number; end: number },
): void {
    const fingerprint = fingerprintReport(report, start, end);
    const group = groups.get(key);
    if (group !== undefined) {
        group.copies += 1;
        group.conflicting ||= group.fingerprint !== fingerprint;
        return;
    }
    groups.set(key, { records: report.records, start, fingerprint, copies: 1, conflicting: false });
}

function reportOutcome(record: AdminMailInboxDmarcRecord): DmarcOutcome {
    const dkim = normalize(record.dkim);
    const spf = normalize(record.spf);
    if (dkim === 'pass' || spf === 'pass') {
        return 'passed';
    }
    return dkim === 'fail' && spf === 'fail' ? 'failed' : 'unknown';
}

function emptyCounts(): DmarcCounts {
    return { passed: 0, failed: 0, unknown: 0 };
}

function summarizeCounts(counts: DmarcCounts): AdminDmarcCounts {
    const evaluatedMessages = counts.passed + counts.failed;
    return {
        ...counts,
        messages: evaluatedMessages + counts.unknown,
        failureRate: evaluatedMessages === 0 ? null : counts.failed / evaluatedMessages,
    };
}

/**
 * Counts whole report periods contained in the selected UTC days. Reporters can
 * express the end as the last second or as the next midnight. Partial periods
 * are skipped because their aggregate rows cannot be prorated.
 */
export function summarizeAdminDmarcReports(
    reports: readonly AdminDmarcSourcedReport[],
    selectedPeriod: AdminDmarcSummaryWindow,
): AdminDmarcSummary {
    const windowStart = parseUtc(selectedPeriod.startUtc);
    const windowEnd = parseUtc(selectedPeriod.endExclusiveUtc);
    if (!(windowStart < windowEnd)) {
        throw new RangeError('The DMARC summary requires an ordered UTC date window.');
    }

    let unverifiedReports = 0;
    let outOfPeriodReports = 0;
    let invalidReports = 0;
    const groups = new Map<string, ReportGroup>();
    for (const item of reports) {
        const start = parseUtc(item.report.dateRangeStartUtc);
        const end = parseUtc(item.report.dateRangeEndUtc);
        if (!(start <= end)) {
            invalidReports += 1;
            continue;
        }
        if (!isWithinPeriod(start, end, windowStart, windowEnd)) {
            outOfPeriodReports += 1;
            continue;
        }
        if (item.provenance === 'unverified') {
            unverifiedReports += 1;
            continue;
        }
        const key = reportKey(item.report);
        if (key === null) {
            invalidReports += 1;
            continue;
        }
        addReport(groups, key, item.report, { start, end });
    }

    return { ...summarizeGroups(groups), unverifiedReports, outOfPeriodReports, invalidReports };
}

function isWithinPeriod(start: number, end: number, periodStart: number, periodEnd: number): boolean {
    return start >= periodStart && start < periodEnd && end <= periodEnd;
}

function compareSources(left: AdminDmarcSourceSummary, right: AdminDmarcSourceSummary): number {
    if (left.failed !== right.failed) {
        return right.failed - left.failed;
    }
    if (left.messages !== right.messages) {
        return right.messages - left.messages;
    }
    return left.sourceIp.localeCompare(right.sourceIp);
}
function summarizeGroups(
    groups: ReadonlyMap<string, ReportGroup>,
): Omit<AdminDmarcSummary, 'unverifiedReports' | 'outOfPeriodReports' | 'invalidReports'> {
    const totals = emptyCounts();
    const sources = new Map<string, { counts: DmarcCounts; firstReported: number }>();
    let countedReports = 0;
    let duplicateReports = 0;
    let conflictingReports = 0;
    let invalidRecords = 0;
    for (const group of groups.values()) {
        if (group.conflicting) {
            conflictingReports += group.copies;
            continue;
        }
        countedReports += 1;
        duplicateReports += group.copies - 1;
        for (const record of group.records) {
            if (!Number.isSafeInteger(record.count) || record.count <= 0) {
                invalidRecords += 1;
                continue;
            }
            const outcome = reportOutcome(record);
            const sourceIp = normalize(record.sourceIp);
            const source = sources.get(sourceIp) ?? { counts: emptyCounts(), firstReported: group.start };
            source.firstReported = Math.min(source.firstReported, group.start);
            source.counts[outcome] += record.count;
            sources.set(sourceIp, source);
            totals[outcome] += record.count;
        }
    }
    return {
        totals: summarizeCounts(totals),
        sources: Array.from(sources, ([sourceIp, source]) => ({
            sourceIp,
            ...summarizeCounts(source.counts),
            firstReportedAtUtc: adminUtcInstant(new Date(source.firstReported).toISOString()),
        })).sort(compareSources),
        countedReports,
        duplicateReports,
        conflictingReports,
        invalidRecords,
    };
}

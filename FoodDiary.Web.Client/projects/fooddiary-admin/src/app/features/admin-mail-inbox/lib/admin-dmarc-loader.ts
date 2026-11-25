import { catchError, from, map, mergeMap, type Observable, of, switchMap, toArray } from 'rxjs';

import { adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import type { AdminMailInboxDmarcReport, AdminMailInboxMessageSummary } from '../models/admin-mail-inbox.data';
import { type AdminDmarcSummary, type AdminDmarcSummaryWindow, summarizeAdminDmarcReports } from './admin-dmarc-summary';
import type { AdminMailInboxFacade } from './admin-mail-inbox.facade';
import { DMARC_MAILBOX } from './admin-mail-inbox-preferences';

export type AdminDmarcPeriod = '7' | '30';

type DmarcReportMessage = {
    message: AdminMailInboxMessageSummary;
    report: AdminMailInboxDmarcReport | null;
    status: 'verified' | 'unverified' | 'unavailable' | 'load-error';
    reporter: string;
    domain: string;
};

export type AdminDmarcView = {
    summary: AdminDmarcSummary;
    reports: DmarcReportMessage[];
    totalReports: number;
    incomplete: boolean;
    hasDuplicateOrConflictingReports: boolean;
};

const REPORT_LOAD_LIMIT = 50;

export function loadAdminDmarcView(facade: AdminMailInboxFacade, period: AdminDmarcPeriod): Observable<AdminDmarcView> {
    const now = new Date();
    const end = new Date(now);
    end.setUTCHours(0, 0, 0, 0);
    const start = new Date(end);
    start.setUTCDate(start.getUTCDate() - Number(period));
    const periodBounds: AdminDmarcSummaryWindow = {
        startUtc: adminUtcInstant(start.toISOString()),
        endExclusiveUtc: adminUtcInstant(end.toISOString()),
    };

    return facade
        .getMessagePage(1, REPORT_LOAD_LIMIT, { recipient: DMARC_MAILBOX, fromUtc: periodBounds.startUtc, toUtc: now.toISOString() })
        .pipe(
            switchMap(page =>
                from(page.items).pipe(
                    // The MailInbox service limits concurrent detail parsing across all callers.
                    mergeMap(message => loadDmarcReport(facade, message), 1),
                    toArray(),
                    map(reports => buildView(reports, page.totalItems, periodBounds)),
                ),
            ),
        );
}

function buildView(reports: DmarcReportMessage[], totalReports: number, periodBounds: AdminDmarcSummaryWindow): AdminDmarcView {
    const summary = summarizeAdminDmarcReports(
        reports.flatMap(item =>
            item.report === null
                ? []
                : [{ report: item.report, provenance: item.report.isSourceVerified === true ? 'verified' : 'unverified' }],
        ),
        periodBounds,
    );
    const unavailableReports = reports.some(item => item.status === 'unavailable' || item.status === 'load-error');
    return {
        reports,
        summary,
        totalReports,
        incomplete: unavailableReports || reports.length < totalReports || summary.invalidReports + summary.invalidRecords > 0,
        hasDuplicateOrConflictingReports: summary.duplicateReports + summary.conflictingReports > 0,
    };
}

function loadDmarcReport(facade: AdminMailInboxFacade, message: AdminMailInboxMessageSummary): Observable<DmarcReportMessage> {
    return facade.getMessage(message.id).pipe(
        map((detail): DmarcReportMessage => {
            const report = detail.dmarcReport ?? null;
            const status = report === null ? 'unavailable' : report.isSourceVerified === true ? 'verified' : 'unverified';
            return {
                message,
                report,
                status,
                reporter: report?.organizationName ?? message.fromAddress ?? '—',
                domain: report?.domain ?? '—',
            };
        }),
        catchError(() =>
            of<DmarcReportMessage>({ message, report: null, status: 'load-error', reporter: message.fromAddress ?? '—', domain: '—' }),
        ),
    );
}

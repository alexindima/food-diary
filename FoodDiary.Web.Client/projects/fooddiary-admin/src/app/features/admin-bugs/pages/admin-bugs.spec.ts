import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminBugsService } from '../api/admin-bugs.service';
import type { AdminBugReport, AdminBugReportPage } from '../models/admin-bug-report';
import { AdminBugsPageComponent } from './admin-bugs';

const getPage = vi.fn<AdminBugsService['getPage']>();

function testFinding(id: string, changes: Partial<AdminBugReport> = {}): AdminBugReport {
    return {
        id: adminId<'bug-report'>(id),
        title: null,
        subject: 'Navigation finding',
        receivedAtUtc: adminUtcInstant('2026-10-12T05:00:00Z'),
        source: 'logs',
        sourceMessageId: null,
        auditRunId: adminId<'audit-run'>('audit'),
        auditFromUtc: adminUtcInstant('2026-10-11T05:00:00Z'),
        auditToUtc: adminUtcInstant('2026-10-12T05:00:00Z'),
        status: 'draft_ready',
        attempt: 1,
        shortSummary: null,
        summary: null,
        mergeRequestUrl: null,
        contentExpired: false,
        ...changes,
    };
}

beforeEach(async () => {
    getPage.mockReset().mockReturnValue(of({ items: [], totalItems: 0, isConfigured: true, latestAudit: null }));
    await TestBed.configureTestingModule({
        imports: [AdminBugsPageComponent],
        providers: [
            provideRouter(
                [
                    {
                        path: 'bugs',
                        children: [
                            { path: '', component: AdminBugsPageComponent },
                            { path: ':id', component: AdminBugsPageComponent },
                        ],
                    },
                ],
                withComponentInputBinding({ unmatchedInputBehavior: 'undefinedIfStale' }),
            ),
            ...provideTranslateTesting(),
            { provide: AdminBugsService, useValue: { getPage } },
        ],
    }).compileComponents();
});

it('distinguishes an unconfigured connection from an empty result and failure', async () => {
    getPage.mockReturnValueOnce(of({ items: [], totalItems: 0, isConfigured: false, latestAudit: null }));
    const fixture = await RouterTestingHarness.create('/bugs');
    expect(fixture.routeNativeElement?.textContent).toContain('ADMIN_BUGS.NOT_CONFIGURED');
    expect(fixture.routeNativeElement?.textContent).not.toContain('ADMIN_BUGS.EMPTY_EMAIL');
    getPage.mockReturnValueOnce(throwError(() => new Error('offline')));
    await fixture.navigateByUrl('/bugs?search=disconnected');
    expect(fixture.routeNativeElement?.querySelector('fd-admin-load-error')).not.toBeNull();
    expect(fixture.routeNativeElement?.textContent).not.toContain('ADMIN_BUGS.NOT_CONFIGURED');
});

it('restores URL filters and cancels stale requests when the period changes', async () => {
    const stale = new Subject<AdminBugReportPage>();
    const totalItems = 51;
    getPage.mockReturnValueOnce(stale).mockReturnValueOnce(of({ items: [], totalItems, isConfigured: true, latestAudit: null }));
    const fixture = await RouterTestingHarness.create('/bugs');
    expect(stale.observed).toBe(true);
    const component = await fixture.navigateByUrl(
        '/bugs?period=custom&from=2026-01-01&to=2026-01-02&status=draft_ready&page=2',
        AdminBugsPageComponent,
    );
    expect(getPage).toHaveBeenLastCalledWith(
        expect.objectContaining({ page: 2, status: 'draft_ready', fromUtc: '2026-01-01T00:00:00Z', toUtc: '2026-01-03T00:00:00.000Z' }),
    );
    expect(stale.observed).toBe(false);
    stale.next({ items: [], totalItems: 999, isConfigured: true, latestAudit: null });
    expect(component['result']()?.totalItems).toBe(totalItems);
});

it('does not request reports for an invalid period', async () => {
    await RouterTestingHarness.create('/bugs?period=custom&from=2026-02-30&to=2026-03-01');
    expect(getPage).not.toHaveBeenCalled();
});

it('keeps source filters in URL requests and shows the latest audit even when no findings match', async () => {
    getPage.mockReturnValueOnce(of({ items: [], totalItems: 0, isConfigured: true, latestAudit: null }));
    const fixture = await RouterTestingHarness.create('/bugs?source=logs&search=missing');
    expect(getPage).toHaveBeenCalledWith(expect.objectContaining({ source: 'logs', search: 'missing' }));
    expect(fixture.routeNativeElement?.textContent).toContain('ADMIN_BUGS.AUDIT_NONE');
    expect(fixture.routeNativeElement?.textContent).toContain('ADMIN_BUGS.EMPTY_LOGS');
});

it('refreshes an unchanged URL filter through the rendered Apply action', async () => {
    const fixture = await RouterTestingHarness.create('/bugs');
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(false);
    const count = getPage.mock.calls.length;
    const apply = fixture.routeNativeElement?.querySelector<HTMLButtonElement>('form button[type="submit"]');
    if (apply === null || apply === undefined) {
        throw new Error('Missing bug filter Apply button');
    }
    apply.click();
    await fixture.fixture.whenStable();
    expect(getPage.mock.calls.length).toBe(count + 1);
});

it('defaults to user reports with a pressed source filter and no audit panel', async () => {
    const fixture = await RouterTestingHarness.create('/bugs');
    const host = fixture.routeNativeElement as HTMLElement;
    expect(getPage).toHaveBeenCalledWith(expect.objectContaining({ source: 'email' }));
    expect(host.querySelector('fd-admin-audit-status')).toBeNull();
    expect(host.querySelector('[role="tab"]')).toBeNull();
    expect(host.querySelector('[role="group"]')?.getAttribute('aria-label')).toBe('ADMIN_BUGS.SOURCE_FILTER');
    expect(host.querySelector('button[aria-pressed="true"]')?.textContent).toContain('ADMIN_BUGS.TAB_EMAIL');
});

it('switches source in the URL, resets source-specific filters and preserves the selected period', async () => {
    const fixture = await RouterTestingHarness.create('/bugs?source=email&period=7d&status=failed&search=mail&page=3');
    await fixture.fixture.whenStable();
    const logs = [...(fixture.routeNativeElement?.querySelectorAll<HTMLButtonElement>('fd-ui-tabs button') ?? [])].find(
        button => button.textContent.trim() === 'ADMIN_BUGS.TAB_LOGS',
    );
    if (logs === undefined) {
        throw new Error('Missing the log source control');
    }
    logs.click();
    await fixture.fixture.whenStable();
    fixture.detectChanges();
    const router = TestBed.inject(Router);
    expect(router.parseUrl(router.url).queryParams).toEqual({ source: 'logs', period: '7d', page: '1' });
    expect(getPage).toHaveBeenLastCalledWith(expect.objectContaining({ source: 'logs', page: 1, search: '', status: '' }));
    expect(fixture.routeNativeElement?.querySelector('fd-admin-audit-status')).not.toBeNull();
});

it('returns an old log deep link to the log tab even without a source query parameter', async () => {
    getPage.mockReturnValueOnce(
        of({
            items: [
                testFinding('legacy-log', {
                    subject: 'Log finding',
                    status: 'needs_information',
                    summary: 'Investigation details',
                }),
            ],
            totalItems: 1,
            isConfigured: true,
            latestAudit: null,
        }),
    );
    const fixture = await RouterTestingHarness.create('/bugs/legacy-log');
    expect(fixture.routeNativeElement?.querySelector('a[href^="/bugs"]')?.getAttribute('href')).toBe('/bugs?source=logs');
});

it('retries a failed journal with applied filters and displays the returned report', async () => {
    const finding = testFinding('retry-result', { source: 'email', subject: 'Recovered bug report' });
    getPage.mockReturnValueOnce(throwError(() => new Error('offline')));
    const pending = new Subject<AdminBugReportPage>();
    getPage.mockReturnValueOnce(pending);
    const fixture = await RouterTestingHarness.create('/bugs?search=applied');
    const host = fixture.routeNativeElement as HTMLElement;
    const retry = host.querySelector<HTMLButtonElement>('fd-admin-load-error button');
    const search = host.querySelector<HTMLInputElement>('input[aria-label="ADMIN_BUGS.SEARCH"]');
    if (retry === null || search === null) {
        throw new Error('Missing bug journal retry button or search field');
    }
    search.value = 'draft';
    search.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
    retry.click();
    fixture.detectChanges();
    expect(host.querySelector('[role="status"]')?.textContent).toContain('ADMIN_COMMON.LOADING');
    pending.next({ items: [finding], totalItems: 1, isConfigured: true, latestAudit: null });
    await fixture.fixture.whenStable();
    fixture.detectChanges();

    expect(host.querySelector('fd-admin-load-error')).toBeNull();
    expect(host.querySelector('article h2')?.textContent).toBe(finding.subject);
    expect(getPage).toHaveBeenCalledTimes(2);
    expect(getPage).toHaveBeenLastCalledWith(expect.objectContaining({ search: 'applied' }));
    expect(search.value).toBe('applied');
});

it('groups findings by audit run, keeps legacy findings separate, and only exposes usable review links', async () => {
    const repeatedObservationCount = 16;
    const summary = `${'A long observation. '.repeat(repeatedObservationCount)}\n\nDetailed evidence stays in the full report.`;
    const latestFindings = [
        testFinding('first', { subject: 'Navigation', summary, mergeRequestUrl: 'https://example.test/pull/17' }),
        testFinding('second', { subject: 'Search', status: 'future_status', mergeRequestUrl: 'https://account@example.test/pull/18' }),
        testFinding('third', { subject: 'Action', mergeRequestUrl: 'javascript:void(0)' }),
    ];
    const findings = [
        ...latestFindings,
        testFinding('previous', {
            subject: 'Previous finding',
            auditRunId: adminId<'audit-run'>('previous-run'),
            auditFromUtc: adminUtcInstant('2026-10-10T05:00:00Z'),
            auditToUtc: adminUtcInstant('2026-10-11T05:00:00Z'),
            mergeRequestUrl: 'not a link',
        }),
        testFinding('legacy-one', { subject: 'First legacy finding', auditRunId: null, auditFromUtc: null, auditToUtc: null }),
        testFinding('legacy-two', { subject: 'Second legacy finding', auditRunId: null, auditFromUtc: null, auditToUtc: null }),
    ];
    getPage.mockReturnValueOnce(of({ items: findings, totalItems: findings.length, isConfigured: true, latestAudit: null }));
    const fixture = await RouterTestingHarness.create('/bugs?source=logs');
    const host = fixture.routeNativeElement as HTMLElement;
    const groups = Array.from(host.querySelectorAll('.audit-group'));
    expect(groups.map(group => [...group.querySelectorAll('article h2')].map(title => title.textContent))).toEqual([
        latestFindings.map(finding => finding.subject),
        ['Previous finding'],
        ['First legacy finding'],
        ['Second legacy finding'],
    ]);
    expect(groups[0]?.querySelector('.audit-group__period')?.textContent).toContain('ADMIN_BUGS.AUDIT_PERIOD');
    expect(Array.from(host.querySelectorAll('article a'), link => link.getAttribute('href'))).toEqual(['https://example.test/pull/17']);
    expect(host.querySelector('article a')?.textContent).toContain('#17');
    expect(host.querySelector('.report-card__preview')?.textContent).toMatch(/…$/u);
    expect(host.querySelector('.report-card__preview')?.textContent).not.toContain('Detailed evidence');
    expect(host.querySelector('.report-card__full-summary')?.textContent).toContain('Detailed evidence');
    expect(groups[0]?.textContent).toContain('ADMIN_BUGS.STATUS_UNKNOWN');
});

it('loads a detail by path ID independently of preserved list filters or a conflicting query ID', async () => {
    getPage.mockReturnValueOnce(
        of({
            items: [testFinding('changed-bug', { source: 'email', contentExpired: true })],
            totalItems: 1,
            isConfigured: true,
            latestAudit: null,
        }),
    );
    const fixture = await RouterTestingHarness.create();
    const component = await fixture.navigateByUrl(
        '/bugs/changed-bug?id=other-bug&search=old%20text&status=failed&period=custom&from=2026-02-31&to=2026-10-01&page=2',
        AdminBugsPageComponent,
    );
    expect(getPage).toHaveBeenCalledExactlyOnceWith({ page: 1, limit: 25, id: 'changed-bug' });
    expect(component['result']()?.items[0]?.contentExpired).toBe(true);
});

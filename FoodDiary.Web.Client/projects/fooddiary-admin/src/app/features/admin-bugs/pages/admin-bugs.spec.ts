import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject, of, Subject, throwError } from 'rxjs';
import { beforeEach, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminBugsService } from '../api/admin-bugs.service';
import type { AdminBugReport, AdminBugReportPage } from '../models/admin-bug-report';
import { AdminBugsPageComponent } from './admin-bugs';

const params = new BehaviorSubject(convertToParamMap({}));
const query = new BehaviorSubject(convertToParamMap({}));
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
    params.next(convertToParamMap({}));
    query.next(convertToParamMap({}));
    getPage.mockReset().mockReturnValue(of({ items: [], totalItems: 0, isConfigured: true, latestAudit: null }));
    await TestBed.configureTestingModule({
        imports: [AdminBugsPageComponent],
        providers: [
            provideRouter([]),
            ...provideTranslateTesting(),
            {
                provide: ActivatedRoute,
                useValue: { paramMap: params, queryParamMap: query, snapshot: { queryParamMap: convertToParamMap({}) } },
            },
            { provide: AdminBugsService, useValue: { getPage } },
        ],
    }).compileComponents();
});

it('distinguishes an unconfigured connection from an empty result and failure', () => {
    getPage.mockReturnValueOnce(of({ items: [], totalItems: 0, isConfigured: false, latestAudit: null }));
    const fixture = TestBed.createComponent(AdminBugsPageComponent);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('ADMIN_BUGS.NOT_CONFIGURED');
    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('ADMIN_BUGS.EMPTY_EMAIL');
    getPage.mockReturnValueOnce(throwError(() => new Error('offline')));
    fixture.componentInstance['retry']();
    fixture.detectChanges();
    expect(fixture.componentInstance['failed']()).toBe(true);
    expect(fixture.componentInstance['result']()).toBeNull();
});

it('restores URL filters and cancels stale requests when the period changes', () => {
    const stale = new Subject<AdminBugReportPage>();
    getPage.mockReturnValueOnce(stale);
    const fixture = TestBed.createComponent(AdminBugsPageComponent);
    query.next(convertToParamMap({ period: 'custom', from: '2026-01-01', to: '2026-01-02', status: 'draft_ready', page: '2' }));
    expect(getPage).toHaveBeenLastCalledWith(
        expect.objectContaining({ page: 2, status: 'draft_ready', fromUtc: '2026-01-01T00:00:00Z', toUtc: '2026-01-03T00:00:00.000Z' }),
    );
    stale.next({ items: [], totalItems: 999, isConfigured: true, latestAudit: null });
    expect(fixture.componentInstance['result']()?.totalItems).toBe(0);
});

it('does not request reports for an invalid period', () => {
    query.next(convertToParamMap({ period: 'custom', from: '2026-02-30', to: '2026-03-01' }));
    TestBed.createComponent(AdminBugsPageComponent);
    expect(getPage).not.toHaveBeenCalled();
});

it('keeps source filters in URL requests and shows the latest audit even when no findings match', () => {
    query.next(convertToParamMap({ source: 'logs', search: 'missing' }));
    getPage.mockReturnValueOnce(of({ items: [], totalItems: 0, isConfigured: true, latestAudit: null }));
    const fixture = TestBed.createComponent(AdminBugsPageComponent);
    fixture.detectChanges();
    expect(getPage).toHaveBeenCalledWith(expect.objectContaining({ source: 'logs', search: 'missing' }));
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('ADMIN_BUGS.AUDIT_NONE');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('ADMIN_BUGS.EMPTY_LOGS');
});

it('refreshes an unchanged URL filter through the rendered Apply action', async () => {
    const fixture = TestBed.createComponent(AdminBugsPageComponent);
    fixture.detectChanges();
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(false);
    const count = getPage.mock.calls.length;
    const apply = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('form button[type="submit"]');
    if (apply === null) {
        throw new Error('Missing bug filter Apply button');
    }
    apply.click();
    await fixture.whenStable();
    expect(getPage.mock.calls.length).toBe(count + 1);
});

it('defaults to user reports with a pressed source filter and no audit panel', () => {
    const fixture = TestBed.createComponent(AdminBugsPageComponent);
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    expect(getPage).toHaveBeenCalledWith(expect.objectContaining({ source: 'email' }));
    expect(host.querySelector('fd-admin-audit-status')).toBeNull();
    expect(host.querySelector('[role="tab"]')).toBeNull();
    expect(host.querySelector('[role="group"]')?.getAttribute('aria-label')).toBe('ADMIN_BUGS.SOURCE_FILTER');
    expect(host.querySelector('button[aria-pressed="true"]')?.textContent).toContain('ADMIN_BUGS.TAB_EMAIL');
});

it('switches source in the URL, resets source-specific filters and preserves the selected period', () => {
    query.next(convertToParamMap({ source: 'email', period: '7d', status: 'failed', search: 'mail', page: '3' }));
    const fixture = TestBed.createComponent(AdminBugsPageComponent);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture.componentInstance['selectSource']('logs');
    expect(navigate).toHaveBeenCalledWith(['/bugs'], {
        queryParams: { source: 'logs', page: 1, search: null, status: null },
        queryParamsHandling: 'merge',
    });
    query.next(convertToParamMap({ source: 'logs', period: '7d', page: '1' }));
    fixture.detectChanges();
    expect(getPage).toHaveBeenLastCalledWith(expect.objectContaining({ source: 'logs', page: 1 }));
    expect((fixture.nativeElement as HTMLElement).querySelector('fd-admin-audit-status')).not.toBeNull();
});

it('returns an old log deep link to the log tab even without a source query parameter', () => {
    params.next(convertToParamMap({ id: 'legacy-log' }));
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
    const fixture = TestBed.createComponent(AdminBugsPageComponent);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('a[href^="/bugs"]')?.getAttribute('href')).toBe('/bugs?source=logs');
});

it('retries a failed journal from its visible error and replaces it with the returned report', () => {
    const finding = testFinding('retry-result', { source: 'email', subject: 'Recovered bug report' });
    getPage.mockReturnValueOnce(throwError(() => new Error('offline')));
    const pending = new Subject<AdminBugReportPage>();
    getPage.mockReturnValueOnce(pending);
    const fixture = TestBed.createComponent(AdminBugsPageComponent);
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    const retry = host.querySelector<HTMLButtonElement>('fd-admin-load-error button');
    if (retry === null) {
        throw new Error('Missing bug journal retry button');
    }
    retry.click();
    fixture.detectChanges();
    expect(host.querySelector('[role="status"]')?.textContent).toContain('ADMIN_COMMON.LOADING');
    pending.next({ items: [finding], totalItems: 1, isConfigured: true, latestAudit: null });
    fixture.detectChanges();

    expect(host.querySelector('fd-admin-load-error')).toBeNull();
    expect(host.querySelector('article h2')?.textContent).toBe(finding.subject);
    expect(getPage).toHaveBeenCalledTimes(2);
});

it('groups findings by audit run and only exposes usable review links in the rendered cards', () => {
    query.next(convertToParamMap({ source: 'logs' }));
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
    ];
    getPage.mockReturnValueOnce(of({ items: findings, totalItems: findings.length, isConfigured: true, latestAudit: null }));
    const fixture = TestBed.createComponent(AdminBugsPageComponent);
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    const groups = Array.from(host.querySelectorAll('.audit-group'));
    expect(groups.map(group => group.querySelectorAll('article').length)).toEqual([latestFindings.length, 1]);
    expect(groups[1]?.querySelector('article h2')?.textContent).toBe('Previous finding');
    expect(groups[0]?.querySelector('.audit-group__period')?.textContent).toContain('ADMIN_BUGS.AUDIT_PERIOD');
    expect(Array.from(host.querySelectorAll('article a'), link => link.getAttribute('href'))).toEqual(['https://example.test/pull/17']);
    expect(host.querySelector('article a')?.textContent).toContain('#17');
    expect(host.querySelector('.report-card__preview')?.textContent).toMatch(/…$/u);
    expect(host.querySelector('.report-card__preview')?.textContent).not.toContain('Detailed evidence');
    expect(host.querySelector('.report-card__full-summary')?.textContent).toContain('Detailed evidence');
    expect(groups[0]?.textContent).toContain('ADMIN_BUGS.STATUS_UNKNOWN');
});

it('loads a detail by ID independently of preserved list filters, even if its status or content changed', () => {
    const getDetailPage = vi.fn().mockReturnValue(
        of({
            items: [{ id: 'changed-bug', source: 'email', status: 'draft_ready', contentExpired: true }],
            totalItems: 1,
            isConfigured: true,
        }),
    );
    TestBed.configureTestingModule({
        imports: [AdminBugsPageComponent],
        providers: [
            provideRouter([]),
            provideTranslateTesting(),
            {
                provide: ActivatedRoute,
                useValue: {
                    paramMap: of(convertToParamMap({ id: 'changed-bug' })),
                    queryParamMap: of(
                        convertToParamMap({
                            search: 'old text',
                            status: 'failed',
                            period: 'custom',
                            from: '2026-02-31',
                            to: '2026-10-01',
                            page: '2',
                        }),
                    ),
                },
            },
            { provide: AdminBugsService, useValue: { getPage: getDetailPage } },
        ],
    });
    const fixture = TestBed.createComponent(AdminBugsPageComponent);
    expect(getDetailPage).toHaveBeenCalledWith({ page: 1, limit: 25, id: 'changed-bug' });
    expect(fixture.componentInstance['result']()?.items[0]?.contentExpired).toBe(true);
});

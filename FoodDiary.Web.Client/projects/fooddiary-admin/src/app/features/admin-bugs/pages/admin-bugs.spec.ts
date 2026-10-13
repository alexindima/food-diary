import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject, of, Subject, throwError } from 'rxjs';
import { beforeEach, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminBugsService } from '../api/admin-bugs.service';
import type { AdminBugReportPage } from '../models/admin-bug-report';
import { AdminBugsPageComponent } from './admin-bugs';

const params = new BehaviorSubject(convertToParamMap({}));
const query = new BehaviorSubject(convertToParamMap({}));
const getPage = vi.fn();

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
    getPage.mockReturnValueOnce(of({ items: [], totalItems: 0, isConfigured: false }));
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

it('does not request invalid periods and rejects executable external links', () => {
    query.next(convertToParamMap({ period: 'custom', from: '2026-02-30', to: '2026-03-01' }));
    const fixture = TestBed.createComponent(AdminBugsPageComponent);
    expect(getPage).not.toHaveBeenCalled();
    expect(fixture.componentInstance['safePullRequestUrl']('javascript:alert(1)')).toBeNull();
    expect(fixture.componentInstance['safePullRequestUrl']('https://example.test/pull/1')).toBe('https://example.test/pull/1');
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

it('refreshes the audit result when applying an unchanged URL filter', async () => {
    const fixture = TestBed.createComponent(AdminBugsPageComponent);
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(false);
    const count = getPage.mock.calls.length;
    await fixture.componentInstance['applyAsync']();
    expect(getPage.mock.calls.length).toBe(count + 1);
});

it('defaults to user reports and keeps the audit panel out of that tab', () => {
    const fixture = TestBed.createComponent(AdminBugsPageComponent);
    fixture.detectChanges();
    expect(getPage).toHaveBeenCalledWith(expect.objectContaining({ source: 'email' }));
    expect((fixture.nativeElement as HTMLElement).querySelector('fd-admin-audit-status')).toBeNull();
    expect((fixture.nativeElement as HTMLElement).querySelector('[role="tab"][aria-selected="true"]')?.textContent).toContain(
        'ADMIN_BUGS.TAB_EMAIL',
    );
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
                {
                    id: 'legacy-log',
                    source: 'logs',
                    sourceMessageId: null,
                    subject: 'Log finding',
                    receivedAtUtc: '2026-10-12T00:00:00Z',
                    title: null,
                    shortSummary: null,
                    status: 'needs_information',
                    attempt: 1,
                    summary: 'Investigation details',
                    mergeRequestUrl: null,
                    contentExpired: false,
                    auditRunId: 'audit',
                    auditFromUtc: '2026-10-11T00:00:00Z',
                    auditToUtc: '2026-10-12T00:00:00Z',
                },
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

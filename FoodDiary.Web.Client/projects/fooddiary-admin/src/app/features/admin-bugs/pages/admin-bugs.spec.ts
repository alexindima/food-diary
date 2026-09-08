import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminBugsService } from '../api/admin-bugs.service';
import type { AdminBugReportPage } from '../models/admin-bug-report';
import { AdminBugsPageComponent } from './admin-bugs';

describe('AdminBugsPageComponent', () => {
    const params = new BehaviorSubject(convertToParamMap({}));
    const query = new BehaviorSubject(convertToParamMap({}));
    const getPage = vi.fn();

    beforeEach(async () => {
        params.next(convertToParamMap({}));
        query.next(convertToParamMap({}));
        getPage.mockReset().mockReturnValue(of({ items: [], totalItems: 0, isConfigured: true }));
        await TestBed.configureTestingModule({
            imports: [AdminBugsPageComponent],
            providers: [
                provideRouter([]),
                ...provideTranslateTesting(),
                { provide: ActivatedRoute, useValue: { paramMap: params, queryParamMap: query } },
                { provide: AdminBugsService, useValue: { getPage } },
            ],
        }).compileComponents();
    });

    it('distinguishes an unconfigured connection from an empty result and failure', () => {
        getPage.mockReturnValueOnce(of({ items: [], totalItems: 0, isConfigured: false }));
        const fixture = TestBed.createComponent(AdminBugsPageComponent);
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('ADMIN_BUGS.NOT_CONFIGURED');
        expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('ADMIN_BUGS.EMPTY');
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
        stale.next({ items: [], totalItems: 999, isConfigured: true });
        expect(fixture.componentInstance['result']()?.totalItems).toBe(0);
    });

    it('does not request invalid periods and rejects executable external links', () => {
        query.next(convertToParamMap({ period: 'custom', from: '2026-02-30', to: '2026-03-01' }));
        const fixture = TestBed.createComponent(AdminBugsPageComponent);
        expect(getPage).not.toHaveBeenCalled();
        expect(fixture.componentInstance['safePullRequestUrl']('javascript:alert(1)')).toBeNull();
        expect(fixture.componentInstance['safePullRequestUrl']('https://example.test/pull/1')).toBe('https://example.test/pull/1');
    });
});

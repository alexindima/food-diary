import { OverlayContainer } from '@angular/cdk/overlay';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom, of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { stubScrollIntoView } from '../../../../../../src/testing/dom-testing';
import { provideTranslateTesting } from '../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../shared/models/semantics/admin-meaning';
import { AdminAuthService } from '../admin-auth/contracts/admin-session';
import { adminBugsRoutes } from './admin-bugs.routes';
import { AdminBugsService } from './api/admin-bugs.service';
import type { AdminBugReport } from './models/admin-bug-report';

const REPORT_COUNT = 51;
const FINAL_PAGE = 3;
const BEFORE_FINAL_PAGE = 2;
const latest: AdminBugReport = {
    id: adminId<'bug-report'>('finding-1'),
    title: 'Report available',
    subject: 'QA report',
    source: 'logs',
    sourceMessageId: null,
    auditRunId: adminId<'audit-run'>('audit-1'),
    auditFromUtc: adminUtcInstant('2026-10-11T00:00:00Z'),
    auditToUtc: adminUtcInstant('2026-10-12T00:00:00Z'),
    receivedAtUtc: adminUtcInstant('2026-10-12T00:00:00Z'),
    attempt: 1,
    status: 'failed',
    shortSummary: null,
    summary: null,
    mergeRequestUrl: null,
    contentExpired: false,
};

function configureRoute(): ReturnType<typeof vi.fn<AdminBugsService['getPage']>> {
    const getPage = vi
        .fn<AdminBugsService['getPage']>()
        .mockImplementation(({ page }) =>
            of({ items: Number(page) <= FINAL_PAGE ? [latest] : [], totalItems: REPORT_COUNT, isConfigured: true, latestAudit: null }),
        );
    TestBed.configureTestingModule({
        providers: [
            ...provideTranslateTesting(),
            provideRouter(
                [{ path: 'bugs', children: adminBugsRoutes }],
                withComponentInputBinding({ unmatchedInputBehavior: 'undefinedIfStale' }),
            ),
            { provide: AdminBugsService, useValue: { getPage } },
            {
                provide: AdminAuthService,
                useValue: {
                    applySsoFromQueryAsync: async (): Promise<void> => {},
                    isAuthenticated: (): boolean => true,
                    isAdmin: (): boolean => true,
                    sessionUnavailable: signal(false),
                },
            },
        ],
    });
    return getPage;
}

function button(element: HTMLElement | null, label: string): HTMLButtonElement {
    const control = [...(element?.querySelectorAll('button') ?? [])].find(item => item.textContent.trim() === label);
    if (control === undefined) {
        throw new Error(`Expected the ${label} button`);
    }
    return control;
}

async function updateViewAsync(harness: RouterTestingHarness): Promise<void> {
    await harness.fixture.whenStable();
    harness.detectChanges();
}

describe('Admin bug journal route', () => {
    it('treats a query ID on the list URL as an unrelated parameter', async () => {
        const getPage = configureRoute();
        const harness = await RouterTestingHarness.create('/bugs?id=finding-1&source=logs');
        await updateViewAsync(harness);

        expect(getPage).toHaveBeenCalledExactlyOnceWith({ page: 1, limit: 25, source: 'logs', search: '', status: '' });
        expect(harness.routeNativeElement?.textContent).not.toContain('ADMIN_BUGS.BACK');
    });

    it('restores a stale page, preserves filters through pagination, and lets the operator apply and reset them', async () => {
        const getPage = configureRoute();
        const harness = await RouterTestingHarness.create(
            '/bugs?source=logs&page=9&status=failed&search=reported&period=custom&from=2026-10-11&to=2026-10-12',
        );
        await updateViewAsync(harness);
        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParamMap.get('page')).toBe('3');
        expect(getPage).toHaveBeenLastCalledWith(
            expect.objectContaining({
                source: 'logs',
                page: FINAL_PAGE,
                search: 'reported',
                status: 'failed',
                fromUtc: '2026-10-11T00:00:00Z',
            }),
        );

        button(harness.routeNativeElement, '2').click();
        await updateViewAsync(harness);
        expect(getPage).toHaveBeenLastCalledWith(expect.objectContaining({ source: 'logs', page: BEFORE_FINAL_PAGE, status: 'failed' }));

        const search = harness.routeNativeElement?.querySelector<HTMLInputElement>('input[aria-label="ADMIN_BUGS.SEARCH"]');
        if (search === null || search === undefined) {
            throw new Error('Expected the journal search field');
        }
        search.value = 'follow-up';
        search.dispatchEvent(new Event('input', { bubbles: true }));
        button(harness.routeNativeElement, 'ADMIN_CATALOG.APPLY').click();
        await updateViewAsync(harness);
        expect(getPage).toHaveBeenLastCalledWith(
            expect.objectContaining({ source: 'logs', page: 1, search: 'follow-up', status: 'failed' }),
        );

        button(harness.routeNativeElement, 'ADMIN_BUGS.RESET_FILTERS').click();
        await updateViewAsync(harness);
        const reset = router.parseUrl(router.url).queryParamMap;
        expect(reset.keys.sort()).toEqual(['page', 'source']);
        expect(getPage).toHaveBeenLastCalledWith({ source: 'logs', page: 1, search: '', status: '', limit: 25 });
    });

    it('shows the status identifier when a translation is a structured value', async () => {
        stubScrollIntoView();
        configureRoute();
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', { ADMIN_BUGS: { STATUS_PENDING: { summary: 'Pending' } } }, true);
        await firstValueFrom(translate.use('en'));
        const harness = await RouterTestingHarness.create('/bugs');
        const trigger = harness.routeNativeElement?.querySelector<HTMLButtonElement>('form > fd-ui-select button[aria-haspopup="listbox"]');
        trigger?.click();
        await updateViewAsync(harness);

        const overlay = TestBed.inject(OverlayContainer).getContainerElement();
        expect([...overlay.querySelectorAll('[role="option"]')].map(item => item.textContent.trim())).toContain(
            'ADMIN_BUGS.STATUS_PENDING',
        );
    });
});

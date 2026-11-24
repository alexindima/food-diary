import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { NavigationEnd, provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { filter, firstValueFrom } from 'rxjs';
import { expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../src/testing/translate-testing.module';
import { environment } from '../../../environments/environment';
import type { RecipeModerationPage } from '../../shared/api/sdk/generated/model/recipe-moderation-page';
import { AdminAuthService } from '../admin-auth/contracts/admin-session';
import { adminModerationRoutes } from './admin-moderation.routes';

const REPORTS = '/moderation';
const CHECKS = '/moderation/recipes';
const RULES = '/moderation/rules';
const API = `${environment.apiUrls.auth.replace(/\/auth$/u, '')}/admin/moderation`;
const EMPTY_CHECKS: RecipeModerationPage = {
    items: [],
    totalItems: 0,
    page: 1,
    limit: 20,
    pendingCount: 0,
    needsReviewCount: 0,
    delayedCount: 0,
};

it('navigates among the mounted moderation pages and identifies the current destination to assistive technology', async () => {
    TestBed.configureTestingModule({
        providers: [
            provideRouter(
                [{ path: 'moderation', children: adminModerationRoutes }],
                withComponentInputBinding({ unmatchedInputBehavior: 'undefinedIfStale' }),
            ),
            provideHttpClient(),
            provideHttpClientTesting(),
            ...provideTranslateTesting(),
            {
                provide: AdminAuthService,
                useValue: {
                    applySsoFromQueryAsync: vi.fn<AdminAuthService['applySsoFromQueryAsync']>().mockResolvedValue(undefined),
                    sessionUnavailable: (): boolean => false,
                    isAuthenticated: (): boolean => true,
                    isAdmin: (): boolean => true,
                },
            },
        ],
    });
    const harness = await RouterTestingHarness.create(RULES);
    const http = TestBed.inject(HttpTestingController);
    const pendingRules = http.expectOne({ method: 'GET', url: `${API}/rules` });
    expect(harness.routeNativeElement?.matches('fd-admin-moderation-rules')).toBe(true);
    expectNavigation(harness, RULES);

    await followLinkAsync(harness, CHECKS);
    expect(pendingRules.cancelled).toBe(true);
    http.expectOne(request => request.method === 'GET' && request.url === `${API}/recipes`).flush(EMPTY_CHECKS);
    expect(harness.routeNativeElement?.matches('fd-admin-recipe-moderation')).toBe(true);
    expectNavigation(harness, CHECKS);

    await followLinkAsync(harness, REPORTS);
    http.expectOne(request => request.method === 'GET' && request.url === API).flush({
        data: [],
        page: 1,
        limit: 20,
        totalItems: 0,
        totalPages: 1,
    });
    expect(harness.routeNativeElement?.matches('fd-admin-moderation')).toBe(true);
    expectNavigation(harness, REPORTS);
    http.verify();
});

function expectNavigation(harness: RouterTestingHarness, active: string): void {
    const navigation = harness.routeNativeElement?.querySelector('fd-admin-moderation-navigation [aria-label="ADMIN_NAV.MODERATION"]');
    expect(navigation).toBeTruthy();
    expect(Array.from(navigation?.querySelectorAll('a') ?? [], link => link.getAttribute('href'))).toEqual([REPORTS, CHECKS, RULES]);
    expect(Array.from(navigation?.querySelectorAll('a[aria-current="page"]') ?? [], link => link.getAttribute('href'))).toEqual([active]);
}

async function followLinkAsync(harness: RouterTestingHarness, destination: string): Promise<void> {
    const link = harness.routeNativeElement?.querySelector<HTMLAnchorElement>(`fd-admin-moderation-navigation a[href="${destination}"]`);
    if (link === null || link === undefined) {
        throw new Error(`Expected a rendered moderation link for ${destination}.`);
    }
    const router = TestBed.inject(Router);
    const navigation = firstValueFrom(
        router.events.pipe(filter(event => event instanceof NavigationEnd && event.urlAfterRedirects === destination)),
    );
    link.click();
    await navigation;
    harness.detectChanges();
}

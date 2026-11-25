import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../src/testing/translate-testing.module';
import { routes } from './app.routes';
import { AdminAuthService } from './features/admin-auth/contracts/admin-session';
import { AdminGoogleSignInService } from './features/admin-auth/lib/admin-google-sign-in.service';

const protectedPages = [
    ['/', 'fd-admin-dashboard'],
    ['/acquisition', 'fd-admin-acquisition'],
    ['/analytics/retention', 'fd-admin-retention'],
    ['/users', 'fd-admin-users'],
    ['/users/sample-user', 'fd-admin-user-page'],
    ['/users/login-activity', 'fd-admin-login-activity-page'],
    ['/users/impersonation-sessions', 'fd-admin-impersonation-sessions-page'],
    ['/ai-usage', 'fd-admin-ai-usage'],
    ['/ai-prompts', 'fd-admin-ai-prompts'],
    ['/audit', 'fd-admin-audit'],
    ['/bugs', 'fd-admin-bugs'],
    ['/billing', 'fd-admin-billing'],
    ['/email-templates', 'fd-admin-email-templates'],
    ['/mail-inbox', 'fd-admin-mail-inbox'],
    ['/feedback', 'fd-admin-mail-inbox'],
    ['/outgoing-emails', 'fd-admin-outgoing-emails'],
    ['/mail-dmarc', 'fd-admin-dmarc'],
    ['/catalog', 'fd-admin-catalog'],
    ['/meal-plans', 'fd-admin-meal-plans'],
    ['/daily-advices', 'fd-admin-daily-advices'],
    ['/lessons', 'fd-admin-lessons'],
    ['/achievements', 'fd-admin-achievements'],
    ['/moderation', 'fd-admin-moderation'],
] as const;

function configureAdminRoutes(isAdmin: boolean): void {
    TestBed.configureTestingModule({
        providers: [
            provideRouter(routes),
            provideHttpClient(),
            provideHttpClientTesting(),
            ...provideTranslateTesting(),
            {
                provide: AdminAuthService,
                useValue: {
                    applySsoFromQueryAsync: vi.fn<AdminAuthService['applySsoFromQueryAsync']>().mockResolvedValue(undefined),
                    isAuthenticated: (): boolean => true,
                    isAdmin: (): boolean => isAdmin,
                    getUserId: (): null => null,
                    tryUpgradeToAdminAsync: vi.fn<AdminAuthService['tryUpgradeToAdminAsync']>().mockResolvedValue(false),
                    sessionUnavailable: signal(false),
                    ssoRateLimited: signal(false),
                },
            },
            {
                provide: AdminGoogleSignInService,
                useValue: { renderAsync: vi.fn<AdminGoogleSignInService['renderAsync']>().mockResolvedValue(undefined) },
            },
        ],
    });
}

describe('Admin application route boundaries', () => {
    it.each(protectedPages)('loads %s in the actual lazy route for an administrator', async (url, selector) => {
        configureAdminRoutes(true);
        const harness = await RouterTestingHarness.create(url);
        const http = TestBed.inject(HttpTestingController);

        expect(harness.routeNativeElement?.matches(selector)).toBe(true);
        expect(TestBed.inject(Router).url).toBe(url);

        for (const request of http.match(() => true)) {
            if (!request.cancelled) {
                request.flush({}, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service unavailable' });
            }
        }
        await harness.fixture.whenStable();
        http.verify();
    });

    it.each(protectedPages)('blocks %s before starting feature requests when the role check fails', async (url, selector) => {
        configureAdminRoutes(false);
        const harness = await RouterTestingHarness.create(url);
        const router = TestBed.inject(Router);
        const recovery = router.parseUrl(router.url);

        expect(harness.routeNativeElement?.matches('fd-admin-unauthorized')).toBe(true);
        expect(harness.routeNativeElement?.matches(selector)).toBe(false);
        expect(recovery.queryParamMap.get('reason')).toBe('forbidden');
        expect(recovery.queryParamMap.get('returnUrl')).toBe(url);
        TestBed.inject(HttpTestingController).verify();
    });

    it('renders the public confirmation form without sending an administrative request', async () => {
        configureAdminRoutes(false);
        const harness = await RouterTestingHarness.create('/sign-in?confirm=1&returnUrl=%2Fusers');

        expect(harness.routeNativeElement?.matches('fd-admin-sign-in')).toBe(true);
        expect(harness.routeNativeElement?.querySelector('form')).toBeTruthy();
        TestBed.inject(HttpTestingController).verify();
    });
});

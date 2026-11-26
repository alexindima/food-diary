import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
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
            provideRouter(routes, withComponentInputBinding({ unmatchedInputBehavior: 'undefinedIfStale' })),
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

    it.each([
        { path: '/feedback', queryMode: 'false', recipient: 'feedback@fooddiary.club' },
        { path: '/mail-inbox', queryMode: 'true', recipient: 'other@example.test' },
    ])('uses the configured mailbox mode for $path even with a conflicting query value', async ({ path, queryMode, recipient }) => {
        configureAdminRoutes(true);
        const harness = await RouterTestingHarness.create(`${path}?feedbackMailbox=${queryMode}&recipient=other@example.test`);
        const http = TestBed.inject(HttpTestingController);
        const request = http.expectOne(candidate => candidate.url.endsWith('/admin/mail-inbox/messages/page'));
        expect(request.request.params.get('recipient')).toBe(recipient);
        request.flush({
            totalItems: 1,
            items: [
                {
                    id: 'mail-message',
                    subject: 'Message',
                    toRecipients: [recipient],
                    category: 'general',
                    status: 'received',
                    isTrustedRelay: false,
                    receivedAtUtc: '2026-11-01T10:00:00Z',
                },
            ],
        });

        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(harness.routeNativeElement?.querySelector('tbody a')?.getAttribute('href')).toContain(`${path}?`);
        http.verify();
    });

    it('renders the public confirmation form without sending an administrative request', async () => {
        configureAdminRoutes(false);
        const harness = await RouterTestingHarness.create('/sign-in?confirm=1&confirm=0&returnUrl=%2Fusers');

        expect(harness.routeNativeElement?.matches('fd-admin-sign-in')).toBe(true);
        expect(harness.routeNativeElement?.querySelector('form')).toBeTruthy();
        TestBed.inject(HttpTestingController).verify();
    });
});

const MILLISECONDS_PER_DAY = 86_400_000;
const SHORT_DMARC_PERIOD = 7;
const LONG_DMARC_PERIOD = 30;

it('cancels the old DMARC period on navigation and retries the first repeated period', async () => {
    configureAdminRoutes(true);
    const harness = await RouterTestingHarness.create('/mail-dmarc?period=7');
    const host = harness.routeNativeElement;
    if (host === null) {
        throw new Error('Expected the DMARC route to render');
    }
    const http = TestBed.inject(HttpTestingController);
    const oldRequest = expectDmarcListRequest(http, SHORT_DMARC_PERIOD);

    const destination = '/mail-dmarc?period=30&period=7';
    await harness.navigateByUrl(destination);
    expect(harness.routeNativeElement).toBe(host);
    expect(oldRequest.cancelled).toBe(true);
    expect(host.querySelector('[role="radio"][aria-checked="true"]')?.textContent).toContain('ADMIN_DMARC.PERIOD_30');
    expect(host.querySelector('[role="status"]')?.textContent).toContain('ADMIN_COMMON.LOADING');
    expectDmarcListRequest(http, LONG_DMARC_PERIOD).flush(null, {
        status: HttpStatusCode.ServiceUnavailable,
        statusText: 'Service unavailable',
    });
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(host.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_COMMON.LOAD_ERROR');
    const retry = host.querySelector<HTMLButtonElement>('fd-admin-load-error button');
    if (retry === null) {
        throw new Error('Expected the DMARC retry button');
    }
    retry.click();
    harness.detectChanges();
    expectDmarcListRequest(http, LONG_DMARC_PERIOD).flush({ items: [], totalItems: 0 });
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(TestBed.inject(Router).url).toBe(destination);
    expect(host.querySelector('fd-admin-load-error')).toBeNull();
    expect(host.querySelector('[role="status"]')?.textContent).toContain('ADMIN_DMARC.NO_VERIFIED_REPORTS');
    http.verify();
});

function expectDmarcListRequest(http: HttpTestingController, days: number): TestRequest {
    const request = http.expectOne(candidate => candidate.method === 'GET' && candidate.url.endsWith('/admin/mail-inbox/messages/page'));
    const params = request.request.params;
    const from = Date.parse(params.get('fromUtc') ?? '');
    const to = Date.parse(params.get('toUtc') ?? '');
    expect(params.get('recipient')).toBe('dmarc@fooddiary.club');
    expect(Math.floor((to - from) / MILLISECONDS_PER_DAY)).toBe(days);
    return request;
}

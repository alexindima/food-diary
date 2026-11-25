import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { Component, signal, type WritableSignal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { environment } from '../../../../environments/environment';
import { adminAuthGuard } from '../../../guards/admin-auth.guard';
import { AdminAuthService } from '../../admin-auth/contracts/admin-session';
import { UnauthorizedComponent } from './unauthorized';

// eslint-disable-next-line max-lines-per-function -- Keep component setup with its recovery and SSO outcomes.
describe('UnauthorizedComponent', () => {
    const defaultInputs = { reason: 'unauthenticated', returnUrl: '/users?code=sso-code&page=2' };
    let component: UnauthorizedComponent;
    let fixture: ComponentFixture<UnauthorizedComponent>;
    let authService: {
        tryApplySsoFromReturnUrlAsync: ReturnType<typeof vi.fn>;
        ssoRateLimited: WritableSignal<boolean>;
        sessionUnavailable: WritableSignal<boolean>;
        restoreSessionAsync: ReturnType<typeof vi.fn>;
        isAdmin: ReturnType<typeof vi.fn>;
        isAuthenticated: ReturnType<typeof vi.fn>;
    };
    let router: { navigateByUrl: ReturnType<typeof vi.fn> };
    beforeEach(async () => {
        authService = {
            tryApplySsoFromReturnUrlAsync: vi.fn(),
            ssoRateLimited: signal(false),
            sessionUnavailable: signal(false),
            restoreSessionAsync: vi.fn().mockResolvedValue(undefined),
            isAdmin: vi.fn().mockReturnValue(true),
            isAuthenticated: vi.fn().mockReturnValue(true),
        };
        router = {
            navigateByUrl: vi.fn(),
        };
        router.navigateByUrl.mockResolvedValue(true);

        await TestBed.configureTestingModule({
            imports: [UnauthorizedComponent],
            providers: [
                provideTranslateTesting(),
                { provide: AdminAuthService, useValue: authService },
                { provide: Router, useValue: router },
                { provide: ActivatedRoute, useValue: { snapshot: { fragment: null } } },
            ],
        }).compileComponents();
    });

    function createComponent({ reason, returnUrl }: { reason: string; returnUrl?: string } = defaultInputs): void {
        fixture = TestBed.createComponent(UnauthorizedComponent);
        fixture.componentRef.setInput('reason', reason);
        fixture.componentRef.setInput('returnUrl', returnUrl);
        component = fixture.componentInstance;
    }

    it('should create', () => {
        createComponent();
        fixture.detectChanges();

        expect(component).toBeTruthy();
        expect(component.reason()).toBe('unauthenticated');
        expect(component.returnUrl()).toBe('/users?code=sso-code&page=2');
    });

    it('offers restoration retry and returns to the requested route after recovery', async () => {
        authService.sessionUnavailable.set(true);
        createComponent({ reason: 'unavailable', returnUrl: '/users?page=2' });
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelector('h1')?.textContent, 'An outage must offer session recovery instead of permission denial').toContain(
            'ADMIN_AUTH.SESSION_UNAVAILABLE_TITLE',
        );
        let complete = (): void => {};
        authService.restoreSessionAsync.mockReturnValue(
            new Promise<void>(resolve => {
                complete = (): void => {
                    resolve();
                };
            }),
        );
        element.querySelector('button')?.click();
        fixture.componentRef.setInput('returnUrl', '/audit');
        fixture.detectChanges();
        authService.sessionUnavailable.set(false);
        complete();
        await fixture.whenStable();
        expect(authService.restoreSessionAsync).toHaveBeenCalledWith(true);
        expect(router.navigateByUrl).toHaveBeenCalledWith('/users?page=2', { replaceUrl: true });
    });

    it('should try to recover from sso return url on init', async () => {
        authService.tryApplySsoFromReturnUrlAsync.mockResolvedValue('/users?page=2');

        createComponent();
        fixture.detectChanges();
        await fixture.whenStable();

        expect(authService.tryApplySsoFromReturnUrlAsync).toHaveBeenCalledWith('/users?code=sso-code&page=2');
        expect(router.navigateByUrl).toHaveBeenCalledWith('/users?page=2', { replaceUrl: true });
    });

    it.each([
        { returnUrl: undefined, calls: [['/']] },
        { returnUrl: '', calls: [] },
    ])('keeps missing and explicit empty return URL behavior for $returnUrl', ({ returnUrl, calls }) => {
        createComponent({ reason: 'unauthenticated', returnUrl });
        fixture.detectChanges();

        expect(authService.tryApplySsoFromReturnUrlAsync.mock.calls).toEqual(calls);
    });

    it('should not try to recover when reason is forbidden', () => {
        createComponent({
            reason: 'forbidden',
            returnUrl: '/users?page=2',
        });
        fixture.detectChanges();

        expect(authService.tryApplySsoFromReturnUrlAsync).not.toHaveBeenCalled();
        expect(router.navigateByUrl).not.toHaveBeenCalled();
    });
});

function createRateFeedback(reason: string): HTMLElement {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
        imports: [UnauthorizedComponent],
        providers: [
            provideTranslateTesting(),
            {
                provide: AdminAuthService,
                useValue: { ssoRateLimited: signal(true), sessionUnavailable: signal(false), tryApplySsoFromReturnUrlAsync: vi.fn() },
            },
            { provide: Router, useValue: { navigateByUrl: vi.fn() } },
            { provide: ActivatedRoute, useValue: { snapshot: { fragment: null } } },
        ],
    });
    const fixture = TestBed.createComponent(UnauthorizedComponent);
    fixture.componentRef.setInput('reason', reason);
    fixture.componentRef.setInput('returnUrl', '/users');
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
}

describe('UnauthorizedComponent throttling feedback', () => {
    it('announces throttling as a temporary sign-in problem', () => {
        const element = createRateFeedback('unauthenticated');
        expect(element.querySelector('h1')?.textContent).toContain('ADMIN_AUTH.RATE_LIMITED_TITLE');
        expect(element.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_AUTH.RATE_LIMITED_MESSAGE');
        expect(element.textContent).not.toContain('permission');
    });

    it('keeps a forbidden response as permission denial despite a previous throttling flag', () => {
        const element = createRateFeedback('forbidden');
        expect(element.textContent).toContain('You do not have permission');
        expect(element.querySelector('[role="alert"]')).toBeNull();
    });
});

@Component({ selector: 'fd-test-protected-users', template: '<h1>Protected users</h1>' })
class ProtectedUsersFixture {}

@Component({ selector: 'fd-test-confirm-sign-in', template: '<h1>Confirm sign-in</h1>' })
class ConfirmSignInFixture {}

const RETURN_URL = '/users?page=2';
const EXPECTED_ACCOUNT = 'requested-admin';
const ACCOUNT_FRAGMENT = `account=${EXPECTED_ACCOUNT}`;
const RESTORE_ENDPOINT = `${environment.apiUrls.auth}/session`;

function configureRecoveryRoutes(): void {
    TestBed.configureTestingModule({
        providers: [
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            provideRouter(
                [
                    { path: 'users', component: ProtectedUsersFixture, canActivate: [adminAuthGuard] },
                    { path: 'unauthorized', component: UnauthorizedComponent },
                    { path: 'sign-in', component: ConfirmSignInFixture },
                ],
                withComponentInputBinding({ unmatchedInputBehavior: 'undefinedIfStale' }),
            ),
        ],
    });
}

describe('Native Admin recovery with the protected account guard', () => {
    beforeEach(configureRecoveryRoutes);

    it.each([
        { account: EXPECTED_ACCOUNT, title: 'Protected users', fragment: null, returnUrl: null },
        { account: 'other-admin', title: 'Confirm sign-in', fragment: ACCOUNT_FRAGMENT, returnUrl: RETURN_URL },
    ])('continues only for the intended account after retry succeeds as $account', async ({ account, title, fragment, returnUrl }) => {
        const harness = await openUnavailableRouteAsync();
        const http = TestBed.inject(HttpTestingController);
        const router = TestBed.inject(Router);
        const retry = recoveryButton(harness);
        retry.click();
        retry.click();
        harness.detectChanges();
        expect(retry.disabled).toBe(true);
        const response = await nextRestoreAsync(http);
        response.flush({ sessionId: 'restored-session', roles: ['Admin'], user: { id: account } });
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(harness.routeNativeElement?.querySelector('h1')?.textContent.trim()).toBe(title);
        });

        const result = router.parseUrl(router.url);
        expect(result.fragment).toBe(fragment);
        expect(result.queryParamMap.get('returnUrl')).toBe(returnUrl);
        if (account === EXPECTED_ACCOUNT) {
            expect(router.url).toBe(RETURN_URL);
        }
        http.verify();
    });

    it.each(['unavailable', 'forbidden'])('retains the intended account when the role recheck is %s', async reason => {
        const harness = await RouterTestingHarness.create();
        const http = TestBed.inject(HttpTestingController);
        const router = TestBed.inject(Router);
        const navigation = harness.navigateByUrl(`${RETURN_URL}#${ACCOUNT_FRAGMENT}`);
        const initial = await nextRestoreAsync(http);
        initial.flush({ sessionId: 'known-session', roles: ['User'], user: { id: EXPECTED_ACCOUNT } });
        const recheck = await nextRestoreAsync(http);
        expect(recheck.request.headers.get('X-FoodDiary-Session')).toBe('known-session');
        if (reason === 'unavailable') {
            recheck.flush({}, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
        } else {
            recheck.flush({ sessionId: 'known-session', roles: ['User'], user: { id: EXPECTED_ACCOUNT } });
        }
        await navigation;

        const button = recoveryButton(harness);
        expect(button.textContent).toContain(reason === 'unavailable' ? 'SESSION_RECOVERY.RETRY' : 'Go to login');
        const result = router.parseUrl(router.url);
        expect(result.fragment).toBe(ACCOUNT_FRAGMENT);
        expect(result.queryParamMap.get('returnUrl')).toBe(RETURN_URL);
        expect(result.queryParamMap.get('reason')).toBe(reason);
        if (reason === 'forbidden') {
            button.click();
            await vi.waitFor(() => {
                harness.detectChanges();
                expect(harness.routeNativeElement?.querySelector('h1')?.textContent.trim()).toBe('Confirm sign-in');
            });
            const signIn = router.parseUrl(router.url);
            expect(signIn.fragment).toBe(ACCOUNT_FRAGMENT);
            expect(signIn.queryParamMap.get('returnUrl')).toBe(RETURN_URL);
            expect(signIn.queryParamMap.get('confirm')).toBe('1');
        }
        http.verify();
    });
});

describe('Native Admin recovery after another failed retry', () => {
    beforeEach(configureRecoveryRoutes);

    it.each([
        { hasCookie: true, message: 'You do not have permission' },
        { hasCookie: false, message: 'Your admin session is missing or expired' },
    ])('changes an unavailable page to the definitive denial when a cookie is present: $hasCookie', async ({ hasCookie, message }) => {
        const harness = await openUnavailableRouteAsync();
        const http = TestBed.inject(HttpTestingController);
        const router = TestBed.inject(Router);

        recoveryButton(harness).click();
        const unavailable = await nextRestoreAsync(http);
        unavailable.flush({}, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(recoveryButton(harness).disabled).toBe(false);
        });
        expect(harness.routeNativeElement?.querySelector('[role="status"]')?.textContent).toContain(
            'ADMIN_AUTH.SESSION_UNAVAILABLE_MESSAGE',
        );

        recoveryButton(harness).click();
        const response = await nextRestoreAsync(http);
        if (hasCookie) {
            response.flush({ sessionId: 'known-session', roles: ['User'], user: { id: EXPECTED_ACCOUNT } });
        } else {
            response.flush({}, { status: HttpStatusCode.Unauthorized, statusText: 'Expired' });
        }
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(harness.routeNativeElement?.textContent).toContain(message);
        });
        expect(recoveryButton(harness).textContent).toContain('Go to login');
        expect(router.parseUrl(router.url).queryParamMap.get('returnUrl')).toBe(RETURN_URL);
        http.verify();
    });

    it.each(['/unauthorized?reason=unauthenticated', '/unauthorized?reason=unauthenticated&returnUrl='])(
        'offers the homepage on manual sign-in from %s',
        async url => {
            const harness = await RouterTestingHarness.create(url);
            expect(harness.routeNativeElement?.textContent).toContain('Your admin session is missing or expired');
            recoveryButton(harness).click();
            const router = TestBed.inject(Router);
            await vi.waitFor(() => {
                expect(router.parseUrl(router.url).queryParamMap.get('returnUrl')).toBe('/');
            });
            TestBed.inject(HttpTestingController).verify();
        },
    );
});

it('retains the initial recovery context when query and fragment change on the reused page', async () => {
    configureRecoveryRoutes();
    const recovery = vi.spyOn(TestBed.inject(AdminAuthService), 'tryApplySsoFromReturnUrlAsync').mockResolvedValue(null);
    const rawReturnUrl = '/users?code=sso-code&page=2';
    const query = new URLSearchParams([
        ['reason', 'unauthenticated'],
        ['reason', 'forbidden'],
        ['returnUrl', rawReturnUrl],
        ['returnUrl', '/audit'],
    ]);
    const harness = await RouterTestingHarness.create();
    const first = await harness.navigateByUrl(`/unauthorized?${query.toString()}#${ACCOUNT_FRAGMENT}`, UnauthorizedComponent);
    const second = await harness.navigateByUrl(
        '/unauthorized?reason=unauthenticated&returnUrl=%2Faudit#account=other-admin',
        UnauthorizedComponent,
    );
    expect(second).toBe(first);
    expect(recovery).toHaveBeenCalledExactlyOnceWith(rawReturnUrl);

    await harness.navigateByUrl('/unauthorized?reason=forbidden&returnUrl=%2Faudit#account=other-admin', UnauthorizedComponent);
    expect(harness.routeNativeElement?.textContent).toContain('Your admin session is missing or expired');
    recoveryButton(harness).click();
    const router = TestBed.inject(Router);
    await vi.waitFor(() => {
        expect(router.url).toContain('/sign-in?');
    });
    const signIn = router.parseUrl(router.url);
    expect(signIn.queryParams).toEqual({ returnUrl: RETURN_URL, confirm: '1' });
    expect(signIn.fragment).toBe(ACCOUNT_FRAGMENT);
    TestBed.inject(HttpTestingController).verify();
});

async function openUnavailableRouteAsync(): Promise<RouterTestingHarness> {
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl(`${RETURN_URL}#${ACCOUNT_FRAGMENT}`);
    const response = await nextRestoreAsync(TestBed.inject(HttpTestingController));
    response.flush({}, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
    await navigation;
    return harness;
}

async function nextRestoreAsync(http: HttpTestingController): Promise<TestRequest> {
    return vi.waitFor(() => http.expectOne({ method: 'POST', url: RESTORE_ENDPOINT }));
}

function recoveryButton(harness: RouterTestingHarness): HTMLButtonElement {
    const button = harness.routeNativeElement?.querySelector('button');
    if (button === null || button === undefined) {
        throw new Error('Expected the native recovery button.');
    }
    return button;
}

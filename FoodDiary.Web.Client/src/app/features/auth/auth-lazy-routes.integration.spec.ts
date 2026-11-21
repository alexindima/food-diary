import { HttpErrorResponse, HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ChangeDetectionStrategy, Component, type Provider, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { EMPTY, firstValueFrom, type Observable, of, Subject } from 'rxjs';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../testing/async-testing';
import { stubScrollIntoView } from '../../../testing/dom-testing';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { AUTH_PASSWORD_RESET_COOLDOWN_SECONDS } from '../../config/runtime-ui.tokens';
import { AuthService } from '../../services/auth.service';
import { AuthCookieSessionService } from '../../shared/auth/auth-cookie-session.service';
import { EmailVerificationCooldownService } from '../../shared/auth/email-verification-cooldown.service';
import { SessionEventsService } from '../../shared/auth/session-events.service';
import { TelegramBackupEmailFlowService } from '../../shared/auth/telegram-backup-email-flow.service';
import { telegramLoginTicket } from '../../shared/auth/telegram-oidc-values';
import { TelegramUnlinkFlowService } from '../../shared/auth/telegram-unlink-flow.service';
import { TelegramWebAppService } from '../../shared/auth/telegram-web-app.service';
import { LocalizationService } from '../../shared/i18n/localization.service';
import { MS_PER_SECOND } from '../../shared/lib/time.constants';
import { UserFacade } from '../../shared/lib/user.facade';
import { entityId } from '../../shared/models/semantics/entity-id';
import type { User } from '../../shared/models/user.data';
import { BrowserStorageService } from '../../shared/platform/browser-storage.service';
import { BrowserWindowService } from '../../shared/platform/browser-window.service';
import { PwaInstallService } from '../../shared/service-worker/pwa-install.service';
import { TelegramAuthService } from './api/telegram-auth.service';
import { authRoutes } from './auth.routes';
import { AuthComponent } from './components/auth/auth';
import { AuthFlowFacade } from './components/auth/auth-lib/auth-flow.facade';
import { AuthGoogleManager } from './components/auth/auth-lib/auth-google.manager';
import { EmailVerificationRealtimeService } from './lib/email-verification-realtime.service';

@Component({
    selector: 'fd-test-auth-login-destination',
    template: '<h1>Diary login destination</h1>',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class LoginDestinationComponent {}

@Component({
    selector: 'fd-test-auth-dashboard-destination',
    template: '<h1>Diary overview destination</h1>',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class DashboardDestinationComponent {}

const MOBILE_LOGIN = '/mobile/login';
const TELEGRAM_ENTRY = '/auth/telegram';
const TELEGRAM_CALLBACK = '/auth/telegram/callback';
const TELEGRAM_PROVIDER_URL = 'https://oauth.telegram.org/auth?client_id=fixture';
const DASHBOARD = '/dashboard';
const REQUIRED_PASSWORD = '/change-password-required';
const ACCOUNT = entityId<'user'>('fixture-route-account');
const EMAIL = 'route-account@example.test';
const FIXTURE_PASSWORD = 'fixture-new-route-password';
const ONE_TIME_TOKEN = 'fixture-one-time-route-token';
const TICKET_LENGTH = 43;
const TELEGRAM_TICKET = telegramLoginTicket('x'.repeat(TICKET_LENGTH));

const signedInUser: User = {
    id: ACCOUNT,
    email: EMAIL,
    hasPassword: true,
    isActive: true,
    isEmailConfirmed: true,
    pushNotificationsEnabled: false,
    fastingPushNotificationsEnabled: false,
    socialPushNotificationsEnabled: false,
    fastingCheckInReminderHours: 12,
    fastingCheckInFollowUpReminderHours: 20,
};

type RouteAuthMethods =
    | 'ensureSessionReadyAsync'
    | 'mustChangePassword'
    | 'requiresEmailVerification'
    | 'getUserId'
    | 'setEmailConfirmed'
    | 'verifyEmail'
    | 'resendEmailVerification'
    | 'confirmPasswordReset'
    | 'completeRequiredPasswordChangeAsync'
    | 'onLogoutAsync'
    | 'acceptExternalAuthentication';
type AuthBoundary = Pick<AuthService, 'isAuthenticated'> & { [Method in RouteAuthMethods]: Mock<AuthService[Method]> };

function authBoundary(authenticated: boolean, requiresPasswordChange: boolean): AuthBoundary {
    const isAuthenticated = signal(authenticated);
    return {
        isAuthenticated,
        ensureSessionReadyAsync: vi.fn<AuthService['ensureSessionReadyAsync']>().mockResolvedValue(undefined),
        mustChangePassword: vi.fn<AuthService['mustChangePassword']>().mockReturnValue(requiresPasswordChange),
        requiresEmailVerification: vi.fn<AuthService['requiresEmailVerification']>().mockReturnValue(false),
        getUserId: vi.fn<AuthService['getUserId']>().mockImplementation(() => (isAuthenticated() ? ACCOUNT : null)),
        setEmailConfirmed: vi.fn<AuthService['setEmailConfirmed']>(),
        verifyEmail: vi.fn<AuthService['verifyEmail']>().mockReturnValue(of(void 0)),
        resendEmailVerification: vi.fn<AuthService['resendEmailVerification']>().mockReturnValue(of(void 0)),
        confirmPasswordReset: vi.fn<AuthService['confirmPasswordReset']>().mockReturnValue(of({ user: signedInUser })),
        completeRequiredPasswordChangeAsync: vi.fn<AuthService['completeRequiredPasswordChangeAsync']>().mockResolvedValue(undefined),
        onLogoutAsync: vi.fn<AuthService['onLogoutAsync']>().mockResolvedValue(undefined),
        acceptExternalAuthentication: vi.fn<AuthService['acceptExternalAuthentication']>(() => {
            isAuthenticated.set(true);
        }),
    };
}

type TelegramApiMethods = 'configuration' | 'startOidc' | 'exchange' | 'complete';
type TelegramBoundary = { [Method in TelegramApiMethods]: Mock<TelegramAuthService[Method]> };
function telegramBoundary(): TelegramBoundary {
    return {
        configuration: vi
            .fn<TelegramAuthService['configuration']>()
            .mockReturnValue(of({ loginEnabled: true, registrationEnabled: true, oidcEnabled: true })),
        startOidc: vi.fn<TelegramAuthService['startOidc']>().mockReturnValue(of({ authorizationUrl: TELEGRAM_PROVIDER_URL })),
        exchange: vi
            .fn<TelegramAuthService['exchange']>()
            .mockReturnValue(of({ ticket: TELEGRAM_TICKET, nextAction: 'login', expiresAtUtc: '2099-01-01T00:00:00Z' })),
        complete: vi.fn<TelegramAuthService['complete']>().mockReturnValue(of({ user: signedInUser })),
    };
}

type BrowserBoundary = { [Method in 'open' | 'replaceCurrentUrl' | 'setInterval' | 'clearInterval']: Mock<BrowserWindowService[Method]> };
type UsersBoundary = Pick<UserFacade, 'user'> & { [Method in 'getInfo' | 'changePassword']: Mock<UserFacade[Method]> };
type RealtimeBoundary = { [Method in 'connectAsync' | 'disconnectAsync']: Mock<EmailVerificationRealtimeService[Method]> };
type AuthFlowBoundary = { [Method in 'login' | 'restoreAccount' | 'requestPasswordReset']: Mock<AuthFlowFacade[Method]> };
type RouteFixture = {
    auth: AuthBoundary;
    authFlow: AuthFlowBoundary;
    telegramApi: TelegramBoundary;
    browser: BrowserBoundary;
    users: UsersBoundary;
    realtime: RealtimeBoundary;
};

function configureAuthRoutes(authenticated = false, requiresPasswordChange = false): RouteFixture {
    const auth = authBoundary(authenticated, requiresPasswordChange);
    const authFlow: AuthFlowBoundary = {
        login: vi.fn<AuthFlowFacade['login']>(),
        restoreAccount: vi.fn<AuthFlowFacade['restoreAccount']>(),
        requestPasswordReset: vi.fn<AuthFlowFacade['requestPasswordReset']>(),
    };
    const telegramApi = telegramBoundary();
    const browser: BrowserBoundary = {
        open: vi.fn<BrowserWindowService['open']>(),
        replaceCurrentUrl: vi.fn<BrowserWindowService['replaceCurrentUrl']>(),
        setInterval: vi.fn<BrowserWindowService['setInterval']>(),
        clearInterval: vi.fn<BrowserWindowService['clearInterval']>(),
    };
    const users: UsersBoundary = {
        user: signal<User | null>(signedInUser),
        getInfo: vi.fn<UserFacade['getInfo']>().mockReturnValue(of(signedInUser)),
        changePassword: vi.fn<UserFacade['changePassword']>().mockReturnValue(of(true)),
    };
    const realtime: RealtimeBoundary = {
        connectAsync: vi.fn<EmailVerificationRealtimeService['connectAsync']>().mockResolvedValue(undefined),
        disconnectAsync: vi.fn<EmailVerificationRealtimeService['disconnectAsync']>().mockResolvedValue(undefined),
    };
    TestBed.configureTestingModule({
        imports: [AuthComponent],
        providers: [
            provideRouter([
                ...authRoutes,
                { path: 'dashboard', component: DashboardDestinationComponent },
                { path: '', pathMatch: 'full', component: LoginDestinationComponent },
            ]),
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: AuthService, useValue: auth },
            { provide: AuthFlowFacade, useValue: authFlow },
            { provide: AUTH_PASSWORD_RESET_COOLDOWN_SECONDS, useValue: 2 },
            { provide: UserFacade, useValue: users },
            { provide: TelegramAuthService, useValue: telegramApi },
            { provide: BrowserWindowService, useValue: browser },
            { provide: EmailVerificationRealtimeService, useValue: realtime },
            ...localBoundaryProviders(),
        ],
    }).overrideProvider(AuthGoogleManager, {
        useValue: { ready: signal(false), initializeAsync: vi.fn().mockResolvedValue(undefined), renderButton: vi.fn() },
    });
    return { auth, authFlow, telegramApi, browser, users, realtime };
}

function localBoundaryProviders(): Provider[] {
    return [
        { provide: PwaInstallService, useValue: { shouldRememberSession: (): boolean => false } },
        { provide: LocalizationService, useValue: { getCurrentLanguage: (): string => 'en' } },
        { provide: EmailVerificationCooldownService, useValue: { remainingSeconds: (): number => 0 } },
        {
            provide: SessionEventsService,
            useValue: { authenticated$: EMPTY, sessionEnded$: EMPTY, localSessionInvalidating$: EMPTY },
        },
        {
            provide: TelegramWebAppService,
            useValue: {
                isMiniAppLaunch: (): boolean => false,
                claimAutomaticSignInAsync: vi.fn().mockResolvedValue(false),
                initializeAsync: vi.fn().mockResolvedValue(undefined),
                getInitData: (): null => null,
            },
        },
        { provide: TelegramBackupEmailFlowService, useValue: { handleCallbackAsync: vi.fn().mockResolvedValue(false) } },
        { provide: TelegramUnlinkFlowService, useValue: { handleCallbackAsync: vi.fn().mockResolvedValue('unhandled') } },
        { provide: BrowserStorageService, useValue: { getJson: (): null => null, setJson: vi.fn(), removeItem: vi.fn() } },
        { provide: AuthCookieSessionService, useValue: { run: <T>(request: () => Observable<T>): Observable<T> => request() } },
    ];
}

function root(harness: RouterTestingHarness): HTMLElement {
    harness.detectChanges();
    const outlet = harness.routeNativeElement;
    if (!(outlet instanceof HTMLElement)) {
        throw new Error('Expected a rendered authentication route');
    }
    return outlet;
}

function action(harness: RouterTestingHarness, label: string): HTMLButtonElement {
    const button = [...root(harness).querySelectorAll('button')].find(
        item => item.getAttribute('aria-label') === label || item.textContent.trim() === label,
    );
    if (button === undefined) {
        throw new Error(`Expected the route action ${label}`);
    }
    return button;
}

function enterPasswords(harness: RouterTestingHarness, values: string[]): void {
    const fields = root(harness).querySelectorAll<HTMLInputElement>('input[type="password"]');
    expect(fields).toHaveLength(values.length);
    values.forEach((value, index) => {
        fields[index].value = value;
        fields[index].dispatchEvent(new Event('input', { bubbles: true }));
    });
    harness.detectChanges();
}

async function expectDashboardAsync(harness: RouterTestingHarness): Promise<void> {
    await vi.waitFor(() => {
        expect(TestBed.inject(Router).url).toBe(DASHBOARD);
        expect(root(harness).querySelector('h1')?.textContent).toBe('Diary overview destination');
    });
}

describe('Lazy mobile and Telegram routes', () => {
    it('restores the account from mobile login and leaves the sign-in form when the diary opens', async () => {
        stubScrollIntoView();
        const { authFlow } = configureAuthRoutes();
        authFlow.login.mockReturnValueOnce(of('accountDeleted'));
        const restoration = new Subject<boolean>();
        authFlow.restoreAccount.mockReturnValueOnce(restoration);
        const harness = await RouterTestingHarness.create(MOBILE_LOGIN);
        const form = root(harness).querySelector('form');
        const email = form?.querySelector<HTMLInputElement>('#auth-login-email');
        if (form === null || email === null || email === undefined) {
            throw new Error('Expected a rendered mobile sign-in form');
        }
        email.value = EMAIL;
        email.dispatchEvent(new Event('input', { bubbles: true }));
        enterPasswords(harness, [FIXTURE_PASSWORD]);

        form.requestSubmit();
        const restore = await vi.waitFor(() => action(harness, 'AUTH.LOGIN.RESTORE'));
        restore.click();
        expect(root(harness).querySelector('form')?.getAttribute('aria-busy')).toBe('true');

        restoration.next(true);
        restoration.complete();
        await expectDashboardAsync(harness);

        expect(form.isConnected).toBe(false);
        expect(authFlow.login).toHaveBeenCalledOnce();
        expect(authFlow.restoreAccount).toHaveBeenCalledOnce();
    });

    it('redirects the mobile alias to its usable login and hands off through the rendered Telegram route', async () => {
        stubScrollIntoView();
        const { telegramApi, browser } = configureAuthRoutes();
        const harness = await RouterTestingHarness.create('/mobile');
        const router = TestBed.inject(Router);

        expect(router.url).toBe(MOBILE_LOGIN);
        expect(root(harness).querySelector('input#auth-login-email[autocomplete="username"]')).not.toBeNull();
        expect(root(harness).querySelector('input#current-password[autocomplete="current-password"]')).not.toBeNull();
        const telegramEntry = await vi.waitFor(() => action(harness, 'AUTH.TELEGRAM.CONTINUE'));
        telegramEntry.click();
        await vi.waitFor(() => {
            expect(router.url).toBe(TELEGRAM_ENTRY);
            expect(root(harness).querySelector('h1')?.textContent).toContain('AUTH.TELEGRAM.TITLE');
        });

        const startTelegram = await vi.waitFor(() => action(harness, 'AUTH.TELEGRAM.CONTINUE'));
        startTelegram.click();
        await vi.waitFor(() => {
            expect(browser.open).toHaveBeenCalledExactlyOnceWith(TELEGRAM_PROVIDER_URL, '_self');
        });
        expect(telegramApi.startOidc).toHaveBeenCalledExactlyOnceWith(false);
        expect(telegramApi.exchange).not.toHaveBeenCalled();
    });

    it('accepts an OAuth return on its own route and completes the displayed sign-in action', async () => {
        const { auth, telegramApi, browser } = configureAuthRoutes();
        const code = 'fixture-code';
        const state = 'fixture-state';
        const harness = await RouterTestingHarness.create(`${TELEGRAM_CALLBACK}?code=${code}&state=${state}`);

        const signIn = await vi.waitFor(() => action(harness, 'AUTH.TELEGRAM.SIGN_IN'));
        expect(browser.replaceCurrentUrl).toHaveBeenCalledExactlyOnceWith(TELEGRAM_ENTRY);
        expect(telegramApi.exchange).toHaveBeenCalledExactlyOnceWith(code, state);
        expect(browser.open).not.toHaveBeenCalled();
        signIn.click();
        await expectDashboardAsync(harness);

        expect(telegramApi.complete).toHaveBeenCalledExactlyOnceWith(TELEGRAM_TICKET, 'login', 'en', expect.any(String));
        expect(auth.acceptExternalAuthentication).toHaveBeenCalledExactlyOnceWith({ user: signedInUser });
    });
});

describe('Mobile password reset cooldown lifetime', () => {
    it('clears an expired countdown once and leaves the next route usable', async () => {
        stubScrollIntoView();
        const { authFlow, browser } = configureAuthRoutes();
        const timerId = 17;
        browser.setInterval.mockReturnValueOnce(timerId);
        authFlow.requestPasswordReset.mockReturnValueOnce(of('success'));
        const harness = await RouterTestingHarness.create(MOBILE_LOGIN);

        action(harness, 'AUTH.LOGIN.FORGOT_PASSWORD').click();
        const email = root(harness).querySelector<HTMLInputElement>('input[autocomplete="email"]');
        if (email === null) {
            throw new Error('Expected a rendered recovery email field');
        }
        email.value = EMAIL;
        email.dispatchEvent(new Event('input', { bubbles: true }));
        action(harness, 'AUTH.RESET.SEND').click();

        await vi.waitFor(() => {
            expect(root(harness).textContent).toContain('AUTH.RESET.RESEND_COOLDOWN');
        });
        expect(action(harness, 'AUTH.RESET.SEND').disabled).toBe(true);
        expect(browser.setInterval).toHaveBeenCalledExactlyOnceWith(expect.any(Function), MS_PER_SECOND);
        const tick = browser.setInterval.mock.calls[0][0];
        tick();
        expect(action(harness, 'AUTH.RESET.SEND').disabled).toBe(true);
        tick();
        expect(action(harness, 'AUTH.RESET.SEND').disabled).toBe(false);
        expect(root(harness).textContent).not.toContain('AUTH.RESET.RESEND_COOLDOWN');
        expect(browser.clearInterval).toHaveBeenCalledExactlyOnceWith(timerId);

        await harness.navigateByUrl('/');
        expect(root(harness).querySelector('h1')?.textContent).toBe('Diary login destination');
        expect(authFlow.requestPasswordReset).toHaveBeenCalledExactlyOnceWith({ email: EMAIL });
        expect(browser.clearInterval).toHaveBeenCalledExactlyOnceWith(timerId);
    });
});

describe('Lazy verification routes', () => {
    it('resends from the pending screen and follows manual confirmation to the diary', async () => {
        const { auth, users, realtime } = configureAuthRoutes(true);
        users.getInfo.mockReturnValueOnce(of({ ...signedInUser, isEmailConfirmed: false }));
        const harness = await RouterTestingHarness.create('/verify-pending');
        expect(root(harness).querySelector('h1')?.textContent).toContain('AUTH.VERIFY_PENDING.TITLE');
        expect(auth.setEmailConfirmed).toHaveBeenCalledExactlyOnceWith(false);

        action(harness, 'AUTH.VERIFY_PENDING.RESEND').click();
        expect(root(harness).querySelector('[role="status"]')?.textContent).toContain('AUTH.VERIFY_PENDING.RESENT');
        expect(auth.resendEmailVerification).toHaveBeenCalledOnce();
        action(harness, 'AUTH.VERIFY_PENDING.REFRESH').click();
        await expectDashboardAsync(harness);

        expect(auth.setEmailConfirmed).toHaveBeenLastCalledWith(true);
        expect(realtime.disconnectAsync).toHaveBeenCalledOnce();
    });

    it('shows a verified email link to a signed-out reader and continues to the login destination', async () => {
        const { auth } = configureAuthRoutes();
        const harness = await RouterTestingHarness.create(`/verify-email?userId=${ACCOUNT}&token=${ONE_TIME_TOKEN}`);
        expect(root(harness).textContent).toContain('AUTH.VERIFY.SUCCESS');
        expect(auth.verifyEmail).toHaveBeenCalledExactlyOnceWith(ACCOUNT, ONE_TIME_TOKEN);
        action(harness, 'AUTH.VERIFY.CONTINUE').click();

        await vi.waitFor(() => {
            expect(TestBed.inject(Router).url).toBe('/?auth=login');
            expect(root(harness).querySelector('h1')?.textContent).toBe('Diary login destination');
        });
    });
});

describe('Lazy password routes', () => {
    it('confirms a reset link using the native password form and opens the diary after success', async () => {
        const { auth } = configureAuthRoutes();
        const harness = await RouterTestingHarness.create(`/reset-password?userId=${ACCOUNT}&token=${ONE_TIME_TOKEN}`);
        expect(root(harness).querySelector('h1')?.textContent).toContain('AUTH.RESET.TITLE');
        enterPasswords(harness, [FIXTURE_PASSWORD, FIXTURE_PASSWORD]);
        action(harness, 'AUTH.RESET.SAVE').click();
        await expectDashboardAsync(harness);

        expect(auth.confirmPasswordReset).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ userId: ACCOUNT, token: ONE_TIME_TOKEN, newPassword: FIXTURE_PASSWORD }),
        );
    });

    it('keeps the required password form gated until the authenticated session resolves', async () => {
        const { auth, users } = configureAuthRoutes(true, true);
        const session = new Subject<void>();
        auth.ensureSessionReadyAsync.mockReturnValueOnce(firstValueFrom(session));
        const harness = await RouterTestingHarness.create(DASHBOARD);
        const pending = harness.navigateByUrl(REQUIRED_PASSWORD);
        await vi.waitFor(() => {
            expect(auth.ensureSessionReadyAsync).toHaveBeenCalledOnce();
        });
        expect(root(harness).querySelector('h1')?.textContent).toBe('Diary overview destination');

        session.next();
        session.complete();
        await pending;
        expect(root(harness).querySelector('h1')?.textContent).toContain('AUTH.REQUIRED_PASSWORD_CHANGE.TITLE');
        enterPasswords(harness, ['fixture-temporary-password', FIXTURE_PASSWORD, FIXTURE_PASSWORD]);
        action(harness, 'AUTH.REQUIRED_PASSWORD_CHANGE.SAVE').click();
        await expectDashboardAsync(harness);

        expect(users.changePassword).toHaveBeenCalledExactlyOnceWith({
            currentPassword: 'fixture-temporary-password',
            newPassword: FIXTURE_PASSWORD,
        });
        expect(auth.completeRequiredPasswordChangeAsync).toHaveBeenCalledExactlyOnceWith(FIXTURE_PASSWORD);
    });

    it('redirects a signed-out visitor away from the required password form', async () => {
        const { users } = configureAuthRoutes();
        const harness = await RouterTestingHarness.create(DASHBOARD);
        const router = TestBed.inject(Router);
        await router.navigateByUrl(REQUIRED_PASSWORD);

        await vi.waitFor(() => {
            expect(router.url).toBe('/?auth=login');
            expect(root(harness).querySelector('h1')?.textContent).toBe('Diary login destination');
        });
        expect(users.changePassword).not.toHaveBeenCalled();
    });
});

describe('Leaving during a required password change', () => {
    const loginUrl = '/?auth=login';

    it('does not start password-based renewal after Logout even while logout navigation is still pending', async () => {
        const { auth, users } = configureAuthRoutes(true, true);
        const write = new Subject<boolean>();
        const logoutReady = new Subject<void>();
        users.changePassword.mockReturnValueOnce(write);
        const harness = await RouterTestingHarness.create(REQUIRED_PASSWORD);
        const router = TestBed.inject(Router);
        auth.onLogoutAsync.mockImplementationOnce(async () => {
            await firstValueFrom(logoutReady);
            await router.navigateByUrl(loginUrl);
        });

        submitRequiredPassword(harness);
        expect(write.observed).toBe(true);
        action(harness, 'AUTH.REQUIRED_PASSWORD_CHANGE.LOGOUT').click();
        expect(auth.onLogoutAsync).toHaveBeenCalledOnce();
        expect(router.url).toBe(REQUIRED_PASSWORD);
        write.next(true);
        write.complete();
        await waitForAsyncTasksAsync();

        expect(auth.completeRequiredPasswordChangeAsync).not.toHaveBeenCalled();
        expect(router.url).toBe(REQUIRED_PASSWORD);
        logoutReady.next();
        logoutReady.complete();
        await vi.waitFor(() => {
            expect(router.url).toBe(loginUrl);
            expect(root(harness).querySelector('h1')?.textContent).toBe('Diary login destination');
        });
    });

    it.each(['success', 'failure'] as const)('keeps the login destination after an earlier renewal ends with %s', async outcome => {
        const { auth, users } = configureAuthRoutes(true, true);
        const renewal = new Subject<void>();
        auth.completeRequiredPasswordChangeAsync.mockReturnValueOnce(firstValueFrom(renewal));
        const harness = await RouterTestingHarness.create(REQUIRED_PASSWORD);
        const router = TestBed.inject(Router);
        auth.onLogoutAsync.mockImplementationOnce(async () => {
            await router.navigateByUrl(loginUrl);
        });

        submitRequiredPassword(harness);
        await vi.waitFor(() => {
            expect(auth.completeRequiredPasswordChangeAsync).toHaveBeenCalledExactlyOnceWith(FIXTURE_PASSWORD);
        });
        action(harness, 'AUTH.REQUIRED_PASSWORD_CHANGE.LOGOUT').click();
        await vi.waitFor(() => {
            expect(root(harness).querySelector('h1')?.textContent).toBe('Diary login destination');
        });
        if (outcome === 'failure') {
            renewal.error(new HttpErrorResponse({ status: HttpStatusCode.ServiceUnavailable }));
        } else {
            renewal.next();
            renewal.complete();
        }
        await waitForAsyncTasksAsync();
        await harness.fixture.whenStable();

        expect(users.changePassword).toHaveBeenCalledOnce();
        expect(router.url).toBe(loginUrl);
        expect(root(harness).querySelector('h1')?.textContent).toBe('Diary login destination');
        expect(root(harness).querySelector('[role="alert"]')).toBeNull();
    });

    it('leaves the reset page open when a password write finishes after route replacement', async () => {
        const { auth, users } = configureAuthRoutes(true, true);
        const write = new Subject<boolean>();
        users.changePassword.mockReturnValueOnce(write);
        const harness = await RouterTestingHarness.create(REQUIRED_PASSWORD);
        const resetUrl = `/reset-password?userId=${ACCOUNT}&token=${ONE_TIME_TOKEN}`;

        submitRequiredPassword(harness);
        expect(write.observed).toBe(true);
        await harness.navigateByUrl(resetUrl);
        expect(root(harness).querySelector('h1')?.textContent).toContain('AUTH.RESET.TITLE');
        write.next(true);
        write.complete();
        await waitForAsyncTasksAsync();
        await harness.fixture.whenStable();

        expect(auth.onLogoutAsync).not.toHaveBeenCalled();
        expect(auth.completeRequiredPasswordChangeAsync).not.toHaveBeenCalled();
        expect(TestBed.inject(Router).url).toBe(resetUrl);
        expect(root(harness).querySelector('h1')?.textContent).toContain('AUTH.RESET.TITLE');
    });
});

it('keeps the required form after a rejected write and retries session renewal without resending the accepted password', async () => {
    const { auth, users } = configureAuthRoutes(true, true);
    users.changePassword.mockReturnValueOnce(of(false));
    auth.completeRequiredPasswordChangeAsync.mockRejectedValueOnce(new Error('Fixture session renewal unavailable'));
    const harness = await RouterTestingHarness.create(REQUIRED_PASSWORD);
    submitRequiredPassword(harness);
    await expectRequiredPasswordErrorAsync(harness, 'AUTH.REQUIRED_PASSWORD_CHANGE.ERROR');
    expect(auth.completeRequiredPasswordChangeAsync).not.toHaveBeenCalled();

    action(harness, 'AUTH.REQUIRED_PASSWORD_CHANGE.SAVE').click();
    await expectRequiredPasswordErrorAsync(harness, 'AUTH.REQUIRED_PASSWORD_CHANGE.SESSION_ERROR');
    expect(users.changePassword).toHaveBeenCalledTimes(2);
    expect(auth.completeRequiredPasswordChangeAsync).toHaveBeenCalledExactlyOnceWith(FIXTURE_PASSWORD);

    action(harness, 'AUTH.REQUIRED_PASSWORD_CHANGE.SAVE').click();
    await expectDashboardAsync(harness);
    expect(users.changePassword).toHaveBeenCalledTimes(2);
    expect(auth.completeRequiredPasswordChangeAsync).toHaveBeenCalledTimes(2);
});

function submitRequiredPassword(harness: RouterTestingHarness): void {
    enterPasswords(harness, ['fixture-temporary-password', FIXTURE_PASSWORD, FIXTURE_PASSWORD]);
    action(harness, 'AUTH.REQUIRED_PASSWORD_CHANGE.SAVE').click();
}

async function expectRequiredPasswordErrorAsync(harness: RouterTestingHarness, key: string): Promise<void> {
    await vi.waitFor(() => {
        expect(root(harness).querySelector('[role="alert"]')?.textContent).toContain(key);
    });
}

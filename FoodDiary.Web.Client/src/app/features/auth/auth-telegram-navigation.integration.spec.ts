import { Location } from '@angular/common';
import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { ChangeDetectionStrategy, Component, DestroyRef, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NavigationEnd, provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { filter, firstValueFrom, type Observable, of, Subject } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { waitForAsyncTasksAsync } from '../../../testing/async-testing';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { authGuard } from '../../guards/auth.guard';
import { AuthService } from '../../services/auth.service';
import { NavigationService } from '../../services/navigation.service';
import type { AuthResponse } from '../../shared/auth/auth.data';
import { AuthRecoveryStateService } from '../../shared/auth/auth-recovery-state.service';
import { TelegramBackupEmailFlowService } from '../../shared/auth/telegram-backup-email-flow.service';
import { telegramLoginTicket } from '../../shared/auth/telegram-oidc-values';
import { TelegramUnlinkFlowService } from '../../shared/auth/telegram-unlink-flow.service';
import { TelegramWebAppService } from '../../shared/auth/telegram-web-app.service';
import { LocalizationService } from '../../shared/i18n/localization.service';
import { UserFacade } from '../../shared/lib/user.facade';
import { entityId } from '../../shared/models/semantics/entity-id';
import type { User } from '../../shared/models/user.data';
import { BrowserStorageService } from '../../shared/platform/browser-storage.service';
import { PwaInstallService } from '../../shared/service-worker/pwa-install.service';
import { TelegramAuthService } from './api/telegram-auth.service';
import { authRoutes } from './auth.routes';
import type { TelegramConfiguration, TelegramIntent } from './models/telegram-auth.data';
import { TelegramAuthComponent } from './pages/telegram-auth/telegram-auth';

const PRIOR_URL = '/explore';
const AUTH_RETURN_URL = '/profile';
const BACKUP_EMAIL_URL = `${environment.apiUrls.auth}/telegram/backup-email/oidc`;
const BACKUP_RECEIPT = 'fooddiary.telegram.backup-email';
const PENDING_BROWSER_LOGOUT = 'pendingBrowserLogout';
const BACKUP_EMAIL = 'telegram-backup-route@example.test';
const CALLBACK_CODE = 'telegram-backup-route-code';
const CALLBACK_STATE_LENGTH = 43;
const CALLBACK_STATE = 'b'.repeat(CALLBACK_STATE_LENGTH);
const signInIntent: TelegramIntent = {
    ticket: telegramLoginTicket('abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG'),
    nextAction: 'login',
    expiresAtUtc: '2099-01-01T00:00:00Z',
};
const sessionResponse: AuthResponse = {
    sessionId: 'telegram-test-session',
    user: {
        id: entityId<'user'>('telegram-test-account'),
        email: null,
        hasPassword: false,
        isActive: true,
        isEmailConfirmed: false,
        pushNotificationsEnabled: false,
        fastingPushNotificationsEnabled: false,
        socialPushNotificationsEnabled: false,
        fastingCheckInReminderHours: 12,
        fastingCheckInFollowUpReminderHours: 20,
    },
};

@Component({
    selector: 'fd-telegram-previous-destination',
    template: '<h1>Explore recipes</h1>',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class RecipesDestination {}

@Component({
    selector: 'fd-telegram-sign-in-destination',
    template: '<h1>Account profile</h1>',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class AuthReturnDestination {}

async function waitForSignInButtonAsync(harness: RouterTestingHarness): Promise<HTMLButtonElement> {
    return vi.waitFor(() => {
        harness.detectChanges();
        const buttons = Array.from(harness.routeNativeElement?.querySelectorAll('button') ?? []);
        const button = buttons.find(control => control.textContent.includes('AUTH.TELEGRAM.SIGN_IN'));
        if (button === undefined) {
            throw new Error(`Expected Telegram sign-in action, got ${harness.routeNativeElement?.textContent ?? ''}`);
        }
        return button;
    });
}

it('keeps the Back destination when a pending Telegram sign-in accepts a session after leaving', async () => {
    const pending = new Subject<AuthResponse>();
    const complete = vi.fn<TelegramAuthService['complete']>().mockReturnValue(pending);
    const isAuthenticated = signal(false);
    const acceptExternalAuthentication = vi.fn<AuthService['acceptExternalAuthentication']>(() => {
        isAuthenticated.set(true);
    });
    TestBed.configureTestingModule({
        providers: [
            provideRouter([
                ...authRoutes,
                { path: 'explore', component: RecipesDestination },
                { path: 'profile', component: AuthReturnDestination },
            ]),
            provideLocationMocks(),
            provideTranslateTesting(),
            {
                provide: AuthService,
                useValue: {
                    isAuthenticated,
                    acceptExternalAuthentication,
                    requiresEmailVerification: signal(false),
                    mustChangePassword: signal(false),
                },
            },
            {
                provide: TelegramAuthService,
                useValue: {
                    configuration: (): Observable<TelegramConfiguration> =>
                        of({ loginEnabled: true, registrationEnabled: true, oidcEnabled: true }),
                    complete,
                },
            },
            { provide: BrowserStorageService, useValue: { getJson: (): TelegramIntent => signInIntent, removeItem: vi.fn() } },
            { provide: TelegramWebAppService, useValue: { claimAutomaticSignInAsync: vi.fn().mockResolvedValue(false) } },
            { provide: TelegramUnlinkFlowService, useValue: { handleCallbackAsync: vi.fn().mockResolvedValue('unhandled') } },
            { provide: TelegramBackupEmailFlowService, useValue: { handleCallbackAsync: vi.fn().mockResolvedValue(false) } },
            { provide: LocalizationService, useValue: { getCurrentLanguage: (): string => 'en' } },
            {
                provide: UserFacade,
                useValue: { user: signal<User | null>(null), getInfo: (): Observable<User> => of(sessionResponse.user) },
            },
        ],
    });
    const router = TestBed.inject(Router);
    router.setUpLocationChangeListener();
    const returnNavigation = vi.spyOn(TestBed.inject(NavigationService), 'navigateToReturnUrlAsync');
    const harness = await RouterTestingHarness.create(PRIOR_URL);
    const query = new URLSearchParams({ returnUrl: AUTH_RETURN_URL });
    await harness.navigateByUrl(`/auth/telegram?${query.toString()}`, TelegramAuthComponent);
    const signIn = await waitForSignInButtonAsync(harness);
    signIn.click();
    await vi.waitFor(() => {
        expect(complete).toHaveBeenCalledExactlyOnceWith(signInIntent.ticket, 'login', 'en', expect.any(String));
    });

    const returned = firstValueFrom(
        router.events.pipe(filter(event => event instanceof NavigationEnd && event.urlAfterRedirects === PRIOR_URL)),
    );
    TestBed.inject(Location).back();
    await returned;
    expect(router.url).toBe(PRIOR_URL);

    pending.next(sessionResponse);
    pending.complete();
    await waitForAsyncTasksAsync();
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(acceptExternalAuthentication).toHaveBeenCalledExactlyOnceWith(sessionResponse);
    expect(isAuthenticated()).toBe(true);
    expect(returnNavigation).not.toHaveBeenCalled();
    expect(router.url).toBe(PRIOR_URL);
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Explore recipes');
});

describe('Telegram backup email callback navigation', () => {
    afterEach(() => {
        TestBed.inject(HttpTestingController).verify();
        localStorage.clear();
        sessionStorage.clear();
        window.history.replaceState({}, '', '/');
        vi.restoreAllMocks();
    });

    it('returns to Profile and preserves the backup receipt for the still-active account', async () => {
        const { flow, harness, confirmation } = await openBackupEmailCallbackAsync();

        await finishBackupEmailCallbackAsync(harness, confirmation);

        expect(TestBed.inject(Router).url).toBe(AUTH_RETURN_URL);
        expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Account profile');
        expect(flow.read()).toMatchObject({ userId: sessionResponse.user.id, email: BACKUP_EMAIL, state: null, failed: false });
        expect(flow.read()?.sentAt).toBeTypeOf('number');
    });

    it('stays signed out when a Telegram backup email acknowledgement arrives after logout in another tab', async () => {
        const { auth, harness, confirmation } = await openBackupEmailCallbackAsync();
        const owner = harness.routeDebugElement?.injector.get(DestroyRef);
        expect(owner?.destroyed).toBe(false);

        await signOutFromAnotherTabAsync(harness);
        expect(owner?.destroyed).toBe(true);
        expect(auth.getUserId()).toBeNull();

        await finishBackupEmailCallbackAsync(harness, confirmation);

        expect(TestBed.inject(Router).url).toBe('/');
        expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Explore recipes');
        expect(auth.getUserId()).toBeNull();
        expect(sessionStorage.getItem(BACKUP_RECEIPT)).toBeNull();
    });
});

function configureBackupEmailCallback(): void {
    TestBed.configureTestingModule({
        providers: [
            provideRouter([
                ...authRoutes,
                { path: 'profile', canActivate: [authGuard], component: AuthReturnDestination },
                { path: '', pathMatch: 'full', component: RecipesDestination },
            ]),
            provideLocationMocks(),
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            {
                provide: LocalizationService,
                useValue: {
                    getCurrentLanguage: (): string => 'en',
                    loadApplicationTranslationsAsync: vi
                        .fn<LocalizationService['loadApplicationTranslationsAsync']>()
                        .mockResolvedValue(undefined),
                },
            },
            { provide: PwaInstallService, useValue: { initialize: vi.fn(), shouldRememberSession: (): boolean => false } },
            {
                provide: UserFacade,
                useValue: {
                    user: signal<User | null>(sessionResponse.user),
                    getInfo: (): Observable<User> => of(sessionResponse.user),
                },
            },
        ],
    });
}

async function openBackupEmailCallbackAsync(): Promise<{
    auth: AuthService;
    flow: TelegramBackupEmailFlowService;
    harness: RouterTestingHarness;
    confirmation: TestRequest;
}> {
    configureBackupEmailCallback();
    const provider = vi.spyOn(window, 'open').mockReturnValue(null);
    const auth = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);
    const restoring = auth.restoreSessionAsync();
    const session = await vi.waitFor(() => http.expectOne({ method: 'POST', url: `${environment.apiUrls.auth}/session` }));
    session.flush({ ...sessionResponse, roles: ['User'] });
    await restoring;
    expect(auth.getUserId()).toBe(sessionResponse.user.id);

    const flow = TestBed.inject(TelegramBackupEmailFlowService);
    const starting = flow.startAsync(BACKUP_EMAIL);
    const start = http.expectOne({ method: 'POST', url: `${BACKUP_EMAIL_URL}/start` });
    expect(start.request.body).toEqual({ email: BACKUP_EMAIL });
    const authorizationUrl = `https://oauth.telegram.org/auth?state=${CALLBACK_STATE}`;
    start.flush({ authorizationUrl });
    await starting;
    expect(provider).toHaveBeenCalledExactlyOnceWith(authorizationUrl, '_self');

    const query = new URLSearchParams({ code: CALLBACK_CODE, state: CALLBACK_STATE });
    const callback = `/auth/telegram/callback?${query.toString()}`;
    window.history.replaceState({}, '', callback);
    const harness = await RouterTestingHarness.create(callback);
    const confirmation = await vi.waitFor(() => http.expectOne({ method: 'POST', url: `${BACKUP_EMAIL_URL}/complete` }));
    expect(confirmation.request.body).toEqual({ code: CALLBACK_CODE, state: CALLBACK_STATE });
    expect(harness.routeNativeElement?.querySelector('[role="status"]')?.textContent).toContain('COMMON.LOADING');
    expect(window.location.pathname).toBe('/auth/telegram');
    expect(window.location.search).toBe('');
    return { auth, flow, harness, confirmation };
}

function publishOtherTabLogout(pending: string | null): void {
    if (pending === null) {
        localStorage.removeItem(PENDING_BROWSER_LOGOUT);
    } else {
        localStorage.setItem(PENDING_BROWSER_LOGOUT, pending);
    }
    const event = new StorageEvent('storage', { key: PENDING_BROWSER_LOGOUT, newValue: pending });
    Object.defineProperty(event, 'storageArea', { value: localStorage });
    window.dispatchEvent(event);
}

async function signOutFromAnotherTabAsync(harness: RouterTestingHarness): Promise<void> {
    const router = TestBed.inject(Router);
    const landing = firstValueFrom(router.events.pipe(filter(event => event instanceof NavigationEnd && event.urlAfterRedirects === '/')));
    publishOtherTabLogout(JSON.stringify({ sessionId: sessionResponse.sessionId, contextId: null, telegram: false }));
    await landing;
    harness.detectChanges();
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Explore recipes');
    publishOtherTabLogout(null);
    expect(TestBed.inject(AuthRecoveryStateService).issue()).toBeNull();
}

async function finishBackupEmailCallbackAsync(harness: RouterTestingHarness, confirmation: TestRequest): Promise<void> {
    confirmation.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
    await waitForAsyncTasksAsync();
    await harness.fixture.whenStable();
    harness.detectChanges();
}

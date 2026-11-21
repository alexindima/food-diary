import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, type Mock, type MockInstance, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';
import { NavigationService } from '../../services/navigation.service';
import { LocalizationService } from '../i18n/localization.service';
import { entityId } from '../models/semantics/entity-id';
import { BrowserStorageService } from '../platform/browser-storage.service';
import { BrowserWindowService } from '../platform/browser-window.service';
import type { AuthResponse } from './auth.data';
import { SessionEventsService } from './session-events.service';
import { TelegramBackupEmailFlowService } from './telegram-backup-email-flow.service';
import { telegramOAuthCode, telegramOAuthState } from './telegram-oidc-values';

describe('TelegramBackupEmailFlowService', () => {
    const stateLength = 43;
    const state = 's'.repeat(stateLength);
    let flow: TelegramBackupEmailFlowService;
    let stored: unknown;
    const authenticated = new Subject<void>();
    const ended = new Subject<void>();
    const auth = { getUserId: vi.fn(), startTelegramBackupEmail: vi.fn(), completeTelegramBackupEmail: vi.fn() };
    const browser = { open: vi.fn() };

    beforeEach(() => {
        stored = null;
        vi.clearAllMocks();
        auth.getUserId.mockReturnValue('user-1');
        auth.startTelegramBackupEmail.mockReturnValue(of({ authorizationUrl: `https://oauth.telegram.org/auth?state=${state}` }));
        auth.completeTelegramBackupEmail.mockReturnValue(of(void 0));
        TestBed.configureTestingModule({
            providers: [
                { provide: AuthService, useValue: auth },
                { provide: BrowserWindowService, useValue: browser },
                { provide: SessionEventsService, useValue: { authenticated$: authenticated, sessionEnded$: ended } },
                {
                    provide: BrowserStorageService,
                    useValue: {
                        getJson: (): unknown => stored,
                        setJson: (_scope: string, _key: string, value: unknown): void => {
                            stored = value;
                        },
                        removeItem: (): void => {
                            stored = null;
                        },
                    },
                },
            ],
        });
        flow = TestBed.inject(TelegramBackupEmailFlowService);
    });

    it('returns a successful email request without logging in or completing a normal login intent', async () => {
        await flow.startAsync('backup@example.com');
        expect(browser.open).toHaveBeenCalledOnce();
        expect(await flow.handleCallbackAsync(telegramOAuthCode('code'), telegramOAuthState(state), false)).toBe(true);
        expect(auth.completeTelegramBackupEmail).toHaveBeenCalledWith('code', state);
        expect(flow.read()?.sentAt).toBeTypeOf('number');
        expect(flow.read()?.state).toBeNull();
        expect(await flow.handleCallbackAsync(telegramOAuthCode('code'), telegramOAuthState(state), false)).toBe(false);
    });

    it.each(['cancelled', 'wrong-state', 'no-code'])('rejects %s without sending a completion', async scenario => {
        await flow.startAsync('backup@example.com');
        await flow.handleCallbackAsync(
            scenario === 'no-code' ? null : telegramOAuthCode('code'),
            telegramOAuthState(scenario === 'wrong-state' ? 'foreign' : state),
            scenario === 'cancelled',
        );
        expect(auth.completeTelegramBackupEmail).not.toHaveBeenCalled();
        expect(flow.read()?.failed).toBe(true);
        expect(flow.read()?.sentAt).toBeNull();
    });

    it('clears pending data when the signed-in account changes or the session ends', async () => {
        await flow.startAsync('backup@example.com');
        auth.getUserId.mockReturnValue('user-2');
        expect(flow.read()).toBeNull();
        flow.markSent('other@example.com');
        ended.next();
        expect(stored).toBeNull();
    });

    it('does not claim an email was sent when completion fails', async () => {
        await flow.startAsync('backup@example.com');
        auth.completeTelegramBackupEmail.mockReturnValue(throwError(() => new Error('expired')));
        await flow.handleCallbackAsync(telegramOAuthCode('code'), telegramOAuthState(state), false);
        expect(flow.read()?.failed).toBe(true);
        expect(flow.read()?.sentAt).toBeNull();
    });

    it('rejects a redirect to an unexpected provider', async () => {
        auth.startTelegramBackupEmail.mockReturnValue(of({ authorizationUrl: `https://example.com/auth?state=${state}` }));
        await expect(flow.startAsync('backup@example.com')).rejects.toThrow();
        expect(browser.open).not.toHaveBeenCalled();
    });
});

const baseUrl = `${environment.apiUrls.auth}/telegram/backup-email/oidc`;
const stateLength = 43;
const original = backupAccount('original-profile', 'original-browser-session');
const replacement = backupAccount('replacement-profile', 'replacement-browser-session');

type BackupEmailHttpFixture = {
    auth: AuthService;
    http: HttpTestingController;
    flow: TelegramBackupEmailFlowService;
    navigateToLanding: Mock<NavigationService['navigateToLandingAsync']>;
    openProvider: MockInstance<Window['open']>;
    openAttemptAsync: (email: string, state: string) => Promise<void>;
};

function createBackupEmailHttpFixture(): BackupEmailHttpFixture {
    const openProvider = vi.spyOn(window, 'open').mockReturnValue(null);
    const navigateToLanding = vi.fn<NavigationService['navigateToLandingAsync']>().mockResolvedValue(undefined);
    TestBed.configureTestingModule({
        providers: [
            provideHttpClient(),
            provideHttpClientTesting(),
            provideRouter([]),
            { provide: NavigationService, useValue: { navigateToLandingAsync: navigateToLanding } },
            {
                provide: LocalizationService,
                useValue: { applyLanguagePreferenceAsync: vi.fn().mockResolvedValue(undefined), clearStoredLanguage: vi.fn() },
            },
        ],
    });
    const auth = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);
    const flow = TestBed.inject(TelegramBackupEmailFlowService);
    flow.clear();

    const openAttemptAsync = async (email: string, state: string): Promise<void> => {
        const started = flow.startAsync(email);
        const request = http.expectOne({ method: 'POST', url: `${baseUrl}/start` });
        expect(request.request.body).toEqual({ email });
        request.flush({ authorizationUrl: `https://oauth.telegram.org/auth?state=${state}` });
        await started;
    };
    return { auth, http, flow, navigateToLanding, openProvider, openAttemptAsync };
}

function cleanBackupEmailHttpFixture({ flow, http }: BackupEmailHttpFixture): void {
    flow.clear();
    http.verify();
    localStorage.clear();
    vi.restoreAllMocks();
}

describe('Telegram backup email HTTP start during sign-out', () => {
    let fixture: BackupEmailHttpFixture;
    beforeEach(() => {
        fixture = createBackupEmailHttpFixture();
    });
    afterEach(() => {
        cleanBackupEmailHttpFixture(fixture);
    });

    it('blocks a backup email start during sign-out and accepts a newly signed-in account', async () => {
        const { auth, http, flow, navigateToLanding, openProvider, openAttemptAsync } = fixture;
        auth.acceptExternalAuthentication(original);
        const signOut = auth.onLogoutAsync();
        const logout = http.expectOne({ method: 'POST', url: `${environment.apiUrls.auth}/logout` });
        expect(auth.getUserId()).toBeNull();
        expect(navigateToLanding).not.toHaveBeenCalled();

        await expect(flow.startAsync('old-profile@example.test')).rejects.toThrow('Authentication required');
        http.expectNone({ method: 'POST', url: `${baseUrl}/start` });
        expect(openProvider).not.toHaveBeenCalled();
        expect(flow.read()).toBeNull();

        logout.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
        await signOut;
        expect(navigateToLanding).toHaveBeenCalledOnce();

        auth.acceptExternalAuthentication(replacement);
        const email = 'new-profile@example.test';
        const state = 'b'.repeat(stateLength);
        await openAttemptAsync(email, state);
        expect(openProvider).toHaveBeenCalledExactlyOnceWith(`https://oauth.telegram.org/auth?state=${state}`, '_self');
        expect(flow.read()).toMatchObject({ userId: replacement.user.id, email, state });
    });
});

describe('Telegram backup email HTTP startup across session changes', () => {
    let fixture: BackupEmailHttpFixture;
    beforeEach(() => {
        fixture = createBackupEmailHttpFixture();
    });
    afterEach(() => {
        cleanBackupEmailHttpFixture(fixture);
    });

    it('does not open a stale backup email handoff after the same account signs in again', async () => {
        const { auth, http, flow, openProvider } = fixture;
        const email = 'original-profile@example.test';
        const state = 'a'.repeat(stateLength);
        auth.acceptExternalAuthentication(original);
        const sessionId = auth.getSessionKey();
        const opening = Promise.allSettled([flow.startAsync(email)]);
        const starting = http.expectOne({ method: 'POST', url: `${baseUrl}/start` });
        expect(starting.request.body).toEqual({ email });

        const signingOut = auth.onLogoutAsync();
        const logout = http.expectOne({ method: 'POST', url: `${environment.apiUrls.auth}/logout` });
        expect(auth.getUserId()).toBeNull();
        logout.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
        await signingOut;
        const nextAuth = { ...original, sessionId: 'fb7f89ba-7ed4-4f0f-931c-67247d6e258f' };
        auth.acceptExternalAuthentication(nextAuth);
        expect(auth.getUserId()).toBe(original.user.id);
        expect(auth.getSessionKey()).toBe(nextAuth.sessionId);
        expect(auth.getSessionKey()).not.toBe(sessionId);

        starting.flush({ authorizationUrl: `https://oauth.telegram.org/auth?state=${state}` });
        await opening;

        expect(openProvider).not.toHaveBeenCalled();
        expect(flow.read()).toBeNull();
        http.expectNone({ method: 'POST', url: `${baseUrl}/complete` });
    });

    it('preserves a new account backup email handoff when the old startup reply arrives', async () => {
        const { auth, http, flow, openProvider, openAttemptAsync } = fixture;
        const originalEmail = 'original-profile@example.test';
        const originalState = 'a'.repeat(stateLength);
        const replacementEmail = 'new-profile@example.test';
        const replacementState = 'b'.repeat(stateLength);
        auth.acceptExternalAuthentication(original);
        const opening = Promise.allSettled([flow.startAsync(originalEmail)]);
        const starting = http.expectOne({ method: 'POST', url: `${baseUrl}/start` });
        expect(starting.request.body).toEqual({ email: originalEmail });

        auth.acceptExternalAuthentication(replacement);
        await openAttemptAsync(replacementEmail, replacementState);
        const current = flow.read();
        expect(current).toMatchObject({ userId: replacement.user.id, email: replacementEmail, state: replacementState });

        starting.flush({ authorizationUrl: `https://oauth.telegram.org/auth?state=${originalState}` });
        await opening;

        expect(auth.getUserId()).toBe(replacement.user.id);
        expect(flow.read()).toEqual(current);
        expect(openProvider).toHaveBeenCalledExactlyOnceWith(`https://oauth.telegram.org/auth?state=${replacementState}`, '_self');
        const code = telegramOAuthCode('local-replacement-start-confirmation');
        const finishing = flow.handleCallbackAsync(code, telegramOAuthState(replacementState), false);
        const completion = http.expectOne({ method: 'POST', url: `${baseUrl}/complete` });
        expect(completion.request.body).toEqual({ code, state: replacementState });
        completion.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
        await expect(finishing).resolves.toBe(true);
        expect(flow.read()).toMatchObject({ userId: replacement.user.id, email: replacementEmail, state: null, failed: false });
    });
});

describe('Telegram backup email HTTP completion after an account change', () => {
    let fixture: BackupEmailHttpFixture;
    beforeEach(() => {
        fixture = createBackupEmailHttpFixture();
    });
    afterEach(() => {
        cleanBackupEmailHttpFixture(fixture);
    });

    it.each([
        { outcome: 'accepted', change: 'an account switch', nextAuth: replacement },
        { outcome: 'rejected', change: 'an account switch', nextAuth: replacement },
        { outcome: 'accepted', change: 'reauthentication', nextAuth: { ...original } },
    ] as const)('keeps the new attempt when the old confirmation is $outcome after $change', async ({ outcome, nextAuth }) => {
        const { auth, http, flow, openAttemptAsync } = fixture;
        const originalState = 'a'.repeat(stateLength);
        const replacementState = 'b'.repeat(stateLength);
        const code = telegramOAuthCode('local-original-confirmation');
        const originalEmail = 'original@example.test';
        const replacementEmail = 'replacement@example.test';
        auth.acceptExternalAuthentication(original);
        await openAttemptAsync(originalEmail, originalState);
        const finishing = flow.handleCallbackAsync(code, telegramOAuthState(originalState), false);
        const confirmation = http.expectOne({ method: 'POST', url: `${baseUrl}/complete` });
        expect(confirmation.request.body).toEqual({ code, state: originalState });

        auth.acceptExternalAuthentication(nextAuth);
        await openAttemptAsync(replacementEmail, replacementState);
        const currentAttempt = flow.read();
        expect(currentAttempt).toMatchObject({ userId: nextAuth.user.id, email: replacementEmail, state: replacementState });

        if (outcome === 'accepted') {
            confirmation.flush(null);
        } else {
            confirmation.flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
        }
        await expect(finishing).resolves.toBe(true);

        expect(auth.getUserId()).toBe(nextAuth.user.id);
        expect(flow.read()).toEqual(currentAttempt);

        const replacementCode = telegramOAuthCode('local-replacement-confirmation');
        const current = flow.handleCallbackAsync(replacementCode, telegramOAuthState(replacementState), false);
        const currentConfirmation = http.expectOne({ method: 'POST', url: `${baseUrl}/complete` });
        expect(currentConfirmation.request.body).toEqual({ code: replacementCode, state: replacementState });
        currentConfirmation.flush(null);
        await expect(current).resolves.toBe(true);
        const confirmed = flow.read();
        expect(confirmed).toMatchObject({ userId: nextAuth.user.id, email: replacementEmail, state: null, failed: false });
        expect(confirmed?.sentAt).toBeTypeOf('number');
    });
});

function backupAccount(id: string, sessionId: string): AuthResponse {
    return {
        sessionId,
        roles: ['User'],
        user: {
            id: entityId<'user'>(id),
            email: null,
            hasPassword: false,
            hasTelegramIdentity: true,
            isActive: true,
            isEmailConfirmed: false,
            pushNotificationsEnabled: true,
            fastingPushNotificationsEnabled: false,
            socialPushNotificationsEnabled: false,
            fastingCheckInReminderHours: 12,
            fastingCheckInFollowUpReminderHours: 20,
        },
    };
}

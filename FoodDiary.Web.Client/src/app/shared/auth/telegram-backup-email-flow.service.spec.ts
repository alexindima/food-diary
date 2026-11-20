import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';
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

describe('Telegram backup email HTTP completion after an account change', () => {
    const baseUrl = `${environment.apiUrls.auth}/telegram/backup-email/oidc`;
    const stateLength = 43;
    const original = backupAccount('original-profile', 'original-browser-session');
    const replacement = backupAccount('replacement-profile', 'replacement-browser-session');
    let auth: AuthService;
    let http: HttpTestingController;
    let flow: TelegramBackupEmailFlowService;

    beforeEach(() => {
        vi.spyOn(window, 'open').mockReturnValue(null);
        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(),
                provideHttpClientTesting(),
                provideRouter([]),
                { provide: LocalizationService, useValue: { applyLanguagePreferenceAsync: vi.fn().mockResolvedValue(undefined) } },
            ],
        });
        auth = TestBed.inject(AuthService);
        http = TestBed.inject(HttpTestingController);
        flow = TestBed.inject(TelegramBackupEmailFlowService);
        flow.clear();
    });

    afterEach(() => {
        flow.clear();
        http.verify();
        localStorage.clear();
        vi.restoreAllMocks();
    });

    async function openAttemptAsync(email: string, state: string): Promise<void> {
        const started = flow.startAsync(email);
        const request = http.expectOne({ method: 'POST', url: `${baseUrl}/start` });
        expect(request.request.body).toEqual({ email });
        request.flush({ authorizationUrl: `https://oauth.telegram.org/auth?state=${state}` });
        await started;
    }

    it.each([
        { outcome: 'accepted', change: 'an account switch', nextAuth: replacement },
        { outcome: 'rejected', change: 'an account switch', nextAuth: replacement },
        { outcome: 'accepted', change: 'reauthentication', nextAuth: { ...original } },
    ] as const)('keeps the new attempt when the old confirmation is $outcome after $change', async ({ outcome, nextAuth }) => {
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

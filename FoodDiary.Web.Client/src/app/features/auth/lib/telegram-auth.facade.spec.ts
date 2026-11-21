import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../services/auth.service';
import type { AuthResponse } from '../../../shared/auth/auth.data';
import { AuthCookieSessionService } from '../../../shared/auth/auth-cookie-session.service';
import { telegramLoginTicket, telegramOAuthCode, telegramOAuthState } from '../../../shared/auth/telegram-oidc-values';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { BrowserStorageService } from '../../../shared/platform/browser-storage.service';
import { BrowserWindowService } from '../../../shared/platform/browser-window.service';
import { TelegramAuthService } from '../api/telegram-auth.service';
import type { TelegramIntent } from '../models/telegram-auth.data';
import { TelegramAuthFacade } from './telegram-auth.facade';

const FUTURE_EXPIRY = '2099-01-01T00:00:00Z';
const TICKET = telegramLoginTicket('abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG');
const intent: TelegramIntent = { ticket: TICKET, nextAction: 'onboarding', expiresAtUtc: FUTURE_EXPIRY };
const response: AuthResponse = {
    accessToken: 'access',
    user: {
        id: entityId<'user'>('user'),
        email: null,
        hasPassword: false,
        isActive: true,
        isEmailConfirmed: false,
        pushNotificationsEnabled: false,
        fastingPushNotificationsEnabled: false,
        socialPushNotificationsEnabled: false,
        fastingCheckInReminderHours: 1,
        fastingCheckInFollowUpReminderHours: 1,
    },
};

const api = {
    configuration: vi.fn(),
    beginMiniApp: vi.fn<TelegramAuthService['beginMiniApp']>(),
    startOidc: vi.fn(),
    exchange: vi.fn(),
    complete: vi.fn(),
};
const auth = { acceptExternalAuthentication: vi.fn() };
const browser = { getTelegramInitData: vi.fn(), getHref: vi.fn(), open: vi.fn(), runWithLockAsync };
const storage = { getItem: vi.fn(), getJson: vi.fn(), setJson: vi.fn(), removeItem: vi.fn() };
let facade: TelegramAuthFacade;

beforeEach(() => {
    vi.resetAllMocks();
    browser.getHref.mockReturnValue(null);
    storage.getItem.mockReturnValue(null);
    TestBed.configureTestingModule({
        providers: [
            TelegramAuthFacade,
            { provide: TelegramAuthService, useValue: api },
            { provide: AuthService, useValue: auth },
            { provide: BrowserWindowService, useValue: browser },
            { provide: BrowserStorageService, useValue: storage },
        ],
    });
    facade = TestBed.inject(TelegramAuthFacade);
});

describe('TelegramAuthFacade', () => {
    it('preserves a checked Mini App proof for the explicit registration or link action', async () => {
        browser.getTelegramInitData.mockReturnValue('signed-init-data');
        api.beginMiniApp.mockReturnValue(of(intent));
        await facade.beginAsync(false);
        expect(api.beginMiniApp).toHaveBeenCalledWith('signed-init-data', false);
        expect(facade.intent()).toEqual(intent);
        expect(api.complete).not.toHaveBeenCalled();
        expect(storage.setJson).toHaveBeenCalledWith('session', 'fooddiary.telegram.intent', intent);
    });

    it('restores the onboarding ticket without silently linking the signed-in account', () => {
        storage.getJson.mockReturnValue(intent);
        facade.restoreIntent();
        expect(facade.intent()).toEqual(intent);
        expect(api.complete).not.toHaveBeenCalled();
    });

    it('removes an expired ticket', () => {
        storage.getJson.mockReturnValue({ ...intent, expiresAtUtc: '2000-01-01T00:00:00Z' });
        facade.restoreIntent();
        expect(facade.intent()).toBeNull();
        expect(storage.removeItem).toHaveBeenCalled();
    });

    it('rejects an unexpected authorization origin', async () => {
        browser.getTelegramInitData.mockReturnValue(null);
        api.startOidc.mockReturnValue(of({ authorizationUrl: 'https://attacker.example/auth' }));
        await facade.beginAsync(false);
        expect(browser.open).not.toHaveBeenCalled();
        expect(facade.errorKey()).toBe('AUTH.TELEGRAM.ERROR');
    });

    it('does not claim that a failed provider exchange expired', async () => {
        api.exchange.mockReturnValue(throwError(() => new Error('Provider metadata could not be read')));
        await facade.exchangeAsync(telegramOAuthCode('code'), telegramOAuthState('state'));
        expect(facade.errorKey()).toBe('AUTH.TELEGRAM.VERIFICATION_FAILED');
        expect(facade.intent()).toBeNull();
        expect(facade.busy()).toBe(false);
    });

    it('accepts a Telegram-only session and clears the used ticket', async () => {
        facade.intent.set(intent);
        api.complete.mockReturnValue(of(response));
        expect(await facade.completeAsync('register', 'ru', 'Asia/Tbilisi')).toBe(true);
        expect(api.complete).toHaveBeenCalledWith(TICKET, 'register', 'ru', 'Asia/Tbilisi');
        expect(auth.acceptExternalAuthentication).toHaveBeenCalledWith(response);
        expect(facade.intent()).toBeNull();
    });
});

describe('TelegramAuthFacade explicit action admission', () => {
    it('rejects a restored ticket at its exact expiry without entering the cookie session', async () => {
        const cookieSession = vi.spyOn(TestBed.inject(AuthCookieSessionService), 'run');
        const expiresAt = Date.parse(intent.expiresAtUtc);
        vi.useFakeTimers({ toFake: ['Date'] });
        try {
            vi.setSystemTime(expiresAt - 1);
            storage.getJson.mockReturnValue(intent);
            facade.restoreIntent();
            expect(facade.intent()).toEqual(intent);
            expect(facade.errorKey()).toBeNull();

            vi.setSystemTime(expiresAt);
            await expect(facade.completeAsync('register', 'ru', 'UTC')).resolves.toBe(false);
            expect(facade.errorKey()).toBe('AUTH.TELEGRAM.EXPIRED');
            expect(facade.busy()).toBe(false);
            expect(cookieSession).not.toHaveBeenCalled();
            expect(api.complete).not.toHaveBeenCalled();
            expect(auth.acceptExternalAuthentication).not.toHaveBeenCalled();
        } finally {
            vi.useRealTimers();
            cookieSession.mockRestore();
        }
    });

    it('keeps the first Mini App start while a duplicate action arrives during its SDK request', async () => {
        const pending = new Subject<TelegramIntent>();
        browser.getTelegramInitData.mockReturnValue('signed-init-data');
        api.beginMiniApp.mockReturnValueOnce(pending).mockReturnValue(of({ ...intent, nextAction: 'link' }));
        const first = facade.beginAsync(false);
        try {
            await vi.waitFor(() => {
                expect(api.beginMiniApp).toHaveBeenCalledExactlyOnceWith('signed-init-data', false);
                expect(pending.observed).toBe(true);
            });
            expect(facade.busy()).toBe(true);

            await facade.beginAsync(true);
            expect(api.beginMiniApp).toHaveBeenCalledTimes(1);
            expect(pending.observed).toBe(true);
            expect(facade.busy()).toBe(true);
            expect(facade.intent()).toBeNull();
            expect(storage.setJson).not.toHaveBeenCalled();
            pending.next(intent);
        } finally {
            pending.complete();
            await first;
        }

        expect(facade.intent()).toEqual(intent);
        expect(storage.setJson).toHaveBeenCalledExactlyOnceWith('session', 'fooddiary.telegram.intent', intent);
        expect(facade.busy()).toBe(false);
        expect(facade.errorKey()).toBeNull();
        expect(api.startOidc).not.toHaveBeenCalled();
        expect(api.complete).not.toHaveBeenCalled();
        expect(browser.open).not.toHaveBeenCalled();
    });
});

async function runWithLockAsync<T>(_name: string, operation: () => Promise<T>): Promise<T> {
    return operation();
}

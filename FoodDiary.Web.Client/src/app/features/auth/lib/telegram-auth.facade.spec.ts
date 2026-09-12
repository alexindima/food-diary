import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../services/auth.service';
import type { AuthResponse } from '../../../shared/auth/auth.data';
import { BrowserStorageService } from '../../../shared/platform/browser-storage.service';
import { BrowserWindowService } from '../../../shared/platform/browser-window.service';
import { TelegramAuthService } from '../api/telegram-auth.service';
import type { TelegramIntent } from '../models/telegram-auth.data';
import { TelegramAuthFacade } from './telegram-auth.facade';

const FUTURE_EXPIRY = '2099-01-01T00:00:00Z';
const TICKET = 'abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG';
const intent: TelegramIntent = { ticket: TICKET, nextAction: 'onboarding', expiresAtUtc: FUTURE_EXPIRY };

describe('TelegramAuthFacade', () => {
    const api = {
        configuration: vi.fn(),
        beginMiniApp: vi.fn(),
        startOidc: vi.fn(),
        exchange: vi.fn(),
        complete: vi.fn(),
    };
    const auth = { acceptExternalAuthentication: vi.fn() };
    const browser = { getTelegramInitData: vi.fn(), getHref: vi.fn(), open: vi.fn() };
    const storage = { getJson: vi.fn(), setJson: vi.fn(), removeItem: vi.fn() };
    let facade: TelegramAuthFacade;

    beforeEach(() => {
        vi.resetAllMocks();
        browser.getHref.mockReturnValue(null);
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

    it('accepts a Telegram-only session and clears the used ticket', async () => {
        const response: AuthResponse = {
            accessToken: 'access',
            refreshToken: 'refresh',
            user: {
                id: 'user',
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
        facade.intent.set(intent);
        api.complete.mockReturnValue(of(response));
        expect(await facade.completeAsync('register', 'ru', 'Asia/Tbilisi')).toBe(true);
        expect(api.complete).toHaveBeenCalledWith(TICKET, 'register', 'ru', 'Asia/Tbilisi');
        expect(auth.acceptExternalAuthentication).toHaveBeenCalledWith(response);
        expect(facade.intent()).toBeNull();
    });
});

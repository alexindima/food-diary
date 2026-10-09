import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { telegramLoginTicket, telegramOAuthCode, telegramOAuthState } from '../../../shared/auth/telegram-oidc-values';
import { TelegramAuthService } from './telegram-auth.service';

describe('Telegram generated transport', () => {
    let service: TelegramAuthService;
    let http: HttpTestingController;
    const base = `${environment.apiUrls.auth}/telegram`;
    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        service = TestBed.inject(TelegramAuthService);
        http = TestBed.inject(HttpTestingController);
    });
    afterEach(() => {
        http.verify();
    });

    it('keeps configuration flags and cookie credentials', () => {
        service.configuration().subscribe(value => {
            expect(value.loginEnabled).toBe(false);
        });
        const request = http.expectOne(`${base}/configuration`);
        expect(request.request.withCredentials).toBe(true);
        request.flush({ loginEnabled: false, registrationEnabled: true, oidcEnabled: true });
    });

    it('starts the selected OIDC flow without a request body', () => {
        service.startOidc(true).subscribe(value => {
            expect(value.authorizationUrl).toBe('https://example.com/authorize');
        });
        const request = http.expectOne(`${base}/oidc/start-link`);
        expect(request.request.body).toBeNull();
        request.flush({ authorizationUrl: 'https://example.com/authorize' });
    });

    it('retains the intent and passes signed Mini App data unchanged', () => {
        service.beginMiniApp('signed-data', false).subscribe(value => {
            expect(value.nextAction).toBe('onboarding');
        });
        const request = http.expectOne(`${base}/mini-app/begin`);
        expect(request.request.body).toEqual({ initData: 'signed-data' });
        request.flush({ ticket: 'ticket', nextAction: 'onboarding', expiresAtUtc: '2026-10-07T00:00:00Z' });
    });

    it('keeps the OAuth callback and completion wire values separate and unchanged', () => {
        const code = telegramOAuthCode('code+special');
        const state = telegramOAuthState('opaque-state');
        const ticket = telegramLoginTicket('abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG');
        service.exchange(code, state).subscribe(intent => {
            expect(intent.ticket).toBe(ticket);
        });
        const exchange = http.expectOne(`${base}/oidc/exchange`);
        expect(exchange.request.body).toEqual({ code: 'code+special', state: 'opaque-state' });
        expect(exchange.request.withCredentials).toBe(true);
        exchange.flush({ ticket, nextAction: 'link', expiresAtUtc: '2099-01-01T00:00:00Z' });

        service.complete(ticket, 'link').subscribe();
        const completion = http.expectOne(`${base}/complete-link`);
        expect(completion.request.body).toEqual({ ticket });
        expect(completion.request.withCredentials).toBe(true);
        completion.flush({
            accessToken: 'access',
            user: {
                id: 'user-1',
                hasPassword: true,
                isActive: true,
                isEmailConfirmed: true,
                pushNotificationsEnabled: true,
                fastingPushNotificationsEnabled: true,
                socialPushNotificationsEnabled: true,
                fastingCheckInReminderHours: 12,
                fastingCheckInFollowUpReminderHours: 20,
            },
        });
    });
});

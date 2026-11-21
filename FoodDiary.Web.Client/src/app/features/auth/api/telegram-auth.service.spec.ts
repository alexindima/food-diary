import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { telegramLoginTicket, telegramOAuthCode, telegramOAuthState } from '../../../shared/auth/telegram-oidc-values';
import { TelegramAuthService } from './telegram-auth.service';

const SESSION_RESPONSE = {
    accessToken: 'fixture-access',
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
};

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

    it.each([
        { link: false, path: 'start' },
        { link: true, path: 'start-link' },
    ])('starts the $path OIDC flow without a request body', ({ link, path }) => {
        service.startOidc(link).subscribe(value => {
            expect(value.authorizationUrl).toBe('https://example.com/authorize');
        });
        const request = http.expectOne({ method: 'POST', url: `${base}/oidc/${path}` });
        expect(request.request.withCredentials).toBe(true);
        expect(request.request.body).toBeNull();
        request.flush({ authorizationUrl: 'https://example.com/authorize' });
    });

    it.each([
        { link: false, path: 'begin', nextAction: 'onboarding' },
        { link: true, path: 'begin-link', nextAction: 'link' },
    ])('retains the intent and passes Mini App data unchanged for $path', ({ link, path, nextAction }) => {
        const initData = 'fixture-init-data';
        service.beginMiniApp(initData, link).subscribe(value => {
            expect(value.nextAction).toBe(nextAction);
        });
        const request = http.expectOne({ method: 'POST', url: `${base}/mini-app/${path}` });
        expect(request.request.withCredentials).toBe(true);
        expect(request.request.body).toEqual({ initData });
        request.flush({ ticket: 'ticket', nextAction, expiresAtUtc: '2026-10-07T00:00:00Z' });
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
        completion.flush(SESSION_RESPONSE);
    });

    it.each(['login', 'register'] as const)('completes %s using the requested language and timezone', action => {
        const ticket = telegramLoginTicket('abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG');
        const language = 'ru';
        const timeZoneId = 'Asia/Tbilisi';
        service.complete(ticket, action, language, timeZoneId).subscribe(session => {
            expect(session.user.id).toBe(SESSION_RESPONSE.user.id);
        });
        const completion = http.expectOne({ method: 'POST', url: `${base}/complete` });
        expect(completion.request.body).toEqual({ ticket, action, language, timeZoneId });
        expect(completion.request.withCredentials).toBe(true);
        completion.flush(SESSION_RESPONSE);
    });
});

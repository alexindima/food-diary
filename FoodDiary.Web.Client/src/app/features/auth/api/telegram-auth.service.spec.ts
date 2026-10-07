import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
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
    afterEach(() => { http.verify(); });

    it('keeps configuration flags and cookie credentials', () => {
        service.configuration().subscribe(value => { expect(value.loginEnabled).toBe(false); });
        const request = http.expectOne(`${base}/configuration`);
        expect(request.request.withCredentials).toBe(true);
        request.flush({ loginEnabled: false, registrationEnabled: true, oidcEnabled: true });
    });

    it('starts the selected OIDC flow without a request body', () => {
        service.startOidc(true).subscribe(value => { expect(value.authorizationUrl).toBe('https://example.com/authorize'); });
        const request = http.expectOne(`${base}/oidc/start-link`);
        expect(request.request.body).toBeNull();
        request.flush({ authorizationUrl: 'https://example.com/authorize' });
    });

    it('retains the intent and passes signed Mini App data unchanged', () => {
        service.beginMiniApp('signed-data', false).subscribe(value => { expect(value.nextAction).toBe('onboarding'); });
        const request = http.expectOne(`${base}/mini-app/begin`);
        expect(request.request.body).toEqual({ initData: 'signed-data' });
        request.flush({ ticket: 'ticket', nextAction: 'onboarding', expiresAtUtc: '2026-10-07T00:00:00Z' });
    });
});

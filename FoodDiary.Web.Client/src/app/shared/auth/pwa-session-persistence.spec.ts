import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { userFixture } from '../../../testing/api-fixtures';
import { AuthService } from '../../services/auth.service';
import { NavigationService } from '../../services/navigation.service';
import { authResponseFromSdk } from '../api/sdk/auth-sdk.mapper';
import { LocalizationService } from '../i18n/localization.service';
import { PwaInstallService } from '../service-worker/pwa-install.service';

const base = environment.apiUrls.auth;
const installed = signal(false);
const response = { sessionId: 'e574a3ec-06a0-4edf-949e-ac5cf0a05d11', rememberMe: false, roles: ['User'], user: userFixture() };
let auth: AuthService;
let requests: HttpTestingController;

beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    installed.set(false);
    TestBed.configureTestingModule({
        providers: [
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: PwaInstallService, useValue: { initialize: vi.fn(), shouldRememberSession: installed, isStandalone: installed } },
            { provide: NavigationService, useValue: { navigateToAuthAsync: vi.fn(), navigateToLandingAsync: vi.fn() } },
            { provide: LocalizationService, useValue: { applyLanguagePreferenceAsync: vi.fn(), clearStoredLanguage: vi.fn() } },
        ],
    });
    auth = TestBed.inject(AuthService);
    requests = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    requests.verify();
    TestBed.resetTestingModule();
    localStorage.clear();
    sessionStorage.clear();
});

describe('PWA persistent authentication', () => {
    it('requests persistence during the first standalone restoration', async () => {
        installed.set(true);
        const restoration = auth.restoreSessionAsync();
        const request = requests.expectOne(candidate => candidate.url === `${base}/session`);
        expect(request.request.params.get('rememberMe')).toBe('true');
        request.flush({ ...response, rememberMe: true });
        await restoration;
        TestBed.tick();
        requests.expectNone(candidate => candidate.url === `${base}/session`);
    });

    it('does not loop when a server cannot promote the existing session', () => {
        auth.acceptExternalAuthentication(authResponseFromSdk(response));
        installed.set(true);
        TestBed.tick();
        requests.expectOne(request => request.url === `${base}/session`).flush(response);
        TestBed.tick();
        requests.expectNone(request => request.url === `${base}/session`);
    });

    it('does not promote an impersonation context after installation', () => {
        auth.acceptExternalAuthentication(authResponseFromSdk({ ...response, impersonationContext: 'support-context' }));
        installed.set(true);
        TestBed.tick();
        requests.expectNone(request => request.url === `${base}/session`);
    });

    it('remembers a password login in an installed PWA without the checkbox', async () => {
        installed.set(true);
        const login = firstValueFrom(auth.login({ email: 'pwa@example.com', password: 'Password123!', rememberMe: false }));
        const request = requests.expectOne(`${base}/login`);
        expect(request.request.body, 'Installed PWA logins must be remembered').toMatchObject({ rememberMe: true });
        request.flush({ ...response, rememberMe: true });
        await login;
    });

    it('always remembers Google independently of the password form', async () => {
        const login = firstValueFrom(auth.loginWithGoogle({ credential: 'test-credential', rememberMe: false }));
        const request = requests.expectOne(`${base}/google`);
        expect(request.request.body, 'Google browser logins must be remembered').toMatchObject({ rememberMe: true });
        request.flush({ ...response, rememberMe: true });
        await login;
    });

    it('promotes an existing browser session when installation is detected', () => {
        auth.acceptExternalAuthentication(authResponseFromSdk(response));
        TestBed.tick();
        requests.expectNone(request => request.url === `${base}/session`);
        installed.set(true);
        TestBed.tick();
        const promotion = requests.expectOne(request => request.url === `${base}/session`);
        expect(promotion.request.params.get('rememberMe')).toBe('true');
        expect(promotion.request.headers.get('X-FoodDiary-Session')).toBe(response.sessionId);
        promotion.flush({ ...response, rememberMe: true });
        TestBed.tick();
        requests.expectNone(request => request.url === `${base}/session`);
    });

    it('keeps the ordinary browser password choice', async () => {
        const login = firstValueFrom(auth.login({ email: 'browser@example.com', password: 'Password123!', rememberMe: false }));
        const request = requests.expectOne(`${base}/login`);
        expect(request.request.body).toMatchObject({ rememberMe: false });
        request.flush(response);
        await login;
        TestBed.tick();
    });
});

import { HTTP_INTERCEPTORS, HttpStatusCode, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { DefaultUrlSerializer, provideRouter } from '@angular/router';
import { provideTranslateService, type TranslationObject } from '@ngx-translate/core';
import { type Observable, of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, type MockInstance, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { SKIP_GLOBAL_LOADING } from '../../constants/global-loading-context.tokens';
import { TelegramAuthFacade } from '../../features/auth/lib/telegram-auth.facade';
import { AuthInterceptor } from '../../interceptor/auth.interceptor';
import { AuthService } from '../../services/auth.service';
import { FrontendObservabilityService } from '../../services/frontend-observability.service';
import { NavigationService } from '../../services/navigation.service';
import type { AuthenticationHttpResponse } from '../api/sdk/generated/model/authentication-http-response';
import { AuthRecoveryStateService } from '../auth/auth-recovery-state.service';
import { BrowserSessionStateService } from '../auth/browser-session-state.service';
import { TelegramWebAppService } from '../auth/telegram-web-app.service';
import { FoodDiaryTranslationLoader } from '../i18n/food-diary-translation.loader';
import { BrowserStorageService } from '../platform/browser-storage.service';
import { BrowserWindowService } from '../platform/browser-window.service';
import { AppBootstrapService } from './app-bootstrap.service';

const base = environment.apiUrls.auth;
const proof = `user=${encodeURIComponent(JSON.stringify({ id: 123, first_name: 'Synthetic & fixture + Кириллица' }))}&auth_date=1793664000&hash=synthetic-signature`;
const launch = `/profile#tgWebAppData=${encodeURIComponent(proof)}&tgWebAppVersion=8.0`;
const restoredSession = {
    sessionId: 'dd281aa8-5d85-4fda-bc28-a7e1fd6085db',
    accessToken: 'synthetic-mini-app-token',
    roles: ['User'],
    user: {
        id: 'restored-user',
        email: 'restored@example.test',
        isActive: true,
        isEmailConfirmed: true,
        hasPassword: true,
        pushNotificationsEnabled: false,
        fastingPushNotificationsEnabled: false,
        socialPushNotificationsEnabled: false,
        fastingCheckInReminderHours: 12,
        fastingCheckInFollowUpReminderHours: 20,
        language: 'ru',
        theme: 'dark',
        uiStyle: 'modern',
        surfaceStyle: 'matte',
    },
} satisfies AuthenticationHttpResponse;
const startupAttributes = ['lang', 'data-i18n-ready', 'data-theme', 'data-ui-style', 'data-surface-style', 'style'] as const;
let requests: HttpTestingController;
let auth: AuthService;
let recovery: AuthRecoveryStateService;
let replaceUrl: MockInstance<BrowserWindowService['replaceCurrentUrl']>;
let initialAttributes: ReadonlyArray<readonly [string, string | null]>;

beforeEach(() => {
    initialAttributes = startupAttributes.map(name => [name, document.documentElement.getAttribute(name)] as const);
    localStorage.clear();
    sessionStorage.clear();
    window.history.replaceState({}, '', launch);
    TestBed.configureTestingModule({
        providers: [
            TelegramAuthFacade,
            provideHttpClient(withInterceptorsFromDi()),
            provideHttpClientTesting(),
            provideRouter([]),
            provideTranslateService(),
            { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
            { provide: FrontendObservabilityService, useValue: { initialize: vi.fn() } },
            { provide: NavigationService, useValue: { navigateToAuthAsync: vi.fn(), navigateToLandingAsync: vi.fn() } },
            {
                provide: FoodDiaryTranslationLoader,
                useValue: { loadApplicationTranslations: (): Observable<TranslationObject> => of({}) },
            },
        ],
    });
    const browser = TestBed.inject(BrowserWindowService);
    // These scenarios exercise session restoration with an initialized native SDK.
    // SDK loading and its ordering are covered by the bootstrap/service tests.
    vi.spyOn(browser, 'getTelegramInitData').mockReturnValue(proof);
    vi.spyOn(browser, 'yieldToBrowserAsync').mockResolvedValue(undefined);
    replaceUrl = vi.spyOn(browser, 'replaceCurrentUrl').mockImplementation(() => {});
    auth = TestBed.inject(AuthService);
    recovery = TestBed.inject(AuthRecoveryStateService);
    requests = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    for (const [name, value] of initialAttributes) {
        if (value === null) {
            document.documentElement.removeAttribute(name);
        } else {
            document.documentElement.setAttribute(name, value);
        }
    }
    requests.verify();
    vi.restoreAllMocks();
    localStorage.clear();
    sessionStorage.clear();
    window.history.replaceState({}, '', '/');
});

async function restoreAsync(status: number, error: string, endpoint = 'refresh'): Promise<void> {
    const startup = TestBed.inject(AppBootstrapService).initializeSessionAsync();
    await vi.waitFor(() => {
        const request = requests.expectOne(`${base}/${endpoint}`);
        expect(request.request.headers.get('X-FoodDiary-Client')).toBe('telegram');
        if (endpoint === 'refresh') {
            expect(request.request.body).toEqual({});
        }
        request.flush({ error }, { status, statusText: 'Session unavailable' });
    });
    await startup;
}

describe('Telegram launch with the real session restoration boundary', () => {
    it('sends the original complete launch proof after Angular normalizes the fragment before the SDK reads it', async () => {
        const setItem = vi.spyOn(TestBed.inject(BrowserStorageService), 'setItem');
        const setJson = vi.spyOn(TestBed.inject(BrowserStorageService), 'setJson');
        await restoreAsync(HttpStatusCode.BadRequest, 'Validation.Required');

        const serializer = new DefaultUrlSerializer();
        const normalized = serializer.serialize(serializer.parse(launch));
        window.history.replaceState({}, '', normalized);
        const lateSdkProof = new URLSearchParams(window.location.hash.slice(1)).get('tgWebAppData');
        expect(new URLSearchParams(lateSdkProof ?? '').has('hash'), 'The fixture must reproduce late SDK parsing loss').toBe(false);
        vi.spyOn(TestBed.inject(BrowserWindowService), 'getTelegramInitData').mockReturnValue(lateSdkProof);

        const begin = TestBed.inject(TelegramAuthFacade).beginAsync(false);
        const request = await vi.waitFor(() => requests.expectOne(`${base}/telegram/mini-app/begin`));
        request.flush({
            ticket: 'abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG',
            nextAction: 'onboarding',
            expiresAtUtc: '2099-01-01T00:00:00Z',
        });
        await begin;
        expect(request.request.body).toEqual({ initData: proof });
        expect(TestBed.inject(TelegramWebAppService).isMiniAppLaunch()).toBe(true);
        for (const call of [...setItem.mock.calls, ...setJson.mock.calls]) {
            expect(JSON.stringify(call)).not.toContain(proof);
        }
    });

    it('continues to signed Telegram authentication when the refresh cookie does not exist', async () => {
        await restoreAsync(HttpStatusCode.BadRequest, 'Validation.Required');
        expect(auth.isAuthReady(), 'Missing Mini App refresh cookie must finish anonymous restoration').toBe(true);
        expect(auth.isAuthenticated()).toBe(false);
        expect(recovery.issue()).toBeNull();
        expect(replaceUrl).toHaveBeenCalledExactlyOnceWith(
            `/auth/telegram?returnUrl=%2Fprofile#tgWebAppData=${encodeURIComponent(proof)}&tgWebAppVersion=8.0`,
        );
    });

    it('keeps the restored account and its confirmed appearance when the optional profile read is unavailable', async () => {
        const startup = TestBed.inject(AppBootstrapService).initializeSessionAsync();
        const refresh = await vi.waitFor(() => requests.expectOne({ method: 'POST', url: `${base}/refresh` }));
        refresh.flush(restoredSession);

        const profile = await vi.waitFor(() => requests.expectOne({ method: 'GET', url: `${environment.apiUrls.users}/info` }));
        expect(profile.request.context.get(SKIP_GLOBAL_LOADING)).toBe(true);
        expect(profile.request.headers.get('Authorization')).toBe(`Bearer ${restoredSession.accessToken}`);
        expect(document.documentElement.getAttribute('lang')).toBe('ru');
        expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
        profile.flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Profile temporarily unavailable' });
        await startup;

        expect(auth.isAuthReady()).toBe(true);
        expect(auth.isAuthenticated()).toBe(true);
        expect(auth.getUserId()).toBe(restoredSession.user.id);
        expect(recovery.issue()).toBeNull();
        expect(replaceUrl).not.toHaveBeenCalled();
        const root = document.documentElement;
        expect([root.lang, root.dataset['theme'], root.dataset['uiStyle'], root.dataset['surfaceStyle']]).toEqual([
            'ru',
            'dark',
            'modern',
            'matte',
        ]);
    });

    it.each([
        [HttpStatusCode.BadRequest, 'Validation.Invalid'],
        [HttpStatusCode.TooManyRequests, 'RateLimit.Exceeded'],
        [HttpStatusCode.ServiceUnavailable, 'Service.Unavailable'],
    ])('preserves recovery instead of restarting sign-in after %s / %s', async (status, error) => {
        await restoreAsync(status, error);
        expect(auth.isAuthReady()).toBe(false);
        expect(recovery.issue()).toBe('restore');
        expect(replaceUrl).not.toHaveBeenCalled();
    });

    it('does not treat a required field error from impersonation restoration as a missing Telegram cookie', async () => {
        TestBed.inject(BrowserSessionStateService).setContext('support-context');
        await restoreAsync(HttpStatusCode.BadRequest, 'Validation.Required', 'session');
        expect(auth.isAuthReady()).toBe(false);
        expect(recovery.issue()).toBe('restore');
        expect(replaceUrl).not.toHaveBeenCalled();
    });
});

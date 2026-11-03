import { HTTP_INTERCEPTORS, HttpStatusCode, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { DefaultUrlSerializer } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, type MockInstance, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { TelegramAuthFacade } from '../../features/auth/lib/telegram-auth.facade';
import { AuthInterceptor } from '../../interceptor/auth.interceptor';
import { AuthService } from '../../services/auth.service';
import { FrontendObservabilityService } from '../../services/frontend-observability.service';
import { NavigationService } from '../../services/navigation.service';
import { UserService } from '../api/user.service';
import { AuthRecoveryStateService } from '../auth/auth-recovery-state.service';
import { BrowserSessionStateService } from '../auth/browser-session-state.service';
import { TelegramWebAppService } from '../auth/telegram-web-app.service';
import { LocalizationService } from '../i18n/localization.service';
import { BrowserStorageService } from '../platform/browser-storage.service';
import { BrowserWindowService } from '../platform/browser-window.service';
import { ThemeService } from '../theme/theme.service';
import { AppBootstrapService } from './app-bootstrap.service';

const base = environment.apiUrls.auth;
const proof = `user=${encodeURIComponent(JSON.stringify({ id: 123, first_name: 'Synthetic & fixture + Кириллица' }))}&auth_date=1793664000&hash=synthetic-signature`;
const launch = `/profile#tgWebAppData=${encodeURIComponent(proof)}&tgWebAppVersion=8.0`;
let requests: HttpTestingController;
let auth: AuthService;
let recovery: AuthRecoveryStateService;
let replaceUrl: MockInstance<BrowserWindowService['replaceCurrentUrl']>;

beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    window.history.replaceState({}, '', launch);
    TestBed.configureTestingModule({
        providers: [
            TelegramAuthFacade,
            provideHttpClient(withInterceptorsFromDi()),
            provideHttpClientTesting(),
            { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
            { provide: FrontendObservabilityService, useValue: { initialize: vi.fn() } },
            { provide: NavigationService, useValue: { navigateToAuthAsync: vi.fn(), navigateToLandingAsync: vi.fn() } },
            {
                provide: LocalizationService,
                useValue: { initializeLocalizationAsync: vi.fn().mockResolvedValue(undefined) },
            },
            { provide: ThemeService, useValue: {} },
            { provide: UserService, useValue: {} },
        ],
    });
    const browser = TestBed.inject(BrowserWindowService);
    vi.spyOn(browser, 'yieldToBrowserAsync').mockResolvedValue(undefined);
    replaceUrl = vi.spyOn(browser, 'replaceCurrentUrl').mockImplementation(() => {});
    auth = TestBed.inject(AuthService);
    recovery = TestBed.inject(AuthRecoveryStateService);
    requests = TestBed.inject(HttpTestingController);
});

afterEach(() => {
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

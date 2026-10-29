import { HTTP_INTERCEPTORS, HttpStatusCode, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, type MockInstance, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { AuthInterceptor } from '../../interceptor/auth.interceptor';
import { AuthService } from '../../services/auth.service';
import { FrontendObservabilityService } from '../../services/frontend-observability.service';
import { NavigationService } from '../../services/navigation.service';
import { UserService } from '../api/user.service';
import { AuthRecoveryStateService } from '../auth/auth-recovery-state.service';
import { BrowserSessionStateService } from '../auth/browser-session-state.service';
import { LocalizationService } from '../i18n/localization.service';
import { BrowserWindowService } from '../platform/browser-window.service';
import { ThemeService } from '../theme/theme.service';
import { AppBootstrapService } from './app-bootstrap.service';

const base = environment.apiUrls.auth;
let requests: HttpTestingController;
let auth: AuthService;
let recovery: AuthRecoveryStateService;
let replaceUrl: MockInstance<BrowserWindowService['replaceCurrentUrl']>;

beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    window.history.replaceState({}, '', '/profile#tgWebAppData=signed-fixture&tgWebAppVersion=8.0');
    TestBed.configureTestingModule({
        providers: [
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
    it('continues to signed Telegram authentication when the refresh cookie does not exist', async () => {
        await restoreAsync(HttpStatusCode.BadRequest, 'Validation.Required');
        expect(auth.isAuthReady(), 'Missing Mini App refresh cookie must finish anonymous restoration').toBe(true);
        expect(auth.isAuthenticated()).toBe(false);
        expect(recovery.issue()).toBeNull();
        expect(replaceUrl).toHaveBeenCalledExactlyOnceWith(
            '/auth/telegram?returnUrl=%2Fprofile#tgWebAppData=signed-fixture&tgWebAppVersion=8.0',
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

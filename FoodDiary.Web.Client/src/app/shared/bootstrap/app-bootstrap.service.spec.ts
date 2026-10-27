import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../services/auth.service';
import { FrontendObservabilityService } from '../../services/frontend-observability.service';
import { UserService } from '../api/user.service';
import { AuthRecoveryStateService } from '../auth/auth-recovery-state.service';
import { TelegramWebAppService } from '../auth/telegram-web-app.service';
import { LocalizationService } from '../i18n/localization.service';
import { BrowserWindowService } from '../platform/browser-window.service';
import { ThemeService } from '../theme/theme.service';
import { AppBootstrapService } from './app-bootstrap.service';

let service: AppBootstrapService;
let authService: {
    isAuthenticated: ReturnType<typeof vi.fn>;
    restoreSessionAsync: ReturnType<typeof vi.fn>;
};
let localizationService: {
    applyLanguagePreferenceAsync: ReturnType<typeof vi.fn>;
    initializeLocalizationAsync: ReturnType<typeof vi.fn>;
    loadApplicationTranslationsAsync: ReturnType<typeof vi.fn>;
};
let themeService: {
    initializeTheme: ReturnType<typeof vi.fn>;
    syncWithUserPreferences: ReturnType<typeof vi.fn>;
};
let userService: {
    getInfoSilently: ReturnType<typeof vi.fn>;
};
let observabilityService: {
    initialize: ReturnType<typeof vi.fn>;
};
let browserWindow: {
    isAvailable: ReturnType<typeof vi.fn>;
    yieldToBrowserAsync: ReturnType<typeof vi.fn<BrowserWindowService['yieldToBrowserAsync']>>;
    getHref: ReturnType<typeof vi.fn>;
    replaceCurrentUrl: ReturnType<typeof vi.fn>;
};
let telegram: { isAutomaticSignInAllowedAsync: ReturnType<typeof vi.fn> };
let recovery: { pendingLogout: ReturnType<typeof vi.fn>; issue: ReturnType<typeof vi.fn> };

beforeEach(() => {
    authService = {
        isAuthenticated: vi.fn().mockReturnValue(true),
        restoreSessionAsync: vi.fn().mockResolvedValue(undefined),
    };
    localizationService = {
        applyLanguagePreferenceAsync: vi.fn().mockResolvedValue(undefined),
        initializeLocalizationAsync: vi.fn().mockResolvedValue(undefined),
        loadApplicationTranslationsAsync: vi.fn().mockResolvedValue(undefined),
    };
    themeService = {
        initializeTheme: vi.fn(),
        syncWithUserPreferences: vi.fn(),
    };
    userService = {
        getInfoSilently: vi.fn().mockReturnValue(of({ language: 'ru', theme: 'leaf', uiStyle: 'modern' })),
    };
    observabilityService = {
        initialize: vi.fn(),
    };
    browserWindow = {
        isAvailable: vi.fn().mockReturnValue(false),
        yieldToBrowserAsync: vi.fn<BrowserWindowService['yieldToBrowserAsync']>().mockResolvedValue(undefined),
        getHref: vi.fn().mockReturnValue('https://fooddiary.example/'),
        replaceCurrentUrl: vi.fn(),
    };
    telegram = { isAutomaticSignInAllowedAsync: vi.fn().mockResolvedValue(false) };
    recovery = { pendingLogout: vi.fn().mockReturnValue(null), issue: vi.fn().mockReturnValue(null) };

    TestBed.configureTestingModule({
        providers: [
            AppBootstrapService,
            { provide: AuthService, useValue: authService },
            { provide: LocalizationService, useValue: localizationService },
            { provide: ThemeService, useValue: themeService },
            { provide: UserService, useValue: userService },
            { provide: FrontendObservabilityService, useValue: observabilityService },
            { provide: BrowserWindowService, useValue: browserWindow },
            { provide: TelegramWebAppService, useValue: telegram },
            { provide: AuthRecoveryStateService, useValue: recovery },
        ],
    });

    service = TestBed.inject(AppBootstrapService);
});

describe('Telegram Mini App bootstrap', () => {
    it('routes a fresh anonymous Mini App launch to Telegram authentication with its proof intact', async () => {
        authService.isAuthenticated.mockReturnValue(false);
        telegram.isAutomaticSignInAllowedAsync.mockResolvedValue(true);
        browserWindow.getHref.mockReturnValue('https://fooddiary.example/#tgWebAppData=signed-proof&tgWebAppVersion=8.0');
        await service.initializeSessionAsync();
        expect(browserWindow.replaceCurrentUrl).toHaveBeenCalledExactlyOnceWith(
            '/auth/telegram#tgWebAppData=signed-proof&tgWebAppVersion=8.0',
        );
        expect(userService.getInfoSilently).not.toHaveBeenCalled();
    });

    it('keeps the destination when a linked user opens settings from the bot', async () => {
        authService.isAuthenticated.mockReturnValue(false);
        telegram.isAutomaticSignInAllowedAsync.mockResolvedValue(true);
        browserWindow.getHref.mockReturnValue('https://fooddiary.example/profile#tgWebAppData=proof');
        await service.initializeSessionAsync();
        expect(browserWindow.replaceCurrentUrl).toHaveBeenCalledWith('/auth/telegram?returnUrl=%2Fprofile#tgWebAppData=proof');
    });

    it.each([
        'https://fooddiary.example/auth/telegram#tgWebAppData=proof',
        'https://fooddiary.example/auth/telegram/callback?code=code&state=state',
        null,
    ])('does not replace an explicit auth URL or an SSR context %s', async href => {
        authService.isAuthenticated.mockReturnValue(false);
        telegram.isAutomaticSignInAllowedAsync.mockResolvedValue(true);
        browserWindow.getHref.mockReturnValue(href);
        await service.initializeSessionAsync();
        expect(browserWindow.replaceCurrentUrl).not.toHaveBeenCalled();
    });

    it('does not replace an already restored Mini App session', async () => {
        telegram.isAutomaticSignInAllowedAsync.mockResolvedValue(true);
        await service.initializeSessionAsync();
        expect(telegram.isAutomaticSignInAllowedAsync).not.toHaveBeenCalled();
        expect(browserWindow.replaceCurrentUrl).not.toHaveBeenCalled();
        expect(userService.getInfoSilently).toHaveBeenCalledOnce();
    });

    it('does not restart sign-in after recovering a pending logout', async () => {
        authService.isAuthenticated.mockReturnValue(false);
        recovery.pendingLogout.mockReturnValueOnce({ telegram: true });
        telegram.isAutomaticSignInAllowedAsync.mockResolvedValue(true);
        await service.initializeSessionAsync();
        expect(telegram.isAutomaticSignInAllowedAsync).not.toHaveBeenCalled();
        expect(browserWindow.replaceCurrentUrl).not.toHaveBeenCalled();
    });

    it.each(['restore', 'logout'])('preserves the %s recovery screen on failures', async issue => {
        authService.isAuthenticated.mockReturnValue(false);
        recovery.issue.mockReturnValue(issue);
        telegram.isAutomaticSignInAllowedAsync.mockResolvedValue(true);
        await service.initializeSessionAsync();
        expect(browserWindow.replaceCurrentUrl).not.toHaveBeenCalled();
        expect(telegram.isAutomaticSignInAllowedAsync).not.toHaveBeenCalled();
    });

    it('leaves an ordinary website visit on its public landing page', async () => {
        authService.isAuthenticated.mockReturnValue(false);
        await service.initializeSessionAsync();
        expect(browserWindow.replaceCurrentUrl).not.toHaveBeenCalled();
    });
});

describe('AppBootstrapService', () => {
    it.each([false, true])(
        'keeps localization, session restoration and app creation in separate browser tasks: authenticated=%s',
        async authenticated => {
            authService.isAuthenticated.mockReturnValue(authenticated);
            const continuePhases: Array<() => void> = [];
            browserWindow.yieldToBrowserAsync.mockImplementation(
                async () =>
                    new Promise<void>(resolve => {
                        continuePhases.push(resolve);
                    }),
            );
            let completed = false;
            const bootstrap = service.initializeSessionAsync().then(() => {
                completed = true;
            });

            await vi.waitFor(() => {
                expect(completed || continuePhases.length > 0).toBe(true);
            });

            expect(localizationService.initializeLocalizationAsync).not.toHaveBeenCalled();
            continuePhases[0]?.();
            await vi.waitFor(() => {
                expect(completed || continuePhases.length > 1).toBe(true);
            });
            expect(localizationService.initializeLocalizationAsync).toHaveBeenCalled();
            expect(authService.restoreSessionAsync).not.toHaveBeenCalled();
            continuePhases[1]?.();
            await vi.waitFor(() => {
                expect(completed || continuePhases.length > 2).toBe(true);
            });
            expect(authService.restoreSessionAsync).toHaveBeenCalled();
            expect(completed, 'App creation must wait for a browser task boundary after synchronous transferred-state initialization').toBe(
                false,
            );
            expect(userService.getInfoSilently).not.toHaveBeenCalled();

            continuePhases[2]?.();
            await bootstrap;
            expect(completed).toBe(true);
            expect(userService.getInfoSilently).toHaveBeenCalledTimes(authenticated ? 1 : 0);
        },
    );

    it('initializes theme and observability through dedicated methods', () => {
        service.initializeTheme();
        service.initializeObservability();

        expect(themeService.initializeTheme).toHaveBeenCalled();
        expect(observabilityService.initialize).toHaveBeenCalled();
    });

    it('initializes authenticated session preferences', async () => {
        await service.initializeSessionAsync();

        expect(localizationService.initializeLocalizationAsync).toHaveBeenCalled();
        expect(authService.restoreSessionAsync).toHaveBeenCalled();
        expect(localizationService.loadApplicationTranslationsAsync).toHaveBeenCalledTimes(2);
        expect(userService.getInfoSilently).toHaveBeenCalled();
        expect(localizationService.applyLanguagePreferenceAsync).toHaveBeenCalledWith('ru');
        expect(themeService.syncWithUserPreferences).toHaveBeenCalledWith('leaf', 'modern', undefined);
    });

    it('skips user preference loading when session is anonymous', async () => {
        authService.isAuthenticated.mockReturnValue(false);

        await service.initializeSessionAsync();

        expect(userService.getInfoSilently).not.toHaveBeenCalled();
        expect(localizationService.loadApplicationTranslationsAsync).not.toHaveBeenCalled();
        expect(themeService.syncWithUserPreferences).not.toHaveBeenCalled();
    });
});

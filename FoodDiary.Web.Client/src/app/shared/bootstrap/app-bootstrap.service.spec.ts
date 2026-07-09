import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../services/auth.service';
import { FrontendObservabilityService } from '../../services/frontend-observability.service';
import { UserService } from '../api/user.service';
import { LocalizationService } from '../i18n/localization.service';
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

    TestBed.configureTestingModule({
        providers: [
            AppBootstrapService,
            { provide: AuthService, useValue: authService },
            { provide: LocalizationService, useValue: localizationService },
            { provide: ThemeService, useValue: themeService },
            { provide: UserService, useValue: userService },
            { provide: FrontendObservabilityService, useValue: observabilityService },
        ],
    });

    service = TestBed.inject(AppBootstrapService);
});

describe('AppBootstrapService', () => {
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
        expect(themeService.syncWithUserPreferences).toHaveBeenCalledWith('leaf', 'modern');
    });

    it('skips user preference loading when session is anonymous', async () => {
        authService.isAuthenticated.mockReturnValue(false);

        await service.initializeSessionAsync();

        expect(userService.getInfoSilently).not.toHaveBeenCalled();
        expect(localizationService.loadApplicationTranslationsAsync).not.toHaveBeenCalled();
        expect(themeService.syncWithUserPreferences).not.toHaveBeenCalled();
    });
});

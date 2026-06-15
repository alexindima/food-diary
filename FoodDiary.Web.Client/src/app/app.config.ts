import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import {
    type ApplicationConfig,
    ErrorHandler,
    inject,
    isDevMode,
    provideAppInitializer,
    provideBrowserGlobalErrorListeners,
    provideZonelessChangeDetection,
} from '@angular/core';
import { provideClientHydration, withEventReplay, withNoIncrementalHydration } from '@angular/platform-browser';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling, withPreloading } from '@angular/router';
import { provideServiceWorker } from '@angular/service-worker';
import { provideTranslateService, TranslateLoader } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

import { environment } from '../environments/environment';
import { routes } from './app.routes';
import { AuthInterceptor } from './interceptor/auth.interceptor';
import { FrontendObservabilityInterceptor } from './interceptor/frontend-observability.interceptor';
import { GlobalLoadingInterceptor } from './interceptor/global-loading.interceptor';
import { RetryInterceptor } from './interceptor/retry.interceptor';
import { AuthService } from './services/auth.service';
import { GlobalErrorHandler } from './services/error-handler.service';
import { FrontendObservabilityService } from './services/frontend-observability.service';
import { IdleSelectivePreloadingStrategy } from './services/idle-selective-preloading.strategy';
import { LoggingApiService } from './services/logging-api.service';
import { UserService } from './shared/api/user.service';
import { FoodDiaryTranslationLoader } from './shared/i18n/food-diary-translation.loader';
import { LocalizationService } from './shared/i18n/localization.service';
import { isMobileShellWindow } from './shared/platform/mobile-shell-runtime';
import { ThemeService } from './shared/theme/theme.service';

const isBrowserEnvironment = typeof window !== 'undefined';
const isMobileShellEnvironment = isBrowserEnvironment && isMobileShellWindow(window);

export const appConfig: ApplicationConfig = {
    providers: [
        ...(environment.enableGlobalErrorHandler === true && isBrowserEnvironment
            ? [
                  provideBrowserGlobalErrorListeners(),
                  {
                      provide: ErrorHandler,
                      useClass: GlobalErrorHandler,
                  },
                  GlobalErrorHandler,
              ]
            : []),
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GlobalLoadingInterceptor,
            multi: true,
        },
        {
            provide: HTTP_INTERCEPTORS,
            useClass: FrontendObservabilityInterceptor,
            multi: true,
        },
        {
            provide: HTTP_INTERCEPTORS,
            useClass: RetryInterceptor,
            multi: true,
        },
        {
            provide: HTTP_INTERCEPTORS,
            useClass: AuthInterceptor,
            multi: true,
        },
        provideAppInitializer(() => {
            inject(ThemeService).initializeTheme();
        }),
        provideAppInitializer(async () => {
            const localizationService = inject(LocalizationService);
            const authService = inject(AuthService);
            const themeService = inject(ThemeService);
            const userService = inject(UserService);

            return localizationService.initializeLocalizationAsync().then(async () => {
                await authService.restoreSessionAsync();

                if (!authService.isAuthenticated()) {
                    return;
                }

                await localizationService.loadApplicationTranslationsAsync();
                const user = await firstValueFrom(userService.getInfoSilently());
                await localizationService.applyLanguagePreferenceAsync(user?.language ?? null);
                await localizationService.loadApplicationTranslationsAsync();
                themeService.syncWithUserPreferences(user?.theme, user?.uiStyle);
            });
        }),
        provideAppInitializer(() => {
            inject(FrontendObservabilityService).initialize();
        }),
        provideZonelessChangeDetection(),
        provideRouter(
            routes,
            withComponentInputBinding(),
            withInMemoryScrolling({
                scrollPositionRestoration: 'top',
            }),
            withPreloading(IdleSelectivePreloadingStrategy),
        ),
        provideHttpClient(withInterceptorsFromDi()),
        FoodDiaryTranslationLoader,
        provideTranslateService({
            loader: { provide: TranslateLoader, useExisting: FoodDiaryTranslationLoader },
        }),
        ...(isBrowserEnvironment
            ? [
                  provideServiceWorker('ngsw-worker.js', {
                      enabled: !isDevMode() && !isMobileShellEnvironment,
                      registrationStrategy: 'registerWhenStable:30000',
                  }),
              ]
            : []),
        FrontendObservabilityService,
        LocalizationService,
        ThemeService,
        LoggingApiService,
        provideClientHydration(withEventReplay(), withNoIncrementalHydration()),
    ],
};

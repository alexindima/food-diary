import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import {
    ApplicationConfig,
    ErrorHandler,
    importProvidersFrom,
    inject,
    isDevMode,
    provideAppInitializer,
    provideBrowserGlobalErrorListeners,
    provideZonelessChangeDetection,
} from '@angular/core';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling, withPreloading } from '@angular/router';
import { routes } from './app.routes';
import { HTTP_INTERCEPTORS, provideHttpClient, withFetch, withInterceptorsFromDi } from '@angular/common/http';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { LocalizationService } from './services/localization.service';
import { AuthService } from './services/auth.service';
import { FrontendObservabilityInterceptor } from './interceptor/frontend-observability.interceptor';
import { RetryInterceptor } from './interceptor/retry.interceptor';
import { AuthInterceptor } from './interceptor/auth.interceptor';
import { GlobalLoadingInterceptor } from './interceptor/global-loading.interceptor';
import { FrontendObservabilityService } from './services/frontend-observability.service';
import { LoggingApiService } from './services/logging-api.service';
import { provideServiceWorker } from '@angular/service-worker';
import { UserService } from './shared/api/user.service';
import { firstValueFrom } from 'rxjs';
import { environment } from '../environments/environment';
import { GlobalErrorHandler } from './services/error-handler.service';
import { IdleSelectivePreloadingStrategy } from './services/idle-selective-preloading.strategy';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { ThemeService } from './services/theme.service';

const isBrowserEnvironment = typeof window !== 'undefined';

export const appConfig: ApplicationConfig = {
    providers: [
        ...(environment.enableGlobalErrorHandler && isBrowserEnvironment
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
        provideAppInitializer(() => {
            const localizationService = inject(LocalizationService);
            const authService = inject(AuthService);
            const themeService = inject(ThemeService);
            const userService = inject(UserService);

            return localizationService.initializeLocalization().then(async () => {
                await authService.restoreSession();

                if (!authService.isAuthenticated()) {
                    return;
                }

                const user = await firstValueFrom(userService.getInfoSilently());
                await localizationService.applyLanguagePreference(user?.language ?? null);
                themeService.syncWithUserPreferences(user?.theme, user?.uiStyle);
            });
        }),
        provideAppInitializer(() => {
            inject(FrontendObservabilityService).initialize();
        }),
        provideAnimationsAsync(),
        provideZonelessChangeDetection(),
        provideRouter(
            routes,
            withComponentInputBinding(),
            withInMemoryScrolling({
                scrollPositionRestoration: 'top',
            }),
            withPreloading(IdleSelectivePreloadingStrategy),
        ),
        provideHttpClient(withInterceptorsFromDi(), withFetch()),
        importProvidersFrom(TranslateModule.forRoot()),
        provideTranslateHttpLoader({
            prefix: './assets/i18n/',
            suffix: `.json?v=${environment.buildVersion ?? 'dev'}`,
        }),
        ...(isBrowserEnvironment
            ? [
                  provideServiceWorker('ngsw-worker.js', {
                      enabled: !isDevMode(),
                      registrationStrategy: 'registerWhenStable:30000',
                  }),
              ]
            : []),
        TranslateService,
        FrontendObservabilityService,
        LocalizationService,
        ThemeService,
        LoggingApiService,
        ...(isBrowserEnvironment ? [provideClientHydration(withEventReplay())] : []),
    ],
};

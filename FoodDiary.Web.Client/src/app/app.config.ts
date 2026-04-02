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
import { PreloadAllModules, provideRouter, withComponentInputBinding, withPreloading } from '@angular/router';
import { routes } from './app.routes';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { LocalizationService } from './services/localization.service';
import { AuthService } from './services/auth.service';
import { RetryInterceptor } from './interceptor/retry.interceptor';
import { AuthInterceptor } from './interceptor/auth.interceptor';
import { LoggingApiService } from './services/logging-api.service';
import { provideServiceWorker } from '@angular/service-worker';
import { FdUiSnackBarModule } from 'fd-ui-kit/material';
import { UserService } from './shared/api/user.service';
import { firstValueFrom } from 'rxjs';
import { environment } from '../environments/environment';
import { GlobalErrorHandler } from './services/error-handler.service';

export const appConfig: ApplicationConfig = {
    providers: [
        ...(environment.enableGlobalErrorHandler
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
            useClass: RetryInterceptor,
            multi: true,
        },
        {
            provide: HTTP_INTERCEPTORS,
            useClass: AuthInterceptor,
            multi: true,
        },
        provideAppInitializer(() => {
            const localizationService = inject(LocalizationService);
            const authService = inject(AuthService);
            const userService = inject(UserService);

            return localizationService.initializeLocalization().then(async () => {
                authService.initializeAuth();

                if (!authService.isAuthenticated()) {
                    return;
                }

                const user = await firstValueFrom(userService.getInfo());
                await localizationService.applyLanguagePreference(user?.language ?? null);
            });
        }),
        provideAnimationsAsync(),
        provideZonelessChangeDetection(),
        provideRouter(routes, withComponentInputBinding(), withPreloading(PreloadAllModules)),
        provideHttpClient(withInterceptorsFromDi()),
        importProvidersFrom(TranslateModule.forRoot(), FdUiSnackBarModule),
        provideTranslateHttpLoader({
            prefix: './assets/i18n/',
            suffix: `.json?v=${environment.buildVersion ?? 'dev'}`,
        }),
        provideServiceWorker('ngsw-worker.js', {
            enabled: !isDevMode(),
            registrationStrategy: 'registerWhenStable:30000',
        }),
        TranslateService,
        LocalizationService,
        LoggingApiService,
    ],
};

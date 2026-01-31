import { provideAnimations } from '@angular/platform-browser/animations';
import {
    ApplicationConfig,
    ErrorHandler,
    importProvidersFrom,
    inject,
    isDevMode,
    provideAppInitializer,
    provideZonelessChangeDetection
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { LocalizationService } from './services/localization.service';
import { AuthService } from './services/auth.service';
import { AuthInterceptor } from './interceptor/auth.interceptor';
import { GlobalErrorHandler } from './services/error-handler.service';
import { LoggingApiService } from './services/logging-api.service';
import { provideServiceWorker } from '@angular/service-worker';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { FdUiSnackBarModule } from 'fd-ui-kit/material';
import { UserService } from './services/user.service';
import { firstValueFrom } from 'rxjs';

export const appConfig: ApplicationConfig = {
    providers: [
        /*{
            provide: ErrorHandler,
            useClass: GlobalErrorHandler,
        },*/
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
        provideAnimations(),
        provideZonelessChangeDetection(),
        provideRouter(routes, withComponentInputBinding()),
        provideHttpClient(withInterceptorsFromDi()),
        importProvidersFrom(
            TranslateModule.forRoot(),
            FdUiSnackBarModule,
        ),
        provideTranslateHttpLoader({
            prefix: './assets/i18n/',
            suffix: '.json',
        }),
        provideCharts(withDefaultRegisterables()),
        provideServiceWorker('ngsw-worker.js', {
            enabled: !isDevMode(),
            registrationStrategy: 'registerWhenStable:30000'
        }),
        TranslateService,
        LocalizationService,
        LoggingApiService,
    ],
};

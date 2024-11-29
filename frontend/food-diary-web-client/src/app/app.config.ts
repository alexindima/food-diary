import { NG_EVENT_PLUGINS } from '@taiga-ui/event-plugins';
import { provideAnimations } from '@angular/platform-browser/animations';
import { APP_INITIALIZER, ApplicationConfig, ErrorHandler, importProvidersFrom, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { HTTP_INTERCEPTORS, HttpClient, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { LocalizationService } from './services/localization.service';
import { AuthService } from './services/auth.service';
import { AuthInterceptor } from './interceptor/auth.interceptor';
import { GlobalErrorHandler } from './services/error-handler.service';
import { LoggingApiService } from './services/logging-api.service';

export function initializeLocalization(localizationService: LocalizationService) {
    return (): void => localizationService.initializeLocalization();
}

export function initializeAuthFactory(authService: AuthService) {
    return (): void => authService.initializeAuth();
}

export function HttpLoaderFactory(http: HttpClient): TranslateHttpLoader {
    return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}

export const appConfig: ApplicationConfig = {
    providers: [
        {
            provide: ErrorHandler,
            useClass: GlobalErrorHandler,
        },
        {
            provide: APP_INITIALIZER,
            useFactory: initializeLocalization,
            deps: [LocalizationService],
            multi: true,
        },
        {
            provide: APP_INITIALIZER,
            useFactory: initializeAuthFactory,
            deps: [AuthService],
            multi: true,
        },
        {
            provide: HTTP_INTERCEPTORS,
            useClass: AuthInterceptor,
            multi: true,
        },
        provideAnimations(),
        provideZoneChangeDetection({ eventCoalescing: true }),
        provideRouter(routes, withComponentInputBinding()),
        NG_EVENT_PLUGINS,
        provideHttpClient(withInterceptorsFromDi()),
        importProvidersFrom(
            TranslateModule.forRoot({
                loader: {
                    provide: TranslateLoader,
                    useFactory: HttpLoaderFactory,
                    deps: [HttpClient],
                },
            }),
        ),
        LocalizationService,
        TranslateService,
        LoggingApiService,
    ],
};

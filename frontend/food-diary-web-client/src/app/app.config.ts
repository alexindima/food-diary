import { NG_EVENT_PLUGINS } from '@taiga-ui/event-plugins';
import { provideAnimations } from '@angular/platform-browser/animations';
import {
    APP_INITIALIZER,
    ApplicationConfig,
    importProvidersFrom,
    provideZoneChangeDetection
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { HttpClient, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { AppConfigService } from './services/app-config.service';
import { Observable } from 'rxjs';
import { AppConfig } from './types/app.data';

export function HttpLoaderFactory(http: HttpClient): TranslateHttpLoader {
    return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}

export function loadAppConfig(appConfig: AppConfigService) {
    return (): Observable<AppConfig> => appConfig.loadConfig();
}

export const appConfig: ApplicationConfig = {
    providers: [
        provideAnimations(),
        provideZoneChangeDetection({ eventCoalescing: true }),
        provideRouter(routes),
        {
            provide: APP_INITIALIZER,
            useFactory: loadAppConfig,
            deps: [AppConfigService],
            multi: true
        },
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
        )
    ],
};

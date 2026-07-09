import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { type ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideTranslateService, TranslateLoader } from '@ngx-translate/core';

import { environment } from '../environments/environment';
import { routes } from './app.routes';
import { adminAuthInterceptor } from './features/admin-auth/lib/admin-auth.interceptor';
import { AdminTranslationLoader } from './shared/i18n/admin-translation.loader';

export const appConfig: ApplicationConfig = {
    providers: [
        ...(environment.enableGlobalErrorHandler ? [provideBrowserGlobalErrorListeners()] : []),
        provideHttpClient(withInterceptors([adminAuthInterceptor])),
        provideRouter(routes),
        AdminTranslationLoader,
        provideTranslateService({
            lang: 'en',
            fallbackLang: 'en',
            loader: { provide: TranslateLoader, useExisting: AdminTranslationLoader },
        }),
    ],
};

import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { type ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';

import { environment } from '../environments/environment';
import { routes } from './app.routes';
import { adminAuthInterceptor } from './features/admin-auth/lib/admin-auth.interceptor';

export const appConfig: ApplicationConfig = {
    providers: [
        ...(environment.enableGlobalErrorHandler ? [provideBrowserGlobalErrorListeners()] : []),
        provideHttpClient(withInterceptors([adminAuthInterceptor])),
        provideRouter(routes),
    ],
};

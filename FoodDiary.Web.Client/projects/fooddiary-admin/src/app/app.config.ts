import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { adminAuthInterceptor } from './features/admin-auth/lib/admin-auth.interceptor';
import { environment } from '../environments/environment';

export const appConfig: ApplicationConfig = {
    providers: [
        ...(environment.enableGlobalErrorHandler ? [provideBrowserGlobalErrorListeners()] : []),
        provideHttpClient(withInterceptors([adminAuthInterceptor])),
        provideRouter(routes),
    ],
};

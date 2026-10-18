import { type EnvironmentProviders, inject, isDevMode, provideAppInitializer, type Provider } from '@angular/core';

import { ADMIN_DEVELOPMENT_SCREENS } from './admin-development-screen';
import { AdminWebmcpService } from './admin-webmcp.service';

export const ADMIN_WEBMCP_PROVIDERS: Array<Provider | EnvironmentProviders> = isDevMode()
    ? [
          { provide: ADMIN_DEVELOPMENT_SCREENS, useExisting: AdminWebmcpService },
          provideAppInitializer(() => {
              inject(AdminWebmcpService).initialize();
          }),
      ]
    : [];

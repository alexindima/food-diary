import { type EnvironmentProviders, inject, isDevMode, provideAppInitializer, type Provider } from '@angular/core';

import { ADMIN_DEVELOPMENT_SCREENS } from './admin-development-screen';
import { AdminWebmcpService } from './admin-webmcp.service';

export function provideAdminWebmcp(): Array<Provider | EnvironmentProviders> {
    return isDevMode()
        ? [
              { provide: ADMIN_DEVELOPMENT_SCREENS, useExisting: AdminWebmcpService },
              provideAppInitializer(() => {
                  inject(AdminWebmcpService).initialize();
              }),
          ]
        : [];
}

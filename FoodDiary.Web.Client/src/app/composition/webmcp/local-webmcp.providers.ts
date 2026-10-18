import { type EnvironmentProviders, inject, isDevMode, provideAppInitializer, type Provider } from '@angular/core';

import { MEAL_DEVELOPMENT_TOOLS } from '../../features/meals/contracts/meal-development-tools';
import { DEVELOPMENT_REQUESTS, DEVELOPMENT_SCREENS } from '../../shared/development/development-tools';
import { LocalWebmcpPilotService } from './local-webmcp-pilot.service';
import { LocalWebmcpRequestsService } from './local-webmcp-requests.service';
import { LocalWebmcpScreensService } from './local-webmcp-screens.service';

export const LOCAL_WEBMCP_PROVIDERS: Array<Provider | EnvironmentProviders> = isDevMode()
    ? [
          { provide: MEAL_DEVELOPMENT_TOOLS, useExisting: LocalWebmcpPilotService },
          { provide: DEVELOPMENT_SCREENS, useExisting: LocalWebmcpScreensService },
          { provide: DEVELOPMENT_REQUESTS, useExisting: LocalWebmcpRequestsService },
          provideAppInitializer(() => {
              inject(LocalWebmcpPilotService).initialize();
          }),
      ]
    : [];

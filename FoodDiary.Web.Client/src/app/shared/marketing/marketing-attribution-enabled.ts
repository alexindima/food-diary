import { InjectionToken } from '@angular/core';

import { environment } from '../../../environments/environment';

export const MARKETING_ATTRIBUTION_ENABLED = new InjectionToken<boolean>('MARKETING_ATTRIBUTION_ENABLED', {
    factory: (): boolean => environment.enableMarketingAttribution === true,
});

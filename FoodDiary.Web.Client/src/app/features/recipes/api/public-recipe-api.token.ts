import { InjectionToken } from '@angular/core';

import { environment } from '../../../../environments/environment';

export const PUBLIC_RECIPE_API_URL = new InjectionToken<string>('PUBLIC_RECIPE_API_URL', {
    providedIn: 'root',
    factory: (): string => `${environment.apiUrls.recipes}/public`,
});

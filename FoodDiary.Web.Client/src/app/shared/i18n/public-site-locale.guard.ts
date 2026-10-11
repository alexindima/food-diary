import { inject } from '@angular/core';
import type { CanActivateFn } from '@angular/router';

import { publicSiteLocale, stripPublicLocale } from '../../config/public-site-paths';
import { AuthService } from '../../services/auth.service';
import { LocalizationService } from './localization.service';

export const publicSiteLocaleGuard: CanActivateFn = async (_route, state) => {
    const localization = inject(LocalizationService);
    const auth = inject(AuthService);
    // The landing auth guard redirects signed-in visitors to their dashboard.
    // Preserve their account language during that redirect.
    if (stripPublicLocale(state.url) === '/' && auth.isAuthenticated()) {
        return true;
    }
    await localization.applyLanguagePreferenceAsync(publicSiteLocale(state.url));
    await localization.loadRouteTranslationsAsync(state.url);
    return true;
};

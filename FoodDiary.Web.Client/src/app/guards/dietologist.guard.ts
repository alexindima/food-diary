import { inject } from '@angular/core';
import { type CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../services/auth.service';
import { NavigationService } from '../services/navigation.service';
import { LocalizationService } from '../shared/i18n/localization.service';

export const dietologistGuard: CanActivateFn = async (_route, state) => {
    const authService = inject(AuthService);
    const navigationService = inject(NavigationService);
    const localizationService = inject(LocalizationService);
    const router = inject(Router);

    if (!authService.isAuthenticated()) {
        await navigationService.navigateToAuthAsync('login', state.url);
        return false;
    }

    if (!authService.isEmailConfirmed()) {
        await navigationService.navigateToEmailVerificationPendingAsync();
        return false;
    }

    if (!authService.isDietologist()) {
        return router.createUrlTree(['/']);
    }

    await localizationService.loadApplicationTranslationsAsync();
    return true;
};

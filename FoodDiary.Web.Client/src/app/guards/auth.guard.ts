import { inject } from '@angular/core';
import type { CanActivateFn } from '@angular/router';

import { AuthService } from '../services/auth.service';
import { NavigationService } from '../services/navigation.service';
import { LocalizationService } from '../shared/i18n/localization.service';

export const authGuard: CanActivateFn = async (_route, state) => {
    const authService = inject(AuthService);
    const navigationService = inject(NavigationService);
    const localizationService = inject(LocalizationService);
    await authService.ensureSessionReadyAsync();

    if (authService.isAuthenticated()) {
        if (authService.mustChangePassword()) {
            await navigationService.navigateToRequiredPasswordChangeAsync();
            return false;
        }
        if (!authService.isEmailConfirmed()) {
            await navigationService.navigateToEmailVerificationPendingAsync();
            return false;
        }
        await localizationService.loadApplicationTranslationsAsync();
        return true;
    }

    await navigationService.navigateToAuthAsync('login', state.url);
    return false;
};

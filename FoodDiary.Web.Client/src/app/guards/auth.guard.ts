import { inject } from '@angular/core';
import type { CanActivateFn } from '@angular/router';

import { AuthService } from '../services/auth.service';
import { NavigationService } from '../services/navigation.service';

export const authGuard: CanActivateFn = async (_route, state) => {
    const authService = inject(AuthService);
    const navigationService = inject(NavigationService);
    await authService.ensureSessionReadyAsync();

    if (authService.isAuthenticated()) {
        if (!authService.isEmailConfirmed()) {
            await navigationService.navigateToEmailVerificationPendingAsync();
            return false;
        }
        return true;
    }

    await navigationService.navigateToAuthAsync('login', state.url);
    return false;
};

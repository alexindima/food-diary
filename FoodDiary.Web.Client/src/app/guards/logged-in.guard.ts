import { inject } from '@angular/core';
import { type CanActivateFn } from '@angular/router';

import { AuthService } from '../services/auth.service';
import { NavigationService } from '../services/navigation.service';

export const loggedInGuard: CanActivateFn = async (_route, state) => {
    const authService = inject(AuthService);
    const navigationService = inject(NavigationService);
    await authService.ensureSessionReadyAsync();

    if (!authService.isAuthenticated()) {
        return true;
    }

    if (state.url.includes('adminReturnUrl=')) {
        return true;
    }

    await navigationService.navigateToHomeAsync();
    return false;
};

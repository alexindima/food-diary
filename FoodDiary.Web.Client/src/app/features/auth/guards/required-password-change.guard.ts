import { inject } from '@angular/core';
import type { CanActivateFn } from '@angular/router';

import { AuthService } from '../../../services/auth.service';
import { NavigationService } from '../../../services/navigation.service';

export const requiredPasswordChangeGuard: CanActivateFn = async () => {
    const authService = inject(AuthService);
    const navigationService = inject(NavigationService);
    await authService.ensureSessionReadyAsync();

    if (!authService.isAuthenticated()) {
        await navigationService.navigateToAuthAsync('login');
        return false;
    }

    if (!authService.mustChangePassword()) {
        await navigationService.navigateToHomeAsync();
        return false;
    }

    return true;
};

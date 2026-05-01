import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../services/auth.service';
import { NavigationService } from '../services/navigation.service';

export const dietologistGuard: CanActivateFn = async (_route, state) => {
    const authService = inject(AuthService);
    const navigationService = inject(NavigationService);
    const router = inject(Router);

    if (!authService.isAuthenticated()) {
        await navigationService.navigateToAuth('login', state.url);
        return false;
    }

    if (!authService.isEmailConfirmed()) {
        await navigationService.navigateToEmailVerificationPending();
        return false;
    }

    if (!authService.isDietologist()) {
        return router.createUrlTree(['/']);
    }

    return true;
};

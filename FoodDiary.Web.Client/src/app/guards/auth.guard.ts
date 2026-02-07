import { CanActivateFn } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { NavigationService } from '../services/navigation.service';

export const authGuard: CanActivateFn = async (_route, state) => {
    const authService = inject(AuthService);
    const navigationService = inject(NavigationService);

    if (authService.isAuthenticated()) {
        if (!authService.isEmailConfirmed()) {
            await navigationService.navigateToEmailVerificationPending();
            return false;
        }
        return true;
    }

    await navigationService.navigateToAuth('login', state.url);
    return false;
};

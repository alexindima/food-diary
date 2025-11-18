import { CanActivateFn } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { NavigationService } from '../services/navigation.service';

export const loggedInGuard: CanActivateFn = async (_route, _state) => {
    const authService = inject(AuthService);
    const navigationService = inject(NavigationService);

    if (!authService.isAuthenticated()) {
        return true;
    }

    await navigationService.navigateToHome();
    return false;
};

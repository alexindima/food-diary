import { inject } from '@angular/core';
import { type CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../services/auth.service';

export const loggedInGuard: CanActivateFn = async () => {
    const authService = inject(AuthService);
    const router = inject(Router);
    await authService.ensureSessionReadyAsync();

    if (!authService.isAuthenticated()) {
        return true;
    }

    return router.createUrlTree(['/dashboard']);
};

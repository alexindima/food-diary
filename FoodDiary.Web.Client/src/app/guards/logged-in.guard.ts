import { inject } from '@angular/core';
import { type CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../services/auth.service';

export const loggedInGuard: CanActivateFn = async (_route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);
    await authService.ensureSessionReadyAsync();

    if (!authService.isAuthenticated()) {
        return true;
    }

    const params = new URL(state.url, 'http://localhost').searchParams;
    if (params.get('auth') === 'login' && (params.get('adminReturnUrl')?.trim().length ?? 0) > 0) {
        return true;
    }

    return router.createUrlTree(['/dashboard']);
};

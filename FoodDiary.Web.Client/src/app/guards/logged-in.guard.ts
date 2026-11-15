import { inject } from '@angular/core';
import { type CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../services/auth.service';

export const loggedInGuard: CanActivateFn = async (_route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);
    const params = new URL(state.url, 'http://localhost').searchParams;
    const adminReturnUrl = params.get('adminReturnUrl')?.trim() ?? '';
    if (adminReturnUrl.length > 0 && params.get('auth') === 'login') {
        return router.createUrlTree(['/auth/confirm-admin'], { queryParams: { adminReturnUrl } });
    }
    await authService.ensureSessionReadyAsync();
    return authService.isAuthenticated() ? router.createUrlTree(['/dashboard']) : true;
};

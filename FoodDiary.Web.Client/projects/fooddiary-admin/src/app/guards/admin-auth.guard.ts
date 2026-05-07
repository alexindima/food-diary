import { inject } from '@angular/core';
import { type CanActivateFn, Router } from '@angular/router';

import { AdminAuthService } from '../features/admin-auth/lib/admin-auth.service';

export const adminAuthGuard: CanActivateFn = async (_route, state) => {
    const authService = inject(AdminAuthService);
    const router = inject(Router);

    await authService.applySsoFromQueryAsync();
    authService.refreshTokenState();

    if (!authService.isAuthenticated()) {
        return router.createUrlTree(['/unauthorized'], {
            queryParams: { reason: 'unauthenticated', returnUrl: state.url },
        });
    }

    if (!authService.isAdmin()) {
        const upgraded = await authService.tryUpgradeToAdminAsync();
        authService.refreshTokenState();

        if (upgraded && authService.isAdmin()) {
            return true;
        }

        return router.createUrlTree(['/unauthorized'], {
            queryParams: { reason: 'forbidden', returnUrl: state.url },
        });
    }

    return true;
};

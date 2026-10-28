import { inject } from '@angular/core';
import { type CanActivateFn, Router, type UrlTree } from '@angular/router';

import { AdminAuthService } from '../features/admin-auth/contracts/admin-session';

export const adminAuthGuard: CanActivateFn = async (_route, state) => {
    const authService = inject(AdminAuthService);
    const router = inject(Router);

    await authService.applySsoFromQueryAsync();
    const destination = sanitizedAdminDestination(router, state.url);
    const returnUrl = destination === true ? state.url : destination.toString();

    if (authService.sessionUnavailable()) {
        return router.createUrlTree(['/unauthorized'], {
            queryParams: { reason: 'unavailable', returnUrl },
        });
    }

    if (!authService.isAuthenticated()) {
        return router.createUrlTree(['/unauthorized'], {
            queryParams: { reason: 'unauthenticated', returnUrl },
        });
    }

    if (!authService.isAdmin()) {
        const upgraded = await authService.tryUpgradeToAdminAsync();

        if (authService.sessionUnavailable()) {
            return router.createUrlTree(['/unauthorized'], {
                queryParams: { reason: 'unavailable', returnUrl },
            });
        }

        if (upgraded && authService.isAdmin()) {
            return destination;
        }

        return router.createUrlTree(['/unauthorized'], {
            queryParams: { reason: 'forbidden', returnUrl },
        });
    }

    return destination;
};

function sanitizedAdminDestination(router: Router, url: string): true | UrlTree {
    const destination = router.parseUrl(url);
    const fragmentParams = new URLSearchParams(destination.fragment ?? '');
    if (!Object.hasOwn(destination.queryParams, 'code') && !fragmentParams.has('code')) {
        return true;
    }

    delete destination.queryParams['code'];
    if (fragmentParams.has('code')) {
        fragmentParams.delete('code');
        const nextFragment = fragmentParams.toString();
        destination.fragment = nextFragment.length > 0 ? nextFragment : null;
    }

    return destination;
}

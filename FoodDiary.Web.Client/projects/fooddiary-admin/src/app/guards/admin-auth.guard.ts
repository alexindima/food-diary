import { inject } from '@angular/core';
import { type CanActivateFn, Router, type UrlTree } from '@angular/router';

import { AdminAuthService } from '../features/admin-auth/contracts/admin-session';
import { adminSignInAccount, adminSignInAccountFragment } from '../features/admin-auth/contracts/admin-sign-in-navigation';

export const adminAuthGuard: CanActivateFn = async (_route, state) => {
    const authService = inject(AdminAuthService);
    const router = inject(Router);

    const tree = router.parseUrl(state.url);
    const fragments = new URLSearchParams(tree.fragment ?? '');
    const account = adminSignInAccount(tree.fragment);
    const fragment = adminSignInAccountFragment(account);
    const exchanged = fragments.has('code') || Object.hasOwn(tree.queryParams, 'code');
    await authService.applySsoFromQueryAsync();
    const destination = sanitizedAdminDestination(tree, fragments);
    const returnUrl = destination === true ? state.url : destination.toString();
    const signIn = (): UrlTree =>
        router.createUrlTree(['/sign-in'], {
            queryParams: { returnUrl, confirm: exchanged ? '1' : '0' },
            fragment,
        });

    if (authService.sessionUnavailable()) {
        return router.createUrlTree(['/unauthorized'], {
            queryParams: { reason: 'unavailable', returnUrl },
            fragment,
        });
    }

    if (!authService.isAuthenticated()) {
        return authService.ssoRateLimited()
            ? router.createUrlTree(['/unauthorized'], {
                  queryParams: { reason: 'unauthenticated', returnUrl },
                  fragment,
              })
            : signIn();
    }

    if (account !== null && authService.getUserId() !== account) {
        return signIn();
    }

    return resolveAdminRoleAsync(authService, router, returnUrl, destination);
};

async function resolveAdminRoleAsync(
    auth: AdminAuthService,
    router: Router,
    returnUrl: string,
    destination: true | UrlTree,
): Promise<true | UrlTree> {
    if (auth.isAdmin() || (await auth.tryUpgradeToAdminAsync())) {
        return destination;
    }
    if (auth.sessionUnavailable()) {
        return router.createUrlTree(['/unauthorized'], {
            queryParams: { reason: 'unavailable', returnUrl },
        });
    }
    return router.createUrlTree(['/unauthorized'], { queryParams: { reason: 'forbidden', returnUrl } });
}

function sanitizedAdminDestination(destination: UrlTree, fragmentParams: URLSearchParams): true | UrlTree {
    const hasHandoffFragment = fragmentParams.has('code') || fragmentParams.has('account');
    if (!hasHandoffFragment && !Object.hasOwn(destination.queryParams, 'code')) {
        return true;
    }

    delete destination.queryParams['code'];
    if (hasHandoffFragment) {
        fragmentParams.delete('code');
        fragmentParams.delete('account');
        const nextFragment = fragmentParams.toString();
        destination.fragment = nextFragment.length > 0 ? nextFragment : null;
    }

    return destination;
}

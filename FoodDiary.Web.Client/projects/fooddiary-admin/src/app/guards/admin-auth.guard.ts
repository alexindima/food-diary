import { inject } from '@angular/core';
import { type CanActivateFn, Router, type UrlTree } from '@angular/router';

import { AdminAuthService } from '../features/admin-auth/contracts/admin-session';
import { adminSignInAccount, adminSignInAccountFragment } from '../features/admin-auth/contracts/admin-sign-in-navigation';

type UnauthorizedReason = 'unavailable' | 'unauthenticated' | 'forbidden';

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
    const unauthorized = (reason: UnauthorizedReason): UrlTree =>
        router.createUrlTree(['/unauthorized'], { queryParams: { reason, returnUrl }, fragment });

    if (authService.sessionUnavailable()) {
        return unauthorized('unavailable');
    }

    if (!authService.isAuthenticated()) {
        return authService.ssoRateLimited() ? unauthorized('unauthenticated') : signIn();
    }

    if (account !== null && authService.getUserId() !== account) {
        return signIn();
    }

    return resolveAdminRoleAsync(authService, destination, unauthorized);
};

async function resolveAdminRoleAsync(
    auth: AdminAuthService,
    destination: true | UrlTree,
    unauthorized: (reason: UnauthorizedReason) => UrlTree,
): Promise<true | UrlTree> {
    if (auth.isAdmin() || (await auth.tryUpgradeToAdminAsync())) {
        return destination;
    }
    return unauthorized(auth.sessionUnavailable() ? 'unavailable' : 'forbidden');
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

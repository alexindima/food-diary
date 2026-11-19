import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, DefaultUrlSerializer, Router, RouterStateSnapshot } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AdminAuthService } from '../features/admin-auth/contracts/admin-session';
import { adminAuthGuard } from './admin-auth.guard';

let authService: {
    applySsoFromQueryAsync: ReturnType<typeof vi.fn>;
    isAuthenticated: ReturnType<typeof vi.fn>;
    isAdmin: ReturnType<typeof vi.fn>;
    tryUpgradeToAdminAsync: ReturnType<typeof vi.fn>;
    getUserId: ReturnType<typeof vi.fn>;
    ssoRateLimited: ReturnType<typeof signal<boolean>>;
    sessionUnavailable: ReturnType<typeof signal<boolean>>;
};
let router: {
    createUrlTree: ReturnType<typeof vi.fn>;
    parseUrl: ReturnType<typeof vi.fn>;
};
let route: ActivatedRouteSnapshot;
let state: RouterStateSnapshot;

beforeEach(() => {
    authService = {
        applySsoFromQueryAsync: vi.fn(),
        isAuthenticated: vi.fn(),
        isAdmin: vi.fn(),
        tryUpgradeToAdminAsync: vi.fn(),
        getUserId: vi.fn(),
        ssoRateLimited: signal(false),
        sessionUnavailable: signal(false),
    };
    authService.applySsoFromQueryAsync.mockResolvedValue(void 0);
    authService.tryUpgradeToAdminAsync.mockResolvedValue(false);

    router = {
        createUrlTree: vi.fn(),
        parseUrl: vi.fn((url: string) => new DefaultUrlSerializer().parse(url)),
    };
    router.createUrlTree.mockImplementation((commands: unknown[], extras: unknown) => ({ commands, extras }));

    TestBed.configureTestingModule({
        providers: [
            { provide: AdminAuthService, useValue: authService },
            { provide: Router, useValue: router },
        ],
    });

    route = new ActivatedRouteSnapshot();
    state = Object.assign(Object.create(RouterStateSnapshot.prototype), { url: '/users?page=2' }) as RouterStateSnapshot;
});

describe('admin rejected SSO recovery', () => {
    it.each([
        ['/users?page=2#code=rejected-code', '/users?page=2'],
        ['/users?page=2&code=rejected-code#details', '/users?page=2#details'],
    ])('does not offer an already attempted SSO code for recovery: %s', async (url, returnUrl) => {
        authService.isAuthenticated.mockReturnValue(false);
        state = Object.assign(Object.create(RouterStateSnapshot.prototype), { url }) as RouterStateSnapshot;

        await TestBed.runInInjectionContext(async () => adminAuthGuard(route, state));

        expect(router.createUrlTree, 'Rejected SSO codes must not be exchanged again by the recovery page').toHaveBeenCalledWith(
            ['/sign-in'],
            { queryParams: { returnUrl, confirm: '1' }, fragment: undefined },
        );
    });
});

describe('adminAuthGuard', () => {
    it.each([false, true])('routes unavailable restoration to recovery even with known metadata %s', async known => {
        authService.isAuthenticated.mockReturnValue(known);
        authService.isAdmin.mockReturnValue(known);
        authService.sessionUnavailable.set(true);
        await TestBed.runInInjectionContext(async () => adminAuthGuard(route, state));
        expect(router.createUrlTree, 'An unavailable session must use the recovery reason').toHaveBeenCalledWith(['/unauthorized'], {
            queryParams: { reason: 'unavailable', returnUrl: state.url },
        });
    });

    it('should allow authenticated admin', async () => {
        authService.isAuthenticated.mockReturnValue(true);
        authService.isAdmin.mockReturnValue(true);

        const result = await TestBed.runInInjectionContext(async () => adminAuthGuard(route, state));

        expect(result).toBe(true);
        expect(authService.applySsoFromQueryAsync).toHaveBeenCalled();
    });

    it('tries diary SSO for a missing administrative cookie', async () => {
        authService.isAuthenticated.mockReturnValue(false);

        const result = await TestBed.runInInjectionContext(async () => adminAuthGuard(route, state));

        expect(router.createUrlTree).toHaveBeenCalledWith(['/sign-in'], {
            queryParams: { returnUrl: '/users?page=2', confirm: '0' },
            fragment: undefined,
        });
        expect(result).toEqual({
            commands: ['/sign-in'],
            extras: { queryParams: { returnUrl: '/users?page=2', confirm: '0' }, fragment: undefined },
        });
    });

    it('lands on recovery after a throttled handoff and keeps the return route and account', async () => {
        authService.isAuthenticated.mockReturnValue(false);
        authService.ssoRateLimited.set(true);
        state = Object.assign(Object.create(RouterStateSnapshot.prototype), {
            url: '/users?page=2#account=diary-admin&code=one-time-code',
        }) as RouterStateSnapshot;

        await TestBed.runInInjectionContext(async () => adminAuthGuard(route, state));

        expect(router.createUrlTree).toHaveBeenCalledExactlyOnceWith(['/unauthorized'], {
            queryParams: { reason: 'unauthenticated', returnUrl: '/users?page=2' },
            fragment: 'account=diary-admin',
        });
    });

    it.each([
        ['diary-admin', true],
        ['other-admin', false],
    ])('restores the cookie for the requested diary account %s', async (user, allowed) => {
        authService.isAuthenticated.mockReturnValue(true);
        authService.isAdmin.mockReturnValue(true);
        authService.getUserId.mockReturnValue(user);
        state = Object.assign(Object.create(RouterStateSnapshot.prototype), {
            url: '/users?page=2#account=diary-admin',
        }) as RouterStateSnapshot;

        const result = await TestBed.runInInjectionContext(async () => adminAuthGuard(route, state));

        if (allowed) {
            expect(result).toEqual(new DefaultUrlSerializer().parse('/users?page=2'));
        } else {
            expect(router.createUrlTree).toHaveBeenCalledWith(['/sign-in'], {
                queryParams: { returnUrl: '/users?page=2', confirm: '0' },
                fragment: 'account=diary-admin',
            });
        }
    });
});

describe('admin authorization after SSO', () => {
    it.each([
        ['/users?page=2#code=one-time-code', '/users?page=2'],
        ['/users?page=2&code=one-time-code#details', '/users?page=2#details'],
        ['/users?code=one-time-code#code=one-time-code&tab=activity', '/users#tab=activity'],
    ])('redirects successful SSO navigation from %s to %s', async (url, expectedUrl) => {
        authService.isAuthenticated.mockReturnValue(true);
        authService.isAdmin.mockReturnValue(true);
        state = Object.assign(Object.create(RouterStateSnapshot.prototype), { url }) as RouterStateSnapshot;

        const result = await TestBed.runInInjectionContext(async () => adminAuthGuard(route, state));

        expect(result).toEqual(new DefaultUrlSerializer().parse(expectedUrl));
    });

    it('cleans the SSO destination after an admin upgrade', async () => {
        authService.isAuthenticated.mockReturnValue(true);
        authService.isAdmin.mockReturnValueOnce(false).mockReturnValueOnce(true);
        authService.tryUpgradeToAdminAsync.mockResolvedValue(true);
        state = Object.assign(Object.create(RouterStateSnapshot.prototype), { url: '/users#code=one-time-code' }) as RouterStateSnapshot;

        const result = await TestBed.runInInjectionContext(async () => adminAuthGuard(route, state));

        expect(result).toEqual(new DefaultUrlSerializer().parse('/users'));
    });

    it('should allow non-admin after successful admin upgrade', async () => {
        authService.isAuthenticated.mockReturnValue(true);
        authService.isAdmin.mockReturnValueOnce(false).mockReturnValueOnce(true);
        authService.tryUpgradeToAdminAsync.mockResolvedValue(true);

        const result = await TestBed.runInInjectionContext(async () => adminAuthGuard(route, state));

        expect(authService.tryUpgradeToAdminAsync).toHaveBeenCalled();
        expect(result).toBe(true);
    });

    it('should redirect forbidden when admin upgrade fails', async () => {
        authService.isAuthenticated.mockReturnValue(true);
        authService.isAdmin.mockReturnValue(false);
        authService.tryUpgradeToAdminAsync.mockResolvedValue(false);

        const result = await TestBed.runInInjectionContext(async () => adminAuthGuard(route, state));

        expect(router.createUrlTree).toHaveBeenCalledWith(['/unauthorized'], {
            queryParams: { reason: 'forbidden', returnUrl: '/users?page=2' },
        });
        expect(result).toEqual({
            commands: ['/unauthorized'],
            extras: { queryParams: { reason: 'forbidden', returnUrl: '/users?page=2' } },
        });
    });
});

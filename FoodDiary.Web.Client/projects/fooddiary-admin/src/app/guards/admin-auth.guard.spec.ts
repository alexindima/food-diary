import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AdminAuthService } from '../features/admin-auth/lib/admin-auth.service';
import { adminAuthGuard } from './admin-auth.guard';

describe('adminAuthGuard', () => {
    let authService: {
        applySsoFromQuery: ReturnType<typeof vi.fn>;
        refreshTokenState: ReturnType<typeof vi.fn>;
        isAuthenticated: ReturnType<typeof vi.fn>;
        isAdmin: ReturnType<typeof vi.fn>;
        tryUpgradeToAdmin: ReturnType<typeof vi.fn>;
    };
    let router: {
        createUrlTree: ReturnType<typeof vi.fn>;
    };
    let route: ActivatedRouteSnapshot;
    let state: RouterStateSnapshot;

    beforeEach(() => {
        authService = {
            applySsoFromQuery: vi.fn(),
            refreshTokenState: vi.fn(),
            isAuthenticated: vi.fn(),
            isAdmin: vi.fn(),
            tryUpgradeToAdmin: vi.fn(),
        };
        authService.applySsoFromQuery.mockResolvedValue(undefined);
        authService.tryUpgradeToAdmin.mockResolvedValue(false);

        router = {
            createUrlTree: vi.fn(),
        };
        router.createUrlTree.mockImplementation((commands: unknown[], extras: unknown) => ({ commands, extras }));

        TestBed.configureTestingModule({
            providers: [
                { provide: AdminAuthService, useValue: authService },
                { provide: Router, useValue: router },
            ],
        });

        route = new ActivatedRouteSnapshot();
        state = Object.assign(Object.create(RouterStateSnapshot.prototype), { url: '/users?page=2' });
    });

    it('should allow authenticated admin', async () => {
        authService.isAuthenticated.mockReturnValue(true);
        authService.isAdmin.mockReturnValue(true);

        const result = await TestBed.runInInjectionContext(() => adminAuthGuard(route, state));

        expect(result).toBe(true);
        expect(authService.applySsoFromQuery).toHaveBeenCalled();
        expect(authService.refreshTokenState).toHaveBeenCalledTimes(1);
    });

    it('should redirect unauthenticated user to unauthorized page', async () => {
        authService.isAuthenticated.mockReturnValue(false);

        const result = await TestBed.runInInjectionContext(() => adminAuthGuard(route, state));

        expect(router.createUrlTree).toHaveBeenCalledWith(['/unauthorized'], {
            queryParams: { reason: 'unauthenticated', returnUrl: '/users?page=2' },
        });
        expect(result).toEqual({
            commands: ['/unauthorized'],
            extras: { queryParams: { reason: 'unauthenticated', returnUrl: '/users?page=2' } },
        });
    });

    it('should allow non-admin after successful admin upgrade', async () => {
        authService.isAuthenticated.mockReturnValue(true);
        authService.isAdmin.mockReturnValueOnce(false).mockReturnValueOnce(true);
        authService.tryUpgradeToAdmin.mockResolvedValue(true);

        const result = await TestBed.runInInjectionContext(() => adminAuthGuard(route, state));

        expect(authService.tryUpgradeToAdmin).toHaveBeenCalled();
        expect(authService.refreshTokenState).toHaveBeenCalledTimes(2);
        expect(result).toBe(true);
    });

    it('should redirect forbidden when admin upgrade fails', async () => {
        authService.isAuthenticated.mockReturnValue(true);
        authService.isAdmin.mockReturnValue(false);
        authService.tryUpgradeToAdmin.mockResolvedValue(false);

        const result = await TestBed.runInInjectionContext(() => adminAuthGuard(route, state));

        expect(router.createUrlTree).toHaveBeenCalledWith(['/unauthorized'], {
            queryParams: { reason: 'forbidden', returnUrl: '/users?page=2' },
        });
        expect(result).toEqual({
            commands: ['/unauthorized'],
            extras: { queryParams: { reason: 'forbidden', returnUrl: '/users?page=2' } },
        });
    });
});

import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AdminAuthService } from '../features/admin-auth/lib/admin-auth.service';
import { adminAuthGuard } from './admin-auth.guard';

describe('adminAuthGuard', () => {
    let authService: {
        applySsoFromQueryAsync: ReturnType<typeof vi.fn>;
        refreshTokenState: ReturnType<typeof vi.fn>;
        isAuthenticated: ReturnType<typeof vi.fn>;
        isAdmin: ReturnType<typeof vi.fn>;
        tryUpgradeToAdminAsync: ReturnType<typeof vi.fn>;
    };
    let router: {
        createUrlTree: ReturnType<typeof vi.fn>;
    };
    let route: ActivatedRouteSnapshot;
    let state: RouterStateSnapshot;

    beforeEach(() => {
        authService = {
            applySsoFromQueryAsync: vi.fn(),
            refreshTokenState: vi.fn(),
            isAuthenticated: vi.fn(),
            isAdmin: vi.fn(),
            tryUpgradeToAdminAsync: vi.fn(),
        };
        authService.applySsoFromQueryAsync.mockResolvedValue(undefined);
        authService.tryUpgradeToAdminAsync.mockResolvedValue(false);

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
        state = Object.assign(Object.create(RouterStateSnapshot.prototype), { url: '/users?page=2' }) as RouterStateSnapshot;
    });

    it('should allow authenticated admin', async () => {
        authService.isAuthenticated.mockReturnValue(true);
        authService.isAdmin.mockReturnValue(true);

        const result = await TestBed.runInInjectionContext(async () => adminAuthGuard(route, state));

        expect(result).toBe(true);
        expect(authService.applySsoFromQueryAsync).toHaveBeenCalled();
        expect(authService.refreshTokenState).toHaveBeenCalledTimes(1);
    });

    it('should redirect unauthenticated user to unauthorized page', async () => {
        authService.isAuthenticated.mockReturnValue(false);

        const result = await TestBed.runInInjectionContext(async () => adminAuthGuard(route, state));

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
        authService.tryUpgradeToAdminAsync.mockResolvedValue(true);

        const result = await TestBed.runInInjectionContext(async () => adminAuthGuard(route, state));

        expect(authService.tryUpgradeToAdminAsync).toHaveBeenCalled();
        expect(authService.refreshTokenState).toHaveBeenCalledTimes(2);
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

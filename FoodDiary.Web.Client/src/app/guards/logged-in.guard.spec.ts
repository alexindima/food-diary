import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { type ActivatedRouteSnapshot, Router, type RouterStateSnapshot } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../services/auth.service';
import { loggedInGuard } from './logged-in.guard';

describe('loggedInGuard', () => {
    let authServiceMock: {
        getToken: ReturnType<typeof vi.fn>;
        isAuthenticated: ReturnType<typeof signal>;
        ensureSessionReadyAsync: ReturnType<typeof vi.fn>;
    };
    let routerMock: { createUrlTree: ReturnType<typeof vi.fn> };
    let dashboardUrlTree: object;
    let route: ActivatedRouteSnapshot;
    let state: RouterStateSnapshot;

    beforeEach(() => {
        const isAuthenticated = signal(false);

        authServiceMock = {
            getToken: vi.fn(),
            isAuthenticated,
            ensureSessionReadyAsync: vi.fn(),
        };
        authServiceMock.ensureSessionReadyAsync.mockResolvedValue(void 0);

        dashboardUrlTree = { toString: (): string => '/dashboard' };
        routerMock = { createUrlTree: vi.fn().mockReturnValue(dashboardUrlTree) };

        TestBed.configureTestingModule({
            providers: [
                { provide: AuthService, useValue: authServiceMock },
                { provide: Router, useValue: routerMock },
            ],
        });

        const routeStub = {};
        const stateStub = { url: '/?auth=login' };
        route = routeStub as ActivatedRouteSnapshot;
        state = stateStub as RouterStateSnapshot;
    });

    it('should allow access when not authenticated', async () => {
        authServiceMock.isAuthenticated.set(false);

        const result = await TestBed.runInInjectionContext(async () => loggedInGuard(route, state));

        expect(result).toBe(true);
        expect(authServiceMock.ensureSessionReadyAsync).toHaveBeenCalled();
        expect(routerMock.createUrlTree).not.toHaveBeenCalled();
    });

    it('should return dashboard url tree when authenticated', async () => {
        authServiceMock.isAuthenticated.set(true);

        const result = await TestBed.runInInjectionContext(async () => loggedInGuard(route, state));

        expect(result).toBe(dashboardUrlTree);
        expect(routerMock.createUrlTree).toHaveBeenCalledWith(['/dashboard']);
    });

    it('should allow authenticated user to complete the admin sign-in handoff', async () => {
        authServiceMock.isAuthenticated.set(true);
        const adminRedirectState = { url: '/?auth=login&adminReturnUrl=%2F' };
        state = adminRedirectState as RouterStateSnapshot;

        const result = await TestBed.runInInjectionContext(async () => loggedInGuard(route, state));

        expect(result).toBe(true);
        expect(routerMock.createUrlTree).not.toHaveBeenCalled();
    });

    it.each(['/?adminReturnUrl=%2F', '/?auth=login&adminReturnUrl='])('should redirect an incomplete admin handoff %s', async url => {
        authServiceMock.isAuthenticated.set(true);
        const handoffState = { url };
        state = handoffState as RouterStateSnapshot;
        const result = await TestBed.runInInjectionContext(async () => loggedInGuard(route, state));
        expect(result).toBe(dashboardUrlTree);
    });
});

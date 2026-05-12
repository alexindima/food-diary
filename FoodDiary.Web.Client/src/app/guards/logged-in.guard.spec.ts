import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import type { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../services/auth.service';
import { NavigationService } from '../services/navigation.service';
import { loggedInGuard } from './logged-in.guard';

describe('loggedInGuard', () => {
    let authServiceMock: {
        getToken: ReturnType<typeof vi.fn>;
        isAuthenticated: ReturnType<typeof signal>;
        ensureSessionReadyAsync: ReturnType<typeof vi.fn>;
    };
    let navigationServiceMock: { navigateToHomeAsync: ReturnType<typeof vi.fn> };
    let route: ActivatedRouteSnapshot;
    let state: RouterStateSnapshot;

    beforeEach(() => {
        const isAuthenticated = signal(false);

        authServiceMock = {
            getToken: vi.fn(),
            isAuthenticated,
            ensureSessionReadyAsync: vi.fn(),
        };
        authServiceMock.ensureSessionReadyAsync.mockResolvedValue(undefined);

        navigationServiceMock = { navigateToHomeAsync: vi.fn() };
        navigationServiceMock.navigateToHomeAsync.mockReturnValue(Promise.resolve());

        TestBed.configureTestingModule({
            providers: [
                { provide: AuthService, useValue: authServiceMock },
                { provide: NavigationService, useValue: navigationServiceMock },
            ],
        });

        const routeStub = {};
        const stateStub = { url: '/auth/login' };
        route = routeStub as ActivatedRouteSnapshot;
        state = stateStub as RouterStateSnapshot;
    });

    it('should allow access when not authenticated', async () => {
        authServiceMock.isAuthenticated.set(false);

        const result = await TestBed.runInInjectionContext(async () => loggedInGuard(route, state));

        expect(result).toBe(true);
        expect(authServiceMock.ensureSessionReadyAsync).toHaveBeenCalled();
        expect(navigationServiceMock.navigateToHomeAsync).not.toHaveBeenCalled();
    });

    it('should redirect to dashboard when authenticated', async () => {
        authServiceMock.isAuthenticated.set(true);

        const result = await TestBed.runInInjectionContext(async () => loggedInGuard(route, state));

        expect(result).toBe(false);
        expect(navigationServiceMock.navigateToHomeAsync).toHaveBeenCalled();
    });

    it('should allow access for authenticated user when adminReturnUrl is present', async () => {
        authServiceMock.isAuthenticated.set(true);
        const adminRedirectState = { url: '/auth/login?adminReturnUrl=%2F' };
        state = adminRedirectState as RouterStateSnapshot;

        const result = await TestBed.runInInjectionContext(async () => loggedInGuard(route, state));

        expect(result).toBe(true);
        expect(navigationServiceMock.navigateToHomeAsync).not.toHaveBeenCalled();
    });
});

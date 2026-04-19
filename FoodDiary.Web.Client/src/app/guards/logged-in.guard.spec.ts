import { beforeEach, describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { loggedInGuard } from './logged-in.guard';
import { AuthService } from '../services/auth.service';
import { NavigationService } from '../services/navigation.service';
import { signal } from '@angular/core';

describe('loggedInGuard', () => {
    let authServiceMock: { getToken: ReturnType<typeof vi.fn>; isAuthenticated: ReturnType<typeof signal> };
    let navigationServiceMock: { navigateToHome: ReturnType<typeof vi.fn> };
    let route: ActivatedRouteSnapshot;
    let state: RouterStateSnapshot;

    beforeEach(() => {
        const isAuthenticated = signal(false);

        authServiceMock = {
            getToken: vi.fn(),
            isAuthenticated,
        };

        navigationServiceMock = { navigateToHome: vi.fn() };
        navigationServiceMock.navigateToHome.mockReturnValue(Promise.resolve());

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

        const result = await TestBed.runInInjectionContext(() => loggedInGuard(route, state));

        expect(result).toBe(true);
        expect(navigationServiceMock.navigateToHome).not.toHaveBeenCalled();
    });

    it('should redirect to dashboard when authenticated', async () => {
        authServiceMock.isAuthenticated.set(true);

        const result = await TestBed.runInInjectionContext(() => loggedInGuard(route, state));

        expect(result).toBe(false);
        expect(navigationServiceMock.navigateToHome).toHaveBeenCalled();
    });
});

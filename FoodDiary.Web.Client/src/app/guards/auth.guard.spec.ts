import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';
import { NavigationService } from '../services/navigation.service';
import { signal } from '@angular/core';

describe('authGuard', () => {
    let authServiceMock: { getToken: ReturnType<typeof vi.fn>; isAuthenticated: ReturnType<typeof signal>; isEmailConfirmed: ReturnType<typeof signal> };
    let navigationServiceMock: {
        navigateToAuth: ReturnType<typeof vi.fn>;
        navigateToEmailVerificationPending: ReturnType<typeof vi.fn>;
    };
    let route: ActivatedRouteSnapshot;
    let state: RouterStateSnapshot;

    beforeEach(() => {
        const isAuthenticated = signal(false);
        const isEmailConfirmed = signal(true);

        authServiceMock = {
            getToken: vi.fn(),
            isAuthenticated,
            isEmailConfirmed,
        };

        navigationServiceMock = {
            navigateToAuth: vi.fn(),
            navigateToEmailVerificationPending: vi.fn(),
        };
        navigationServiceMock.navigateToAuth.mockReturnValue(Promise.resolve());
        navigationServiceMock.navigateToEmailVerificationPending.mockReturnValue(Promise.resolve());

        TestBed.configureTestingModule({
            providers: [
                { provide: AuthService, useValue: authServiceMock },
                { provide: NavigationService, useValue: navigationServiceMock },
            ],
        });

        const routeStub = {};
        const stateStub = { url: '/products' };
        route = routeStub as ActivatedRouteSnapshot;
        state = stateStub as RouterStateSnapshot;
    });

    it('should allow access when authenticated and email confirmed', async () => {
        authServiceMock.isAuthenticated.set(true);
        authServiceMock.isEmailConfirmed.set(true);

        const result = await TestBed.runInInjectionContext(() => authGuard(route, state));

        expect(result).toBe(true);
        expect(navigationServiceMock.navigateToAuth).not.toHaveBeenCalled();
        expect(navigationServiceMock.navigateToEmailVerificationPending).not.toHaveBeenCalled();
    });

    it('should redirect to email verification when email not confirmed', async () => {
        authServiceMock.isAuthenticated.set(true);
        authServiceMock.isEmailConfirmed.set(false);

        const result = await TestBed.runInInjectionContext(() => authGuard(route, state));

        expect(result).toBe(false);
        expect(navigationServiceMock.navigateToEmailVerificationPending).toHaveBeenCalled();
    });

    it('should redirect to login when not authenticated', async () => {
        authServiceMock.isAuthenticated.set(false);

        const result = await TestBed.runInInjectionContext(() => authGuard(route, state));

        expect(result).toBe(false);
        expect(navigationServiceMock.navigateToAuth).toHaveBeenCalledWith('login', '/products');
    });

    it('should include returnUrl in login redirect', async () => {
        authServiceMock.isAuthenticated.set(false);
        const stateStub = { url: '/recipes/add' };
        state = stateStub as RouterStateSnapshot;

        const result = await TestBed.runInInjectionContext(() => authGuard(route, state));

        expect(result).toBe(false);
        expect(navigationServiceMock.navigateToAuth).toHaveBeenCalledWith('login', '/recipes/add');
    });
});

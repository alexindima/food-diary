import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';
import { NavigationService } from '../services/navigation.service';
import { signal } from '@angular/core';

describe('authGuard', () => {
    let authServiceMock: any;
    let navigationServiceMock: any;
    let route: ActivatedRouteSnapshot;
    let state: RouterStateSnapshot;

    beforeEach(() => {
        const isAuthenticated = signal(false);
        const isEmailConfirmed = signal(true);

        authServiceMock = {
            getToken: vi.fn(),
            isAuthenticated,
            isEmailConfirmed,
        } as any;

        navigationServiceMock = {
            navigateToAuth: vi.fn(),
            navigateToEmailVerificationPending: vi.fn(),
        } as any;
        navigationServiceMock.navigateToAuth.mockReturnValue(Promise.resolve());
        navigationServiceMock.navigateToEmailVerificationPending.mockReturnValue(Promise.resolve());

        TestBed.configureTestingModule({
            providers: [
                { provide: AuthService, useValue: authServiceMock },
                { provide: NavigationService, useValue: navigationServiceMock },
            ],
        });

        route = {} as ActivatedRouteSnapshot;
        state = { url: '/products' } as RouterStateSnapshot;
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
        state = { url: '/recipes/add' } as RouterStateSnapshot;

        const result = await TestBed.runInInjectionContext(() => authGuard(route, state));

        expect(result).toBe(false);
        expect(navigationServiceMock.navigateToAuth).toHaveBeenCalledWith('login', '/recipes/add');
    });
});

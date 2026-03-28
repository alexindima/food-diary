import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { loggedInGuard } from './logged-in.guard';
import { AuthService } from '../services/auth.service';
import { NavigationService } from '../services/navigation.service';
import { signal } from '@angular/core';

describe('loggedInGuard', () => {
    let authServiceMock: jasmine.SpyObj<AuthService> & {
        isAuthenticated: ReturnType<typeof signal<boolean>>;
    };
    let navigationServiceMock: jasmine.SpyObj<NavigationService>;
    let route: ActivatedRouteSnapshot;
    let state: RouterStateSnapshot;

    beforeEach(() => {
        const isAuthenticated = signal(false);

        authServiceMock = {
            ...jasmine.createSpyObj('AuthService', ['getToken']),
            isAuthenticated,
        } as unknown as typeof authServiceMock;

        navigationServiceMock = jasmine.createSpyObj('NavigationService', ['navigateToHome']);
        navigationServiceMock.navigateToHome.and.returnValue(Promise.resolve());

        TestBed.configureTestingModule({
            providers: [
                { provide: AuthService, useValue: authServiceMock },
                { provide: NavigationService, useValue: navigationServiceMock },
            ],
        });

        route = {} as ActivatedRouteSnapshot;
        state = { url: '/auth/login' } as RouterStateSnapshot;
    });

    it('should allow access when not authenticated', async () => {
        authServiceMock.isAuthenticated.set(false);

        const result = await TestBed.runInInjectionContext(() => loggedInGuard(route, state));

        expect(result).toBeTrue();
        expect(navigationServiceMock.navigateToHome).not.toHaveBeenCalled();
    });

    it('should redirect to home when authenticated', async () => {
        authServiceMock.isAuthenticated.set(true);

        const result = await TestBed.runInInjectionContext(() => loggedInGuard(route, state));

        expect(result).toBeFalse();
        expect(navigationServiceMock.navigateToHome).toHaveBeenCalled();
    });
});

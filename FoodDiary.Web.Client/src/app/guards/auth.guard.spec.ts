import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import type { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../services/auth.service';
import { NavigationService } from '../services/navigation.service';
import { LocalizationService } from '../shared/i18n/localization.service';
import { authGuard } from './auth.guard';

type AuthServiceMock = {
    getToken: ReturnType<typeof vi.fn>;
    isAuthenticated: ReturnType<typeof signal>;
    isEmailConfirmed: ReturnType<typeof signal>;
    mustChangePassword: ReturnType<typeof signal>;
    ensureSessionReadyAsync: ReturnType<typeof vi.fn>;
};

type NavigationServiceMock = {
    navigateToAuthAsync: ReturnType<typeof vi.fn>;
    navigateToEmailVerificationPendingAsync: ReturnType<typeof vi.fn>;
    navigateToRequiredPasswordChangeAsync: ReturnType<typeof vi.fn>;
};

type LocalizationServiceMock = {
    loadApplicationTranslationsAsync: ReturnType<typeof vi.fn>;
};

// eslint-disable-next-line max-lines-per-function -- Keep the shared guard setup and its route scenarios together.
describe('authGuard', () => {
    let authServiceMock: AuthServiceMock;
    let navigationServiceMock: NavigationServiceMock;
    let localizationServiceMock: LocalizationServiceMock;
    let route: ActivatedRouteSnapshot;
    let state: RouterStateSnapshot;

    beforeEach(() => {
        const isAuthenticated = signal(false);
        const isEmailConfirmed = signal(true);
        const mustChangePassword = signal(false);

        authServiceMock = {
            getToken: vi.fn(),
            isAuthenticated,
            isEmailConfirmed,
            mustChangePassword,
            ensureSessionReadyAsync: vi.fn(),
        };
        authServiceMock.ensureSessionReadyAsync.mockResolvedValue(void 0);

        navigationServiceMock = {
            navigateToAuthAsync: vi.fn(),
            navigateToEmailVerificationPendingAsync: vi.fn(),
            navigateToRequiredPasswordChangeAsync: vi.fn(),
        };
        navigationServiceMock.navigateToAuthAsync.mockResolvedValue(undefined);
        navigationServiceMock.navigateToEmailVerificationPendingAsync.mockResolvedValue(undefined);
        navigationServiceMock.navigateToRequiredPasswordChangeAsync.mockResolvedValue(undefined);
        localizationServiceMock = {
            loadApplicationTranslationsAsync: vi.fn().mockResolvedValue(void 0),
        };

        TestBed.configureTestingModule({
            providers: [
                { provide: AuthService, useValue: authServiceMock },
                { provide: NavigationService, useValue: navigationServiceMock },
                { provide: LocalizationService, useValue: localizationServiceMock },
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

        const result = await TestBed.runInInjectionContext(async () => authGuard(route, state));

        expect(result).toBe(true);
        expect(authServiceMock.ensureSessionReadyAsync).toHaveBeenCalled();
        expect(localizationServiceMock.loadApplicationTranslationsAsync).toHaveBeenCalled();
        expect(navigationServiceMock.navigateToAuthAsync).not.toHaveBeenCalled();
        expect(navigationServiceMock.navigateToEmailVerificationPendingAsync).not.toHaveBeenCalled();
    });

    it('should redirect to email verification when email not confirmed', async () => {
        authServiceMock.isAuthenticated.set(true);
        authServiceMock.isEmailConfirmed.set(false);

        const result = await TestBed.runInInjectionContext(async () => authGuard(route, state));

        expect(result).toBe(false);
        expect(localizationServiceMock.loadApplicationTranslationsAsync).not.toHaveBeenCalled();
        expect(navigationServiceMock.navigateToEmailVerificationPendingAsync).toHaveBeenCalled();
    });

    it('should redirect to required password change before loading protected routes', async () => {
        authServiceMock.isAuthenticated.set(true);
        authServiceMock.mustChangePassword.set(true);

        const result = await TestBed.runInInjectionContext(async () => authGuard(route, state));

        expect(result).toBe(false);
        expect(navigationServiceMock.navigateToRequiredPasswordChangeAsync).toHaveBeenCalled();
        expect(localizationServiceMock.loadApplicationTranslationsAsync).not.toHaveBeenCalled();
    });

    it('should redirect to login when not authenticated', async () => {
        authServiceMock.isAuthenticated.set(false);

        const result = await TestBed.runInInjectionContext(async () => authGuard(route, state));

        expect(result).toBe(false);
        expect(localizationServiceMock.loadApplicationTranslationsAsync).not.toHaveBeenCalled();
        expect(navigationServiceMock.navigateToAuthAsync).toHaveBeenCalledWith('login', '/products');
    });

    it('should include returnUrl in login redirect', async () => {
        authServiceMock.isAuthenticated.set(false);
        const stateStub = { url: '/recipes/add' };
        state = stateStub as RouterStateSnapshot;

        const result = await TestBed.runInInjectionContext(async () => authGuard(route, state));

        expect(result).toBe(false);
        expect(navigationServiceMock.navigateToAuthAsync).toHaveBeenCalledWith('login', '/recipes/add');
    });
});

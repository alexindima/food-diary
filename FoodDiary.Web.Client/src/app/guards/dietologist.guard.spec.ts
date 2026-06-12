import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { type ActivatedRouteSnapshot, Router, type RouterStateSnapshot, type UrlTree } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';

import { AuthService } from '../services/auth.service';
import { NavigationService } from '../services/navigation.service';
import { LocalizationService } from '../shared/i18n/localization.service';
import { dietologistGuard } from './dietologist.guard';

describe('dietologistGuard', () => {
    it('should redirect anonymous users to auth', async () => {
        const { authServiceMock, localizationServiceMock, navigationServiceMock, state } = setupGuard();
        authServiceMock.isAuthenticated.set(false);

        const result = await TestBed.runInInjectionContext(async () => dietologistGuard(createRouteSnapshot(), state));

        expect(result).toBe(false);
        expect(localizationServiceMock.loadApplicationTranslationsAsync).not.toHaveBeenCalled();
        expect(navigationServiceMock.navigateToAuthAsync).toHaveBeenCalledWith('login', '/dietologist');
    });

    it('should redirect unconfirmed users to email verification', async () => {
        const { authServiceMock, localizationServiceMock, navigationServiceMock, state } = setupGuard();
        authServiceMock.isAuthenticated.set(true);
        authServiceMock.isEmailConfirmed.set(false);

        const result = await TestBed.runInInjectionContext(async () => dietologistGuard(createRouteSnapshot(), state));

        expect(result).toBe(false);
        expect(localizationServiceMock.loadApplicationTranslationsAsync).not.toHaveBeenCalled();
        expect(navigationServiceMock.navigateToEmailVerificationPendingAsync).toHaveBeenCalled();
    });

    it('should redirect non-dietologists home', async () => {
        const { authServiceMock, localizationServiceMock, routerMock, state, urlTree } = setupGuard();
        authServiceMock.isAuthenticated.set(true);
        authServiceMock.isEmailConfirmed.set(true);
        authServiceMock.isDietologist.set(false);

        const result = await TestBed.runInInjectionContext(async () => dietologistGuard(createRouteSnapshot(), state));

        expect(result).toBe(urlTree);
        expect(localizationServiceMock.loadApplicationTranslationsAsync).not.toHaveBeenCalled();
        expect(routerMock.createUrlTree).toHaveBeenCalledWith(['/']);
    });

    it('should allow dietologists', async () => {
        const { authServiceMock, localizationServiceMock, state } = setupGuard();
        authServiceMock.isAuthenticated.set(true);
        authServiceMock.isEmailConfirmed.set(true);
        authServiceMock.isDietologist.set(true);

        const result = await TestBed.runInInjectionContext(async () => dietologistGuard(createRouteSnapshot(), state));

        expect(result).toBe(true);
        expect(localizationServiceMock.loadApplicationTranslationsAsync).toHaveBeenCalled();
    });
});

function setupGuard(): {
    authServiceMock: {
        isAuthenticated: ReturnType<typeof signal<boolean>>;
        isEmailConfirmed: ReturnType<typeof signal<boolean>>;
        isDietologist: ReturnType<typeof signal<boolean>>;
    };
    navigationServiceMock: {
        navigateToAuthAsync: ReturnType<typeof vi.fn>;
        navigateToEmailVerificationPendingAsync: ReturnType<typeof vi.fn>;
    };
    localizationServiceMock: {
        loadApplicationTranslationsAsync: ReturnType<typeof vi.fn>;
    };
    routerMock: {
        createUrlTree: ReturnType<typeof vi.fn>;
    };
    state: RouterStateSnapshot;
    urlTree: UrlTree;
} {
    const urlTreeStub = {};
    const urlTree = urlTreeStub as UrlTree;
    const authServiceMock = {
        isAuthenticated: signal(false),
        isEmailConfirmed: signal(true),
        isDietologist: signal(false),
    };
    const navigationServiceMock = {
        navigateToAuthAsync: vi.fn().mockResolvedValue(void 0),
        navigateToEmailVerificationPendingAsync: vi.fn().mockResolvedValue(void 0),
    };
    const localizationServiceMock = {
        loadApplicationTranslationsAsync: vi.fn().mockResolvedValue(void 0),
    };
    const routerMock = {
        createUrlTree: vi.fn().mockReturnValue(urlTree),
    };

    TestBed.configureTestingModule({
        providers: [
            { provide: AuthService, useValue: authServiceMock },
            { provide: NavigationService, useValue: navigationServiceMock },
            { provide: LocalizationService, useValue: localizationServiceMock },
            { provide: Router, useValue: routerMock },
        ],
    });

    return {
        authServiceMock,
        navigationServiceMock,
        localizationServiceMock,
        routerMock,
        state: createRouterStateSnapshot('/dietologist'),
        urlTree,
    };
}

function createRouteSnapshot(): ActivatedRouteSnapshot {
    const routeSnapshotStub = {};
    return routeSnapshotStub as ActivatedRouteSnapshot;
}

function createRouterStateSnapshot(url: string): RouterStateSnapshot {
    const stateSnapshotStub = { url };
    return stateSnapshotStub as RouterStateSnapshot;
}

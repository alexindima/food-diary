import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { type ActivatedRouteSnapshot, Router, type RouterStateSnapshot, type UrlTree } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';

import { AuthService } from '../services/auth.service';
import { NavigationService } from '../services/navigation.service';
import { dietologistGuard } from './dietologist.guard';

describe('dietologistGuard', () => {
    it('should redirect anonymous users to auth', async () => {
        const { authServiceMock, navigationServiceMock, state } = setupGuard();
        authServiceMock.isAuthenticated.set(false);

        const result = await TestBed.runInInjectionContext(async () => dietologistGuard(createRouteSnapshot(), state));

        expect(result).toBe(false);
        expect(navigationServiceMock.navigateToAuthAsync).toHaveBeenCalledWith('login', '/dietologist');
    });

    it('should redirect unconfirmed users to email verification', async () => {
        const { authServiceMock, navigationServiceMock, state } = setupGuard();
        authServiceMock.isAuthenticated.set(true);
        authServiceMock.isEmailConfirmed.set(false);

        const result = await TestBed.runInInjectionContext(async () => dietologistGuard(createRouteSnapshot(), state));

        expect(result).toBe(false);
        expect(navigationServiceMock.navigateToEmailVerificationPendingAsync).toHaveBeenCalled();
    });

    it('should redirect non-dietologists home', async () => {
        const { authServiceMock, routerMock, state, urlTree } = setupGuard();
        authServiceMock.isAuthenticated.set(true);
        authServiceMock.isEmailConfirmed.set(true);
        authServiceMock.isDietologist.set(false);

        const result = await TestBed.runInInjectionContext(async () => dietologistGuard(createRouteSnapshot(), state));

        expect(result).toBe(urlTree);
        expect(routerMock.createUrlTree).toHaveBeenCalledWith(['/']);
    });

    it('should allow dietologists', async () => {
        const { authServiceMock, state } = setupGuard();
        authServiceMock.isAuthenticated.set(true);
        authServiceMock.isEmailConfirmed.set(true);
        authServiceMock.isDietologist.set(true);

        const result = await TestBed.runInInjectionContext(async () => dietologistGuard(createRouteSnapshot(), state));

        expect(result).toBe(true);
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
        navigateToAuthAsync: vi.fn().mockResolvedValue(undefined),
        navigateToEmailVerificationPendingAsync: vi.fn().mockResolvedValue(undefined),
    };
    const routerMock = {
        createUrlTree: vi.fn().mockReturnValue(urlTree),
    };

    TestBed.configureTestingModule({
        providers: [
            { provide: AuthService, useValue: authServiceMock },
            { provide: NavigationService, useValue: navigationServiceMock },
            { provide: Router, useValue: routerMock },
        ],
    });

    return {
        authServiceMock,
        navigationServiceMock,
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

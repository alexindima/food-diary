import { signal, type WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import type { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { firstValueFrom, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../testing/async-testing';
import { AuthService } from '../../../services/auth.service';
import { NavigationService } from '../../../services/navigation.service';
import { requiredPasswordChangeGuard } from './required-password-change.guard';

let isAuthenticated: WritableSignal<boolean>;
let mustChangePassword: WritableSignal<boolean>;
let ensureSessionReadyAsync: ReturnType<typeof vi.fn<AuthService['ensureSessionReadyAsync']>>;
let navigateToAuthAsync: ReturnType<typeof vi.fn<NavigationService['navigateToAuthAsync']>>;
let navigateToHomeAsync: ReturnType<typeof vi.fn<NavigationService['navigateToHomeAsync']>>;

beforeEach(() => {
    isAuthenticated = signal(false);
    mustChangePassword = signal(false);
    ensureSessionReadyAsync = vi.fn().mockResolvedValue(undefined);
    navigateToAuthAsync = vi.fn().mockResolvedValue(undefined);
    navigateToHomeAsync = vi.fn().mockResolvedValue(undefined);

    TestBed.configureTestingModule({
        providers: [
            { provide: AuthService, useValue: { isAuthenticated, mustChangePassword, ensureSessionReadyAsync } },
            { provide: NavigationService, useValue: { navigateToAuthAsync, navigateToHomeAsync } },
        ],
    });
});

describe('requiredPasswordChangeGuard', () => {
    it('redirects a logged-out user to login and denies the password-change route', async () => {
        mustChangePassword.set(true);

        await expect(runGuardAsync()).resolves.toBe(false);

        expect(ensureSessionReadyAsync).toHaveBeenCalledOnce();
        expect(navigateToAuthAsync).toHaveBeenCalledExactlyOnceWith('login');
        expect(navigateToHomeAsync).not.toHaveBeenCalled();
    });

    it('redirects an authenticated user home when a password change is no longer required', async () => {
        isAuthenticated.set(true);

        await expect(runGuardAsync()).resolves.toBe(false);

        expect(ensureSessionReadyAsync).toHaveBeenCalledOnce();
        expect(navigateToHomeAsync).toHaveBeenCalledExactlyOnceWith();
        expect(navigateToAuthAsync).not.toHaveBeenCalled();
    });

    it('allows an authenticated user with a required password change without redirecting', async () => {
        isAuthenticated.set(true);
        mustChangePassword.set(true);

        await expect(runGuardAsync()).resolves.toBe(true);

        expect(ensureSessionReadyAsync).toHaveBeenCalledOnce();
        expect(navigateToAuthAsync).not.toHaveBeenCalled();
        expect(navigateToHomeAsync).not.toHaveBeenCalled();
    });

    it('waits for session restoration and uses the restored state without an early redirect', async () => {
        const sessionReady = new Subject<void>();
        ensureSessionReadyAsync.mockReturnValue(firstValueFrom(sessionReady));
        const result = runGuardAsync();
        const onResolved = vi.fn();
        void result.then(onResolved);

        await waitForAsyncTasksAsync();
        expect(ensureSessionReadyAsync).toHaveBeenCalledOnce();
        expect(onResolved).not.toHaveBeenCalled();
        expect(navigateToAuthAsync).not.toHaveBeenCalled();
        expect(navigateToHomeAsync).not.toHaveBeenCalled();

        isAuthenticated.set(true);
        mustChangePassword.set(true);
        sessionReady.next();
        sessionReady.complete();

        await expect(result).resolves.toBe(true);
        expect(onResolved).toHaveBeenCalledExactlyOnceWith(true);
        expect(navigateToAuthAsync).not.toHaveBeenCalled();
        expect(navigateToHomeAsync).not.toHaveBeenCalled();
    });
});

async function runGuardAsync(): Promise<unknown> {
    const routeStub = {};
    const stateStub = { url: '/change-password-required' };
    return TestBed.runInInjectionContext(async () =>
        requiredPasswordChangeGuard(routeStub as ActivatedRouteSnapshot, stateStub as RouterStateSnapshot),
    );
}

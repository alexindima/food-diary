import { DOCUMENT } from '@angular/common';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { afterEach, describe, expect, it, type Mock, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { AuthService } from '../../services/auth.service';
import { authRoutes } from './auth.routes';

const ADMIN_ORIGIN = 'https://admin.example.test';
const AUTH_PATH = '/auth/confirm-admin';
const ACCOUNT = 'diary-admin';
const EXPECTED_RETURN = '/users?status=active';
const CODE = { code: 'one-use-fixture', expiresAtUtc: '2026-01-01T00:01:00Z' };
const originalAdminUrl = environment.adminAppUrl;

@Component({ selector: 'fd-auth-test-profile', template: '<p>Profile destination</p>' })
class ProfileDestinationComponent {}

type ConfirmationBoundary = {
    ensureSessionReadyAsync: Mock<AuthService['ensureSessionReadyAsync']>;
    startAdminSso: Mock<AuthService['startAdminSso']>;
    replace: Mock<(url: string) => void>;
};

function configureConfirmation(account: string | null, expectedAccount: string | null): ConfirmationBoundary {
    const ensureSessionReadyAsync = vi.fn<AuthService['ensureSessionReadyAsync']>().mockResolvedValue(undefined);
    const startAdminSso = vi.fn<AuthService['startAdminSso']>().mockReturnValue(of(CODE));
    const replace = vi.fn<(url: string) => void>();
    const hash = expectedAccount === null ? '' : `#${new URLSearchParams({ account: expectedAccount })}`;
    const location = { origin: 'https://diary.example.test', hash, replace };
    const confirmationDocument = new Proxy(document, {
        get(target, property): unknown {
            if (property === 'location') {
                return location;
            }
            const value: unknown = Reflect.get(target, property, target);
            return typeof value === 'function' ? (value.bind(target) as unknown) : value;
        },
    });
    const confirmationProviders = [{ provide: DOCUMENT, useValue: confirmationDocument }];
    const routes = authRoutes.map(route =>
        route.path === AUTH_PATH.slice(1) ? { ...route, providers: [...(route.providers ?? []), ...confirmationProviders] } : route,
    );
    TestBed.configureTestingModule({
        providers: [
            provideRouter([...routes, { path: 'profile', component: ProfileDestinationComponent }], withComponentInputBinding()),
            provideTranslateTesting(),
            {
                provide: AuthService,
                useValue: {
                    ensureSessionReadyAsync,
                    startAdminSso,
                    isAdmin: (): boolean => account !== null,
                    getUserId: (): string | null => account,
                },
            },
        ],
    });
    return { ensureSessionReadyAsync, startAdminSso, replace };
}

function handoffUrl(): string {
    return `${AUTH_PATH}?${new URLSearchParams({ adminReturnUrl: EXPECTED_RETURN })}`;
}

afterEach(() => {
    environment.adminAppUrl = originalAdminUrl;
});

describe('Client admin confirmation route', () => {
    it('keeps the first entry return path while the diary session is pending', async () => {
        environment.adminAppUrl = ADMIN_ORIGIN;
        const { ensureSessionReadyAsync, startAdminSso, replace } = configureConfirmation(ACCOUNT, ACCOUNT);
        const session = new Subject<void>();
        ensureSessionReadyAsync.mockReturnValueOnce(firstValueFrom(session));
        const laterReturn = new URLSearchParams({ adminReturnUrl: '/other' });
        const harness = await RouterTestingHarness.create(`${handoffUrl()}&${laterReturn}`);
        expect(harness.routeNativeElement?.querySelector('[role="status"]')?.textContent).toContain('COMMON.LOADING');
        expect(startAdminSso).not.toHaveBeenCalled();
        expect(replace).not.toHaveBeenCalled();
        await harness.navigateByUrl(`${AUTH_PATH}?${laterReturn}`);
        expect(ensureSessionReadyAsync).toHaveBeenCalledOnce();

        session.next();
        session.complete();
        await vi.waitFor(() => {
            expect(replace).toHaveBeenCalledExactlyOnceWith(`${ADMIN_ORIGIN}${EXPECTED_RETURN}#code=${CODE.code}&account=${ACCOUNT}`);
        });
        expect(startAdminSso).toHaveBeenCalledOnce();
    });

    it('opens the configured admin home after confirming a matching session with an empty return path', async () => {
        environment.adminAppUrl = ADMIN_ORIGIN;
        const { startAdminSso, replace } = configureConfirmation(ACCOUNT, ACCOUNT);
        await RouterTestingHarness.create(`${AUTH_PATH}?adminReturnUrl=`);

        await vi.waitFor(() => {
            expect(replace).toHaveBeenCalledExactlyOnceWith(`${ADMIN_ORIGIN}/#code=${CODE.code}&account=${ACCOUNT}`);
        });
        expect(startAdminSso).toHaveBeenCalledOnce();
    });

    it.each([
        { account: ACCOUNT, expectedAccount: 'different-account', issueFails: false },
        { account: null, expectedAccount: null, issueFails: false },
        { account: ACCOUNT, expectedAccount: null, issueFails: true },
    ])('continues through the confirmation form with account $expectedAccount and service failure $issueFails', async scenario => {
        environment.adminAppUrl = ADMIN_ORIGIN;
        const { startAdminSso, replace } = configureConfirmation(scenario.account, scenario.expectedAccount);
        if (scenario.issueFails) {
            startAdminSso.mockReturnValueOnce(throwError(() => new Error('Handoff temporarily unavailable')));
        }
        await RouterTestingHarness.create(scenario.expectedAccount === null ? AUTH_PATH : handoffUrl());
        await vi.waitFor(() => {
            expect(replace).toHaveBeenCalledOnce();
        });
        const destination = new URL(replace.mock.lastCall?.[0] ?? '');
        expect(destination.origin).toBe(ADMIN_ORIGIN);
        expect(destination.pathname).toBe('/sign-in');
        expect(destination.searchParams.get('returnUrl')).toBe(scenario.expectedAccount === null ? '/' : EXPECTED_RETURN);
        expect(destination.searchParams.get('confirm')).toBe('1');
        const fragment = new URLSearchParams(destination.hash.slice(1));
        expect(fragment.get('account')).toBe(scenario.expectedAccount ?? scenario.account);
        expect(fragment.has('code')).toBe(false);
        expect(startAdminSso).toHaveBeenCalledTimes(Number(scenario.issueFails));
    });

    it('offers the profile link when an admin endpoint is not configured', async () => {
        environment.adminAppUrl = '';
        const { ensureSessionReadyAsync, replace } = configureConfirmation(ACCOUNT, null);
        const harness = await RouterTestingHarness.create(AUTH_PATH);
        harness.detectChanges();
        expect(harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent).toContain('AUTH.CONFIRM.ERROR');
        expect(harness.routeNativeElement?.querySelector('a')?.getAttribute('href')).toBe('/profile');
        expect(ensureSessionReadyAsync).not.toHaveBeenCalled();
        expect(replace).not.toHaveBeenCalled();
    });
});

describe('Client admin confirmation navigation lifetime', () => {
    it('does not start a handoff if the user leaves before the diary session is ready', async () => {
        environment.adminAppUrl = ADMIN_ORIGIN;
        const { ensureSessionReadyAsync, startAdminSso, replace } = configureConfirmation(ACCOUNT, ACCOUNT);
        const session = new Subject<void>();
        ensureSessionReadyAsync.mockReturnValueOnce(firstValueFrom(session));
        const harness = await RouterTestingHarness.create(handoffUrl());
        expect(ensureSessionReadyAsync).toHaveBeenCalledOnce();

        await harness.navigateByUrl('/profile');
        session.next();
        session.complete();
        await harness.fixture.whenStable();

        expect(harness.routeNativeElement?.textContent).toContain('Profile destination');
        expect(startAdminSso).not.toHaveBeenCalled();
        expect(replace).not.toHaveBeenCalled();
    });

    it('cancels a started handoff when the user leaves in the same tab', async () => {
        environment.adminAppUrl = ADMIN_ORIGIN;
        const { startAdminSso, replace } = configureConfirmation(ACCOUNT, ACCOUNT);
        const pending = new Subject<typeof CODE>();
        startAdminSso.mockReturnValueOnce(pending);
        const harness = await RouterTestingHarness.create(handoffUrl());
        await vi.waitFor(() => {
            expect(pending.observed).toBe(true);
        });

        await harness.navigateByUrl('/profile');
        expect(pending.observed).toBe(false);
        pending.next(CODE);
        pending.complete();
        await harness.fixture.whenStable();
        expect(harness.routeNativeElement?.textContent).toContain('Profile destination');
        expect(replace).not.toHaveBeenCalled();
    });
});

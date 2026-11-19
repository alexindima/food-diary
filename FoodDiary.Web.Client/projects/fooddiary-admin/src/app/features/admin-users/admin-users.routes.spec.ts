import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../shared/models/semantics/admin-meaning';
import { AdminAuthService } from '../admin-auth/contracts/admin-session';
import { adminUsersRoutes } from './admin-users.routes';
import { AdminUsersFacade } from './lib/admin-users.facade';
import type { PagedResponse } from './models/admin-user.models';

const PAGE_SIZE = 20;
const FIRST_PAGE = 1;
const LARGE_PAGE = 5;
const SECOND_PAGE = 2;
const TWO_PAGES_OF_ITEMS = 21;
const RECOVERY_REQUESTS = 3;
const EMPTY_PAGE = { items: [], page: FIRST_PAGE, limit: PAGE_SIZE, totalPages: 1, totalItems: 0 };

const getLoginEvents = vi.fn<AdminUsersFacade['getLoginEvents']>();
const getImpersonationSessions = vi.fn<AdminUsersFacade['getImpersonationSessions']>();
const getUsers = vi.fn<AdminUsersFacade['getUsers']>();
const getUser = vi.fn<AdminUsersFacade['getUser']>();
const getUserRoleAudit = vi.fn<AdminUsersFacade['getUserRoleAudit']>();

const historyRoutes = [
    {
        path: '/users/login-activity',
        searchLabel: 'Search login activity',
        filters: { userId: 'selected-user', provider: 'Google', device: 'mobile' },
        emptyText: 'No login activity found.',
        request: getLoginEvents,
    },
    {
        path: '/users/impersonation-sessions',
        searchLabel: 'Search impersonation sessions',
        filters: { actorId: 'audit-admin', targetId: 'selected-user' },
        emptyText: 'No impersonation sessions found.',
        request: getImpersonationSessions,
    },
] as const;

beforeEach(() => {
    vi.resetAllMocks();
    getLoginEvents.mockReturnValue(of(EMPTY_PAGE));
    getImpersonationSessions.mockReturnValue(of(EMPTY_PAGE));
    getUsers.mockReturnValue(of(EMPTY_PAGE));
    getUserRoleAudit.mockReturnValue(of([]));
    TestBed.configureTestingModule({
        providers: [
            provideTranslateTesting(),
            provideRouter([{ path: 'users', children: adminUsersRoutes }]),
            {
                provide: AdminAuthService,
                useValue: {
                    applySsoFromQueryAsync: vi.fn<AdminAuthService['applySsoFromQueryAsync']>().mockResolvedValue(undefined),
                    sessionUnavailable: (): boolean => false,
                    isAuthenticated: (): boolean => true,
                    isAdmin: (): boolean => true,
                },
            },
            { provide: AdminUsersFacade, useValue: { getLoginEvents, getImpersonationSessions, getUsers, getUser, getUserRoleAudit } },
        ],
    });
});

describe.each(historyRoutes)('Protected history route $path', ({ path, filters, request, emptyText }) => {
    it('hides a stale load failure when navigation selects an invalid period', async () => {
        request.mockReturnValueOnce(throwError(() => new Error('Load failed')));
        const harness = await RouterTestingHarness.create(path);
        const router = TestBed.inject(Router);
        expect(harness.routeNativeElement?.querySelector('fd-admin-load-error')).not.toBeNull();
        expect(request).toHaveBeenCalledOnce();

        await router.navigateByUrl(`${path}?period=custom&from=2026-02-01&to=2026-01-01`);
        harness.detectChanges();

        expect(request).toHaveBeenCalledOnce();
        expect(harness.routeNativeElement?.querySelector('fd-admin-load-error')).toBeNull();
        expect(harness.routeNativeElement?.textContent).toContain(emptyText);
    });

    it('retries the same filtered period and corrects a page made obsolete by the result', async () => {
        request.mockReturnValueOnce(throwError(() => new Error('Load failed')));
        request.mockReturnValueOnce(
            of({ items: [], page: LARGE_PAGE, limit: PAGE_SIZE, totalPages: SECOND_PAGE, totalItems: TWO_PAGES_OF_ITEMS }),
        );
        request.mockReturnValueOnce(
            of({ items: [], page: SECOND_PAGE, limit: PAGE_SIZE, totalPages: SECOND_PAGE, totalItems: TWO_PAGES_OF_ITEMS }),
        );
        const params = new URLSearchParams({
            ...filters,
            from: '2026-02-01',
            to: '2026-02-03',
            period: 'custom',
            page: `${LARGE_PAGE}`,
            search: ' admin ',
        });
        const harness = await RouterTestingHarness.create(`${path}?${params}`);
        const router = TestBed.inject(Router);
        const expectedFilters = { ...filters, fromUtc: '2026-02-01T00:00:00Z', toUtc: '2026-02-04T00:00:00.000Z' };
        expect(request).toHaveBeenCalledExactlyOnceWith(LARGE_PAGE, PAGE_SIZE, 'admin', expectedFilters);
        const retry = harness.routeNativeElement?.querySelector<HTMLButtonElement>('fd-admin-load-error button');
        expect(retry).toBeDefined();
        retry?.click();

        await vi.waitFor(() => {
            expect(router.parseUrl(router.url).queryParamMap.get('page')).toBe(`${SECOND_PAGE}`);
        });
        harness.detectChanges();
        expect(request.mock.calls).toEqual([
            [LARGE_PAGE, PAGE_SIZE, 'admin', expectedFilters],
            [LARGE_PAGE, PAGE_SIZE, 'admin', expectedFilters],
            [SECOND_PAGE, PAGE_SIZE, 'admin', expectedFilters],
        ]);
        expect(harness.routeNativeElement?.querySelector('fd-admin-load-error')).toBeNull();
        expect(harness.routeNativeElement?.textContent).toContain(String(TWO_PAGES_OF_ITEMS));

        await router.navigateByUrl(`${path}?period=custom`);
        harness.detectChanges();
        expect(harness.routeNativeElement?.querySelector('.summary')?.textContent.trim()).toMatch(/: 0$/u);
        expect(request).toHaveBeenCalledTimes(RECOVERY_REQUESTS);
    });
});

it('corrects an obsolete user list page and opens the account from the native row while keeping list filters', async () => {
    const account = {
        id: adminId<'user'>('selected-user'),
        email: 'selected@example.test',
        isActive: false,
        isEmailConfirmed: false,
        roles: ['User'],
        createdOnUtc: adminUtcInstant('2026-01-01T12:00:00Z'),
        deletedAt: adminUtcInstant('2026-02-01T12:00:00Z'),
    };
    getUsers.mockReturnValue(
        of({ items: [account], page: SECOND_PAGE, limit: PAGE_SIZE, totalPages: SECOND_PAGE, totalItems: TWO_PAGES_OF_ITEMS }),
    );
    getUser.mockReturnValueOnce(throwError(() => new Error('Account temporarily unavailable'))).mockReturnValueOnce(of(account));
    const harness = await RouterTestingHarness.create(`/users?page=${LARGE_PAGE}&status=deleted&emailConfirmed=false`);
    const router = TestBed.inject(Router);
    await vi.waitFor(() => {
        expect(router.parseUrl(router.url).queryParamMap.get('page')).toBe(`${SECOND_PAGE}`);
    });
    harness.detectChanges();
    expect(getUsers.mock.calls.map(([page]) => page)).toEqual([LARGE_PAGE, SECOND_PAGE]);

    const row = harness.routeNativeElement?.querySelector<HTMLElement>('tbody tr.users-table__row');
    expect(row?.textContent).toContain(account.email);
    row?.focus();
    row?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
    await vi.waitFor(() => {
        expect(router.url).toBe(`/users/${account.id}?page=${SECOND_PAGE}&status=deleted&emailConfirmed=false`);
    });
    harness.detectChanges();
    expect(getUser).toHaveBeenCalledExactlyOnceWith(account.id);
    expect(getLoginEvents).not.toHaveBeenCalled();
    const retry = harness.routeNativeElement?.querySelector<HTMLButtonElement>('fd-admin-load-error button');
    expect(retry).not.toBeNull();
    retry?.click();
    harness.detectChanges();

    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toContain(account.email);
    expect(getUser.mock.calls).toEqual([[account.id], [account.id]]);
    expect(getUserRoleAudit).toHaveBeenCalledExactlyOnceWith(account.id);
    expect(getLoginEvents).toHaveBeenCalledExactlyOnceWith(FIRST_PAGE, expect.any(Number), null, { userId: account.id });
});

describe.each(historyRoutes)('Search and request replacement $path', ({ path, searchLabel, filters, request }) => {
    it('starts the latest search on page one, preserves filters, and cancels the obsolete request', async () => {
        const pending = new Subject<PagedResponse<never>>();
        request.mockReturnValueOnce(pending);
        const params = new URLSearchParams({ ...filters, page: `${SECOND_PAGE}` });
        const harness = await RouterTestingHarness.create(`${path}?${params}`);
        const router = TestBed.inject(Router);
        expect(pending.observed).toBe(true);

        const page = harness.routeNativeElement;
        const search = inputByLabel(page, searchLabel);
        search.value = 'new results';
        search.dispatchEvent(new Event('input', { bubbles: true }));

        await vi.waitFor(() => {
            expect(request).toHaveBeenLastCalledWith(FIRST_PAGE, PAGE_SIZE, 'new results', filters);
        });
        expect(pending.observed).toBe(false);
        const applied = router.parseUrl(router.url).queryParamMap;
        expect(applied.get('page')).toBe(`${FIRST_PAGE}`);
        for (const [key, value] of Object.entries(filters)) {
            expect(applied.get(key)).toBe(value);
        }
    });
});

function inputByLabel(root: HTMLElement | null, label: string): HTMLInputElement {
    const input = root?.querySelector<HTMLInputElement>(`input[aria-label="${label}"]`);
    if (input === null || input === undefined) {
        throw new Error(`Expected the ${label} search input.`);
    }
    return input;
}

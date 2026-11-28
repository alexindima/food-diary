import { TestBed } from '@angular/core/testing';
import { NavigationEnd, provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { filter as filterEvents, of, Subject, take, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../src/testing/translate-testing.module';
import { ADMIN_DEVELOPMENT_SCREENS, type AdminDevelopmentScreen } from '../../shared/development/admin-development-screen';
import { adminId, adminUtcInstant } from '../../shared/models/semantics/admin-meaning';
import { AdminAuthService } from '../admin-auth/contracts/admin-session';
import { adminUsersRoutes } from './admin-users.routes';
import { AdminUsersService } from './api/admin-users.service';
import type { PagedResponse } from './models/admin-user.models';

const PAGE_SIZE = 20;
const FIRST_PAGE = 1;
const LARGE_PAGE = 5;
const SECOND_PAGE = 2;
const TWO_PAGES_OF_ITEMS = 21;
const RECOVERY_REQUESTS = 3;
const EMPTY_PAGE = { items: [], page: FIRST_PAGE, limit: PAGE_SIZE, totalPages: 1, totalItems: 0 };

const getLoginEvents = vi.fn<AdminUsersService['getLoginEvents']>();
const getImpersonationSessions = vi.fn<AdminUsersService['getImpersonationSessions']>();
const getUsers = vi.fn<AdminUsersService['getUsers']>();
const getUser = vi.fn<AdminUsersService['getUser']>();
const getUserRoleAudit = vi.fn<AdminUsersService['getUserRoleAudit']>();
const getPremiumGrant = vi.fn<AdminUsersService['getPremiumGrant']>();
const attachScreen = vi.fn<(screen: AdminDevelopmentScreen) => () => void>();
const detachScreen = vi.fn<() => void>();

const historyRoutes = [
    {
        path: '/users/login-activity',
        searchLabel: 'Search login activity',
        firstFilter: { key: 'provider', label: 'ADMIN_ACTIVITY.provider' },
        filters: { userId: 'selected-user', provider: 'Google', device: 'mobile' },
        emptyText: 'No login activity found.',
        request: getLoginEvents,
    },
    {
        path: '/users/impersonation-sessions',
        searchLabel: 'Search impersonation sessions',
        firstFilter: { key: 'actorId', label: 'ADMIN_ACTIVITY.actorId' },
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
    getPremiumGrant.mockReturnValue(of(null));
    attachScreen.mockReturnValue(detachScreen);
    TestBed.configureTestingModule({
        providers: [
            provideTranslateTesting(),
            provideRouter(
                [{ path: 'users', children: adminUsersRoutes }],
                withComponentInputBinding({ unmatchedInputBehavior: 'undefinedIfStale' }),
            ),
            {
                provide: AdminAuthService,
                useValue: {
                    applySsoFromQueryAsync: vi.fn<AdminAuthService['applySsoFromQueryAsync']>().mockResolvedValue(undefined),
                    sessionUnavailable: (): boolean => false,
                    isAuthenticated: (): boolean => true,
                    isAdmin: (): boolean => true,
                },
            },
            { provide: ADMIN_DEVELOPMENT_SCREENS, useValue: { attach: attachScreen } },
            {
                provide: AdminUsersService,
                useValue: { getLoginEvents, getImpersonationSessions, getUsers, getUser, getUserRoleAudit, getPremiumGrant },
            },
        ],
    });
});

it.each([{ path: '/users', request: getUsers }, ...historyRoutes.map(({ path, request }) => ({ path, request }))])(
    'keeps $path filters and page when an obsolete response arrives at NavigationEnd',
    async ({ path, request }) => {
        const pending = new Subject<PagedResponse<never>>();
        request
            .mockReturnValueOnce(pending)
            .mockReturnValue(of({ ...EMPTY_PAGE, totalPages: SECOND_PAGE, totalItems: TWO_PAGES_OF_ITEMS }));
        const harness = await RouterTestingHarness.create(`${path}?page=2&search=previous`);
        const router = TestBed.inject(Router);
        const restored = `${path}?search=restored`;
        const pendingAtNavigation = vi.fn<(observed: boolean) => void>();
        router.events
            .pipe(
                filterEvents(event => event instanceof NavigationEnd && event.urlAfterRedirects === restored),
                take(1),
            )
            .subscribe(() => {
                pendingAtNavigation(pending.observed);
                pending.next({ ...EMPTY_PAGE, page: SECOND_PAGE, totalPages: SECOND_PAGE, totalItems: TWO_PAGES_OF_ITEMS });
                pending.complete();
            });

        await router.navigateByUrl(restored);
        await harness.fixture.whenStable();
        harness.detectChanges();

        expect(pendingAtNavigation).toHaveBeenCalledExactlyOnceWith(true);
        expect(router.url).toBe(restored);
        expect(request).toHaveBeenLastCalledWith(1, PAGE_SIZE, 'restored', expect.any(Object));
    },
);

describe.each(historyRoutes)('Protected history route $path', ({ path, filters, firstFilter, request, emptyText }) => {
    it('hides a stale load failure when navigation selects an invalid period', async () => {
        request.mockReturnValueOnce(throwError(() => new Error('Load failed')));
        const harness = await RouterTestingHarness.create(path);
        await harness.fixture.whenStable();
        const router = TestBed.inject(Router);
        expect(harness.routeNativeElement?.querySelector('fd-admin-load-error')).not.toBeNull();
        expect(request).toHaveBeenCalledOnce();

        await router.navigateByUrl(`${path}?period=custom&from=2026-02-01&to=2026-01-01`);
        harness.detectChanges();

        expect(request).toHaveBeenCalledOnce();
        expect(harness.routeNativeElement?.querySelector('fd-admin-load-error')).toBeNull();
        expect(harness.routeNativeElement?.textContent).toContain(emptyText);
    });

    it('retries with the applied period and filters and corrects an obsolete page', async () => {
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
        await harness.fixture.whenStable();
        const router = TestBed.inject(Router);
        const expectedFilters = { ...filters, fromUtc: '2026-02-01T00:00:00Z', toUtc: '2026-02-04T00:00:00.000Z' };
        expect(request).toHaveBeenCalledExactlyOnceWith(LARGE_PAGE, PAGE_SIZE, 'admin', expectedFilters);
        const draft = 'draft-filter';
        const filter = inputByLabel(harness.routeNativeElement, firstFilter.label);
        filter.value = draft;
        filter.dispatchEvent(new Event('input', { bubbles: true }));
        harness.detectChanges();
        expect(request).toHaveBeenCalledOnce();
        const retry = harness.routeNativeElement?.querySelector<HTMLButtonElement>('fd-admin-load-error button');
        expect(retry).toBeDefined();
        retry?.click();

        await vi.waitFor(() => {
            expect(router.parseUrl(router.url).queryParamMap.get('page')).toBe(`${SECOND_PAGE}`);
        });
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(request.mock.calls).toEqual([
            [LARGE_PAGE, PAGE_SIZE, 'admin', expectedFilters],
            [LARGE_PAGE, PAGE_SIZE, 'admin', expectedFilters],
            [SECOND_PAGE, PAGE_SIZE, 'admin', expectedFilters],
        ]);
        expect(router.parseUrl(router.url).queryParamMap.get(firstFilter.key)).toBe(params.get(firstFilter.key));
        expect(harness.routeNativeElement?.querySelector('fd-admin-load-error')).toBeNull();
        expect(harness.routeNativeElement?.textContent).toContain(String(TWO_PAGES_OF_ITEMS));

        await router.navigateByUrl(`${path}?period=custom`);
        harness.detectChanges();
        expect(harness.routeNativeElement?.querySelector('.summary')?.textContent.trim()).toMatch(/: 0$/u);
        expect(request).toHaveBeenCalledTimes(RECOVERY_REQUESTS);
    });
});

it('publishes the visible account list and allows opening only an account on the current page', async () => {
    const account = {
        id: adminId<'user'>('current-account'),
        email: 'current@example.test',
        isActive: true,
        isEmailConfirmed: true,
        roles: ['User'],
        createdOnUtc: adminUtcInstant('2026-01-01T12:00:00Z'),
    };
    const harness = await RouterTestingHarness.create('/users');
    await harness.fixture.whenStable();
    const router = TestBed.inject(Router);
    const screen = attachScreen.mock.lastCall?.[0];
    if (screen?.setFiltersAsync === undefined || screen.openEntityAsync === undefined) {
        throw new Error('Expected the registered Users screen actions.');
    }
    expect(screen.kind).toBe('admin-users');
    expect(screen.read()).toMatchObject({ filters: { status: null }, loading: false, items: [], totalItems: 0 });
    getUsers.mockReturnValueOnce(of({ ...EMPTY_PAGE, items: [account], totalItems: 1 }));
    await screen.setFiltersAsync({ status: 'inactive', search: 'current' });
    await harness.fixture.whenStable();
    expect(getUsers).toHaveBeenLastCalledWith(FIRST_PAGE, PAGE_SIZE, 'current', expect.objectContaining({ status: 'inactive' }));
    expect(screen.read()).toMatchObject({
        filters: { status: 'inactive', search: 'current' },
        loading: false,
        items: [{ id: account.id }],
        totalItems: 1,
    });
    const openEntityAsync = screen.openEntityAsync;
    await expect(async () => {
        await openEntityAsync('off-page-account');
    }).rejects.toThrow('ID is not in the current list.');
    expect(getUser).not.toHaveBeenCalled();
    expect(router.url).toBe('/users?status=inactive&search=current');

    getUser.mockReturnValueOnce(of(account));
    await openEntityAsync(account.id);
    harness.detectChanges();
    expect(router.url).toBe('/users/current-account?status=inactive&search=current');
    expect(getUser).toHaveBeenCalledExactlyOnceWith(account.id);
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe(account.email);
    expect(detachScreen).toHaveBeenCalledOnce();
});

it('renders a list load error and retries the same account filters with native controls', async () => {
    const pending = new Subject<PagedResponse<never>>();
    getUsers.mockReturnValueOnce(pending);
    const harness = await RouterTestingHarness.create('/users?status=inactive&search=support');
    expect(harness.routeNativeElement?.textContent).toContain('Loading users...');
    pending.error(new Error('Accounts request failed'));
    await harness.fixture.whenStable();
    harness.detectChanges();
    const retry = harness.routeNativeElement?.querySelector<HTMLButtonElement>('fd-admin-load-error button');
    expect(retry).toBeInstanceOf(HTMLButtonElement);
    retry?.click();
    await harness.fixture.whenStable();
    harness.detectChanges();

    const expectedFilters = { status: 'inactive', role: '', emailConfirmed: '', lastActiveFrom: '', lastActiveTo: '' };
    expect(getUsers.mock.calls).toEqual([
        [FIRST_PAGE, PAGE_SIZE, 'support', expectedFilters],
        [FIRST_PAGE, PAGE_SIZE, 'support', expectedFilters],
    ]);
    expect(harness.routeNativeElement?.querySelector('fd-admin-load-error')).toBeNull();
    expect(harness.routeNativeElement?.textContent).toContain('No users found.');
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

it.each(['', 'other-account'])('uses the account path when the URL also has matrix id "%s"', async matrixId => {
    const requested = {
        id: adminId<'user'>('requested-account'),
        email: 'requested@example.test',
        isActive: true,
        isEmailConfirmed: true,
        roles: ['User'],
        createdOnUtc: adminUtcInstant('2026-01-01T12:00:00Z'),
    };
    const next = { ...requested, id: adminId<'user'>('next-account'), email: 'next@example.test' };
    getUser.mockImplementation(id => {
        if (id === requested.id) {
            return of(requested);
        }
        return id === next.id ? of(next) : throwError(() => new Error('Unexpected account'));
    });
    const harness = await RouterTestingHarness.create(`/users/${requested.id};id=${matrixId}`);
    expect(getUser).toHaveBeenCalledExactlyOnceWith(requested.id);
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe(requested.email);

    await harness.navigateByUrl(`/users/${requested.id};id=changed-matrix-id`);
    expect(getUser).toHaveBeenCalledExactlyOnceWith(requested.id);
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe(requested.email);

    await harness.navigateByUrl(`/users/${next.id};id=changed-matrix-id`);
    expect(getUser.mock.calls).toEqual([[requested.id], [next.id]]);
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe(next.email);
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

describe.each(historyRoutes)('History pagination $path', ({ path, filters, request }) => {
    it('requests both pages through native navigation while preserving the audit filters', async () => {
        request.mockReturnValue(of({ ...EMPTY_PAGE, totalPages: SECOND_PAGE, totalItems: TWO_PAGES_OF_ITEMS }));
        const params = new URLSearchParams({ ...filters, search: 'admin' });
        const harness = await RouterTestingHarness.create(`${path}?${params}`);
        await harness.fixture.whenStable();
        const router = TestBed.inject(Router);
        const buttons = (): NodeListOf<HTMLButtonElement> | undefined =>
            harness.routeNativeElement?.querySelectorAll<HTMLButtonElement>('fd-ui-pagination button');
        const next = Array.from(buttons() ?? []).at(-1);
        expect(next?.disabled).toBe(false);
        next?.click();
        await vi.waitFor(() => {
            expect(request).toHaveBeenLastCalledWith(SECOND_PAGE, PAGE_SIZE, 'admin', filters);
        });
        await harness.fixture.whenStable();
        harness.detectChanges();

        const query = router.parseUrl(router.url).queryParamMap;
        expect(query.get('page')).toBe(String(SECOND_PAGE));
        for (const [key, value] of Object.entries(filters)) {
            expect(query.get(key)).toBe(value);
        }
        const lastPageNext = Array.from(buttons() ?? []).at(-1);
        expect(lastPageNext).toMatchObject({ disabled: true });
        lastPageNext?.click();
        expect(request).toHaveBeenCalledTimes(SECOND_PAGE);
        const previous = buttons()?.item(0);
        expect(previous?.disabled).toBe(false);
        previous?.click();
        await vi.waitFor(() => {
            expect(request).toHaveBeenLastCalledWith(FIRST_PAGE, PAGE_SIZE, 'admin', filters);
        });
    });
});

function inputByLabel(root: HTMLElement | null, label: string): HTMLInputElement {
    const input = root?.querySelector<HTMLInputElement>(`input[aria-label="${label}"]`);
    if (input === null || input === undefined) {
        throw new Error(`Expected the ${label} search input.`);
    }
    return input;
}

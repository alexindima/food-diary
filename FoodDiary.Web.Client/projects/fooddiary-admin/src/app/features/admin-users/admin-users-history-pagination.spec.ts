import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { NavigationEnd, provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { filter, take } from 'rxjs';
import { expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../src/testing/translate-testing.module';
import { environment } from '../../../environments/environment';
import { AdminAuthService } from '../admin-auth/contracts/admin-session';
import { adminUsersRoutes } from './admin-users.routes';

const API_ROOT = environment.apiUrls.auth.replace(/\/auth$/, '');
const FIRST_PAGE = 1;
const PAGE_SIZE = 20;
const ORIGINAL_PAGE = { data: [], page: FIRST_PAGE, limit: PAGE_SIZE, totalItems: PAGE_SIZE + 1, totalPages: 2 };
const FILTERED_PAGE = { ...ORIGINAL_PAGE, totalItems: 0, totalPages: FIRST_PAGE };

it.each([
    { path: 'login-activity', endpoint: 'login-events' },
    { path: 'impersonation-sessions', endpoint: 'impersonation-sessions' },
])('ignores pending pagination when filtered $path results shrink before rendering', async ({ path, endpoint }) => {
    TestBed.configureTestingModule({
        providers: [
            provideRouter([{ path: 'users', children: adminUsersRoutes }]),
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            {
                provide: AdminAuthService,
                useValue: {
                    applySsoFromQueryAsync: vi.fn<AdminAuthService['applySsoFromQueryAsync']>().mockResolvedValue(undefined),
                    sessionUnavailable: (): boolean => false,
                    isAuthenticated: (): boolean => true,
                    isAdmin: (): boolean => true,
                },
            },
        ],
    });
    const url = `/users/${path}`;
    const api = `${API_ROOT}/admin/users/${endpoint}`;
    const harness = await RouterTestingHarness.create(url);
    const requests = TestBed.inject(HttpTestingController);
    requests.expectOne(request => request.url === api).flush(ORIGINAL_PAGE);
    harness.detectChanges();
    const next = harness.routeNativeElement?.querySelector<HTMLButtonElement>('fd-ui-pagination button:last-child');
    if (next === null || next === undefined) {
        throw new Error('Expected the history pagination controls.');
    }
    expect(next.disabled).toBe(false);

    const router = TestBed.inject(Router);
    const filtered = `${url}?search=recent`;
    const beforeRender = vi.fn<(connected: boolean) => void>();
    router.events
        .pipe(
            filter(event => event instanceof NavigationEnd && event.urlAfterRedirects === filtered),
            take(1),
        )
        .subscribe(() => {
            requests.expectOne(request => request.url === api && request.params.get('search') === 'recent').flush(FILTERED_PAGE);
            beforeRender(next.isConnected);
            next.click();
        });
    await router.navigateByUrl(filtered);
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(beforeRender).toHaveBeenCalledExactlyOnceWith(true);
    expect(router.url).toBe(filtered);
    expect(harness.routeNativeElement?.querySelector('fd-ui-pagination button')).toBeNull();
    requests.expectNone(request => request.url === api);
    requests.verify();
});

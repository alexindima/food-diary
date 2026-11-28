import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { NavigationEnd, provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { filter, of, Subject, take } from 'rxjs';
import { expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../src/testing/translate-testing.module';
import { environment } from '../../../environments/environment';
import { AdminAuthService } from '../admin-auth/contracts/admin-session';
import { adminModerationRoutes } from './admin-moderation.routes';
import { AdminModerationService } from './api/admin-moderation.service';

const PATH = '/moderation';
const API = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/moderation`;
const PAGE_SIZE = 20;
const originalPage = { data: [], page: 1, limit: PAGE_SIZE, totalItems: PAGE_SIZE + 1, totalPages: 2 };
const filteredPage = { ...originalPage, totalItems: 0, totalPages: 1 };

function configureModeration(): void {
    TestBed.configureTestingModule({
        providers: [
            provideRouter(
                [{ path: 'moderation', children: adminModerationRoutes }],
                withComponentInputBinding({ unmatchedInputBehavior: 'undefinedIfStale' }),
            ),
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
}

it('keeps the selected filter when an earlier Next click reaches a now-empty moderation list', async () => {
    configureModeration();
    const harness = await RouterTestingHarness.create(PATH);
    const http = TestBed.inject(HttpTestingController);
    const initial = http.expectOne(request => request.url === API);
    expect(initial.request.params.get('status')).toBe('Pending');
    initial.flush(originalPage);
    await harness.fixture.whenStable();
    harness.detectChanges();
    const next = harness.routeNativeElement?.querySelector<HTMLButtonElement>('fd-ui-pagination button:last-child');
    if (next === null || next === undefined) {
        throw new Error('Expected the moderation paginator.');
    }
    expect(next.disabled).toBe(false);

    const router = TestBed.inject(Router);
    const filtered = `${PATH}?targetId=selected-recipe`;
    const beforeRender = vi.fn<(connected: boolean) => void>();
    router.events
        .pipe(
            filter(event => event instanceof NavigationEnd && event.urlAfterRedirects === filtered),
            take(1),
        )
        .subscribe(() => {
            beforeRender(next.isConnected);
            next.click();
        });
    await router.navigateByUrl(filtered);
    harness.detectChanges();
    http.expectOne(request => request.url === API && request.params.get('targetId') === 'selected-recipe').flush(filteredPage);
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(beforeRender).toHaveBeenCalledExactlyOnceWith(true);
    expect(router.url).toBe(filtered);
    expect(harness.routeNativeElement?.querySelector('tbody')?.textContent).toContain('ADMIN_COMMON.EMPTY');
    expect(harness.routeNativeElement?.querySelector('fd-ui-pagination button')).toBeNull();
    http.expectNone(request => request.url === API);
    http.verify();
});

it('keeps moderation filters and page when an obsolete response arrives at NavigationEnd', async () => {
    configureModeration();
    const response = { ...originalPage, items: [] };
    const pending = new Subject<typeof response>();
    const getReports = vi.fn<AdminModerationService['getReports']>().mockReturnValueOnce(pending).mockReturnValue(of(response));
    TestBed.overrideProvider(AdminModerationService, { useValue: { getReports } });
    const harness = await RouterTestingHarness.create(`${PATH}?page=2&targetId=previous`);
    const router = TestBed.inject(Router);
    const restored = `${PATH}?targetId=restored`;
    const pendingAtNavigation = vi.fn<(pending: boolean) => void>();
    router.events
        .pipe(
            filter(event => event instanceof NavigationEnd && event.urlAfterRedirects === restored),
            take(1),
        )
        .subscribe(() => {
            pendingAtNavigation(pending.observed);
            pending.next({ ...response, page: 2 });
            pending.complete();
        });

    await router.navigateByUrl(restored);
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(pendingAtNavigation).toHaveBeenCalledExactlyOnceWith(true);
    expect(router.url).toBe(restored);
    expect(getReports).toHaveBeenLastCalledWith(1, PAGE_SIZE, 'Pending', expect.objectContaining({ targetId: 'restored' }));
});

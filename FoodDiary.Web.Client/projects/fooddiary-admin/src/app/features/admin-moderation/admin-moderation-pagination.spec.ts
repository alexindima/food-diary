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
import { adminModerationRoutes } from './admin-moderation.routes';

const PATH = '/moderation';
const API = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/moderation`;
const PAGE_SIZE = 20;
const originalPage = { data: [], page: 1, limit: PAGE_SIZE, totalItems: PAGE_SIZE + 1, totalPages: 2 };
const filteredPage = { ...originalPage, totalItems: 0, totalPages: 1 };

it('keeps the selected filter when an earlier Next click reaches a now-empty moderation list', async () => {
    TestBed.configureTestingModule({
        providers: [
            provideRouter([{ path: 'moderation', children: adminModerationRoutes }]),
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
    const harness = await RouterTestingHarness.create(PATH);
    const http = TestBed.inject(HttpTestingController);
    const initial = http.expectOne(request => request.url === API);
    expect(initial.request.params.get('status')).toBe('Pending');
    initial.flush(originalPage);
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
            http.expectOne(request => request.url === API && request.params.get('targetId') === 'selected-recipe').flush(filteredPage);
            beforeRender(next.isConnected);
            next.click();
        });
    await router.navigateByUrl(filtered);
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(beforeRender).toHaveBeenCalledExactlyOnceWith(true);
    expect(router.url).toBe(filtered);
    expect(harness.routeNativeElement?.querySelector('tbody')?.textContent).toContain('ADMIN_COMMON.EMPTY');
    expect(harness.routeNativeElement?.querySelector('fd-ui-pagination button')).toBeNull();
    http.expectNone(request => request.url === API);
    http.verify();
});

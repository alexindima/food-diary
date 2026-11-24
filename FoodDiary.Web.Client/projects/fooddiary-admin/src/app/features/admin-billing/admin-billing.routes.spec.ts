import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { NavigationEnd, provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdUiTabsComponent } from 'fd-ui-kit';
import { filter, of, Subject, take } from 'rxjs';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../shared/models/semantics/admin-meaning';
import { AdminAuthService } from '../admin-auth/contracts/admin-session';
import { adminBillingRoutes } from './admin-billing.routes';
import { AdminBillingService } from './api/admin-billing.service';
import { AdminBillingFacade } from './lib/admin-billing.facade';
import type { AdminBillingRevenueSummary } from './models/admin-billing.models';

@Component({
    template: '',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class OtherPage {}

const PAGE_SIZE = 20;
const TOTAL_PAGES = 2;
const TOTAL_ITEMS = 21;
const STALE_PAGE = 9;
const subscription = {
    id: adminId<'billing-subscription'>('owned-subscription'),
    userId: adminId<'user'>('owned-user'),
    userEmail: 'premium@example.com',
    provider: 'Paddle',
    externalCustomerId: 'cus_owned',
    status: 'active',
    cancelAtPeriodEnd: false,
    createdOnUtc: adminUtcInstant('2026-04-28T00:00:00Z'),
};
const empty = { items: [], page: 1, limit: PAGE_SIZE, totalPages: 0, totalItems: 0 };
type BillingRequests = Pick<
    AdminBillingService,
    'getSubscriptions' | 'getRevenueSummary' | 'getPayments' | 'getWebhookEvents' | 'getCancellations'
>;
type BillingApiMock = { [Method in keyof BillingRequests]: Mock<BillingRequests[Method]> };
const createBillingServiceMock = (): BillingApiMock => ({
    getSubscriptions: vi
        .fn<AdminBillingService['getSubscriptions']>()
        .mockReturnValue(of({ ...empty, items: [subscription], totalPages: TOTAL_PAGES, totalItems: TOTAL_ITEMS })),
    getRevenueSummary: vi
        .fn<AdminBillingService['getRevenueSummary']>()
        .mockReturnValue(of({ fromUtc: '2026-04-01T00:00:00Z', toUtc: '2026-05-01T00:00:00Z', currencies: [] })),
    getPayments: vi.fn<AdminBillingService['getPayments']>().mockReturnValue(of(empty)),
    getWebhookEvents: vi.fn<AdminBillingService['getWebhookEvents']>().mockReturnValue(of(empty)),
    getCancellations: vi.fn<AdminBillingService['getCancellations']>().mockReturnValue(of({ ...empty, reasons: [], feedbackCount: 0 })),
});

describe('Protected billing route and filters', () => {
    it('normalizes the filtered list page and clears an unrelated status on cancellation tab changes', async () => {
        const api = createBillingServiceMock();
        const harness = await openBillingRouteAsync(api, `/billing?page=${STALE_PAGE}&status=active&status=ignored&provider=Paddle`);
        await harness.fixture.whenStable();
        const router = TestBed.inject(Router);
        expect(api.getSubscriptions).toHaveBeenCalledWith(
            STALE_PAGE,
            PAGE_SIZE,
            expect.objectContaining({ status: 'active', provider: 'Paddle' }),
        );
        expect(api.getSubscriptions).toHaveBeenLastCalledWith(
            2,
            PAGE_SIZE,
            expect.objectContaining({ status: 'active', provider: 'Paddle' }),
        );
        const settledQuery = router.parseUrl(router.url).queryParamMap;
        expect(settledQuery.get('page')).toBe('2');
        expect(settledQuery.get('status')).toBe('active');

        const host = requireBillingHost(harness);
        enterBillingFilter(host, 'Search billing records', ' owned@example.test ');
        clickBillingButton(host, 'Apply');
        await harness.fixture.whenStable();
        expect(api.getSubscriptions).toHaveBeenLastCalledWith(1, PAGE_SIZE, expect.objectContaining({ search: 'owned@example.test' }));

        clickBillingButton(host, 'ADMIN_BILLING.CANCELLATIONS.TITLE');
        await harness.fixture.whenStable();
        expect(api.getCancellations).toHaveBeenLastCalledWith(
            1,
            PAGE_SIZE,
            expect.objectContaining({ status: null, provider: 'Paddle', search: 'owned@example.test' }),
        );
        expect(router.parseUrl(router.url).queryParams).not.toHaveProperty('status');
    });

    it('keeps the selected page and does not send a request for invalid pagination intents', async () => {
        const api = createBillingServiceMock();
        const harness = await openBillingRouteAsync(api, '/billing?page=2');
        await harness.fixture.whenStable();
        const billing = harness.routeDebugElement?.injector.get(AdminBillingFacade);
        expect(billing).toBeDefined();
        api.getSubscriptions.mockClear();

        billing?.goToPage(0);
        billing?.goToPage(TOTAL_PAGES + 1);
        await harness.fixture.whenStable();

        expect(api.getSubscriptions).not.toHaveBeenCalled();
        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParams).toMatchObject({ page: '2' });
        expect(requireBillingHost(harness).textContent).toContain('premium@example.com');
    });
});

describe('Billing tab changes', () => {
    it.each([
        ['Subscriptions', 'subscriptions', 'getSubscriptions'],
        ['Payments', 'payments', 'getPayments'],
        ['Webhooks', 'webhook-events', 'getWebhookEvents'],
    ] as const)('opens %s with a clean status after reviewing scheduled cancellations', async (label, tab, request) => {
        const api = createBillingServiceMock();
        const harness = await openBillingRouteAsync(api, '/billing?tab=cancellations&status=scheduled');
        expect(api.getCancellations).toHaveBeenCalledWith(1, PAGE_SIZE, expect.objectContaining({ status: 'scheduled' }));

        clickBillingButton(requireBillingHost(harness), label);
        await harness.fixture.whenStable();

        expect(api[request]).toHaveBeenCalledExactlyOnceWith(1, PAGE_SIZE, expect.objectContaining({ status: null }));
        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParams).toMatchObject({ tab });
        expect(router.parseUrl(router.url).queryParams).not.toHaveProperty('status');
    });

    it('keeps the current route when a tab control publishes an unsupported selection', async () => {
        const api = createBillingServiceMock();
        const harness = await openBillingRouteAsync(api, '/billing?tab=subscriptions');
        const tabs = harness.fixture.debugElement.query(By.directive(FdUiTabsComponent)).injector.get(FdUiTabsComponent);
        tabs.selectedValue.set('scheduled');
        await harness.fixture.whenStable();

        expect(api.getSubscriptions).toHaveBeenCalledOnce();
        expect(TestBed.inject(Router).url).toBe('/billing?tab=subscriptions');
        expect(requireBillingHost(harness).querySelector('[aria-label="Billing subscriptions"]')).not.toBeNull();
    });
});

describe('Billing report supersession', () => {
    it('refreshes current drafts and cancels both outstanding reads on departure', async () => {
        const api = createBillingServiceMock();
        const firstRecords = new Subject<typeof empty>();
        const refreshedRecords = new Subject<typeof empty>();
        const firstRevenue = new Subject<AdminBillingRevenueSummary>();
        const refreshedRevenue = new Subject<AdminBillingRevenueSummary>();
        api.getSubscriptions.mockReturnValueOnce(firstRecords).mockReturnValueOnce(refreshedRecords);
        api.getRevenueSummary.mockReturnValueOnce(firstRevenue).mockReturnValueOnce(refreshedRevenue);
        const url = '/billing?search=applied&period=custom&from=2024-02-29&to=2024-02-29';
        const harness = await openBillingRouteAsync(api, url);
        expect(firstRecords.observed).toBe(true);
        expect(firstRevenue.observed).toBe(true);
        const host = requireBillingHost(harness);

        enterBillingFilter(host, 'Search billing records', 'draft');
        harness.detectChanges();
        expect(api.getSubscriptions).toHaveBeenCalledOnce();
        clickBillingButton(host, 'Refresh');
        harness.detectChanges();

        expect(firstRecords.observed).toBe(false);
        expect(firstRevenue.observed).toBe(false);
        expect(refreshedRecords.observed).toBe(true);
        expect(refreshedRevenue.observed).toBe(true);
        expect(api.getSubscriptions).toHaveBeenLastCalledWith(
            1,
            PAGE_SIZE,
            expect.objectContaining({ search: 'draft', fromUtc: '2024-02-29T00:00:00.000Z', toUtc: '2024-02-29T23:59:59.9999999Z' }),
        );
        expect(api.getRevenueSummary).toHaveBeenLastCalledWith({ fromUtc: '2024-02-29T00:00:00Z', toUtc: '2024-03-01T00:00:00Z' });
        expect(TestBed.inject(Router).url).toBe(url);

        await harness.navigateByUrl('/other', OtherPage);
        expect(refreshedRecords.observed).toBe(false);
        expect(refreshedRevenue.observed).toBe(false);
    });

    it.each([
        ['payments', 'getPayments'],
        ['webhook-events', 'getWebhookEvents'],
        ['cancellations', 'getCancellations'],
    ] as const)('ignores obsolete %s rows and failure after switching tabs', async (tab, request) => {
        const api = createBillingServiceMock();
        const pending = new Subject<typeof empty & { reasons: never[]; feedbackCount: number }>();
        api[request].mockReturnValueOnce(pending);
        const harness = await openBillingRouteAsync(api, `/billing?tab=${tab}`);
        expect(requireBillingHost(harness).textContent).toContain('Loading billing records');
        expect(pending.observed).toBe(true);

        const router = TestBed.inject(Router);
        await router.navigateByUrl('/billing?tab=subscriptions');
        await harness.fixture.whenStable();
        expect(pending.observed).toBe(false);
        pending.next({ ...empty, reasons: [], feedbackCount: 0 });
        pending.error(new Error('obsolete request failed'));
        harness.detectChanges();

        const host = requireBillingHost(harness);
        expect(host.textContent).toContain('premium@example.com');
        expect(host.textContent).toContain(`Total records: ${TOTAL_ITEMS}`);
        expect(host.querySelector('[role="alert"]:not([hidden])')).toBeNull();
    });
});

it('keeps the new URL when the previous page responds during navigation', async () => {
    const api = createBillingServiceMock();
    const pending = new Subject<typeof empty>();
    api.getSubscriptions.mockReturnValueOnce(pending);
    const harness = await openBillingRouteAsync(api, '/billing?page=2&search=previous');
    const router = TestBed.inject(Router);
    const restoredUrl = '/billing?search=restored';
    const pendingAtNavigation = vi.fn<(observed: boolean) => void>();
    router.events
        .pipe(
            filter(event => event instanceof NavigationEnd && event.urlAfterRedirects === restoredUrl),
            take(1),
        )
        .subscribe(() => {
            pendingAtNavigation(pending.observed);
            pending.next({ ...empty, page: TOTAL_PAGES, totalPages: TOTAL_PAGES, totalItems: TOTAL_ITEMS });
        });

    await router.navigateByUrl(restoredUrl);
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(pendingAtNavigation).toHaveBeenCalledExactlyOnceWith(true);
    expect(router.url).toBe(restoredUrl);
    expect(api.getSubscriptions).toHaveBeenLastCalledWith(1, PAGE_SIZE, expect.objectContaining({ search: 'restored' }));
    expect(requireBillingHost(harness).textContent).toContain(subscription.userEmail);
});

async function openBillingRouteAsync(api: BillingApiMock, url: string): Promise<RouterTestingHarness> {
    TestBed.configureTestingModule({
        providers: [
            provideRouter(
                [
                    { path: 'billing', children: adminBillingRoutes },
                    { path: 'other', component: OtherPage },
                ],
                withComponentInputBinding({ unmatchedInputBehavior: 'undefinedIfStale' }),
            ),
            provideTranslateTesting(),
            { provide: AdminBillingService, useValue: api },
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
    return RouterTestingHarness.create(url);
}

function requireBillingHost(harness: RouterTestingHarness): HTMLElement {
    if (harness.routeNativeElement === null) {
        throw new Error('Expected the guarded Billing route to render');
    }
    return harness.routeNativeElement;
}

function enterBillingFilter(host: HTMLElement, labelText: string, value: string): void {
    const label = Array.from(host.querySelectorAll('label')).find(item => item.textContent.trim() === labelText);
    const input = label === undefined ? null : host.querySelector<HTMLInputElement>(`input[id="${label.htmlFor}"]`);
    if (input === null) {
        throw new Error(`Expected Billing input ${labelText}`);
    }
    input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true }));
}

function clickBillingButton(host: HTMLElement, text: string): void {
    const control = Array.from(host.querySelectorAll('button')).find(button => button.textContent.trim() === text);
    if (control === undefined) {
        throw new Error(`Expected Billing action ${text}`);
    }
    control.click();
}

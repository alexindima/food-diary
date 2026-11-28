import { formatDate } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminBillingService } from '../api/admin-billing.service';
import { AdminBillingFacade } from '../lib/admin-billing.facade';
import type {
    AdminBillingPayment,
    AdminBillingRevenueSummary,
    AdminBillingSubscription,
    AdminBillingWebhookEvent,
} from '../models/admin-billing.models';
import { AdminBillingComponent } from './admin-billing';

const PAGE_SIZE = 20;
const TOTAL_PAGES = 2;
const TOTAL_ITEMS = 21;
const THREE_DECIMAL_AMOUNT = 7.991;
const TWO_DECIMAL_AMOUNT = 7.99;
const CANCELLATION_TOTAL = 41;
const CANCELLATION_FEEDBACK_COUNT = 7;

type BillingApiMock = {
    getRevenueSummary: ReturnType<typeof vi.fn>;
    getSubscriptions: ReturnType<typeof vi.fn>;
    getPayments: ReturnType<typeof vi.fn>;
    getWebhookEvents: ReturnType<typeof vi.fn>;
    getCancellations: ReturnType<typeof vi.fn>;
};
type SubscriptionsPage = typeof subscriptionsPage;
type BillingTestContext = {
    billingApi: BillingApiMock;
    billing: AdminBillingFacade;
    component: AdminBillingComponent;
    fixture: RouterTestingHarness['fixture'];
};

async function setupBillingAsync(billingApi: BillingApiMock = createBillingServiceMock()): Promise<BillingTestContext> {
    await TestBed.configureTestingModule({
        providers: [
            provideRouter(
                [{ path: '', component: AdminBillingComponent }],
                withComponentInputBinding({ unmatchedInputBehavior: 'undefinedIfStale' }),
            ),
            provideTranslateTesting(),
            { provide: AdminBillingService, useValue: billingApi },
        ],
    }).compileComponents();

    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/', AdminBillingComponent);
    const fixture = harness.fixture;
    const billing = harness.routeDebugElement?.injector.get(AdminBillingFacade);
    if (billing === undefined) {
        throw new Error('Expected the billing page facade');
    }
    fixture.detectChanges();

    return { billingApi, billing, component, fixture };
}

function createBillingServiceMock(): BillingApiMock {
    return {
        getCancellations: vi
            .fn()
            .mockReturnValue(of({ items: [], page: 1, limit: PAGE_SIZE, totalPages: 0, totalItems: 0, feedbackCount: 0, reasons: [] })),
        getRevenueSummary: vi.fn().mockReturnValue(
            of({
                fromUtc: '2026-04-01T00:00:00Z',
                toUtc: '2026-05-01T00:00:00Z',
                currencies: [],
            }),
        ),
        getSubscriptions: vi.fn().mockReturnValue(of(subscriptionsPage)),
        getPayments: vi.fn().mockReturnValue(
            of({
                items: [
                    {
                        id: 'payment-1',
                        userId: 'user-1',
                        userEmail: 'buyer@example.com',
                        provider: 'Paddle',
                        externalPaymentId: 'pay_123',
                        status: 'paid',
                        kind: 'webhook',
                        createdOnUtc: '2026-04-28T00:00:00Z',
                    },
                ],
                page: 1,
                limit: PAGE_SIZE,
                totalPages: 1,
                totalItems: 1,
            }),
        ),
        getWebhookEvents: vi.fn().mockReturnValue(
            of({
                items: [
                    {
                        id: 'event-row-1',
                        provider: 'Paddle',
                        eventId: 'evt_1',
                        eventType: 'transaction.completed',
                        status: 'processed',
                        processedAtUtc: '2026-04-28T00:00:00Z',
                        createdOnUtc: '2026-04-28T00:00:00Z',
                    },
                ],
                page: 1,
                limit: PAGE_SIZE,
                totalPages: 1,
                totalItems: 1,
            }),
        ),
    };
}

const subscriptionsPage = {
    items: [
        {
            id: adminId<'billing-subscription'>('subscription-1'),
            userId: adminId<'user'>('user-1'),
            userEmail: 'premium@example.com',
            provider: 'Paddle',
            externalCustomerId: 'cus_123',
            status: 'active',
            cancelAtPeriodEnd: false,
            createdOnUtc: adminUtcInstant('2026-04-28T00:00:00Z'),
        },
    ],
    page: 1,
    limit: PAGE_SIZE,
    totalPages: TOTAL_PAGES,
    totalItems: TOTAL_ITEMS,
} satisfies {
    items: AdminBillingSubscription[];
    page: number;
    limit: number;
    totalPages: number;
    totalItems: number;
};

describe('AdminBillingComponent loading', () => {
    it.each([
        [THREE_DECIMAL_AMOUNT, '7.991 BHD'],
        [TWO_DECIMAL_AMOUNT, '7.99 BHD'],
        [0, '0.00 BHD'],
    ])('should preserve payment precision for %s', async (amount, expected) => {
        const api = createBillingServiceMock();
        const payment: AdminBillingPayment = {
            id: adminId<'billing-payment'>('payment-precision'),
            userId: adminId<'user'>('user-1'),
            userEmail: null,
            provider: 'Stripe',
            externalPaymentId: 'in_paid',
            status: 'completed',
            kind: 'transaction',
            createdOnUtc: adminUtcInstant('2026-04-28T00:00:00Z'),
            amount,
            currency: 'BHD',
        };
        api.getPayments.mockReturnValueOnce(of({ items: [payment], page: 1, limit: PAGE_SIZE, totalPages: 1, totalItems: 1 }));
        const { billing, fixture } = await setupBillingAsync(api);
        await TestBed.inject(Router).navigateByUrl('/?tab=payments');
        await fixture.whenStable();

        expect(billing.paymentItems()[0]?.amountText).toBe(expected);
    });

    it('should load subscriptions on init', async () => {
        const { billingApi, billing, fixture } = await setupBillingAsync();
        await fixture.whenStable();

        expect(billingApi.getSubscriptions).toHaveBeenCalledWith(1, PAGE_SIZE, {
            provider: null,
            status: null,
            kind: null,
            search: null,
            fromUtc: null,
            toUtc: null,
        });
        expect(billing.subscriptions()).toEqual(subscriptionsPage.items);
        expect(billing.totalPages()).toBe(TOTAL_PAGES);
        expect(billing.totalItems()).toBe(TOTAL_ITEMS);
        expect(billing.isLoading()).toBe(false);
    });

    it('should clear state on load error', async () => {
        const { billingApi, billing, fixture } = await setupBillingAsync();
        await fixture.whenStable();
        billingApi.getSubscriptions.mockReturnValueOnce(throwError(() => new Error('network')));

        billing.load();
        await fixture.whenStable();

        expect(billing.subscriptions()).toEqual([]);
        expect(billing.totalItems()).toBe(0);
        expect(billing.recordsFailed()).toBe(true);
        expect(billing.isLoading()).toBe(false);
    });

    it('should ignore stale load responses after tab changes', async () => {
        const billingApi = createBillingServiceMock();
        const pendingSubscriptions = new Subject<SubscriptionsPage>();
        billingApi.getSubscriptions.mockReturnValueOnce(pendingSubscriptions.asObservable());
        const { billing, fixture } = await setupBillingAsync(billingApi);
        expect(pendingSubscriptions.observed).toBe(true);

        billing.setTab('payments');
        await fixture.whenStable();
        expect(pendingSubscriptions.observed).toBe(false);
        pendingSubscriptions.next({
            ...subscriptionsPage,
            totalItems: 99,
        });

        expect(billing.activeTab()).toBe('payments');
        expect(billing.subscriptions()).toEqual([]);
        expect(billing.totalItems()).toBe(1);
        expect(billing.isLoading()).toBe(false);
    });
});

describe('AdminBillingComponent filters', () => {
    it('keeps a relative preset through filters, tab changes and paging; reset clears it', async () => {
        const { billing, fixture } = await setupBillingAsync();
        const router = TestBed.inject(Router);
        await router.navigateByUrl('/?period=7d');
        await fixture.whenStable();
        billing.provider.set('Paddle');
        billing.applyFilters();
        await fixture.whenStable();
        expect(new URL(router.url, 'http://localhost').searchParams.get('period')).toBe('7d');
        billing.goToPage(2);
        await fixture.whenStable();
        expect(new URL(router.url, 'http://localhost').searchParams.get('period')).toBe('7d');
        billing.setTab('payments');
        await fixture.whenStable();
        expect(new URL(router.url, 'http://localhost').searchParams.get('period')).toBe('7d');
        billing.resetFilters();
        await fixture.whenStable();
        const params = new URL(router.url, 'http://localhost').searchParams;
        expect(params.get('period')).toBe('all');
        expect(params.has('from')).toBe(false);
        expect(params.has('provider')).toBe(false);
    });

    it('restores filters and the tab for an invalid range, then reset recovers without requesting invalid dates', async () => {
        const { billingApi, billing, fixture } = await setupBillingAsync();
        const router = TestBed.inject(Router);
        billingApi.getPayments.mockClear();
        await router.navigateByUrl('/?tab=payments&period=custom&from=2026-02-31&to=2026-10-01&provider=Paddle');
        await fixture.whenStable();
        expect(billing.activeTab()).toBe('payments');
        expect(billing.provider()).toBe('Paddle');
        expect(billingApi.getPayments).not.toHaveBeenCalled();
        billing.resetFilters();
        await fixture.whenStable();
        expect(billingApi.getPayments).toHaveBeenCalled();
        expect(new URL(router.url, 'http://localhost').searchParams.get('period')).toBe('all');
    });
});

describe('Billing refresh validation', () => {
    it('does not refresh stale payment or revenue dates after opening an invalid period', async () => {
        const { billingApi, fixture } = await setupBillingAsync();
        const router = TestBed.inject(Router);
        await router.navigateByUrl('/?tab=payments&period=custom&from=2026-04-01&to=2026-04-30');
        await fixture.whenStable();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).toContain('pay_123');
        billingApi.getPayments.mockClear();
        billingApi.getRevenueSummary.mockClear();

        await router.navigateByUrl('/?tab=payments&period=custom&from=2026-02-31&to=2026-04-30');
        await fixture.whenStable();
        fixture.detectChanges();
        const refresh = Array.from(host.querySelectorAll('button')).find(button => button.textContent.trim() === 'Refresh');
        expect(refresh).toBeDefined();
        refresh?.click();
        await fixture.whenStable();

        expect(billingApi.getPayments).not.toHaveBeenCalled();
        expect(billingApi.getRevenueSummary).not.toHaveBeenCalled();
        expect(host.textContent).not.toContain('pay_123');
    });
});

describe('Billing API filters', () => {
    it('should switch to payments and include kind filter', async () => {
        const { billingApi, billing, fixture } = await setupBillingAsync();
        await fixture.whenStable();
        billing.kind.set('webhook');
        billing.setTab('payments');
        await fixture.whenStable();

        expect(billing.activeTab()).toBe('payments');
        expect(billing.page()).toBe(1);
        expect(billingApi.getPayments).toHaveBeenCalledWith(1, PAGE_SIZE, {
            provider: null,
            status: null,
            kind: 'webhook',
            search: null,
            fromUtc: null,
            toUtc: null,
        });
        expect(billing.payments()[0].externalPaymentId).toBe('pay_123');
    });

    it('should apply filters with utc day bounds', async () => {
        const { billingApi, billing, fixture } = await setupBillingAsync();
        await fixture.whenStable();
        billing.provider.set(' Paddle ');
        billing.status.set(' paid ');
        billing.search.set(' buyer@example.com ');
        billing.fromDate.set('2026-04-01');
        billing.toDate.set('2026-04-30');

        billing.applyFilters();
        await fixture.whenStable();

        expect(billingApi.getSubscriptions).toHaveBeenLastCalledWith(1, PAGE_SIZE, {
            provider: 'Paddle',
            status: 'paid',
            kind: null,
            search: 'buyer@example.com',
            fromUtc: '2026-04-01T00:00:00.000Z',
            toUtc: '2026-04-30T23:59:59.9999999Z',
        });
    });
});

describe('AdminBilling inclusive calendar-day boundaries', () => {
    const cases = [
        { tab: 'subscriptions', method: 'getSubscriptions' },
        { tab: 'payments', method: 'getPayments' },
        { tab: 'webhook-events', method: 'getWebhookEvents' },
    ] as const;

    it.each(cases)('preserves sub-millisecond inclusion for $tab without changing revenue bounds', async ({ tab, method }) => {
        const { billingApi, fixture } = await setupBillingAsync();
        await TestBed.inject(Router).navigateByUrl(`/?tab=${tab}&period=custom&from=2024-02-29&to=2024-02-29`);
        await fixture.whenStable();
        expect(billingApi[method]).toHaveBeenLastCalledWith(
            1,
            PAGE_SIZE,
            expect.objectContaining({ fromUtc: '2024-02-29T00:00:00.000Z', toUtc: '2024-02-29T23:59:59.9999999Z' }),
        );
        expect(billingApi.getRevenueSummary).toHaveBeenLastCalledWith(
            expect.objectContaining({ fromUtc: '2024-02-29T00:00:00Z', toUtc: '2024-03-01T00:00:00Z' }),
        );
    });
});

describe('AdminBillingComponent metadata', () => {
    it('should format metadata json for side panel', async () => {
        const { billing, fixture } = await setupBillingAsync();
        await fixture.whenStable();
        billing.showMetadata('{"payment_id":"pay_123"}');

        expect(billing.selectedMetadata()).toContain('"payment_id": "pay_123"');
    });

    it('removes the previous receipt from the panel when the next provider response omits metadata', async () => {
        const { billing, fixture } = await setupBillingAsync();
        const host = fixture.nativeElement as HTMLElement;
        billing.showMetadata('{"receipt":"prior"}');
        fixture.detectChanges();
        expect(host.querySelector('fd-admin-billing-metadata-panel pre')?.textContent).toContain('"receipt": "prior"');

        billing.showMetadata(null);
        fixture.detectChanges();

        expect(host.querySelector('fd-admin-billing-metadata-panel')).toBeNull();
    });

    it('opens malformed provider metadata as plain text and closes the panel from the toolbar', async () => {
        const api = createBillingServiceMock();
        const providerMetadataJson = '<b>legacy receipt</b>';
        const payment: AdminBillingPayment = {
            id: adminId<'billing-payment'>('receipt-1'),
            userId: adminId<'user'>('user-1'),
            userEmail: 'customer@example.test',
            provider: 'Paddle',
            externalPaymentId: 'pay_12345678901234567890',
            status: 'paid',
            kind: 'transaction',
            createdOnUtc: adminUtcInstant('2026-04-28T00:00:00Z'),
            providerMetadataJson,
            amount: TWO_DECIMAL_AMOUNT,
        };
        api.getPayments.mockReturnValue(of({ items: [payment], totalPages: 1, totalItems: 1 }));
        const { fixture } = await setupBillingAsync(api);
        await TestBed.inject(Router).navigateByUrl('/?tab=payments');
        await fixture.whenStable();
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const row = host.querySelector('table[aria-label="Billing payments"] tbody tr');
        if (row === null) {
            throw new Error('The payment row was not rendered');
        }
        expect(row.textContent).toContain('pay_1234...567890');
        expect(row.querySelectorAll('td')[5].textContent.trim()).toBe('7.99');
        row.querySelector('button')?.click();
        fixture.detectChanges();

        const panel = host.querySelector('fd-admin-billing-metadata-panel');
        if (panel === null) {
            throw new Error('The receipt panel did not open');
        }
        expect(panel.querySelector('pre')?.textContent).toBe(providerMetadataJson);
        expect(panel.querySelector('pre b')).toBeNull();
        panel.querySelector('button')?.click();
        fixture.detectChanges();
        expect(host.querySelector('fd-admin-billing-metadata-panel')).toBeNull();
    });
});

describe('Billing record and revenue failures', () => {
    it.each([
        ['payments', 'getPayments', 'No payments found.'],
        ['webhook-events', 'getWebhookEvents', 'No webhook events found.'],
        ['cancellations', 'getCancellations', 'ADMIN_BILLING.CANCELLATIONS.EMPTY'],
    ] as const)('retries the failed %s list without refreshing revenue or using unapplied filters', async (tab, request, empty) => {
        const { billingApi, billing, fixture } = await setupBillingAsync();
        const router = TestBed.inject(Router);
        await router.navigateByUrl(`/?tab=${tab}&search=applied`);
        await fixture.whenStable();
        billingApi[request].mockReturnValueOnce(throwError(() => ({ status: 503 })));
        const host = fixture.nativeElement as HTMLElement;
        const refresh = Array.from(host.querySelectorAll('button')).find(button => button.textContent.trim() === 'Refresh');
        expect(refresh).toBeDefined();
        refresh?.click();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(host.querySelector('[role="alert"]:not([hidden])')?.textContent).toContain('ADMIN_COMMON.LOAD_ERROR');
        expect(host.textContent).not.toContain(empty);
        expect(host.querySelector('.admin-billing__summary')).toBeNull();
        const failedRequest = billingApi[request].mock.lastCall;
        const revenueRequests = billingApi.getRevenueSummary.mock.calls.length;
        const recordsRequests = billingApi[request].mock.calls.length;
        billing.search.set('draft');
        host.querySelector<HTMLButtonElement>('fd-admin-load-error button')?.click();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(billingApi[request].mock.calls).toHaveLength(recordsRequests + 1);
        expect(billingApi[request].mock.lastCall).toEqual(failedRequest);
        expect(billingApi.getRevenueSummary.mock.calls).toHaveLength(revenueRequests);
        expect(host.querySelector('fd-admin-load-error')).toBeNull();
        expect(host.querySelector('.admin-billing__summary')).not.toBeNull();
    });
});

it('retries revenue using the committed period without refreshing the visible records or applying drafts', async () => {
    const { billingApi, billing, fixture } = await setupBillingAsync();
    const recovery = new Subject<AdminBillingRevenueSummary>();
    billingApi.getRevenueSummary.mockReturnValueOnce(throwError(() => new Error('revenue unavailable'))).mockReturnValueOnce(recovery);
    const router = TestBed.inject(Router);
    const url = '/?tab=payments&period=custom&from=2026-04-30&to=2026-04-30&search=applied';
    await router.navigateByUrl(url);
    await fixture.whenStable();
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    const records = host.querySelector('[aria-label="Billing payments"]');
    const retry = host.querySelector<HTMLButtonElement>(
        'section[aria-labelledby="billing-revenue-error-title"] fd-admin-load-error button',
    );
    expect(records?.textContent).toContain('buyer@example.com');
    expect(host.querySelector('#billing-revenue-title')).toBeNull();
    expect(retry).not.toBeNull();
    billing.search.set('unapplied search');
    billing.fromDate.set('2026-01-01');
    retry?.click();
    fixture.detectChanges();

    const request = { fromUtc: '2026-04-30T00:00:00Z', toUtc: '2026-05-01T00:00:00Z' };
    expect(billingApi.getRevenueSummary.mock.calls.slice(1)).toEqual([[request], [request]]);
    expect(billingApi.getPayments).toHaveBeenCalledOnce();
    expect(host.querySelector('[aria-label="Billing payments"]')).toBe(records);
    recovery.next({ ...request, currencies: [] });
    recovery.complete();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(host.querySelector('#billing-revenue-title')).not.toBeNull();
    expect(host.querySelector('fd-admin-load-error')).toBeNull();
    expect(host.querySelector('[aria-label="Billing payments"]')).toBe(records);
    expect(router.url).toBe(url);
    expect(billing.search()).toBe('unapplied search');
    expect(billing.fromDate()).toBe('2026-01-01');
});

describe('Webhook receipt details', () => {
    it('prefers receipt time for an unprocessed retry and falls back to creation time on legacy records', async () => {
        const api = createBillingServiceMock();
        const receivedAtUtc = adminUtcInstant('2026-04-28T05:00:00Z');
        const createdOnUtc = adminUtcInstant('2026-04-27T05:00:00Z');
        const retry: AdminBillingWebhookEvent = {
            id: adminId<'billing-webhook-event'>('retry-row'),
            provider: 'Paddle',
            eventId: 'evt_retry',
            eventType: 'transaction.completed',
            receivedAtUtc,
            createdOnUtc,
            status: 'retrying',
            processedAtUtc: null,
            attemptCount: 2,
            nextAttemptAtUtc: adminUtcInstant('2026-04-29T05:00:00Z'),
            errorMessage: 'Provider timeout',
            payloadJson: '{"attempt":2}',
        };
        const legacy: AdminBillingWebhookEvent = {
            id: adminId<'billing-webhook-event'>('legacy-row'),
            provider: 'Paddle',
            eventId: 'evt_legacy',
            eventType: 'transaction.completed',
            createdOnUtc,
            status: 'queued',
        };
        api.getWebhookEvents.mockReturnValue(of({ items: [retry, legacy], totalPages: 1, totalItems: 2 }));
        const { fixture } = await setupBillingAsync(api);
        await TestBed.inject(Router).navigateByUrl('/?tab=webhook-events');
        await fixture.whenStable();
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        const rows = host.querySelectorAll<HTMLTableRowElement>('table[aria-label="Billing webhook events"] tbody tr');
        expect(rows).toHaveLength(2);
        expect(rows[0].cells[0].textContent.trim()).toBe(formatDate(receivedAtUtc, 'short', 'en-US'));
        expect(rows[1].cells[0].textContent.trim()).toBe(formatDate(createdOnUtc, 'short', 'en-US'));
        expect(rows[0].textContent).toContain('Provider timeout');
        expect(rows[0].textContent).toContain(retry.nextAttemptAtUtc);
        expect(rows[1].querySelector('button')?.disabled).toBe(true);
        rows[0].querySelector('button')?.click();
        fixture.detectChanges();
        expect(host.querySelector('fd-admin-billing-metadata-panel pre')?.textContent).toContain('"attempt": 2');
    });
});

describe('Admin cancellation report', () => {
    it('uses next UTC midnight as the exclusive cancellation date bound', async () => {
        const { billingApi, fixture } = await setupBillingAsync();
        await TestBed.inject(Router).navigateByUrl('/?tab=cancellations&period=custom&from=2024-02-29&to=2024-02-29');
        await fixture.whenStable();
        expect(billingApi.getCancellations, 'Cancellation report must include the entire final UTC calendar day.').toHaveBeenLastCalledWith(
            1,
            PAGE_SIZE,
            expect.objectContaining({ fromUtc: '2024-02-29T00:00:00.000Z', toUtc: '2024-03-01T00:00:00Z' }),
        );
    });

    it('shows filtered totals and reasons independently of the current page rows', async () => {
        const api = createBillingServiceMock();
        api.getCancellations.mockReturnValue(
            of({
                items: [
                    {
                        id: adminId<'billing-cancellation'>('cancellation-1'),
                        userId: adminId<'user'>('user-1'),
                        userEmail: 'synthetic@example.test',
                        provider: 'Paddle',
                        externalSubscriptionId: 'sub_owned',
                        plan: 'monthly',
                        source: 'fooddiary',
                        outcome: 'scheduled',
                        requestedAtUtc: adminUtcInstant('2026-11-01T10:00:00Z'),
                        effectiveAtUtc: adminUtcInstant('2026-12-01T10:00:00Z'),
                        reasonCode: null,
                        comment: '<script>feedback</script>',
                        feedbackSubmittedAtUtc: adminUtcInstant('2026-11-01T10:01:00Z'),
                    },
                ],
                page: 1,
                limit: PAGE_SIZE,
                totalPages: 3,
                totalItems: 41,
                feedbackCount: 7,
                reasons: [
                    { reasonCode: 'too_expensive', count: 6 },
                    { reasonCode: null, count: 35 },
                ],
            }),
        );
        const { billing, fixture } = await setupBillingAsync(api);
        await fixture.whenStable();
        api.getRevenueSummary.mockClear();
        await TestBed.inject(Router).navigateByUrl('/?tab=cancellations');
        await fixture.whenStable();
        expect(api.getRevenueSummary).not.toHaveBeenCalled();
        fixture.detectChanges();
        expect(api.getCancellations).toHaveBeenCalledOnce();
        expect(billing.totalItems()).toBe(CANCELLATION_TOTAL);
        expect(billing.cancellationFeedbackCount()).toBe(CANCELLATION_FEEDBACK_COUNT);
        expect(billing.cancellationReasons()).toEqual([
            { reasonCode: 'too_expensive', count: 6 },
            { reasonCode: null, count: 35 },
        ]);
        const host = fixture.nativeElement as HTMLElement;
        const report = host.querySelector('fd-admin-billing-cancellations-table');
        expect(report?.textContent).toContain('<script>feedback</script>');
        expect(report?.querySelector('script')).toBeNull();
    });
});

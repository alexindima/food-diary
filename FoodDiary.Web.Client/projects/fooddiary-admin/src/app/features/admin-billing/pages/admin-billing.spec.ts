import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminBillingService } from '../api/admin-billing.service';
import { AdminBillingFacade } from '../lib/admin-billing.facade';
import type { AdminBillingSubscription } from '../models/admin-billing.models';
import { AdminBillingComponent } from './admin-billing';

const PAGE_SIZE = 20;
const TOTAL_PAGES = 2;
const TOTAL_ITEMS = 21;
const THREE_DECIMAL_AMOUNT = 7.991;
const TWO_DECIMAL_AMOUNT = 7.99;

type BillingApiMock = {
    getRevenueSummary: ReturnType<typeof vi.fn>;
    getSubscriptions: ReturnType<typeof vi.fn>;
    getPayments: ReturnType<typeof vi.fn>;
    getWebhookEvents: ReturnType<typeof vi.fn>;
};
type SubscriptionsPage = typeof subscriptionsPage;
type BillingTestContext = {
    billingApi: BillingApiMock;
    billing: AdminBillingFacade;
    component: AdminBillingComponent;
    fixture: ComponentFixture<AdminBillingComponent>;
};

async function setupBillingAsync(billingApi: BillingApiMock = createBillingServiceMock()): Promise<BillingTestContext> {
    await TestBed.configureTestingModule({
        imports: [AdminBillingComponent],
        providers: [
            provideRouter([]),
            provideTranslateTesting(),
            AdminBillingFacade,
            { provide: AdminBillingService, useValue: billingApi },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(AdminBillingComponent);
    const component = fixture.componentInstance;
    const billing = TestBed.inject(AdminBillingFacade);
    fixture.detectChanges();

    return { billingApi, billing, component, fixture };
}

function createBillingServiceMock(): BillingApiMock {
    return {
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
        const { billing, fixture } = await setupBillingAsync();
        await fixture.whenStable();
        billing.payments.set([
            {
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
            },
        ]);

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

        expect(billing.subscriptions()).toEqual([]);
        expect(billing.totalItems()).toBe(0);
        expect(billing.errorMessage()).toBe('network');
        expect(billing.isLoading()).toBe(false);
    });

    it('should ignore stale load responses after tab changes', async () => {
        const billingApi = createBillingServiceMock();
        const pendingSubscriptions = new Subject<SubscriptionsPage>();
        billingApi.getSubscriptions.mockReturnValueOnce(pendingSubscriptions.asObservable());
        const { billing, fixture } = await setupBillingAsync(billingApi);
        await fixture.whenStable();

        billing.setTab('payments');
        await fixture.whenStable();
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
            toUtc: '2026-04-30T23:59:59.999Z',
        });
    });
});

describe('AdminBillingComponent metadata', () => {
    it('should format metadata json for side panel', async () => {
        const { billing, fixture } = await setupBillingAsync();
        await fixture.whenStable();
        billing.showMetadata('{"payment_id":"pay_123"}');

        expect(billing.selectedMetadata()).toContain('"payment_id": "pay_123"');
    });
});

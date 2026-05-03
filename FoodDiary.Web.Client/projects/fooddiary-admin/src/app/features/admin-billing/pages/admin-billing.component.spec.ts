import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AdminBillingService, type AdminBillingSubscription } from '../api/admin-billing.service';
import { AdminBillingComponent } from './admin-billing.component';

describe('AdminBillingComponent', () => {
    let component: AdminBillingComponent;
    let fixture: ComponentFixture<AdminBillingComponent>;
    let billingService: {
        getSubscriptions: ReturnType<typeof vi.fn>;
        getPayments: ReturnType<typeof vi.fn>;
        getWebhookEvents: ReturnType<typeof vi.fn>;
    };

    const subscriptionsPage = {
        items: [
            {
                id: 'subscription-1',
                userId: 'user-1',
                userEmail: 'premium@example.com',
                provider: 'Paddle',
                externalCustomerId: 'cus_123',
                status: 'active',
                cancelAtPeriodEnd: false,
                createdOnUtc: '2026-04-28T00:00:00Z',
            },
        ],
        page: 1,
        limit: 20,
        totalPages: 2,
        totalItems: 21,
    } satisfies {
        items: AdminBillingSubscription[];
        page: number;
        limit: number;
        totalPages: number;
        totalItems: number;
    };

    beforeEach(async () => {
        billingService = {
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
                    limit: 20,
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
                    limit: 20,
                    totalPages: 1,
                    totalItems: 1,
                }),
            ),
        };

        await TestBed.configureTestingModule({
            imports: [AdminBillingComponent],
            providers: [{ provide: AdminBillingService, useValue: billingService }],
        }).compileComponents();

        fixture = TestBed.createComponent(AdminBillingComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should load subscriptions on init', () => {
        expect(billingService.getSubscriptions).toHaveBeenCalledWith(1, 20, {
            provider: null,
            status: null,
            kind: null,
            search: null,
            fromUtc: null,
            toUtc: null,
        });
        expect(component.subscriptions()).toEqual(subscriptionsPage.items);
        expect(component.totalPages()).toBe(2);
        expect(component.totalItems()).toBe(21);
        expect(component.isLoading()).toBe(false);
    });

    it('should switch to payments and include kind filter', () => {
        component.kind.set('webhook');
        component.setTab('payments');

        expect(component.activeTab()).toBe('payments');
        expect(component.page()).toBe(1);
        expect(billingService.getPayments).toHaveBeenCalledWith(1, 20, {
            provider: null,
            status: null,
            kind: 'webhook',
            search: null,
            fromUtc: null,
            toUtc: null,
        });
        expect(component.payments()[0].externalPaymentId).toBe('pay_123');
    });

    it('should apply filters with utc day bounds', () => {
        component.provider.set(' Paddle ');
        component.status.set(' paid ');
        component.search.set(' buyer@example.com ');
        component.fromDate.set('2026-04-01');
        component.toDate.set('2026-04-30');

        component.applyFilters();

        expect(billingService.getSubscriptions).toHaveBeenLastCalledWith(1, 20, {
            provider: 'Paddle',
            status: 'paid',
            kind: null,
            search: 'buyer@example.com',
            fromUtc: '2026-04-01T00:00:00.000Z',
            toUtc: '2026-04-30T23:59:59.999Z',
        });
    });

    it('should format metadata json for side panel', () => {
        component.showMetadata('{"payment_id":"pay_123"}');

        expect(component.selectedMetadata()).toContain('"payment_id": "pay_123"');
    });

    it('should clear state on load error', () => {
        billingService.getSubscriptions.mockReturnValueOnce(throwError(() => new Error('network')));

        component.load();

        expect(component.subscriptions()).toEqual([]);
        expect(component.totalItems()).toBe(0);
        expect(component.isLoading()).toBe(false);
    });
});

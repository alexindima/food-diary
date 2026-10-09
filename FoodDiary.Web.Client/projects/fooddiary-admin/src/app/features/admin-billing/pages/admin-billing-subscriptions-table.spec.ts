import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import type { AdminBillingSubscriptionViewModel } from '../models/admin-billing-view.models';
import { AdminBillingSubscriptionsTableComponent } from './admin-billing-subscriptions-table';

const subscription: AdminBillingSubscriptionViewModel = {
    id: adminId<'billing-subscription'>('subscription-1'),
    userId: adminId<'user'>('user-1'),
    userEmail: 'subscriber@example.com',
    provider: 'Stripe',
    externalCustomerId: 'cus_123',
    externalSubscriptionId: 'sub_123',
    externalPaymentMethodId: 'pm_123',
    externalPriceId: 'price_123',
    plan: 'Premium',
    status: 'Active',
    currentPeriodStartUtc: adminUtcInstant('2026-01-01T00:00:00Z'),
    currentPeriodEndUtc: adminUtcInstant('2026-02-01T00:00:00Z'),
    cancelAtPeriodEnd: true,
    nextBillingAttemptUtc: adminUtcInstant('2026-02-01T00:00:00Z'),
    lastWebhookEventId: adminId<'billing-webhook-event'>('webhook-1'),
    lastSyncedAtUtc: adminUtcInstant('2026-01-05T00:00:00Z'),
    createdOnUtc: adminUtcInstant('2026-01-01T00:00:00Z'),
    modifiedOnUtc: adminUtcInstant('2026-01-05T00:00:00Z'),
    currentPeriodStartText: 'Jan 1, 2026',
    currentPeriodEndText: 'Feb 1, 2026',
    nextBillingAttemptText: 'Feb 1, 2026',
    updatedText: 'Jan 5, 2026',
    externalCustomerIdText: 'cus_123',
    externalSubscriptionIdText: 'sub_123',
    externalPaymentMethodIdText: 'pm_123',
};

function createComponent(items: AdminBillingSubscriptionViewModel[]): ComponentFixture<AdminBillingSubscriptionsTableComponent> {
    const fixture = TestBed.createComponent(AdminBillingSubscriptionsTableComponent);
    fixture.componentRef.setInput('items', items);
    fixture.detectChanges();
    return fixture;
}

function host(fixture: ComponentFixture<AdminBillingSubscriptionsTableComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

describe('AdminBillingSubscriptionsTableComponent', () => {
    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [AdminBillingSubscriptionsTableComponent],
            providers: [provideRouter([]), provideTranslateTesting()],
        }).compileComponents();
    });

    it('renders subscription rows with canceling state', () => {
        const fixture = createComponent([subscription]);

        expect(host(fixture).textContent).toContain('subscriber@example.com');
        expect(host(fixture).textContent).toContain('Premium');
        expect(host(fixture).textContent).toContain('canceling');
        expect(host(fixture).textContent).toContain('Sub: sub_123');
    });

    it('renders empty state and fallback plan', () => {
        const emptyFixture = createComponent([]);
        expect(host(emptyFixture).textContent).toContain('No subscriptions found.');

        const fallbackFixture = createComponent([{ ...subscription, plan: null, cancelAtPeriodEnd: false }]);
        expect(host(fallbackFixture).textContent).toContain('-');
        expect(host(fallbackFixture).textContent).not.toContain('canceling');
    });
});

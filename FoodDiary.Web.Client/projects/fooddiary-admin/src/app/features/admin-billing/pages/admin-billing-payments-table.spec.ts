import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { AdminBillingPaymentViewModel } from './admin-billing.types';
import { AdminBillingPaymentsTableComponent } from './admin-billing-payments-table';

const METADATA_JSON = '{"source":"stripe"}';

const payment: AdminBillingPaymentViewModel = {
    id: 'payment-1',
    userId: 'user-1',
    userEmail: 'payer@example.com',
    billingSubscriptionId: 'subscription-1',
    provider: 'Stripe',
    externalPaymentId: 'pi_123',
    externalCustomerId: 'cus_123',
    externalSubscriptionId: 'sub_123',
    externalPaymentMethodId: 'pm_123',
    externalPriceId: 'price_123',
    plan: 'Premium',
    status: 'Paid',
    kind: 'Invoice',
    amount: 1299,
    currency: 'USD',
    currentPeriodStartUtc: '2026-01-01T00:00:00Z',
    currentPeriodEndUtc: '2026-02-01T00:00:00Z',
    webhookEventId: 'webhook-1',
    providerMetadataJson: METADATA_JSON,
    createdOnUtc: '2026-01-05T00:00:00Z',
    modifiedOnUtc: null,
    createdText: 'Jan 5, 2026',
    amountText: '$12.99',
    externalPaymentIdText: 'pi_123',
    externalCustomerIdText: 'cus_123',
    webhookEventIdText: 'webhook-1',
};

function createComponent(items: AdminBillingPaymentViewModel[]): ComponentFixture<AdminBillingPaymentsTableComponent> {
    const fixture = TestBed.createComponent(AdminBillingPaymentsTableComponent);
    fixture.componentRef.setInput('items', items);
    fixture.detectChanges();
    return fixture;
}

function host(fixture: ComponentFixture<AdminBillingPaymentsTableComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function requireMetadataButton(fixture: ComponentFixture<AdminBillingPaymentsTableComponent>): HTMLButtonElement {
    const button = host(fixture).querySelector<HTMLButtonElement>('button');

    if (button === null) {
        throw new Error('Expected metadata button to exist.');
    }

    return button;
}

describe('AdminBillingPaymentsTableComponent', () => {
    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [AdminBillingPaymentsTableComponent],
        }).compileComponents();
    });

    it('renders payment rows and emits metadata payload', () => {
        const fixture = createComponent([payment]);
        const metadataSpy = vi.fn();
        fixture.componentInstance.metadataOpen.subscribe(metadataSpy);

        host(fixture).querySelector('button')?.click();

        expect(host(fixture).textContent).toContain('payer@example.com');
        expect(host(fixture).textContent).toContain('$12.99');
        expect(metadataSpy).toHaveBeenCalledWith(METADATA_JSON);
    });

    it('renders empty state and disables metadata when payload is missing', () => {
        const fixture = createComponent([]);
        expect(host(fixture).textContent).toContain('No payments found.');

        const disabledFixture = createComponent([{ ...payment, providerMetadataJson: null }]);
        expect(requireMetadataButton(disabledFixture).disabled).toBe(true);
    });
});

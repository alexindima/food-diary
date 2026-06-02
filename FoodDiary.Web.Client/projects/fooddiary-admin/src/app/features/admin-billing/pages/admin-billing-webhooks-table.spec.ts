import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { AdminBillingWebhookEventViewModel } from './admin-billing.types';
import { AdminBillingWebhooksTableComponent } from './admin-billing-webhooks-table';

const PAYLOAD_JSON = '{"event":"invoice.paid"}';

const webhookEvent: AdminBillingWebhookEventViewModel = {
    id: 'webhook-1',
    provider: 'Stripe',
    eventId: 'evt_123',
    eventType: 'invoice.paid',
    externalObjectId: 'in_123',
    status: 'Processed',
    processedAtUtc: '2026-01-05T00:00:00Z',
    payloadJson: PAYLOAD_JSON,
    errorMessage: null,
    createdOnUtc: '2026-01-05T00:00:00Z',
    modifiedOnUtc: null,
    processedText: 'Jan 5, 2026',
    eventIdText: 'evt_123',
    externalObjectIdText: 'in_123',
};

function createComponent(items: AdminBillingWebhookEventViewModel[]): ComponentFixture<AdminBillingWebhooksTableComponent> {
    const fixture = TestBed.createComponent(AdminBillingWebhooksTableComponent);
    fixture.componentRef.setInput('items', items);
    fixture.detectChanges();
    return fixture;
}

function host(fixture: ComponentFixture<AdminBillingWebhooksTableComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function requireMetadataButton(fixture: ComponentFixture<AdminBillingWebhooksTableComponent>): HTMLButtonElement {
    const button = host(fixture).querySelector<HTMLButtonElement>('button');

    if (button === null) {
        throw new Error('Expected metadata button to exist.');
    }

    return button;
}

describe('AdminBillingWebhooksTableComponent', () => {
    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [AdminBillingWebhooksTableComponent],
        }).compileComponents();
    });

    it('renders webhook rows and emits payload metadata', () => {
        const fixture = createComponent([webhookEvent]);
        const metadataSpy = vi.fn();
        fixture.componentInstance.metadataOpen.subscribe(metadataSpy);

        host(fixture).querySelector('button')?.click();

        expect(host(fixture).textContent).toContain('invoice.paid');
        expect(host(fixture).textContent).toContain('in_123');
        expect(host(fixture).textContent).toContain('-');
        expect(metadataSpy).toHaveBeenCalledWith(PAYLOAD_JSON);
    });

    it('renders empty state and disables metadata when payload is missing', () => {
        const emptyFixture = createComponent([]);
        expect(host(emptyFixture).textContent).toContain('No webhook events found.');

        const disabledFixture = createComponent([{ ...webhookEvent, payloadJson: null, errorMessage: 'Invalid signature' }]);
        expect(host(disabledFixture).textContent).toContain('Invalid signature');
        expect(requireMetadataButton(disabledFixture).disabled).toBe(true);
    });
});

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { type AdminBillingPayment, AdminBillingService } from './admin-billing.service';

const PAGE = 2;
const LIMIT = 20;
const TOTAL_PAGES = 4;
const TOTAL_ITEMS = 61;

describe('AdminBillingService', () => {
    let service: AdminBillingService;
    let httpMock: HttpTestingController;

    const baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/billing`;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [AdminBillingService, provideHttpClient(), provideHttpClientTesting()],
        });

        service = TestBed.inject(AdminBillingService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should request filtered payments and map paged response', () => {
        const response = {
            data: [
                {
                    id: 'payment-1',
                    userId: 'user-1',
                    userEmail: 'buyer@example.com',
                    provider: 'Paddle',
                    externalPaymentId: 'pay_123',
                    status: 'paid',
                    kind: 'webhook',
                    amount: 7.99,
                    currency: 'USD',
                    createdOnUtc: '2026-04-28T00:00:00Z',
                },
            ],
            page: PAGE,
            limit: LIMIT,
            totalPages: TOTAL_PAGES,
            totalItems: TOTAL_ITEMS,
        } satisfies {
            data: AdminBillingPayment[];
            page: number;
            limit: number;
            totalPages: number;
            totalItems: number;
        };

        service
            .getPayments(PAGE, LIMIT, {
                provider: 'Paddle',
                status: 'paid',
                kind: 'webhook',
                search: 'buyer@example.com',
                fromUtc: '2026-04-01T00:00:00.000Z',
                toUtc: '2026-04-30T23:59:59.999Z',
            })
            .subscribe(result => {
                expect(result.items).toEqual(response.data);
                expect(result.page).toBe(PAGE);
                expect(result.totalItems).toBe(TOTAL_ITEMS);
            });

        const req = httpMock.expectOne(
            r =>
                r.url === `${baseUrl}/payments` &&
                r.params.get('page') === String(PAGE) &&
                r.params.get('limit') === String(LIMIT) &&
                r.params.get('provider') === 'Paddle' &&
                r.params.get('status') === 'paid' &&
                r.params.get('kind') === 'webhook' &&
                r.params.get('search') === 'buyer@example.com' &&
                r.params.get('fromUtc') === '2026-04-01T00:00:00.000Z' &&
                r.params.get('toUtc') === '2026-04-30T23:59:59.999Z',
        );
        expect(req.request.method).toBe('GET');
        req.flush(response);
    });

    it('should omit empty filters for subscriptions', () => {
        service.getSubscriptions(1, LIMIT, { provider: '', status: null, search: undefined }).subscribe(result => {
            expect(result.items).toEqual([]);
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/subscriptions`);
        expect(req.request.params.get('provider')).toBeNull();
        expect(req.request.params.get('status')).toBeNull();
        expect(req.request.params.get('search')).toBeNull();
        req.flush({
            data: [],
            page: 1,
            limit: LIMIT,
            totalPages: 0,
            totalItems: 0,
        });
    });

    it('should request webhook events endpoint', () => {
        service.getWebhookEvents(1, LIMIT, { provider: 'YooKassa' }).subscribe(result => {
            expect(result.items[0].eventId).toBe('evt_1');
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/webhook-events` && r.params.get('provider') === 'YooKassa');
        expect(req.request.method).toBe('GET');
        req.flush({
            data: [
                {
                    id: 'event-row-1',
                    provider: 'YooKassa',
                    eventId: 'evt_1',
                    eventType: 'payment.succeeded',
                    status: 'processed',
                    processedAtUtc: '2026-04-28T00:00:00Z',
                    createdOnUtc: '2026-04-28T00:00:00Z',
                },
            ],
            page: 1,
            limit: LIMIT,
            totalPages: 1,
            totalItems: 1,
        });
    });
});

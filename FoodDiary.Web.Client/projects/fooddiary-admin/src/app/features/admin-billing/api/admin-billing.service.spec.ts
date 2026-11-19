import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import type { AdminBillingPayment } from '../models/admin-billing.models';
import { AdminBillingService } from './admin-billing.service';

const PAGE = 2;
const LIMIT = 20;
const TOTAL_PAGES = 4;
const TOTAL_ITEMS = 61;
const NET_REVENUE = 95;
const CANCELLATION_TOTAL = 41;
const CANCELLATION_FEEDBACK_COUNT = 7;
const BASE_URL = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/billing`;
const createdOnUtc = '2026-04-28T00:00:00Z';
const currentPeriodEndUtc = '2026-05-28T00:00:00Z';

let service: AdminBillingService;
let httpMock: HttpTestingController;

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

describe('AdminBillingFacade payments', () => {
    it('should request revenue summary without pagination parameters', () => {
        service.getRevenueSummary({ fromUtc: '2026-04-01T00:00:00.000Z', toUtc: '2026-05-01T00:00:00.000Z' }).subscribe(result => {
            expect(result.currencies[0]?.net).toBe(NET_REVENUE);
        });

        const req = httpMock.expectOne(
            request =>
                request.url === `${BASE_URL}/revenue-summary` &&
                request.params.get('fromUtc') === '2026-04-01T00:00:00.000Z' &&
                request.params.get('toUtc') === '2026-05-01T00:00:00.000Z' &&
                !request.params.has('page'),
        );
        req.flush({
            fromUtc: '2026-04-01T00:00:00Z',
            toUtc: '2026-05-01T00:00:00Z',
            currencies: [
                {
                    currency: 'USD',
                    gross: 100,
                    refunds: 5,
                    chargebacks: 0,
                    reversals: 0,
                    net: 95,
                    successfulPayments: 4,
                    tax: 16,
                    paddleFees: 5.5,
                    paddleEarnings: 78.5,
                    earningsTrackedPayments: 4,
                },
            ],
        });
    });

    it('should request filtered payments and map paged response', () => {
        const response = createPaymentsResponse();

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
                r.url === `${BASE_URL}/payments` &&
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
});

describe('AdminBillingFacade subscriptions', () => {
    it('should omit empty filters for subscriptions', () => {
        service.getSubscriptions(1, LIMIT, { provider: '', status: null, search: undefined }).subscribe(result => {
            expect(result.items).toEqual([]);
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/subscriptions`);
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
});

describe('AdminBillingFacade webhook events', () => {
    it('should request webhook events endpoint', () => {
        service.getWebhookEvents(1, LIMIT, { provider: 'YooKassa' }).subscribe(result => {
            expect(result.items[0].eventId).toBe('evt_1');
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/webhook-events` && r.params.get('provider') === 'YooKassa');
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

function createPaymentsResponse(): {
    data: AdminBillingPayment[];
    page: number;
    limit: number;
    totalPages: number;
    totalItems: number;
} {
    return {
        data: [
            {
                id: adminId<'billing-payment'>('payment-1'),
                userId: adminId<'user'>('user-1'),
                userEmail: 'buyer@example.com',
                provider: 'Paddle',
                externalPaymentId: 'pay_123',
                status: 'paid',
                kind: 'webhook',
                amount: 7.99,
                currency: 'USD',
                createdOnUtc: adminUtcInstant('2026-04-28T00:00:00Z'),
            },
        ],
        page: PAGE,
        limit: LIMIT,
        totalPages: TOTAL_PAGES,
        totalItems: TOTAL_ITEMS,
    };
}

describe('Admin cancellation adapter', () => {
    it('preserves filtering, pagination and summary totals from the API', () => {
        service
            .getCancellations(2, LIMIT, {
                status: 'scheduled',
                search: 'owned',
                fromUtc: '2026-11-01T00:00:00Z',
                toUtc: '2026-12-01T00:00:00Z',
            })
            .subscribe(report => {
                expect(report.totalItems).toBe(CANCELLATION_TOTAL);
                expect(report.feedbackCount).toBe(CANCELLATION_FEEDBACK_COUNT);
                expect(report.reasons).toEqual([{ reasonCode: null, count: 34 }]);
                expect(report.items).toEqual([]);
            });
        const request = httpMock.expectOne(
            value =>
                value.url === `${BASE_URL}/cancellations` &&
                value.params.get('page') === '2' &&
                value.params.get('status') === 'scheduled' &&
                value.params.get('toUtc') === '2026-12-01T00:00:00Z',
        );
        request.flush({
            data: [],
            page: 2,
            limit: LIMIT,
            totalPages: 3,
            totalItems: 41,
            feedbackCount: 7,
            reasons: [{ reasonCode: null, count: 34 }],
        });
    });
});

describe('Admin billing customer records', () => {
    it('maps scheduled subscriptions without a customer email and retains provider lifecycle fields', () => {
        const subscription = {
            id: 'owned-subscription',
            userId: 'owned-user',
            provider: 'Paddle',
            externalCustomerId: 'cus_owned',
            externalSubscriptionId: 'sub_owned',
            status: 'active',
            cancelAtPeriodEnd: true,
            currentPeriodStartUtc: createdOnUtc,
            currentPeriodEndUtc,
            nextBillingAttemptUtc: null,
            lastWebhookEventId: 'owned-webhook',
            createdOnUtc,
        };
        const receive = vi.fn();
        service.getSubscriptions(1, LIMIT, { search: 'sub_owned' }).subscribe(receive);

        const request = httpMock.expectOne(candidate => candidate.url === `${BASE_URL}/subscriptions`);
        expect(request.request.params.get('search')).toBe('sub_owned');
        request.flush(singleRecordPage(subscription));

        expect(receive).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({
                items: [{ ...subscription, userEmail: null, lastSyncedAtUtc: undefined, modifiedOnUtc: undefined }],
            }),
        );
    });

    it('retains zero-valued payment receipt amounts and links when the email is absent', () => {
        const payment = {
            id: 'owned-receipt',
            userId: 'owned-user',
            provider: 'Stripe',
            externalPaymentId: 'in_owned',
            billingSubscriptionId: 'owned-subscription',
            webhookEventId: 'owned-webhook',
            status: 'paid',
            kind: 'transaction',
            amount: 0,
            currency: 'EUR',
            tax: 0,
            payoutCurrency: 'USD',
            payoutEarnings: 0,
            providerMetadataJson: '{"invoice":"in_owned"}',
            currentPeriodStartUtc: createdOnUtc,
            currentPeriodEndUtc,
            createdOnUtc,
        };
        const receive = vi.fn();
        service.getPayments(1, LIMIT, { search: 'in_owned' }).subscribe(receive);

        httpMock.expectOne(candidate => candidate.url === `${BASE_URL}/payments`).flush(singleRecordPage(payment));

        expect(receive).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ items: [{ ...payment, userEmail: null, modifiedOnUtc: undefined }] }),
        );
    });
});

describe('Admin cancellation feedback details', () => {
    it('distinguishes omitted feedback from an actual cancellation response', () => {
        const cancellation = {
            id: 'owned-cancellation',
            userId: 'owned-user',
            provider: 'Paddle',
            externalSubscriptionId: 'sub_owned',
            source: 'fooddiary',
            outcome: 'scheduled',
            requestedAtUtc: createdOnUtc,
            effectiveAtUtc: currentPeriodEndUtc,
        };
        const feedback = {
            ...cancellation,
            id: 'with-feedback',
            userEmail: 'owned@example.test',
            plan: 'monthly',
            reasonCode: 'too_expensive',
            comment: 'Short-term budget change',
            feedbackSubmittedAtUtc: '2026-04-29T00:00:00Z',
        };
        const receive = vi.fn();
        service.getCancellations(1, LIMIT, {}).subscribe(receive);

        httpMock
            .expectOne(candidate => candidate.url === `${BASE_URL}/cancellations`)
            .flush({
                ...singleRecordPage(cancellation),
                data: [cancellation, feedback],
                totalItems: 2,
                feedbackCount: 1,
                reasons: [{ count: 1 }, { reasonCode: feedback.reasonCode, count: 1 }],
            });

        expect(receive).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({
                feedbackCount: 1,
                items: [
                    { ...cancellation, userEmail: null, plan: null, reasonCode: null, comment: null, feedbackSubmittedAtUtc: null },
                    feedback,
                ],
                reasons: [
                    { reasonCode: null, count: 1 },
                    { reasonCode: feedback.reasonCode, count: 1 },
                ],
            }),
        );
    });
});

function singleRecordPage<T>(item: T): { data: T[]; page: number; limit: number; totalPages: number; totalItems: number } {
    return { data: [item], page: 1, limit: LIMIT, totalPages: 1, totalItems: 1 };
}

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { PremiumBillingService } from './premium-billing.service';

const BILLING_FLAGS = {
    renewalEnabled: false,
    premiumTrialActive: false,
    premiumTrialUsed: false,
    canStartPremiumTrial: true,
    provider: 'paddle',
    availableProviders: ['paddle'],
};

let service: PremiumBillingService;
let httpMock: HttpTestingController;

const baseUrl = environment.apiUrls.billing;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [PremiumBillingService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(PremiumBillingService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('Premium cancellation adapter', () => {
    it('cancels without sending any survey data in the cancellation request', () => {
        service.cancelSubscription().subscribe(value => {
            expect(value.cancellationId).toBe('owned-cancellation');
            expect(value.overview.cancelAtPeriodEnd).toBe(true);
        });
        const request = httpMock.expectOne(`${baseUrl}/cancel`);
        expect(request.request.method).toBe('POST');
        expect(request.request.body).toBeNull();
        request.flush({
            cancellationId: 'owned-cancellation',
            overview: {
                ...BILLING_FLAGS,
                isPremium: true,
                cancelAtPeriodEnd: true,
                manageBillingAvailable: true,
                cancellationAvailable: false,
            },
        });
    });

    it('saves optional feedback through its separate endpoint', () => {
        service
            .submitCancellationFeedback(entityId<'billing-cancellation'>('owned-cancellation'), {
                reasonCode: null,
                comment: 'Comment only',
            })
            .subscribe();
        const request = httpMock.expectOne(`${baseUrl}/cancellations/owned-cancellation/feedback`);
        expect(request.request.method).toBe('POST');
        expect(request.request.body).toEqual({ reasonCode: null, comment: 'Comment only' });
        request.flush(null);
    });
});

describe('PremiumBillingService', () => {
    it('loads billing overview', () => {
        service.getOverview().subscribe();

        const req = httpMock.expectOne(`${baseUrl}/overview`);
        expect(req.request.method).toBe('GET');
        req.flush({
            isPremium: false,
            subscriptionStatus: null,
            plan: null,
            currentPeriodEndUtc: null,
            cancelAtPeriodEnd: false,
            manageBillingAvailable: false,
            ...BILLING_FLAGS,
        });
    });

    it('creates checkout session for selected plan', () => {
        service.createCheckoutSession('yearly').subscribe();

        const req = httpMock.expectOne(`${baseUrl}/checkout-session`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({ plan: 'yearly' });
        expect(req.request.headers.get('Idempotency-Key')).toMatch(/^[0-9a-f-]{36}$/u);
        req.flush({
            sessionId: 'cs_test_123',
            url: 'https://checkout.stripe.com/pay/cs_test_123',
            plan: 'yearly',
        });
    });

    it('starts premium trial', () => {
        service.startPremiumTrial().subscribe();

        const req = httpMock.expectOne(`${baseUrl}/trial`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toBeNull();
        req.flush({
            isPremium: true,
            subscriptionStatus: 'trialing',
            plan: null,
            currentPeriodEndUtc: '2026-06-01T00:00:00Z',
            cancelAtPeriodEnd: false,
            manageBillingAvailable: false,
            ...BILLING_FLAGS,
            premiumTrialActive: true,
            premiumTrialUsed: true,
            canStartPremiumTrial: false,
        });
    });

    it('creates checkout session for selected provider', () => {
        service.createCheckoutSession('monthly', 'paddle').subscribe();

        const req = httpMock.expectOne(`${baseUrl}/checkout-session`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({ plan: 'monthly', provider: 'paddle' });
        expect(req.request.headers.get('Idempotency-Key')).toMatch(/^[0-9a-f-]{36}$/u);
        req.flush({
            sessionId: 'cs_test_456',
            url: 'https://checkout.paddle.com/pay/cs_test_456',
            plan: 'monthly',
        });
    });

    it('creates portal session', () => {
        service.createPortalSession().subscribe();

        const req = httpMock.expectOne(`${baseUrl}/portal-session`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toBeNull();
        req.flush({
            url: 'https://billing.stripe.com/session/test',
        });
    });
});

import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { ApiErrorHttpResponse } from '../../../shared/api/sdk/generated/model/api-error-http-response';
import type { BillingOverviewHttpResponse } from '../../../shared/api/sdk/generated/model/billing-overview-http-response';
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
const NO_SUBSCRIPTION = {
    ...BILLING_FLAGS,
    isPremium: false,
    cancelAtPeriodEnd: false,
    manageBillingAvailable: false,
} satisfies BillingOverviewHttpResponse;
const STARS_MINIMUM = 1;
const STARS_MAXIMUM = 10_000;

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

describe('Billing overview with optional provider data', () => {
    it('keeps an active plan, billing dates, renewal state, and the available Stars tariff', async () => {
        const pending = firstValueFrom(service.getOverview());
        const overview = {
            ...NO_SUBSCRIPTION,
            isPremium: true,
            plan: 'monthly',
            subscriptionProvider: 'paddle',
            subscriptionStatus: 'active',
            currentPeriodStartUtc: '2026-03-08T07:55:00Z',
            currentPeriodEndUtc: '2026-04-08T07:55:00Z',
            nextBillingAttemptUtc: '2026-04-08T07:55:00Z',
            cancellationAvailable: true,
            renewalEnabled: true,
            manageBillingAvailable: true,
            canStartPremiumTrial: false,
            availableProviders: ['paddle', 'TelegramStars'],
            telegramStarsPrice: STARS_MAXIMUM,
        } satisfies BillingOverviewHttpResponse;
        httpMock.expectOne({ method: 'GET', url: `${baseUrl}/overview` }).flush(overview);

        await expect(pending).resolves.toEqual({
            ...overview,
            premiumTrialStartUtc: null,
            premiumTrialEndUtc: null,
            paddleClientToken: null,
        });
    });

    it('accepts the lowest valid tariff while preserving an absent subscription', async () => {
        const pending = firstValueFrom(service.getOverview());
        httpMock.expectOne({ method: 'GET', url: `${baseUrl}/overview` }).flush({
            ...NO_SUBSCRIPTION,
            availableProviders: ['telegramStars'],
            provider: 'telegramStars',
            telegramStarsPrice: STARS_MINIMUM,
        });

        await expect(pending).resolves.toMatchObject({
            isPremium: false,
            telegramStarsPrice: STARS_MINIMUM,
            plan: null,
            subscriptionStatus: null,
            subscriptionProvider: null,
            cancellationAvailable: false,
        });
    });

    it('does not publish a saved Stars amount when the provider is unavailable', async () => {
        const pending = firstValueFrom(service.getOverview());
        httpMock.expectOne({ method: 'GET', url: `${baseUrl}/overview` }).flush({ ...NO_SUBSCRIPTION, telegramStarsPrice: STARS_MINIMUM });

        await expect(pending).resolves.toMatchObject({ availableProviders: ['paddle'], telegramStarsPrice: null });
    });

    it.each([
        { scenario: 'missing', price: undefined },
        { scenario: 'fractional', price: 1.5 },
        { scenario: 'zero', price: 0 },
        { scenario: 'above the maximum', price: STARS_MAXIMUM + 1 },
    ])('rejects a $scenario tariff when Stars are available', async ({ price }) => {
        const pending = firstValueFrom(service.getOverview());
        const overview = {
            ...NO_SUBSCRIPTION,
            availableProviders: ['telegramStars'],
            telegramStarsPrice: price,
        } satisfies BillingOverviewHttpResponse;
        httpMock.expectOne({ method: 'GET', url: `${baseUrl}/overview` }).flush(overview);

        await expect(pending).rejects.toThrow('Invalid Telegram Stars tariff');
    });
});

describe('Billing API error responses', () => {
    it('propagates a missing billing profile and loads the overview on the next request', async () => {
        const pending = firstValueFrom(service.getOverview());
        const profileError = { error: 'User.NotFound' } satisfies ApiErrorHttpResponse;
        httpMock.expectOne({ method: 'GET', url: `${baseUrl}/overview` }).flush(profileError, {
            status: HttpStatusCode.NotFound,
            statusText: 'Not Found',
        });
        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.NotFound, error: profileError });

        const retry = firstValueFrom(service.getOverview());
        httpMock.expectOne({ method: 'GET', url: `${baseUrl}/overview` }).flush(NO_SUBSCRIPTION);
        await expect(retry).resolves.toMatchObject({ isPremium: false, canStartPremiumTrial: true });
    });

    it('preserves trial eligibility conflict for the caller', async () => {
        const pending = firstValueFrom(service.startPremiumTrial());
        const consumed = { error: 'Billing.TrialAlreadyUsed' } satisfies ApiErrorHttpResponse;
        const request = httpMock.expectOne({ method: 'POST', url: `${baseUrl}/trial` });
        expect(request.request.body).toBeNull();
        request.flush(consumed, { status: HttpStatusCode.Conflict, statusText: 'Conflict' });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.Conflict, error: consumed });
    });

    it('preserves an in-progress checkout response without opening another session', async () => {
        const pending = firstValueFrom(service.createCheckoutSession('monthly', 'paddle'));
        const inProgress = { error: 'Billing.CheckoutAlreadyInProgress' } satisfies ApiErrorHttpResponse;
        const request = httpMock.expectOne({ method: 'POST', url: `${baseUrl}/checkout-session` });
        expect(request.request.body).toEqual({ plan: 'monthly', provider: 'paddle' });
        expect(request.request.headers.get('Idempotency-Key')).toMatch(/^[0-9a-f-]{36}$/u);
        request.flush(inProgress, { status: HttpStatusCode.Conflict, statusText: 'Conflict' });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.Conflict, error: inProgress });
        httpMock.expectNone({ method: 'POST', url: `${baseUrl}/checkout-session` });
    });

    it('propagates unavailable customer portal management without exposing a URL', async () => {
        const pending = firstValueFrom(service.createPortalSession());
        const unavailable = { error: 'Billing.CustomerPortalUnavailable' } satisfies ApiErrorHttpResponse;
        httpMock.expectOne({ method: 'POST', url: `${baseUrl}/portal-session` }).flush(unavailable, {
            status: HttpStatusCode.NotFound,
            statusText: 'Not Found',
        });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.NotFound, error: unavailable });
    });
});

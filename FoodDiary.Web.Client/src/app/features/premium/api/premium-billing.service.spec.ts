import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { PremiumBillingService } from './premium-billing.service';

describe('PremiumBillingService', () => {
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
        });
    });

    it('creates checkout session for selected plan', () => {
        service.createCheckoutSession('yearly').subscribe();

        const req = httpMock.expectOne(`${baseUrl}/checkout-session`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({ plan: 'yearly' });
        req.flush({
            sessionId: 'cs_test_123',
            url: 'https://checkout.stripe.com/pay/cs_test_123',
            plan: 'yearly',
        });
    });

    it('creates portal session', () => {
        service.createPortalSession().subscribe();

        const req = httpMock.expectOne(`${baseUrl}/portal-session`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({});
        req.flush({
            url: 'https://billing.stripe.com/session/test',
        });
    });
});

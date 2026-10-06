import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { BillingOverview } from '../../../shared/models/billing.models';
import { PaddleCheckoutService } from './paddle-checkout.service';
import { PremiumPlanPricingFacade } from './premium-plan-pricing.facade';

const PRICES = { monthly: { formattedTotal: '$8.00', currencyCode: 'USD' }, yearly: { formattedTotal: '$80.00', currencyCode: 'USD' } };
let preview: ReturnType<typeof vi.fn<PaddleCheckoutService['previewPlanPricesAsync']>>;

beforeEach(() => {
    preview = vi.fn();
});

describe('PremiumPlanPricingFacade', () => {
    it('loads both current prices without passing customer data', async () => {
        preview.mockResolvedValue(PRICES);
        const facade = setupFacade();
        const pending = facade.loadPricesAsync(overview(), 'ru');
        expect(facade.loading()).toBe(true);
        await pending;

        expect(preview).toHaveBeenCalledWith(
            { monthly: 'pri_month', yearly: 'pri_year' },
            { token: 'test_token', environment: 'sandbox', locale: 'ru' },
        );
        expect(facade.prices()).toEqual(PRICES);
        expect(facade.loading()).toBe(false);
        expect(facade.unavailable()).toBe(false);
    });

    it.each(['missing', 'same'])('shows unavailable for %s catalog configuration without a provider call', async problem => {
        const facade = setupFacade();
        await facade.loadPricesAsync({ ...overview(), paddleYearlyPriceId: problem === 'same' ? 'pri_month' : null }, 'en');
        expect(preview).not.toHaveBeenCalled();
        expect(facade.unavailable()).toBe(true);
        expect(facade.prices()).toEqual({});
    });

    it('does not load provider prices on the server', async () => {
        const facade = setupFacade('server');
        await facade.loadPricesAsync(overview(), 'en');
        expect(preview).not.toHaveBeenCalled();
        expect(facade.unavailable()).toBe(false);
    });

    it('does not label another payment provider with Paddle prices', async () => {
        const facade = setupFacade();
        await facade.loadPricesAsync({ ...overview(), availableProviders: ['stripe'] }, 'en');
        expect(preview).not.toHaveBeenCalled();
        expect(facade.prices()).toEqual({});
        expect(facade.unavailable()).toBe(false);
    });

    it('allows retry after failure and clears the old unavailable state', async () => {
        preview.mockRejectedValueOnce(new Error('Provider unavailable')).mockResolvedValueOnce(PRICES);
        const facade = setupFacade();
        await facade.loadPricesAsync(overview(), 'en');
        expect(facade.unavailable()).toBe(true);
        await facade.loadPricesAsync(overview(), 'en');
        expect(facade.prices()).toEqual(PRICES);
        expect(facade.unavailable()).toBe(false);
    });

    it('ignores an old failed request after a successful retry', async () => {
        let rejectOld: (reason: Error) => void = () => {};
        preview
            .mockImplementationOnce(
                async () =>
                    new Promise((_, reject) => {
                        rejectOld = reject;
                    }),
            )
            .mockResolvedValueOnce(PRICES);
        const facade = setupFacade();
        const oldRequest = facade.loadPricesAsync(overview(), 'en');
        await facade.loadPricesAsync(overview(), 'en');
        rejectOld(new Error('Late failure'));
        await oldRequest;
        expect(facade.prices()).toEqual(PRICES);
        expect(facade.unavailable()).toBe(false);
    });

    it('ignores a pending response after its page is destroyed', async () => {
        let complete: (value: typeof PRICES) => void = () => {};
        preview.mockImplementationOnce(
            async () =>
                new Promise(resolve => {
                    complete = resolve;
                }),
        );
        const facade = setupFacade();
        const pending = facade.loadPricesAsync(overview(), 'en');
        TestBed.resetTestingModule();
        complete(PRICES);
        await pending;
        expect(facade.prices()).toEqual({});
    });
});

function setupFacade(platform = 'browser'): PremiumPlanPricingFacade {
    TestBed.configureTestingModule({
        providers: [
            PremiumPlanPricingFacade,
            { provide: PLATFORM_ID, useValue: platform },
            { provide: PaddleCheckoutService, useValue: { previewPlanPricesAsync: preview } },
        ],
    });
    return TestBed.inject(PremiumPlanPricingFacade);
}

function overview(): BillingOverview {
    return {
        isPremium: false,
        subscriptionStatus: null,
        plan: null,
        subscriptionProvider: null,
        currentPeriodStartUtc: null,
        currentPeriodEndUtc: null,
        nextBillingAttemptUtc: null,
        premiumTrialStartUtc: null,
        premiumTrialEndUtc: null,
        cancelAtPeriodEnd: false,
        renewalEnabled: false,
        manageBillingAvailable: false,
        premiumTrialActive: false,
        premiumTrialUsed: false,
        canStartPremiumTrial: true,
        provider: 'paddle',
        availableProviders: ['paddle'],
        paddleClientToken: ' test_token ',
        paddleMonthlyPriceId: ' pri_month ',
        paddleYearlyPriceId: ' pri_year ',
    };
}

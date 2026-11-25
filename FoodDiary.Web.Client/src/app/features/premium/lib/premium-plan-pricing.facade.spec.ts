import { ApplicationRef, PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../testing/async-testing';
import type { BillingOverview } from '../../../shared/models/billing.models';
import { PaddleCheckoutService } from './paddle-checkout.service';
import { PremiumPlanPricingFacade } from './premium-plan-pricing.facade';

const PRICES = { monthly: { formattedTotal: '$8.00', currencyCode: 'USD' }, yearly: { formattedTotal: '$80.00', currencyCode: 'USD' } };
let preview: ReturnType<typeof vi.fn<PaddleCheckoutService['previewPlanPricesAsync']>>;

beforeEach(() => {
    preview = vi.fn();
});

describe('PremiumPlanPricingFacade configuration', () => {
    it.each([
        { token: 'test_token', environment: 'sandbox' },
        { token: 'live_catalog_fixture', environment: 'production' },
    ])('loads both $environment prices without passing customer data', async ({ token, environment }) => {
        preview.mockResolvedValue(PRICES);
        const facade = setupFacade();
        facade.loadPrices({ ...overview(), paddleClientToken: ` ${token} ` }, 'ru');
        expect(facade.loading()).toBe(true);
        await settlePricingAsync();

        expect(preview).toHaveBeenCalledExactlyOnceWith({ monthly: 'pri_month', yearly: 'pri_year' }, { token, environment, locale: 'ru' });
        expect(facade.prices()).toEqual(PRICES);
        expect(facade.loading()).toBe(false);
        expect(facade.unavailable()).toBe(false);
    });

    it.each(['missing', 'same'])('shows unavailable for %s catalog configuration without a provider call', problem => {
        const facade = setupFacade();
        facade.loadPrices({ ...overview(), paddleYearlyPriceId: problem === 'same' ? 'pri_month' : null }, 'en');
        expect(facade.loading()).toBe(false);
        expect(facade.unavailable()).toBe(true);
        expect(facade.prices()).toEqual({});
        TestBed.tick();
        expect(preview).not.toHaveBeenCalled();
    });

    it('does not load provider prices on the server', () => {
        const facade = setupFacade('server');
        facade.loadPrices(overview(), 'en');
        TestBed.tick();
        expect(preview).not.toHaveBeenCalled();
        expect(facade.loading()).toBe(false);
        expect(facade.unavailable()).toBe(false);
    });

    it('does not label another payment provider with Paddle prices', () => {
        const facade = setupFacade();
        facade.loadPrices({ ...overview(), availableProviders: ['stripe'] }, 'en');
        TestBed.tick();
        expect(preview).not.toHaveBeenCalled();
        expect(facade.prices()).toEqual({});
        expect(facade.loading()).toBe(false);
        expect(facade.unavailable()).toBe(false);
    });
});

describe('PremiumPlanPricingFacade request lifecycle', () => {
    it('allows retry after failure and clears the old unavailable state', async () => {
        preview.mockRejectedValueOnce(new Error('Provider unavailable')).mockResolvedValueOnce(PRICES);
        const facade = setupFacade();
        facade.loadPrices(overview(), 'en');
        await settlePricingAsync();
        expect(facade.unavailable()).toBe(true);
        facade.loadPrices(overview(), 'en');
        expect(facade.loading()).toBe(true);
        expect(facade.unavailable()).toBe(false);
        await settlePricingAsync();
        expect(facade.prices()).toEqual(PRICES);
        expect(facade.unavailable()).toBe(false);
    });

    it('starts both same-turn requests and ignores an old failure after the successful retry', async () => {
        let rejectOld: (reason: Error) => void = () => {};
        const oldRequest = new Promise<typeof PRICES>((_, reject) => {
            rejectOld = reject;
        });
        preview.mockReturnValueOnce(oldRequest).mockResolvedValueOnce(PRICES);
        const facade = setupFacade();
        const configuration = overview();

        facade.loadPrices(configuration, 'en');
        facade.loadPrices(configuration, 'en');
        expect(preview).toHaveBeenCalledTimes(2);
        await settlePricingAsync();
        rejectOld(new Error('Late failure'));
        await waitForAsyncTasksAsync();

        expect(facade.prices()).toEqual(PRICES);
        expect(facade.unavailable()).toBe(false);
    });

    it('clears old prices on a reload and ignores a late result after switching providers', async () => {
        let complete: (value: typeof PRICES) => void = () => {};
        const pending = new Promise<typeof PRICES>(resolve => {
            complete = resolve;
        });
        preview.mockResolvedValueOnce(PRICES).mockReturnValueOnce(pending);
        const facade = setupFacade();
        const configuration = overview();
        facade.loadPrices(configuration, 'en');
        await settlePricingAsync();
        expect(facade.prices()).toEqual(PRICES);

        facade.loadPrices(configuration, 'en');
        expect(preview).toHaveBeenCalledTimes(2);
        expect(facade.prices()).toEqual({});
        expect(facade.loading()).toBe(true);
        TestBed.tick();

        facade.loadPrices({ ...configuration, availableProviders: ['stripe'] }, 'en');
        expect(facade.loading()).toBe(false);
        complete(PRICES);
        await settlePricingAsync();

        expect(preview).toHaveBeenCalledTimes(2);
        expect(facade.prices()).toEqual({});
        expect(facade.unavailable()).toBe(false);
    });

    it('ignores a pending response after its page is destroyed', async () => {
        let complete: (value: typeof PRICES) => void = () => {};
        const pending = new Promise<typeof PRICES>(resolve => {
            complete = resolve;
        });
        preview.mockReturnValueOnce(pending);
        const facade = setupFacade();
        facade.loadPrices(overview(), 'en');
        TestBed.tick();
        expect(facade.loading()).toBe(true);
        TestBed.resetTestingModule();
        complete(PRICES);
        await waitForAsyncTasksAsync();
        expect(facade.prices()).toEqual({});
    });
});

async function settlePricingAsync(): Promise<void> {
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
}

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

import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { PaddleCheckoutService, type PaddleEnvironment } from './paddle-checkout.service';

const PADDLE_SCRIPT_URL = 'https://cdn.paddle.com/paddle/v2/paddle.js';

type PaddleApi = NonNullable<Window['Paddle']>;
type PaddleInitializeConfig = Parameters<PaddleApi['Initialize']>[0];
type PaddleCheckoutOpenConfig = Parameters<PaddleApi['Checkout']['open']>[0];

let service: PaddleCheckoutService;
let paddleEnvironmentSet: ReturnType<typeof vi.fn<(environment: PaddleEnvironment) => void>>;
let paddleInitialize: ReturnType<typeof vi.fn<(config: PaddleInitializeConfig) => void>>;
let paddleCheckoutOpen: ReturnType<typeof vi.fn<(config: PaddleCheckoutOpenConfig) => void>>;

beforeEach(() => {
    document.querySelectorAll(`script[src="${PADDLE_SCRIPT_URL}"]`).forEach(script => {
        script.remove();
    });
    Reflect.deleteProperty(window, 'Paddle');

    TestBed.configureTestingModule({});
    service = TestBed.inject(PaddleCheckoutService);
    paddleEnvironmentSet = vi.fn();
    paddleInitialize = vi.fn();
    paddleCheckoutOpen = vi.fn();
});
afterEach(() => vi.useRealTimers());

describe('PaddleCheckoutService checkout', () => {
    it('initializes Paddle and opens a sandbox transaction checkout', async () => {
        addLoadedPaddleScript();
        installPaddleMock();

        await service.openTransactionCheckoutAsync('txn_123', {
            token: 'test_token',
            environment: 'sandbox',
            locale: 'ru',
        });

        expect(paddleEnvironmentSet).toHaveBeenCalledWith('sandbox');
        const initializeConfig = paddleInitialize.mock.calls[0]?.[0];
        expect(initializeConfig).toMatchObject({
            token: 'test_token',
            checkout: {
                settings: {
                    displayMode: 'overlay',
                    allowLogout: false,
                    theme: 'light',
                    locale: 'ru',
                },
            },
        });

        const checkoutConfig = paddleCheckoutOpen.mock.calls[0]?.[0];
        expect(checkoutConfig).toMatchObject({
            transactionId: 'txn_123',
            settings: {
                displayMode: 'overlay',
                successUrl: `${window.location.origin}/premium?checkout=success`,
                allowLogout: false,
                theme: 'light',
                locale: 'ru',
            },
        });
    });

    it('does not initialize Paddle again for the same token and environment', async () => {
        addLoadedPaddleScript();
        installPaddleMock();

        await service.openTransactionCheckoutAsync('txn_1', { token: 'live_token', environment: 'production' });
        await service.openTransactionCheckoutAsync('txn_2', { token: 'live_token', environment: 'production' });

        expect(paddleEnvironmentSet).not.toHaveBeenCalled();
        expect(paddleInitialize).toHaveBeenCalledTimes(1);
        expect(paddleCheckoutOpen).toHaveBeenCalledTimes(2);
    });

    it('rejects when Paddle script loading fails', async () => {
        const openPromise = service.openTransactionCheckoutAsync('txn_123', {
            token: 'live_token',
            environment: 'production',
        });
        const script = document.querySelector<HTMLScriptElement>(`script[src="${PADDLE_SCRIPT_URL}"]`);

        script?.dispatchEvent(new Event('error'));

        await expect(openPromise).rejects.toThrow('Failed to load Paddle.js');
    });
});

describe('PaddleCheckoutService prices', () => {
    it('previews both catalog intervals without opening checkout and shares initialization with checkout', async () => {
        addLoadedPaddleScript();
        const paddle = installPaddleMock();
        const preview = vi.fn().mockResolvedValue(createPricePreview());
        paddle.PricePreview = preview;
        const options = { token: 'test_token', environment: 'sandbox' as const };

        const prices = await service.previewPlanPricesAsync({ monthly: 'pri_month', yearly: 'pri_year' }, options);
        expect(preview).toHaveBeenCalledWith({
            items: [
                { priceId: 'pri_month', quantity: 1 },
                { priceId: 'pri_year', quantity: 1 },
            ],
        });
        expect(prices.monthly).toEqual({ formattedTotal: '$8.00', currencyCode: 'USD' });
        expect(prices.yearly).toEqual({ formattedTotal: '$80.00', currencyCode: 'USD' });
        expect(paddleCheckoutOpen).not.toHaveBeenCalled();

        await service.openTransactionCheckoutAsync('txn_existing', options);
        expect(paddleInitialize).toHaveBeenCalledTimes(1);
        expect(paddleCheckoutOpen).toHaveBeenCalledTimes(1);
    });

    it('can retry price loading after a failed SDK script', async () => {
        const failed = service.previewPlanPricesAsync(
            { monthly: 'pri_month', yearly: 'pri_year' },
            { token: 'test_token', environment: 'sandbox' },
        );
        document.querySelector<HTMLScriptElement>(`script[src="${PADDLE_SCRIPT_URL}"]`)?.dispatchEvent(new Event('error'));
        await expect(failed).rejects.toThrow('Failed to load Paddle.js');
        expect(document.querySelector(`script[src="${PADDLE_SCRIPT_URL}"]`)).toBeNull();

        addLoadedPaddleScript();
        const paddle = installPaddleMock();
        paddle.PricePreview = vi.fn().mockResolvedValue(createPricePreview());
        const result = await service.previewPlanPricesAsync(
            { monthly: 'pri_month', yearly: 'pri_year' },
            { token: 'test_token', environment: 'sandbox' },
        );
        expect(result.yearly.formattedTotal).toBe('$80.00');
        expect(paddleCheckoutOpen).not.toHaveBeenCalled();
    });

    it('ends an unresponsive price request so the page can offer retry', async () => {
        vi.useFakeTimers();
        addLoadedPaddleScript();
        const paddle = installPaddleMock();
        paddle.PricePreview = vi.fn().mockImplementation(async () => new Promise(() => {}));
        const pending = service.previewPlanPricesAsync(
            { monthly: 'pri_month', yearly: 'pri_year' },
            { token: 'test_token', environment: 'sandbox' },
        );
        const rejected = expect(pending).rejects.toThrow('Paddle request timed out');
        const requestTimeoutMs = 15000;
        await vi.advanceTimersByTimeAsync(requestTimeoutMs);
        await rejected;
        expect(paddleCheckoutOpen).not.toHaveBeenCalled();
    });
});

function createPricePreview(): unknown {
    return {
        data: {
            currencyCode: 'USD',
            details: {
                lineItems: [
                    { price: { id: 'pri_month', billingCycle: { interval: 'month', frequency: 1 } }, formattedTotals: { total: '$8.00' } },
                    { price: { id: 'pri_year', billingCycle: { interval: 'year', frequency: 1 } }, formattedTotals: { total: '$80.00' } },
                ],
            },
        },
    };
}

function addLoadedPaddleScript(): void {
    const script = document.createElement('script');
    script.src = PADDLE_SCRIPT_URL;
    document.head.appendChild(script);
}

function installPaddleMock(): PaddleApi {
    window.Paddle = {
        Environment: {
            set: paddleEnvironmentSet,
        },
        Initialize: paddleInitialize,
        Checkout: {
            open: paddleCheckoutOpen,
        },
    };
    return window.Paddle;
}

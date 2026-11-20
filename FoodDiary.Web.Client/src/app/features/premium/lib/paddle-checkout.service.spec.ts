import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { PaddleCheckoutService, type PaddleEnvironment } from './paddle-checkout.service';

const PADDLE_SCRIPT_URL = 'https://cdn.paddle.com/paddle/v2/paddle.js';
const PRICE_IDS = { monthly: 'pri_month', yearly: 'pri_year' };
const SANDBOX_OPTIONS = { token: 'test_token', environment: 'sandbox' } as const;

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

describe('Paddle checkout availability', () => {
    it('keeps scripts and checkout unavailable during server rendering', async () => {
        TestBed.resetTestingModule();
        TestBed.configureTestingModule({ providers: [{ provide: PLATFORM_ID, useValue: 'server' }] });
        const serverCheckout = TestBed.inject(PaddleCheckoutService);

        await expect(serverCheckout.openTransactionCheckoutAsync('txn_ssr', SANDBOX_OPTIONS)).rejects.toThrow(
            'only available in the browser',
        );
        await expect(serverCheckout.previewPlanPricesAsync(PRICE_IDS, SANDBOX_OPTIONS)).rejects.toThrow('only available in the browser');

        expect(document.querySelector(`script[src="${PADDLE_SCRIPT_URL}"]`)).toBeNull();
        expect(paddleCheckoutOpen).not.toHaveBeenCalled();
    });

    it('can retry the price preview if checkout initialized before the price API was available', async () => {
        addLoadedPaddleScript();
        const paddle = installPaddleMock();

        await expect(service.previewPlanPricesAsync(PRICE_IDS, SANDBOX_OPTIONS)).rejects.toThrow('Paddle price preview is unavailable');
        expect(paddleCheckoutOpen).not.toHaveBeenCalled();
        paddle.PricePreview = vi.fn().mockResolvedValue(createPricePreview());

        const prices = await service.previewPlanPricesAsync(PRICE_IDS, SANDBOX_OPTIONS);
        expect(prices.monthly.formattedTotal).toBe('$8.00');
        expect(paddleInitialize).toHaveBeenCalledOnce();
    });
});

describe('Paddle script ownership', () => {
    it('shares one pending script between price preview and transaction checkout', async () => {
        const preview = service.previewPlanPricesAsync(PRICE_IDS, SANDBOX_OPTIONS);
        const checkout = service.openTransactionCheckoutAsync('txn_pending', SANDBOX_OPTIONS);
        const script = requirePaddleScript();
        expect(document.querySelectorAll(`script[src="${PADDLE_SCRIPT_URL}"]`)).toHaveLength(1);
        expect(paddleInitialize).not.toHaveBeenCalled();

        const paddle = installPaddleMock();
        paddle.PricePreview = vi.fn().mockResolvedValue(createPricePreview());
        script.dispatchEvent(new Event('load'));
        const [prices] = await Promise.all([preview, checkout]);

        expect(prices.yearly.formattedTotal).toBe('$80.00');
        expect(paddleInitialize).toHaveBeenCalledOnce();
        expect(paddleCheckoutOpen).toHaveBeenCalledOnce();
    });

    it('waits for an already inserted script without appending a duplicate', async () => {
        addLoadedPaddleScript();
        const script = requirePaddleScript();
        const pending = service.openTransactionCheckoutAsync('txn_existing_script', SANDBOX_OPTIONS);

        expect(requirePaddleScript()).toBe(script);
        expect(document.querySelectorAll(`script[src="${PADDLE_SCRIPT_URL}"]`)).toHaveLength(1);
        expect(paddleInitialize).not.toHaveBeenCalled();
        installPaddleMock();
        script.dispatchEvent(new Event('load'));

        await pending;
        expect(paddleCheckoutOpen).toHaveBeenCalledOnce();
    });

    it('replaces a loaded script that did not provide the Paddle API on retry', async () => {
        const first = service.previewPlanPricesAsync(PRICE_IDS, SANDBOX_OPTIONS);
        const brokenScript = requirePaddleScript();
        brokenScript.dispatchEvent(new Event('load'));
        await expect(first).rejects.toThrow('Paddle.js did not initialize');

        expect(brokenScript.isConnected).toBe(false);
        const retry = service.previewPlanPricesAsync(PRICE_IDS, SANDBOX_OPTIONS);
        const newScript = requirePaddleScript();
        expect(newScript).not.toBe(brokenScript);
        const paddle = installPaddleMock();
        paddle.PricePreview = vi.fn().mockResolvedValue(createPricePreview());
        newScript.dispatchEvent(new Event('load'));

        const prices = await retry;
        expect(prices.yearly.formattedTotal).toBe('$80.00');
        expect(paddleInitialize).toHaveBeenCalledOnce();
        expect(paddleCheckoutOpen).not.toHaveBeenCalled();
    });
});

function requirePaddleScript(): HTMLScriptElement {
    const script = document.querySelector<HTMLScriptElement>(`script[src="${PADDLE_SCRIPT_URL}"]`);
    if (script === null) {
        throw new Error('Expected a Paddle script to be present');
    }
    return script;
}

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

import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { PaddleCheckoutService, type PaddleEnvironment } from './paddle-checkout.service';

const PADDLE_SCRIPT_URL = 'https://cdn.paddle.com/paddle/v2/paddle.js';

type PaddleApi = NonNullable<Window['Paddle']>;
type PaddleInitializeConfig = Parameters<PaddleApi['Initialize']>[0];
type PaddleCheckoutOpenConfig = Parameters<PaddleApi['Checkout']['open']>[0];

let service: PaddleCheckoutService;
let paddleEnvironmentSet: ReturnType<typeof vi.fn<(environment: PaddleEnvironment) => void>>;
let paddleInitialize: ReturnType<typeof vi.fn<(config: PaddleInitializeConfig) => void>>;
let paddleCheckoutOpen: ReturnType<typeof vi.fn<(config: PaddleCheckoutOpenConfig) => void>>;

describe('PaddleCheckoutService', () => {
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

function addLoadedPaddleScript(): void {
    const script = document.createElement('script');
    script.src = PADDLE_SCRIPT_URL;
    document.head.appendChild(script);
}

function installPaddleMock(): void {
    window.Paddle = {
        Environment: {
            set: paddleEnvironmentSet,
        },
        Initialize: paddleInitialize,
        Checkout: {
            open: paddleCheckoutOpen,
        },
    };
}

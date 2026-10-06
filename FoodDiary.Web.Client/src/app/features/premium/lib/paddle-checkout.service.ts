import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID, RendererFactory2, Service } from '@angular/core';

import type { BillingPlan } from '../../../shared/models/billing.models';
import type { PaddlePlanPriceIds, PremiumPlanPrice } from '../models/premium-plan-price';
import { mapPaddlePlanPrices } from './paddle-price-preview.mapper';

export type PaddleEnvironment = 'sandbox' | 'production';

type PaddleCheckoutEvent = {
    name?: string;
    data?: Record<string, unknown>;
};

type PaddleCheckoutSettings = {
    displayMode?: 'overlay' | 'inline';
    successUrl?: string;
    allowLogout?: boolean;
    theme?: 'light' | 'dark';
    locale?: string;
};

type PaddleInitOptions = {
    token: string;
    environment: PaddleEnvironment;
    locale?: string;
};

declare global {
    interface Window {
        Paddle?: {
            Environment: {
                set: (environment: PaddleEnvironment) => void;
            };
            Initialize: (config: {
                token: string;
                pwCustomer?: Record<string, never>;
                eventCallback?: (event: PaddleCheckoutEvent) => void;
                checkout?: {
                    settings?: PaddleCheckoutSettings;
                };
            }) => void;
            Checkout: {
                open: (config: { transactionId: string; settings?: PaddleCheckoutSettings }) => void;
            };
            PricePreview?: (config: { items: Array<{ priceId: string; quantity: number }> }) => Promise<unknown>;
        };
    }
}

@Service()
export class PaddleCheckoutService {
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly renderer = inject(RendererFactory2).createRenderer(null, null);
    private readonly isBrowser = isPlatformBrowser(this.platformId);
    private readonly scriptUrl = 'https://cdn.paddle.com/paddle/v2/paddle.js';
    private scriptLoadPromise: Promise<void> | null = null;
    private initializedToken: string | null = null;
    private initializedEnvironment: PaddleEnvironment | null = null;
    private initializationPromise: Promise<void> | null = null;

    public async previewPlanPricesAsync(
        priceIds: PaddlePlanPriceIds,
        options: PaddleInitOptions,
    ): Promise<Record<BillingPlan, PremiumPlanPrice>> {
        await this.initializeAsync(options);
        const paddle = this.document.defaultView?.Paddle;
        if (paddle?.PricePreview === undefined) {
            throw new Error('Paddle price preview is unavailable');
        }

        const response = await withPaddleTimeoutAsync(
            paddle.PricePreview({
                items: [
                    { priceId: priceIds.monthly, quantity: 1 },
                    { priceId: priceIds.yearly, quantity: 1 },
                ],
            }),
        );
        return mapPaddlePlanPrices(response, priceIds);
    }

    public async openTransactionCheckoutAsync(transactionId: string, options: PaddleInitOptions): Promise<void> {
        if (!this.isBrowser) {
            throw new Error('Paddle Checkout is only available in the browser');
        }

        await this.initializeAsync(options);

        const paddle = this.document.defaultView?.Paddle;
        if (paddle?.Checkout === undefined) {
            throw new Error('Paddle Checkout is unavailable');
        }

        paddle.Checkout.open({
            transactionId,
            settings: {
                displayMode: 'overlay',
                successUrl: this.buildSuccessUrl(),
                allowLogout: false,
                theme: 'light',
                locale: options.locale,
            },
        });
    }

    private async initializeAsync(options: PaddleInitOptions): Promise<void> {
        if (!this.isBrowser) {
            throw new Error('Paddle Checkout is only available in the browser');
        }

        if (this.initializedToken === options.token && this.initializedEnvironment === options.environment) {
            return;
        }

        if (this.initializationPromise !== null) {
            await this.initializationPromise;
            if (this.initializedToken === options.token && this.initializedEnvironment === options.environment) {
                return;
            }
        }

        const initialization = this.initializePaddleAsync(options);
        this.initializationPromise = initialization;
        try {
            await initialization;
        } finally {
            if (this.initializationPromise === initialization) {
                this.initializationPromise = null;
            }
        }
    }

    private async initializePaddleAsync(options: PaddleInitOptions): Promise<void> {
        await this.loadScriptAsync();

        const paddle = this.document.defaultView?.Paddle;
        if (paddle === undefined) {
            throw new Error('Paddle.js did not initialize');
        }

        if (options.environment === 'sandbox') {
            paddle.Environment.set('sandbox');
        }

        paddle.Initialize({
            token: options.token,
            pwCustomer: {},
            checkout: {
                settings: {
                    displayMode: 'overlay',
                    allowLogout: false,
                    theme: 'light',
                    locale: options.locale,
                },
            },
        });

        this.initializedToken = options.token;
        this.initializedEnvironment = options.environment;
    }

    private async loadScriptAsync(): Promise<void> {
        if (!this.isBrowser) {
            return;
        }

        if (this.scriptLoadPromise !== null) {
            return this.scriptLoadPromise;
        }

        this.scriptLoadPromise = new Promise<void>((resolve, reject) => {
            const existingScript = this.document.querySelector<HTMLScriptElement>(`script[src="${this.scriptUrl}"]`);
            if (existingScript !== null) {
                if (this.document.defaultView?.Paddle !== undefined) {
                    resolve();
                    return;
                }

                existingScript.addEventListener(
                    'load',
                    () => {
                        resolve();
                    },
                    { once: true },
                );
                existingScript.addEventListener(
                    'error',
                    () => {
                        reject(new Error('Failed to load Paddle.js'));
                    },
                    { once: true },
                );
                return;
            }

            const script = this.document.createElement('script');
            this.renderer.setAttribute(script, 'src', this.scriptUrl);
            this.renderer.setProperty(script, 'async', true);
            this.renderer.setProperty(script, 'defer', true);
            script.onload = (): void => {
                resolve();
            };
            script.onerror = (): void => {
                reject(new Error('Failed to load Paddle.js'));
            };
            this.renderer.appendChild(this.document.head, script);
        });

        await withPaddleTimeoutAsync(this.scriptLoadPromise).catch(error => {
            this.scriptLoadPromise = null;
            const failedScript = this.document.querySelector<HTMLScriptElement>(`script[src="${this.scriptUrl}"]`);
            failedScript?.remove();
            throw error;
        });
    }

    private buildSuccessUrl(): string {
        return `${this.document.location.origin}/premium?checkout=success`;
    }
}

async function withPaddleTimeoutAsync<T>(operation: Promise<T>): Promise<T> {
    const requestTimeoutMs = 15000;
    let timeout: ReturnType<typeof setTimeout> | undefined;
    const deadline = new Promise<never>((_, reject) => {
        timeout = setTimeout(() => {
            reject(new Error('Paddle request timed out'));
        }, requestTimeoutMs);
    });
    try {
        return await Promise.race([operation, deadline]);
    } finally {
        clearTimeout(timeout);
    }
}

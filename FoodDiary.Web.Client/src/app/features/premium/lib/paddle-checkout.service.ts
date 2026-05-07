import { Injectable } from '@angular/core';

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
        };
    }
}

@Injectable({
    providedIn: 'root',
})
export class PaddleCheckoutService {
    private readonly scriptUrl = 'https://cdn.paddle.com/paddle/v2/paddle.js';
    private scriptLoadPromise: Promise<void> | null = null;
    private initializedToken: string | null = null;
    private initializedEnvironment: PaddleEnvironment | null = null;

    public async openTransactionCheckoutAsync(transactionId: string, options: PaddleInitOptions): Promise<void> {
        await this.initializeAsync(options);

        if (!window.Paddle?.Checkout) {
            throw new Error('Paddle Checkout is unavailable');
        }

        window.Paddle.Checkout.open({
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
        if (this.initializedToken === options.token && this.initializedEnvironment === options.environment) {
            return;
        }

        await this.loadScriptAsync();

        if (!window.Paddle) {
            throw new Error('Paddle.js did not initialize');
        }

        if (options.environment === 'sandbox') {
            window.Paddle.Environment.set('sandbox');
        }

        window.Paddle.Initialize({
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
        if (this.scriptLoadPromise) {
            return this.scriptLoadPromise;
        }

        this.scriptLoadPromise = new Promise<void>((resolve, reject) => {
            const existingScript = document.querySelector<HTMLScriptElement>(`script[src="${this.scriptUrl}"]`);
            if (existingScript) {
                if (window.Paddle) {
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

            const script = document.createElement('script');
            script.src = this.scriptUrl;
            script.async = true;
            script.defer = true;
            script.onload = (): void => {
                resolve();
            };
            script.onerror = (): void => {
                reject(new Error('Failed to load Paddle.js'));
            };
            document.head.appendChild(script);
        });

        return this.scriptLoadPromise;
    }

    private buildSuccessUrl(): string {
        return `${window.location.origin}/premium?checkout=success`;
    }
}

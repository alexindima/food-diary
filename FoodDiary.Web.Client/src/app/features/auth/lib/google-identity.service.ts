import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID, RendererFactory2, Service, signal } from '@angular/core';

type GoogleInitOptions = {
    clientId: string;
    callback: (credential: string) => void;
};

declare global {
    interface Window {
        google?: {
            accounts?: {
                id?: {
                    initialize: (config: {
                        client_id: string;
                        callback: (response: { credential?: string | null }) => void;
                        ux_mode?: 'popup' | 'redirect';
                        auto_select?: boolean;
                        cancel_on_tap_outside?: boolean;
                    }) => void;
                    renderButton: (element: HTMLElement, options: Record<string, unknown>) => void;
                    prompt: () => void;
                    cancel: () => void;
                };
            };
        };
    }
}

@Service()
export class GoogleIdentityService {
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly renderer = inject(RendererFactory2).createRenderer(null, null);
    private readonly isBrowser = isPlatformBrowser(this.platformId);
    private readonly scriptUrl = 'https://accounts.google.com/gsi/client';
    private readonly scriptLoaded = signal<boolean>(false);
    private initializationPromise: Promise<void> | null = null;
    private initializedClientId: string | null = null;
    private callback: ((credential: string) => void) | null = null;

    public async initializeAsync(options: GoogleInitOptions): Promise<void> {
        if (!this.isBrowser) {
            throw new Error('Google Identity Services are only available in the browser');
        }

        if (this.initializedClientId === options.clientId && this.callback !== null) {
            this.callback = options.callback;
            return;
        }

        await this.loadScriptAsync();

        if (window.google?.accounts?.id === undefined) {
            throw new Error('Google Identity Services unavailable');
        }

        this.callback = options.callback;
        window.google.accounts.id.initialize({
            client_id: options.clientId,
            callback: response => {
                if (response.credential !== null && response.credential !== undefined && response.credential.length > 0) {
                    this.callback?.(response.credential);
                }
            },
            ux_mode: 'popup',
            auto_select: false,
            cancel_on_tap_outside: true,
        });

        this.initializedClientId = options.clientId;
    }

    public renderButton(target: HTMLElement, theme: 'outline' | 'filled_blue' = 'outline', locale = 'en'): void {
        if (!this.isBrowser) {
            return;
        }

        if (window.google?.accounts?.id === undefined) {
            return;
        }

        window.google.accounts.id.renderButton(target, {
            type: 'standard',
            size: 'large',
            theme,
            text: 'continue_with',
            shape: 'pill',
            logo_alignment: 'left',
            locale,
        });
    }

    public prompt(): void {
        if (!this.isBrowser) {
            return;
        }

        window.google?.accounts?.id?.prompt();
    }

    public cancel(): void {
        if (!this.isBrowser) {
            return;
        }

        window.google?.accounts?.id?.cancel();
    }

    private async loadScriptAsync(): Promise<void> {
        if (!this.isBrowser) {
            throw new Error('Google Identity Services are only available in the browser');
        }

        if (this.scriptLoaded()) {
            return;
        }
        this.initializationPromise ??= new Promise<void>((resolve, reject) => {
            const script = this.document.createElement('script');
            this.renderer.setAttribute(script, 'src', this.scriptUrl);
            this.renderer.setProperty(script, 'async', true);
            this.renderer.setProperty(script, 'defer', true);
            script.onload = (): void => {
                this.scriptLoaded.set(true);
                resolve();
            };
            script.onerror = (): void => {
                reject(new Error('Failed to load Google Identity script'));
            };
            this.renderer.appendChild(this.document.head, script);
        });
        return this.initializationPromise;
    }
}

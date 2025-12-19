import { Injectable, signal } from '@angular/core';

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

@Injectable({
    providedIn: 'root',
})
export class GoogleIdentityService {
    private readonly scriptUrl = 'https://accounts.google.com/gsi/client';
    private scriptLoaded = signal<boolean>(false);
    private initializationPromise: Promise<void> | null = null;
    private initializedClientId: string | null = null;
    private callback: ((credential: string) => void) | null = null;

    public async initialize(options: GoogleInitOptions): Promise<void> {
        if (this.initializedClientId === options.clientId && this.callback) {
            this.callback = options.callback;
            return;
        }

        await this.loadScript();

        if (!window.google?.accounts?.id) {
            throw new Error('Google Identity Services unavailable');
        }

        this.callback = options.callback;
        window.google.accounts.id.initialize({
            client_id: options.clientId,
            callback: response => {
                if (response.credential) {
                    this.callback?.(response.credential);
                }
            },
            ux_mode: 'popup',
            auto_select: false,
            cancel_on_tap_outside: true,
        });

        this.initializedClientId = options.clientId;
    }

    public renderButton(target: HTMLElement, theme: 'outline' | 'filled_blue' = 'outline'): void {
        if (!window.google?.accounts?.id || !target) {
            return;
        }

        window.google.accounts.id.renderButton(target, {
            type: 'standard',
            size: 'large',
            theme,
            text: 'continue_with',
            shape: 'pill',
            logo_alignment: 'left',
        });
    }

    public prompt(): void {
        window.google?.accounts?.id?.prompt();
    }

    public cancel(): void {
        window.google?.accounts?.id?.cancel();
    }

    private async loadScript(): Promise<void> {
        if (this.scriptLoaded()) {
            return;
        }
        if (!this.initializationPromise) {
            this.initializationPromise = new Promise<void>((resolve, reject) => {
                const script = document.createElement('script');
                script.src = this.scriptUrl;
                script.async = true;
                script.defer = true;
                script.onload = () => {
                    this.scriptLoaded.set(true);
                    resolve();
                };
                script.onerror = () => reject(new Error('Failed to load Google Identity script'));
                document.head.appendChild(script);
            });
        }
        return this.initializationPromise;
    }
}

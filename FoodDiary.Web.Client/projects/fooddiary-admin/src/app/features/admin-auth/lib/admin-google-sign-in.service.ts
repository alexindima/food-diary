import { DOCUMENT } from '@angular/common';
import { inject, RendererFactory2, RendererStyleFlags2, Service } from '@angular/core';

type GoogleIdentity = {
    initialize: (options: {
        client_id: string;
        callback: (response: { credential?: string | null }) => void;
        ux_mode: 'popup';
        auto_select: false;
    }) => void;
    renderButton: (
        target: HTMLElement,
        options: { theme: 'filled_blue' | 'filled_black'; size: 'large'; shape: 'pill'; locale: string },
    ) => void;
};

type GoogleWindow = Window & { google?: { accounts?: { id?: GoogleIdentity } } };

@Service()
export class AdminGoogleSignInService {
    private readonly document = inject(DOCUMENT);
    private readonly renderer = inject(RendererFactory2).createRenderer(null, null);
    private loading: Promise<void> | null = null;

    public async renderAsync(
        target: HTMLElement,
        clientId: string,
        locale: string,
        onCredential: (credential: string) => void,
    ): Promise<void> {
        this.loading ??= new Promise<void>((resolve, reject) => {
            const script = this.document.createElement('script');
            this.renderer.setAttribute(script, 'src', 'https://accounts.google.com/gsi/client');
            script.async = true;
            script.onload = (): void => {
                resolve();
            };
            script.onerror = (): void => {
                reject(new Error('Google Identity Services unavailable'));
            };
            this.renderer.appendChild(this.document.head, script);
        });
        await this.loading;
        const browser = this.document.defaultView as GoogleWindow | null;
        const identity = browser?.google?.accounts?.id;
        if (identity === undefined) {
            throw new Error('Google Identity Services unavailable');
        }
        if (!target.isConnected) {
            return;
        }
        const dark =
            browser?.getComputedStyle(target).colorScheme.includes('dark') === true &&
            browser.matchMedia('(prefers-color-scheme: dark)').matches;
        this.renderer.setStyle(target, 'color-scheme', 'only light', RendererStyleFlags2.DashCase);
        identity.initialize({
            client_id: clientId,
            ux_mode: 'popup',
            auto_select: false,
            callback: ({ credential }) => {
                if (typeof credential === 'string' && credential.length > 0 && target.isConnected) {
                    onCredential(credential);
                }
            },
        });
        identity.renderButton(target, { theme: dark ? 'filled_black' : 'filled_blue', size: 'large', shape: 'pill', locale });
    }
}

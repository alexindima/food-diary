import { inject, Service, signal } from '@angular/core';

import { BrowserStorageService } from '../platform/browser-storage.service';
import { BrowserWindowService } from '../platform/browser-window.service';

/** Public request selectors. None of these values can authenticate without the matching HttpOnly cookie. */
@Service()
export class BrowserSessionStateService {
    private readonly storage = inject(BrowserStorageService);
    private readonly browser = inject(BrowserWindowService);
    public readonly sessionId = signal<string | null>(null);
    public readonly contextId = signal<string | null>(this.storage.getItem('session', 'impersonationContext'));
    public readonly telegram = this.isTelegramLaunch();

    public constructor() {
        for (const area of ['local', 'session'] as const) {
            this.storage.removeItem(area, 'authToken');
            this.storage.removeItem(area, 'refreshToken');
        }
    }

    public setContext(context: string | null): void {
        this.contextId.set(context);
        if (context === null) {
            this.storage.removeItem('session', 'impersonationContext');
        } else {
            this.storage.setItem('session', 'impersonationContext', context);
        }
    }

    public hubUrl(url: string): string {
        const context = this.contextId();
        return context === null ? url : `${url}${url.includes('?') ? '&' : '?'}fd_context=${encodeURIComponent(context)}`;
    }

    private isTelegramLaunch(): boolean {
        const href = this.browser.getHref();
        return (
            this.browser.getTelegramInitData() !== null ||
            (href !== null && new URLSearchParams(new URL(href).hash.slice(1)).has('tgWebAppData'))
        );
    }
}

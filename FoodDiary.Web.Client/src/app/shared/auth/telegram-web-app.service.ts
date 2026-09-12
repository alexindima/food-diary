import { DOCUMENT } from '@angular/common';
import { inject, Service } from '@angular/core';

import { BrowserWindowService } from '../platform/browser-window.service';

const SDK_TIMEOUT_MS = 10_000;

@Service()
export class TelegramWebAppService {
    private readonly document = inject(DOCUMENT);
    private readonly browser = inject(BrowserWindowService);
    private loading: Promise<void> | null = null;

    public isMiniAppLaunch(): boolean {
        if (this.browser.getTelegramInitData() !== null) {
            return true;
        }
        const href = this.browser.getHref();
        return href !== null && new URLSearchParams(new URL(href).hash.slice(1)).has('tgWebAppData');
    }

    public async initializeAsync(): Promise<void> {
        if (this.browser.getTelegramInitData() !== null || !this.isMiniAppLaunch()) {
            return;
        }
        this.loading ??= this.loadAsync().catch((error: unknown) => {
            this.loading = null;
            throw error;
        });
        await this.loading;
        if (this.browser.getTelegramInitData() === null) {
            throw new Error('Telegram Mini App initialization failed');
        }
    }

    private async loadAsync(): Promise<void> {
        return new Promise((resolve, reject) => {
            const script = this.document.createElement('script');
            script.src = 'https://telegram.org/js/telegram-web-app.js?63';
            script.async = true;
            const timeout = this.browser.setTimeout(() => {
                script.remove();
                reject(new Error('Telegram Mini App initialization timed out'));
            }, SDK_TIMEOUT_MS);
            script.onload = (): void => {
                this.browser.clearTimeout(timeout);
                resolve();
            };
            script.onerror = (): void => {
                this.browser.clearTimeout(timeout);
                script.remove();
                reject(new Error('Telegram Mini App SDK unavailable'));
            };
            this.document.head.appendChild(script);
        });
    }
}

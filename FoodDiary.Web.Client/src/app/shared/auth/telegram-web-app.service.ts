import { DOCUMENT } from '@angular/common';
import { inject, Service } from '@angular/core';

import { BrowserStorageService } from '../platform/browser-storage.service';
import { BrowserWindowService } from '../platform/browser-window.service';
import { TELEGRAM_MINI_APP_CONTEXT_KEY } from './browser-session-state.service';

const SDK_TIMEOUT_MS = 10_000;
const LOGGED_OUT_LAUNCH_KEY = 'fooddiary.telegram.logged-out-launch';
const HEX_RADIX = 16;

@Service()
export class TelegramWebAppService {
    private readonly document = inject(DOCUMENT);
    private readonly browser = inject(BrowserWindowService);
    private readonly storage = inject(BrowserStorageService);
    private loading: Promise<void> | null = null;
    private automaticSignInAttempted = false;
    private automaticSignInSuppressed = false;

    public async isAutomaticSignInAllowedAsync(): Promise<boolean> {
        return !this.automaticSignInAttempted && (await this.isLaunchAllowedAsync());
    }

    public async claimAutomaticSignInAsync(): Promise<boolean> {
        if (this.automaticSignInAttempted) {
            return false;
        }
        this.automaticSignInAttempted = true;
        return this.isLaunchAllowedAsync();
    }

    public async suppressAutomaticSignInAsync(): Promise<void> {
        if (!this.isMiniAppLaunch()) {
            return;
        }
        this.automaticSignInSuppressed = true;
        // A digest identifies this launch across reloads without persisting Telegram's proof.
        this.storage.setItem('session', LOGGED_OUT_LAUNCH_KEY, (await this.launchFingerprintAsync()) ?? 'suppressed');
    }

    private async isLaunchAllowedAsync(): Promise<boolean> {
        if (this.automaticSignInSuppressed || !this.isMiniAppLaunch()) {
            return false;
        }
        const loggedOutLaunch = this.storage.getItem('session', LOGGED_OUT_LAUNCH_KEY);
        if (loggedOutLaunch === null) {
            return true;
        }
        const fingerprint = await this.launchFingerprintAsync();
        return fingerprint !== null && fingerprint !== loggedOutLaunch;
    }

    private async launchFingerprintAsync(): Promise<string | null> {
        const href = this.browser.getHref();
        const data =
            this.browser.getTelegramInitData() ??
            (href === null ? null : new URLSearchParams(new URL(href).hash.slice(1)).get('tgWebAppData'));
        const crypto = this.document.defaultView?.crypto.subtle;
        if (data === null || crypto === undefined) {
            return null;
        }
        try {
            const digest = await crypto.digest('SHA-256', new TextEncoder().encode(data));
            return [...new Uint8Array(digest)].map(byte => byte.toString(HEX_RADIX).padStart(2, '0')).join('');
        } catch {
            return null;
        }
    }

    public isMiniAppLaunch(): boolean {
        if (this.browser.getTelegramInitData() !== null || this.storage.getItem('session', TELEGRAM_MINI_APP_CONTEXT_KEY) === 'true') {
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

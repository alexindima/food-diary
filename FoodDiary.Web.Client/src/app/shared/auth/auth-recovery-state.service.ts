import { DestroyRef, inject, Service, signal } from '@angular/core';

import { getRecordProperty, getStringProperty } from '../lib/unknown-value.utils';
import { BrowserStorageService } from '../platform/browser-storage.service';
import { BrowserWindowService } from '../platform/browser-window.service';
import { BrowserSessionStateService } from './browser-session-state.service';

export type PendingLogout = {
    sessionId: string | null;
    contextId: string | null;
    telegram: boolean;
};

const BROWSER_LOGOUT_KEY = 'pendingBrowserLogout';
const TELEGRAM_LOGOUT_KEY = 'pendingTelegramLogout';
const CONTEXT_LOGOUT_KEY = 'pendingImpersonationLogout';
const MAX_RETRY_AFTER_SECONDS = 3600;
const MILLISECONDS_PER_SECOND = 1000;

/** Recovery metadata contains public selectors only, never an authentication proof. */
@Service()
export class AuthRecoveryStateService {
    private readonly storage = inject(BrowserStorageService);
    private readonly telegram = inject(BrowserSessionStateService).telegram;
    public readonly pendingLogout = signal<PendingLogout | null>(this.readPendingLogout());
    public readonly issue = signal<'restore' | 'logout' | null>(this.pendingLogout() === null ? null : 'logout');
    public readonly busy = signal(false);
    private retryAt = 0;
    public returnUrl: string | null = null;

    public constructor() {
        const stop = inject(BrowserWindowService).onLocalStorageChange(event => {
            if (this.isLogoutStorageKey(event.key)) {
                this.pendingLogout.set(this.readPendingLogout());
                this.issue.set(this.pendingLogout() === null ? null : 'logout');
            }
        });
        inject(DestroyRef).onDestroy(stop);
    }

    public beginLogout(pending: PendingLogout): void {
        this.pendingLogout.set(pending);
        this.storage.setJson(
            pending.contextId === null ? 'local' : 'session',
            pending.contextId === null ? this.browserLogoutKey : CONTEXT_LOGOUT_KEY,
            pending,
        );
        this.issue.set('logout');
        this.retryAt = 0;
    }

    public confirmLogout(pending: PendingLogout): void {
        const current = this.pendingLogout();
        if (current?.sessionId !== pending.sessionId || current.contextId !== pending.contextId || current.telegram !== pending.telegram) {
            return;
        }
        this.storage.removeItem(
            pending.contextId === null ? 'local' : 'session',
            pending.contextId === null ? this.browserLogoutKey : CONTEXT_LOGOUT_KEY,
        );
        this.pendingLogout.set(this.readPendingLogout());
        this.issue.set(this.pendingLogout() === null ? null : 'logout');
        this.retryAt = 0;
    }

    public clear(): void {
        this.storage.removeItem('local', this.browserLogoutKey);
        this.storage.removeItem('session', CONTEXT_LOGOUT_KEY);
        this.pendingLogout.set(null);
        this.issue.set(null);
        this.retryAt = 0;
    }

    public restorationFailed(retryAfter: string | null): void {
        this.issue.set(this.pendingLogout() === null ? 'restore' : 'logout');
        const seconds = retryAfter === null ? 0 : Number(retryAfter);
        this.retryAt =
            Number.isFinite(seconds) && seconds > 0 ? Date.now() + Math.min(seconds, MAX_RETRY_AFTER_SECONDS) * MILLISECONDS_PER_SECOND : 0;
    }

    public restorationSucceeded(): void {
        if (this.pendingLogout() === null) {
            this.issue.set(null);
            this.retryAt = 0;
        }
    }

    public canRetry(): boolean {
        return Date.now() >= this.retryAt;
    }

    public isLogoutStorageKey(key: string | null): boolean {
        return key === this.browserLogoutKey;
    }

    private get browserLogoutKey(): string {
        return this.telegram ? TELEGRAM_LOGOUT_KEY : BROWSER_LOGOUT_KEY;
    }

    private readPendingLogout(): PendingLogout | null {
        const pending = this.storage.getJson('session', CONTEXT_LOGOUT_KEY) ?? this.storage.getJson('local', this.browserLogoutKey);
        const sessionId = getStringProperty(pending, 'sessionId');
        const contextId = getStringProperty(pending, 'contextId');
        if (pending === null || (sessionId === undefined && contextId === undefined)) {
            return null;
        }
        if (contextId === undefined && (getRecordProperty(pending, 'telegram') === true) !== this.telegram) {
            return null;
        }
        return { sessionId: sessionId ?? null, contextId: contextId ?? null, telegram: getRecordProperty(pending, 'telegram') === true };
    }
}

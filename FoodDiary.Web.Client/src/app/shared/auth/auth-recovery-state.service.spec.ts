import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it } from 'vitest';

import { getStringProperty } from '../lib/unknown-value.utils';
import { AuthRecoveryStateService } from './auth-recovery-state.service';
import { BrowserSessionStateService } from './browser-session-state.service';

afterEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    TestBed.resetTestingModule();
});

describe('pending logout transport isolation', () => {
    it('does not apply a Mini App logout fence to the ordinary website', () => {
        localStorage.setItem('pendingBrowserLogout', JSON.stringify({ sessionId: 'mini-session', contextId: null, telegram: true }));
        TestBed.configureTestingModule({ providers: [{ provide: BrowserSessionStateService, useValue: { telegram: false } }] });
        const recovery = TestBed.inject(AuthRecoveryStateService);
        expect(recovery.pendingLogout(), 'A Mini App logout must not block website session restoration').toBeNull();
    });

    it('keeps independent website and Mini App pending logout records', () => {
        localStorage.setItem('pendingBrowserLogout', JSON.stringify({ sessionId: 'website-session', contextId: null, telegram: false }));
        TestBed.configureTestingModule({ providers: [{ provide: BrowserSessionStateService, useValue: { telegram: true } }] });
        const recovery = TestBed.inject(AuthRecoveryStateService);
        recovery.beginLogout({ sessionId: 'mini-session', contextId: null, telegram: true });
        recovery.clear();
        const pending: unknown = JSON.parse(localStorage.getItem('pendingBrowserLogout') ?? 'null');
        expect(getStringProperty(pending, 'sessionId'), 'Fresh Mini App sign-in must preserve the independent pending website logout').toBe(
            'website-session',
        );
    });
});

describe('saved logout selectors in browser tabs', () => {
    it('resumes the tab-scoped logout after reload and then retains the independent website logout', () => {
        const browser = { sessionId: 'browser-session', contextId: null, telegram: false };
        const context = { sessionId: null, contextId: 'admin-preview-context', telegram: false };
        const recovery = TestBed.inject(AuthRecoveryStateService);
        recovery.beginLogout(browser);
        recovery.beginLogout(context);
        expect(recovery.pendingLogout()).toEqual(context);
        expect(localStorage.getItem('pendingImpersonationLogout')).toBeNull();
        expect(sessionStorage.getItem('pendingImpersonationLogout')).toBe(JSON.stringify(context));

        TestBed.resetTestingModule();
        const reloaded = TestBed.inject(AuthRecoveryStateService);
        expect(reloaded.pendingLogout()).toEqual(context);
        expect(reloaded.issue()).toBe('logout');
        reloaded.confirmLogout(context);

        expect(sessionStorage.getItem('pendingImpersonationLogout')).toBeNull();
        expect(reloaded.pendingLogout()).toEqual(browser);
        expect(reloaded.issue()).toBe('logout');
    });

    it('reflects a website logout completed in another tab without clearing unrelated session storage', () => {
        const browserKey = 'pendingBrowserLogout';
        const saved = JSON.stringify({ sessionId: 'browser-session', contextId: null, telegram: false });
        localStorage.setItem(browserKey, saved);
        sessionStorage.setItem('draftReturnUrl', '/meals?dateFrom=2026-02-14');
        const recovery = TestBed.inject(AuthRecoveryStateService);
        expect(recovery.issue()).toBe('logout');

        localStorage.removeItem(browserKey);
        const event = new StorageEvent('storage', { key: browserKey, oldValue: saved, newValue: null });
        Object.defineProperty(event, 'storageArea', { value: window.localStorage });
        window.dispatchEvent(event);

        expect(recovery.pendingLogout()).toBeNull();
        expect(recovery.issue()).toBeNull();
        expect(sessionStorage.getItem('draftReturnUrl')).toBe('/meals?dateFrom=2026-02-14');
    });

    it('allows startup with an older saved logout record that has neither selector', () => {
        localStorage.setItem('pendingBrowserLogout', JSON.stringify({ sessionId: null, contextId: null, telegram: false }));
        const recovery = TestBed.inject(AuthRecoveryStateService);

        expect(recovery.pendingLogout()).toBeNull();
        expect(recovery.issue()).toBeNull();
    });

    it('preserves a cross-tab logout when an earlier restoration settles', () => {
        const recovery = TestBed.inject(AuthRecoveryStateService);
        recovery.restorationFailed(null);
        expect(recovery.issue()).toBe('restore');

        const key = 'pendingBrowserLogout';
        const logout = { sessionId: 'previous-session', contextId: null, telegram: false };
        const saved = JSON.stringify(logout);
        localStorage.setItem(key, saved);
        const published = new StorageEvent('storage', { key, newValue: saved });
        Object.defineProperty(published, 'storageArea', { value: window.localStorage });
        window.dispatchEvent(published);
        expect(recovery.issue()).toBe('logout');

        recovery.restorationSucceeded();

        expect(recovery.pendingLogout()).toEqual(logout);
        expect(recovery.issue()).toBe('logout');
        expect(localStorage.getItem(key)).toBe(saved);
        recovery.confirmLogout(logout);
        expect(recovery.pendingLogout()).toBeNull();
        expect(recovery.issue()).toBeNull();
        expect(localStorage.getItem(key)).toBeNull();
    });
});

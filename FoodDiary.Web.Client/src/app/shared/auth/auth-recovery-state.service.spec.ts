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

import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BrowserWindowService } from '../platform/browser-window.service';
import { BrowserSessionStateService, TELEGRAM_MINI_APP_CONTEXT_KEY } from './browser-session-state.service';

describe('Mini App session transport context', () => {
    const browser = { getHref: vi.fn(), getTelegramInitData: vi.fn() };
    const create = (): BrowserSessionStateService => TestBed.runInInjectionContext(() => new BrowserSessionStateService());

    beforeEach(() => {
        sessionStorage.clear();
        localStorage.clear();
        browser.getHref.mockReturnValue('https://fooddiary.example/');
        browser.getTelegramInitData.mockReturnValue(null);
        TestBed.configureTestingModule({ providers: [{ provide: BrowserWindowService, useValue: browser }] });
    });

    it('keeps Telegram transport after navigation and a new application instance', () => {
        browser.getHref.mockReturnValue('https://fooddiary.example/#tgWebAppData=synthetic-proof');
        expect(create().telegram).toBe(true);
        expect(sessionStorage.getItem(TELEGRAM_MINI_APP_CONTEXT_KEY)).toBe('true');
        browser.getHref.mockReturnValue('https://fooddiary.example/dashboard');
        expect(create().telegram).toBe(true);
        expect(sessionStorage.getItem(TELEGRAM_MINI_APP_CONTEXT_KEY)).not.toContain('synthetic-proof');
    });

    it('does not select Telegram for ordinary web or SSR visits', () => {
        expect(create().telegram).toBe(false);
        browser.getHref.mockReturnValue(null);
        expect(create().telegram).toBe(false);
    });

    it('ends the transport context with its tab storage', () => {
        browser.getTelegramInitData.mockReturnValue('synthetic-proof');
        expect(create().telegram).toBe(true);
        sessionStorage.clear();
        browser.getTelegramInitData.mockReturnValue(null);
        expect(create().telegram).toBe(false);
    });

    it('treats a stored transport hint as a selector without restoring credentials', () => {
        sessionStorage.setItem(TELEGRAM_MINI_APP_CONTEXT_KEY, 'true');
        sessionStorage.setItem('authToken', 'obsolete-token');
        localStorage.setItem('refreshToken', 'obsolete-refresh');
        const state = create();
        expect(state.telegram).toBe(true);
        expect(state.sessionId()).toBeNull();
        expect(sessionStorage.getItem('authToken')).toBeNull();
        expect(localStorage.getItem('refreshToken')).toBeNull();
    });
});

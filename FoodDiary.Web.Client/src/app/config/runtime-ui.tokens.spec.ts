import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import {
    ADMIN_LOADING_URL_TTL_MS,
    APP_FILTER_DEBOUNCE_MS,
    APP_MOBILE_VIEWPORT_QUERY,
    APP_SEARCH_DEBOUNCE_MS,
    AUTH_EMAIL_RESEND_COOLDOWN_SECONDS,
    AUTH_LOGIN_AUTOFILL_CHECK_DELAYS_MS,
    AUTH_PASSWORD_RESET_COOLDOWN_SECONDS,
    DASHBOARD_LAYOUT_CONFIG,
    EXPLORE_SEARCH_DEBOUNCE_MS,
    GLOBAL_LOADING_TIMING,
    IDLE_PRELOADING_TIMING_CONFIG,
    NAME_SEARCH_DEBOUNCE_MS,
    RETRY_TIMING_CONFIG,
    ROUTE_LOADING_TIMING,
    SIDEBAR_MOBILE_VIEWPORT_QUERY,
} from './runtime-ui.tokens';

const DEFAULT_DEBOUNCE_MS = 300;
const DEFAULT_EXPLORE_SEARCH_DEBOUNCE_MS = 400;
const DEFAULT_NAME_SEARCH_DEBOUNCE_MS = 600;
const DEFAULT_ADMIN_LOADING_URL_TTL_MS = 30_000;

describe('runtime UI tokens', () => {
    it('should provide loading timing defaults', () => {
        expect(TestBed.inject(GLOBAL_LOADING_TIMING)).toEqual({ showDelayMs: 500, minVisibleMs: 300 });
        expect(TestBed.inject(ROUTE_LOADING_TIMING)).toEqual({ showDelayMs: 150, minVisibleMs: 250 });
    });

    it('should provide viewport and debounce defaults', () => {
        expect(TestBed.inject(APP_MOBILE_VIEWPORT_QUERY)).toBe('(max-width: 768px)');
        expect(TestBed.inject(SIDEBAR_MOBILE_VIEWPORT_QUERY)).toBe('(max-width: 767px)');
        expect(TestBed.inject(APP_SEARCH_DEBOUNCE_MS)).toBe(DEFAULT_DEBOUNCE_MS);
        expect(TestBed.inject(APP_FILTER_DEBOUNCE_MS)).toBe(DEFAULT_DEBOUNCE_MS);
        expect(TestBed.inject(EXPLORE_SEARCH_DEBOUNCE_MS)).toBe(DEFAULT_EXPLORE_SEARCH_DEBOUNCE_MS);
        expect(TestBed.inject(NAME_SEARCH_DEBOUNCE_MS)).toBe(DEFAULT_NAME_SEARCH_DEBOUNCE_MS);
    });

    it('should provide layout, retry and idle defaults', () => {
        expect(TestBed.inject(DASHBOARD_LAYOUT_CONFIG)).toEqual({
            defaultViewportWidth: 1024,
            mobileBreakpointPx: 768,
        });
        expect(TestBed.inject(RETRY_TIMING_CONFIG)).toEqual({ attemptCount: 3, baseDelayMs: 1000 });
        expect(TestBed.inject(IDLE_PRELOADING_TIMING_CONFIG)).toEqual({
            pageReadyFallbackMs: 1500,
            loadEventFallbackMs: 3000,
            idleTimeoutMs: 2000,
            idleFallbackMs: 1200,
        });
        expect(TestBed.inject(ADMIN_LOADING_URL_TTL_MS)).toBe(DEFAULT_ADMIN_LOADING_URL_TTL_MS);
    });

    it('should provide auth timing defaults', () => {
        expect(TestBed.inject(AUTH_PASSWORD_RESET_COOLDOWN_SECONDS)).toBeGreaterThan(0);
        expect(TestBed.inject(AUTH_EMAIL_RESEND_COOLDOWN_SECONDS)).toBeGreaterThan(0);
        expect(TestBed.inject(AUTH_LOGIN_AUTOFILL_CHECK_DELAYS_MS).length).toBeGreaterThan(0);
    });
});

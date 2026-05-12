import { InjectionToken } from '@angular/core';

import {
    AUTH_EMAIL_RESEND_COOLDOWN_SECONDS_DEFAULT,
    AUTH_LOGIN_AUTOFILL_CHECK_DELAYS_MS_DEFAULT,
    AUTH_PASSWORD_RESET_COOLDOWN_SECONDS_DEFAULT,
} from '../features/auth/lib/auth.constants';

export type LoadingTimingConfig = {
    showDelayMs: number;
    minVisibleMs: number;
};

export type DashboardLayoutConfig = {
    defaultViewportWidth: number;
    mobileBreakpointPx: number;
};

export type RetryTimingConfig = {
    attemptCount: number;
    baseDelayMs: number;
};

export type IdlePreloadingTimingConfig = {
    pageReadyFallbackMs: number;
    loadEventFallbackMs: number;
    idleTimeoutMs: number;
    idleFallbackMs: number;
};

const DEFAULT_GLOBAL_LOADING_SHOW_DELAY_MS = 500;
const DEFAULT_GLOBAL_LOADING_MIN_VISIBLE_MS = 300;
const DEFAULT_ROUTE_LOADING_SHOW_DELAY_MS = 150;
const DEFAULT_ROUTE_LOADING_MIN_VISIBLE_MS = 250;
const DEFAULT_FILTER_DEBOUNCE_MS = 300;
const DEFAULT_SEARCH_DEBOUNCE_MS = 300;
const DEFAULT_EXPLORE_SEARCH_DEBOUNCE_MS = 400;
const DEFAULT_NAME_SEARCH_DEBOUNCE_MS = 600;
const DEFAULT_DASHBOARD_VIEWPORT_WIDTH = 1024;
const DEFAULT_DASHBOARD_MOBILE_BREAKPOINT_PX = 768;
const DEFAULT_RETRY_ATTEMPT_COUNT = 3;
const DEFAULT_RETRY_BASE_DELAY_MS = 1_000;
const DEFAULT_PAGE_READY_FALLBACK_MS = 1_500;
const DEFAULT_LOAD_EVENT_FALLBACK_MS = 3_000;
const DEFAULT_IDLE_TIMEOUT_MS = 2_000;
const DEFAULT_IDLE_FALLBACK_MS = 1_200;
const DEFAULT_ADMIN_LOADING_URL_TTL_MS = 30_000;

export const GLOBAL_LOADING_TIMING = new InjectionToken<LoadingTimingConfig>('GLOBAL_LOADING_TIMING', {
    providedIn: 'root',
    factory: (): LoadingTimingConfig => ({
        showDelayMs: DEFAULT_GLOBAL_LOADING_SHOW_DELAY_MS,
        minVisibleMs: DEFAULT_GLOBAL_LOADING_MIN_VISIBLE_MS,
    }),
});

export const ROUTE_LOADING_TIMING = new InjectionToken<LoadingTimingConfig>('ROUTE_LOADING_TIMING', {
    providedIn: 'root',
    factory: (): LoadingTimingConfig => ({
        showDelayMs: DEFAULT_ROUTE_LOADING_SHOW_DELAY_MS,
        minVisibleMs: DEFAULT_ROUTE_LOADING_MIN_VISIBLE_MS,
    }),
});

export const APP_MOBILE_VIEWPORT_QUERY = new InjectionToken<string>('APP_MOBILE_VIEWPORT_QUERY', {
    providedIn: 'root',
    factory: (): string => '(max-width: 768px)',
});

export const SIDEBAR_MOBILE_VIEWPORT_QUERY = new InjectionToken<string>('SIDEBAR_MOBILE_VIEWPORT_QUERY', {
    providedIn: 'root',
    factory: (): string => '(max-width: 767px)',
});

export const APP_SEARCH_DEBOUNCE_MS = new InjectionToken<number>('APP_SEARCH_DEBOUNCE_MS', {
    providedIn: 'root',
    factory: (): number => DEFAULT_SEARCH_DEBOUNCE_MS,
});

export const APP_FILTER_DEBOUNCE_MS = new InjectionToken<number>('APP_FILTER_DEBOUNCE_MS', {
    providedIn: 'root',
    factory: (): number => DEFAULT_FILTER_DEBOUNCE_MS,
});

export const EXPLORE_SEARCH_DEBOUNCE_MS = new InjectionToken<number>('EXPLORE_SEARCH_DEBOUNCE_MS', {
    providedIn: 'root',
    factory: (): number => DEFAULT_EXPLORE_SEARCH_DEBOUNCE_MS,
});

export const NAME_SEARCH_DEBOUNCE_MS = new InjectionToken<number>('NAME_SEARCH_DEBOUNCE_MS', {
    providedIn: 'root',
    factory: (): number => DEFAULT_NAME_SEARCH_DEBOUNCE_MS,
});

export const DASHBOARD_LAYOUT_CONFIG = new InjectionToken<DashboardLayoutConfig>('DASHBOARD_LAYOUT_CONFIG', {
    providedIn: 'root',
    factory: (): DashboardLayoutConfig => ({
        defaultViewportWidth: DEFAULT_DASHBOARD_VIEWPORT_WIDTH,
        mobileBreakpointPx: DEFAULT_DASHBOARD_MOBILE_BREAKPOINT_PX,
    }),
});

export const RETRY_TIMING_CONFIG = new InjectionToken<RetryTimingConfig>('RETRY_TIMING_CONFIG', {
    providedIn: 'root',
    factory: (): RetryTimingConfig => ({
        attemptCount: DEFAULT_RETRY_ATTEMPT_COUNT,
        baseDelayMs: DEFAULT_RETRY_BASE_DELAY_MS,
    }),
});

export const IDLE_PRELOADING_TIMING_CONFIG = new InjectionToken<IdlePreloadingTimingConfig>('IDLE_PRELOADING_TIMING_CONFIG', {
    providedIn: 'root',
    factory: (): IdlePreloadingTimingConfig => ({
        pageReadyFallbackMs: DEFAULT_PAGE_READY_FALLBACK_MS,
        loadEventFallbackMs: DEFAULT_LOAD_EVENT_FALLBACK_MS,
        idleTimeoutMs: DEFAULT_IDLE_TIMEOUT_MS,
        idleFallbackMs: DEFAULT_IDLE_FALLBACK_MS,
    }),
});

export const ADMIN_LOADING_URL_TTL_MS = new InjectionToken<number>('ADMIN_LOADING_URL_TTL_MS', {
    providedIn: 'root',
    factory: (): number => DEFAULT_ADMIN_LOADING_URL_TTL_MS,
});

export const AUTH_PASSWORD_RESET_COOLDOWN_SECONDS = new InjectionToken<number>('AUTH_PASSWORD_RESET_COOLDOWN_SECONDS', {
    providedIn: 'root',
    factory: (): number => AUTH_PASSWORD_RESET_COOLDOWN_SECONDS_DEFAULT,
});

export const AUTH_EMAIL_RESEND_COOLDOWN_SECONDS = new InjectionToken<number>('AUTH_EMAIL_RESEND_COOLDOWN_SECONDS', {
    providedIn: 'root',
    factory: (): number => AUTH_EMAIL_RESEND_COOLDOWN_SECONDS_DEFAULT,
});

export const AUTH_LOGIN_AUTOFILL_CHECK_DELAYS_MS = new InjectionToken<readonly number[]>('AUTH_LOGIN_AUTOFILL_CHECK_DELAYS_MS', {
    providedIn: 'root',
    factory: (): readonly number[] => AUTH_LOGIN_AUTOFILL_CHECK_DELAYS_MS_DEFAULT,
});

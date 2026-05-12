import { InjectionToken } from '@angular/core';

export type LoadingTimingConfig = {
    showDelayMs: number;
    minVisibleMs: number;
};

export type DashboardLayoutConfig = {
    defaultViewportWidth: number;
    mobileBreakpointPx: number;
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

import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../environments/environment';
import { BrowserPerformanceService } from '../shared/platform/browser-performance.service';
import { BrowserWindowService } from '../shared/platform/browser-window.service';
import { FrontendObservabilityService } from './frontend-observability.service';
import { LoggingApiService } from './logging-api.service';

const REQUEST_DURATION_MS = 123.456;
const ROUNDED_REQUEST_DURATION_MS = 123.5;
const ROUTE_DURATION_MS = 42.24;
const ROUNDED_ROUTE_DURATION_MS = 42.2;
const WEB_VITAL_LCP_INITIAL = 1000;
const WEB_VITAL_LCP_DUPLICATE = 1200;
const NAVIGATION_RESPONSE_START_MS = 100;
const PREVIOUS_LOG_EVENT_CALL_INDEX = -2;
const SERVICE_WORKER_VERSION_HASH_LENGTH = 40;
const INITIAL_CLS_SCORE = 0.01;
const EXPECTED_VITAL_DELIVERIES = 5;

let service: FrontendObservabilityService;
let browserPerformanceSpy: {
    observeWebVitalsAsync: ReturnType<typeof vi.fn<BrowserPerformanceService['observeWebVitalsAsync']>>;
    getNavigationResponseStart: ReturnType<typeof vi.fn<BrowserPerformanceService['getNavigationResponseStart']>>;
    now: ReturnType<typeof vi.fn<BrowserPerformanceService['now']>>;
    observeLatestEntry: ReturnType<typeof vi.fn<BrowserPerformanceService['observeLatestEntry']>>;
    observePaintMetric: ReturnType<typeof vi.fn<BrowserPerformanceService['observePaintMetric']>>;
};
let browserWindowSpy: {
    getHref: ReturnType<typeof vi.fn<BrowserWindowService['getHref']>>;
    getPathname: ReturnType<typeof vi.fn<BrowserWindowService['getPathname']>>;
    isAvailable: ReturnType<typeof vi.fn<BrowserWindowService['isAvailable']>>;
    onPageHideOnce: ReturnType<typeof vi.fn<BrowserWindowService['onPageHideOnce']>>;
    onVisibilityHiddenOnce: ReturnType<typeof vi.fn<BrowserWindowService['onVisibilityHiddenOnce']>>;
};
let loggingSpy: { logEvent: ReturnType<typeof vi.fn> };

beforeEach(() => {
    environment.enableClientObservability = true;
    browserPerformanceSpy = {
        observeWebVitalsAsync: vi.fn().mockResolvedValue(undefined),
        getNavigationResponseStart: vi.fn().mockReturnValue(null),
        now: vi.fn().mockReturnValue(0),
        observeLatestEntry: vi.fn().mockReturnValue(null),
        observePaintMetric: vi.fn().mockReturnValue(null),
    };
    browserWindowSpy = {
        getHref: vi.fn().mockReturnValue('https://fooddiary.test/dashboard'),
        getPathname: vi.fn().mockReturnValue('/dashboard'),
        isAvailable: vi.fn().mockReturnValue(true),
        onPageHideOnce: vi.fn(),
        onVisibilityHiddenOnce: vi.fn(),
    };
    loggingSpy = {
        logEvent: vi.fn().mockReturnValue(of(void 0)),
    };

    TestBed.configureTestingModule({
        providers: [
            provideRouter([]),
            FrontendObservabilityService,
            { provide: BrowserPerformanceService, useValue: browserPerformanceSpy },
            { provide: BrowserWindowService, useValue: browserWindowSpy },
            { provide: LoggingApiService, useValue: loggingSpy },
        ],
    });

    service = TestBed.inject(FrontendObservabilityService);
});

afterEach(() => {
    environment.enableClientObservability = false;
    sessionStorage.clear();
});

describe('FrontendObservabilityService PWA diagnostics', () => {
    it('logs PWA failures with a build identifier and without raw errors or browser URLs', () => {
        browserWindowSpy.getHref.mockReturnValue('https://fooddiary.test/profile?token=secret');
        service.recordServiceWorkerFailure('installation', 'a'.repeat(SERVICE_WORKER_VERSION_HASH_LENGTH));
        const payload = loggingSpy.logEvent.mock.calls.at(-1)?.[0] as Record<string, unknown>;
        expect(payload['category']).toBe('client_error');
        expect(payload['name']).toBe('global-error');
        expect(payload['outcome']).toBe('pwa_installation_failed');
        expect(payload['buildVersion']).toBe(environment.buildVersion);
        expect(payload['details']).toEqual({ stage: 'installation', targetVersion: 'a'.repeat(SERVICE_WORKER_VERSION_HASH_LENGTH) });
        expect(payload['location']).toBeUndefined();
        expect(payload['stack']).toBeUndefined();
        expect(JSON.stringify(payload)).not.toContain('secret');
        environment.enableClientObservability = false;
        service.recordServiceWorkerFailure('check');
        expect(loggingSpy.logEvent).toHaveBeenCalledOnce();
    });
});

describe('FrontendObservabilityService URL privacy', () => {
    it('redacts credentials, search values and record identifiers in every telemetry URL field', () => {
        const route = '/meals/5f8d9e1a-3b2c-4d5e-9f0a-123456789abc/edit?token=secret#private';
        vi.spyOn(TestBed.inject(Router), 'url', 'get').mockReturnValue(route);
        browserWindowSpy.getHref.mockReturnValue(`https://fooddiary.test${route}`);
        service.recordHttpRequest({
            url: `https://fooddiary.test/api/v1${route}`,
            method: 'GET',
            statusCode: 200,
            durationMs: 1,
            outcome: 'success',
        });
        service.recordRouteTiming(route, 1, 'success');
        service.recordWebVital('cls', INITIAL_CLS_SCORE, 'score');
        service.recordClientError({ message: 'failure', location: `https://fooddiary.test${route}` });
        for (const [payload] of loggingSpy.logEvent.mock.calls) {
            expect(JSON.stringify(payload), 'Telemetry URLs must not include query secrets or record identifiers').not.toMatch(
                /secret|private|5f8d9e1a/u,
            );
        }
        expect(loggingSpy.logEvent.mock.calls[0][0]).toMatchObject({ route: '/meals/:id/edit', location: '/meals/:id/edit' });
    });
});

describe('FrontendObservabilityService errors and requests', () => {
    it('should not send events when client observability is disabled', () => {
        environment.enableClientObservability = false;

        service.recordClientError({ message: 'Ignored' });

        expect(loggingSpy.logEvent).not.toHaveBeenCalled();
    });

    it('should log client errors with client_error category', () => {
        service.recordClientError({
            message: 'Boom',
            stack: 'stack',
            location: 'http://localhost/test',
        });

        const payload = loggingSpy.logEvent.mock.calls.at(-1)?.[0] as Record<string, unknown>;
        expect(payload['category']).toBe('client_error');
        expect(payload['name']).toBe('global-error');
        expect(payload['level']).toBe('error');
    });

    it('should log http requests with rounded duration and normalized path', () => {
        service.recordHttpRequest({
            url: 'https://fooddiary.club/api/v1/products?page=1',
            method: 'GET',
            statusCode: 200,
            durationMs: REQUEST_DURATION_MS,
            outcome: 'success',
        });

        const payload = loggingSpy.logEvent.mock.calls.at(-1)?.[0] as Record<string, unknown>;
        expect(payload['category']).toBe('http_request');
        expect(payload['durationMs']).toBe(ROUNDED_REQUEST_DURATION_MS);
        expect((payload['details'] as Record<string, unknown>)['url']).toBe('/api/v1/products');
    });

    it('omits malformed HTTP URLs when normalization fails', () => {
        service.recordHttpRequest({
            url: 'http://%',
            method: 'POST',
            statusCode: 0,
            durationMs: REQUEST_DURATION_MS,
            outcome: 'network_error',
        });

        const payload = loggingSpy.logEvent.mock.calls.at(-1)?.[0] as Record<string, unknown>;
        expect(payload['level']).toBe('error');
        expect((payload['details'] as Record<string, unknown>)['url']).toBe('/unknown');
    });

    it('should log route timing with rounded duration and error level', () => {
        service.recordRouteTiming('/meals', ROUTE_DURATION_MS, 'error');

        const payload = loggingSpy.logEvent.mock.calls.at(-1)?.[0] as Record<string, unknown>;
        expect(payload['category']).toBe('route_timing');
        expect(payload['route']).toBe('/meals');
        expect(payload['pageRoute']).toBe('/meals');
        expect(typeof payload['sessionId']).toBe('string');
        expect(payload['durationMs']).toBe(ROUNDED_ROUTE_DURATION_MS);
        expect(payload['level']).toBe('error');
    });

    it('should normalize dynamic route segments for page analytics', () => {
        service.recordRouteTiming('/meals/5f8d9e1a-3b2c-4d5e-9f0a-123456789abc/edit?tab=items', ROUTE_DURATION_MS, 'success');

        const payload = loggingSpy.logEvent.mock.calls.at(-1)?.[0] as Record<string, unknown>;
        expect(payload['route']).toBe('/meals/:id/edit');
        expect(payload['pageRoute']).toBe('/meals/:id/edit');
    });

    it('should reuse the same anonymous telemetry session for multiple events', () => {
        service.recordRouteTiming('/dashboard', ROUTE_DURATION_MS, 'success');
        service.recordRouteTiming('/products', ROUTE_DURATION_MS, 'success');

        const firstPayload = loggingSpy.logEvent.mock.calls.at(PREVIOUS_LOG_EVENT_CALL_INDEX)?.[0] as Record<string, unknown>;
        const secondPayload = loggingSpy.logEvent.mock.calls.at(-1)?.[0] as Record<string, unknown>;
        expect(firstPayload['sessionId']).toBe(secondPayload['sessionId']);
    });
});

describe('FrontendObservabilityService web vitals', () => {
    it('deduplicates identical deliveries but preserves a later final vital value', () => {
        service.recordWebVital('lcp', WEB_VITAL_LCP_INITIAL);
        service.recordWebVital('lcp', WEB_VITAL_LCP_DUPLICATE);
        service.recordWebVital('lcp', WEB_VITAL_LCP_DUPLICATE);

        expect(loggingSpy.logEvent).toHaveBeenCalledTimes(2);
    });

    it('should record navigation timing during initialization for private routes', () => {
        service.initialize();
        const callback = browserPerformanceSpy.observeWebVitalsAsync.mock.calls[0][0];
        vi.spyOn(TestBed.inject(Router), 'url', 'get').mockReturnValue('/meals');
        callback({
            name: 'TTFB',
            value: NAVIGATION_RESPONSE_START_MS,
            delta: NAVIGATION_RESPONSE_START_MS,
            id: 'visit-1',
            rating: 'good',
            navigationType: 'navigate',
        });
        const payload = loggingSpy.logEvent.mock.calls.at(-1)?.[0] as Record<string, unknown>;
        expect(payload['name']).toBe('ttfb');
        expect(payload['route']).toBe('/dashboard');
        expect(payload['value']).toBe(NAVIGATION_RESPONSE_START_MS);
        service.initialize();
        expect(browserPerformanceSpy.observeWebVitalsAsync).toHaveBeenCalledOnce();
        callback({ name: 'CLS', value: 0.012, delta: 0.012, id: 'cls-1', rating: 'good', navigationType: 'navigate' });
        callback({ name: 'INP', value: 120, delta: 120, id: 'inp-1', rating: 'good', navigationType: 'navigate' });
        callback({ name: 'INP', value: 240, delta: 120, id: 'inp-1', rating: 'needs-improvement', navigationType: 'navigate' });
        callback({ name: 'INP', value: 40, delta: 40, id: 'inp-2', rating: 'good', navigationType: 'back-forward-cache' });
        expect(loggingSpy.logEvent.mock.calls[1][0]).toMatchObject({ name: 'cls', value: 0.012, unit: 'score' });
        expect(loggingSpy.logEvent.mock.calls.at(-1)?.[0]).toMatchObject({ name: 'inp', value: 40, details: { metricId: 'inp-2' } });
        expect(loggingSpy.logEvent).toHaveBeenCalledTimes(EXPECTED_VITAL_DELIVERIES);
    });
});

describe('FrontendObservabilityService notification events', () => {
    it('should log notification preference changes as user actions', () => {
        service.recordNotificationPreferenceChanged('push', true, { permission: 'granted' });

        const payload = loggingSpy.logEvent.mock.calls.at(-1)?.[0] as Record<string, unknown>;
        expect(payload['category']).toBe('user_action');
        expect(payload['name']).toBe('notifications.preference.changed');
        expect(payload['outcome']).toBe('enabled');
        expect((payload['details'] as Record<string, unknown>)['preference']).toBe('push');
    });

    it('should log notification subscription events as user actions', () => {
        service.recordNotificationSubscriptionEvent('subscription.ensure', 'blocked', { result: 'blocked' });

        const payload = loggingSpy.logEvent.mock.calls.at(-1)?.[0] as Record<string, unknown>;
        expect(payload['category']).toBe('user_action');
        expect(payload['name']).toBe('notifications.subscription.ensure');
        expect(payload['outcome']).toBe('blocked');
    });

    it('should mark failed notification subscription events as warnings', () => {
        service.recordNotificationSubscriptionEvent('subscription.remove', 'failed');

        const payload = loggingSpy.logEvent.mock.calls.at(-1)?.[0] as Record<string, unknown>;
        expect(payload['name']).toBe('notifications.subscription.remove');
        expect(payload['level']).toBe('warning');
    });
});

describe('FrontendObservabilityService fasting events', () => {
    it('should log fasting reminder preset selection', () => {
        service.recordFastingReminderPresetSelected({
            presetId: 'steady',
            firstReminderHours: 16,
            followUpReminderHours: 24,
        });

        const payload = loggingSpy.logEvent.mock.calls.at(-1)?.[0] as Record<string, unknown>;
        expect(payload['category']).toBe('user_action');
        expect(payload['name']).toBe('fasting.reminder-preset.selected');
        expect((payload['details'] as Record<string, unknown>)['presetId']).toBe('steady');
    });

    it('should log fasting reminder timing saves', () => {
        service.recordFastingReminderTimingSaved({
            firstReminderHours: 12,
            followUpReminderHours: 20,
            source: 'preset',
            presetId: 'starter',
        });

        const payload = loggingSpy.logEvent.mock.calls.at(-1)?.[0] as Record<string, unknown>;
        expect(payload['name']).toBe('fasting.reminder-timing.saved');
        expect((payload['details'] as Record<string, unknown>)['source']).toBe('preset');
    });

    it('should log fasting lifecycle events', () => {
        service.recordFastingLifecycleEvent('check-in.saved', {
            sessionId: 'session-1',
            hungerLevel: 3,
        });

        const payload = loggingSpy.logEvent.mock.calls.at(-1)?.[0] as Record<string, unknown>;
        expect(payload['name']).toBe('fasting.check-in.saved');
        expect((payload['details'] as Record<string, unknown>)['sessionId']).toBe('session-1');
    });
});

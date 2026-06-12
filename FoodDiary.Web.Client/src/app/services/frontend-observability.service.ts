import { inject, Service } from '@angular/core';
import { NavigationCancel, NavigationEnd, NavigationError, NavigationStart, Router } from '@angular/router';
import { filter } from 'rxjs';

import { environment } from '../../environments/environment';
import { ClientTelemetrySessionService } from '../shared/observability/client-telemetry-session.service';
import { type ClientTelemetryEvent, LoggingApiService } from './logging-api.service';

type HttpOutcome = 'success' | 'client_error' | 'server_error' | 'network_error';
type RouteOutcome = 'success' | 'cancelled' | 'error';

const FIRST_NAVIGATION_ENTRY_INDEX = 0;
const DECIMAL_ROUNDING_FACTOR = 10;
const DECIMAL_ROUNDING_DIVISOR = 10;
const UUID_SEGMENT_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/iu;
const NUMERIC_SEGMENT_PATTERN = /^\d+$/u;
const NORMALIZED_ID_SEGMENT = ':id';

@Service()
export class FrontendObservabilityService {
    private readonly router = inject(Router);
    private readonly loggingApiService = inject(LoggingApiService);
    private readonly telemetrySession = inject(ClientTelemetrySessionService);
    private readonly navigationStarts = new Map<number, number>();
    private initialized = false;
    private readonly reportedVitals = new Set<string>();

    public initialize(): void {
        if (this.initialized || environment.enableClientObservability !== true || typeof window === 'undefined') {
            return;
        }

        this.initialized = true;
        this.observeRouteTimings();
        if (!this.isPublicRoute(window.location.pathname)) {
            this.observeWebVitals();
        }
    }

    public recordClientError(error: { message: string; stack?: string; location?: string; details?: Record<string, unknown> }): void {
        this.send({
            category: 'client_error',
            name: 'global-error',
            level: 'error',
            message: error.message,
            stack: error.stack,
            location: error.location,
            details: error.details,
            timestamp: new Date().toISOString(),
            buildVersion: environment.buildVersion,
        });
    }

    public recordHttpRequest(event: { url: string; method: string; statusCode: number; durationMs: number; outcome: HttpOutcome }): void {
        this.send({
            category: 'http_request',
            name: 'api.request',
            level: event.outcome === 'server_error' || event.outcome === 'network_error' ? 'error' : 'info',
            timestamp: new Date().toISOString(),
            location: this.getLocation(),
            route: this.router.url,
            httpMethod: event.method,
            statusCode: event.statusCode,
            durationMs: this.round(event.durationMs),
            outcome: event.outcome,
            buildVersion: environment.buildVersion,
            details: {
                url: this.normalizeUrl(event.url),
            },
        });
    }

    public recordRouteTiming(route: string, durationMs: number, outcome: RouteOutcome): void {
        this.send({
            category: 'route_timing',
            name: 'router.navigation',
            level: outcome === 'error' ? 'error' : 'info',
            timestamp: new Date().toISOString(),
            location: this.getLocation(),
            route,
            durationMs: this.round(durationMs),
            outcome,
            buildVersion: environment.buildVersion,
        });
    }

    public recordWebVital(name: string, value: number, unit: string = 'ms'): void {
        const dedupeKey = `${name}:${unit}`;
        if (this.reportedVitals.has(dedupeKey)) {
            return;
        }

        this.reportedVitals.add(dedupeKey);
        this.send({
            category: 'web_vital',
            name,
            level: 'info',
            timestamp: new Date().toISOString(),
            location: this.getLocation(),
            route: this.router.url,
            value: this.round(value),
            unit,
            buildVersion: environment.buildVersion,
        });
    }

    public recordNotificationSettingsViewed(details: Record<string, unknown>): void {
        this.send({
            category: 'user_action',
            name: 'notifications.settings.viewed',
            level: 'info',
            timestamp: new Date().toISOString(),
            location: this.getLocation(),
            route: this.router.url,
            buildVersion: environment.buildVersion,
            details,
        });
    }

    public recordNotificationPreferenceChanged(
        preference: 'push' | 'fasting' | 'social',
        enabled: boolean,
        details?: Record<string, unknown>,
    ): void {
        this.send({
            category: 'user_action',
            name: 'notifications.preference.changed',
            level: 'info',
            timestamp: new Date().toISOString(),
            location: this.getLocation(),
            route: this.router.url,
            outcome: enabled ? 'enabled' : 'disabled',
            buildVersion: environment.buildVersion,
            details: {
                preference,
                enabled,
                ...details,
            },
        });
    }

    public recordNotificationSubscriptionEvent(
        name: 'subscription.ensure' | 'subscription.remove' | 'test-push.schedule',
        outcome: 'success' | 'blocked' | 'unsupported' | 'unavailable' | 'failed',
        details?: Record<string, unknown>,
    ): void {
        this.send({
            category: 'user_action',
            name: `notifications.${name}`,
            level: outcome === 'failed' ? 'warning' : 'info',
            timestamp: new Date().toISOString(),
            location: this.getLocation(),
            route: this.router.url,
            outcome,
            buildVersion: environment.buildVersion,
            details,
        });
    }

    public recordFastingReminderPresetSelected(details: {
        presetId: string;
        firstReminderHours: number;
        followUpReminderHours: number;
    }): void {
        this.send({
            category: 'user_action',
            name: 'fasting.reminder-preset.selected',
            level: 'info',
            timestamp: new Date().toISOString(),
            location: this.getLocation(),
            route: this.router.url,
            buildVersion: environment.buildVersion,
            details,
        });
    }

    public recordFastingReminderTimingSaved(details: {
        firstReminderHours: number;
        followUpReminderHours: number;
        source: 'preset' | 'manual';
        presetId?: string;
    }): void {
        this.send({
            category: 'user_action',
            name: 'fasting.reminder-timing.saved',
            level: 'info',
            timestamp: new Date().toISOString(),
            location: this.getLocation(),
            route: this.router.url,
            buildVersion: environment.buildVersion,
            details,
        });
    }

    public recordFastingLifecycleEvent(
        name: 'session.started' | 'session.completed' | 'check-in.saved',
        details?: Record<string, unknown>,
    ): void {
        this.send({
            category: 'user_action',
            name: `fasting.${name}`,
            level: 'info',
            timestamp: new Date().toISOString(),
            location: this.getLocation(),
            route: this.router.url,
            buildVersion: environment.buildVersion,
            details,
        });
    }

    private observeRouteTimings(): void {
        this.router.events
            .pipe(
                filter(
                    event =>
                        event instanceof NavigationStart ||
                        event instanceof NavigationEnd ||
                        event instanceof NavigationCancel ||
                        event instanceof NavigationError,
                ),
            )
            .subscribe(event => {
                if (event instanceof NavigationStart) {
                    this.navigationStarts.set(event.id, performance.now());
                    return;
                }

                const startedAt = this.navigationStarts.get(event.id);
                if (startedAt === undefined) {
                    return;
                }

                this.navigationStarts.delete(event.id);
                const durationMs = performance.now() - startedAt;

                if (event instanceof NavigationEnd) {
                    if (!this.isPublicRoute(event.urlAfterRedirects)) {
                        this.recordRouteTiming(event.urlAfterRedirects, durationMs, 'success');
                    }
                    return;
                }

                if (event instanceof NavigationCancel) {
                    if (!this.isPublicRoute(event.url)) {
                        this.recordRouteTiming(event.url, durationMs, 'cancelled');
                    }
                    return;
                }

                if (!this.isPublicRoute(event.url)) {
                    this.recordRouteTiming(event.url, durationMs, 'error');
                }
            });
    }

    private observeWebVitals(): void {
        this.recordNavigationTiming();

        if (typeof PerformanceObserver === 'undefined') {
            return;
        }

        this.observePaintMetrics();
        this.observeLargestContentfulPaint();
    }

    private recordNavigationTiming(): void {
        const entry = performance.getEntriesByType('navigation')[FIRST_NAVIGATION_ENTRY_INDEX];
        if (!(entry instanceof PerformanceNavigationTiming)) {
            return;
        }

        if (entry.responseStart > 0) {
            this.recordWebVital('ttfb', entry.responseStart);
        }
    }

    private observePaintMetrics(): void {
        try {
            const observer = new PerformanceObserver(list => {
                for (const entry of list.getEntries()) {
                    if (entry.name === 'first-contentful-paint') {
                        this.recordWebVital('fcp', entry.startTime);
                    }
                }
            });

            observer.observe({ type: 'paint', buffered: true });
        } catch {
            // Browser support is optional; observability should degrade silently.
        }
    }

    private observeLargestContentfulPaint(): void {
        try {
            let latestEntry: PerformanceEntry | null = null;
            const observer = new PerformanceObserver(list => {
                const entries = list.getEntries();
                latestEntry = entries.at(-1) ?? latestEntry;
            });

            observer.observe({ type: 'largest-contentful-paint', buffered: true });

            const flush = (): void => {
                if (latestEntry !== null) {
                    this.recordWebVital('lcp', latestEntry.startTime);
                }
                observer.disconnect();
            };

            window.addEventListener('pagehide', flush, { once: true });
            document.addEventListener(
                'visibilitychange',
                () => {
                    if (document.visibilityState === 'hidden') {
                        flush();
                    }
                },
                { once: true },
            );
        } catch {
            // Browser support is optional; observability should degrade silently.
        }
    }

    private send(event: ClientTelemetryEvent): void {
        if (environment.enableClientObservability !== true) {
            return;
        }

        this.loggingApiService
            .logEvent({
                ...event,
                sessionId: this.telemetrySession.getSessionId(),
                pageRoute: this.normalizeRoute(event.route ?? this.router.url),
            })
            .subscribe({
                error: () => {
                    // Observability failures should never affect app flow.
                },
            });
    }

    private getLocation(): string {
        return typeof window !== 'undefined' ? window.location.href : '';
    }

    private normalizeUrl(url: string): string {
        try {
            const location = this.getLocation();
            return new URL(url, location.length > 0 ? location : 'http://localhost').pathname;
        } catch {
            return url;
        }
    }

    private normalizeRoute(route: string): string {
        const path = route.split(/[?#]/u, 1)[0];
        if (path.length === 0) {
            return '/';
        }

        return path
            .split('/')
            .map(segment => (this.isDynamicRouteSegment(segment) ? NORMALIZED_ID_SEGMENT : segment))
            .join('/');
    }

    private isDynamicRouteSegment(segment: string): boolean {
        const decodedSegment = this.decodeRouteSegment(segment).toLowerCase();
        return UUID_SEGMENT_PATTERN.test(decodedSegment) || NUMERIC_SEGMENT_PATTERN.test(decodedSegment);
    }

    private decodeRouteSegment(segment: string): string {
        try {
            return decodeURIComponent(segment);
        } catch {
            return segment;
        }
    }

    private round(value: number): number {
        return Math.round(value * DECIMAL_ROUNDING_FACTOR) / DECIMAL_ROUNDING_DIVISOR;
    }

    private isPublicRoute(url: string): boolean {
        const path = url.split(/[?#]/u, 1)[0].toLowerCase();
        return path === '/' || path.startsWith('/auth') || path === '/privacy-policy' || PUBLIC_SEO_PATHS.has(path);
    }
}

const PUBLIC_SEO_PATHS = new Set([
    '/food-diary',
    '/calorie-counter',
    '/meal-planner',
    '/macro-tracker',
    '/intermittent-fasting',
    '/meal-tracker',
    '/weight-loss-app',
    '/dietologist-collaboration',
    '/nutrition-planner',
    '/weight-tracker',
    '/body-progress-tracker',
    '/shopping-list-for-meal-planning',
    '/nutrition-tracker',
    '/food-log',
    '/protein-tracker',
    '/meal-prep-planner',
]);

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { FrontendObservabilityService } from './frontend-observability.service';
import { LoggingApiService } from './logging-api.service';
import { environment } from '../../environments/environment';

describe('FrontendObservabilityService', () => {
    let service: FrontendObservabilityService;
    let loggingSpy: { logEvent: ReturnType<typeof vi.fn> };

    beforeEach(() => {
        environment.enableClientObservability = true;
        loggingSpy = {
            logEvent: vi.fn().mockReturnValue(of(undefined)),
        };

        TestBed.configureTestingModule({
            providers: [provideRouter([]), FrontendObservabilityService, { provide: LoggingApiService, useValue: loggingSpy }],
        });

        service = TestBed.inject(FrontendObservabilityService);
    });

    afterEach(() => {
        environment.enableClientObservability = false;
    });

    it('should log client errors with client_error category', () => {
        service.recordClientError({
            message: 'Boom',
            stack: 'stack',
            location: 'http://localhost/test',
        });

        const payload = loggingSpy.logEvent.mock.calls.at(-1)![0] as Record<string, unknown>;
        expect(payload['category']).toBe('client_error');
        expect(payload['name']).toBe('global-error');
        expect(payload['level']).toBe('error');
    });

    it('should log http requests with rounded duration and normalized path', () => {
        service.recordHttpRequest({
            url: 'https://fooddiary.club/api/v1/products?page=1',
            method: 'GET',
            statusCode: 200,
            durationMs: 123.456,
            outcome: 'success',
        });

        const payload = loggingSpy.logEvent.mock.calls.at(-1)![0] as Record<string, unknown>;
        expect(payload['category']).toBe('http_request');
        expect(payload['durationMs']).toBe(123.5);
        expect((payload['details'] as Record<string, unknown>)['url']).toBe('/api/v1/products');
    });

    it('should deduplicate repeated web vitals by name', () => {
        service.recordWebVital('lcp', 1000);
        service.recordWebVital('lcp', 1200);

        expect(loggingSpy.logEvent).toHaveBeenCalledTimes(1);
    });

    it('should log notification preference changes as user actions', () => {
        service.recordNotificationPreferenceChanged('push', true, { permission: 'granted' });

        const payload = loggingSpy.logEvent.mock.calls.at(-1)![0] as Record<string, unknown>;
        expect(payload['category']).toBe('user_action');
        expect(payload['name']).toBe('notifications.preference.changed');
        expect(payload['outcome']).toBe('enabled');
        expect((payload['details'] as Record<string, unknown>)['preference']).toBe('push');
    });

    it('should log notification subscription events as user actions', () => {
        service.recordNotificationSubscriptionEvent('subscription.ensure', 'blocked', { result: 'blocked' });

        const payload = loggingSpy.logEvent.mock.calls.at(-1)![0] as Record<string, unknown>;
        expect(payload['category']).toBe('user_action');
        expect(payload['name']).toBe('notifications.subscription.ensure');
        expect(payload['outcome']).toBe('blocked');
    });

    it('should log fasting reminder preset selection', () => {
        service.recordFastingReminderPresetSelected({
            presetId: 'steady',
            firstReminderHours: 16,
            followUpReminderHours: 24,
        });

        const payload = loggingSpy.logEvent.mock.calls.at(-1)![0] as Record<string, unknown>;
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

        const payload = loggingSpy.logEvent.mock.calls.at(-1)![0] as Record<string, unknown>;
        expect(payload['name']).toBe('fasting.reminder-timing.saved');
        expect((payload['details'] as Record<string, unknown>)['source']).toBe('preset');
    });

    it('should log fasting lifecycle events', () => {
        service.recordFastingLifecycleEvent('check-in.saved', {
            sessionId: 'session-1',
            hungerLevel: 3,
        });

        const payload = loggingSpy.logEvent.mock.calls.at(-1)![0] as Record<string, unknown>;
        expect(payload['name']).toBe('fasting.check-in.saved');
        expect((payload['details'] as Record<string, unknown>)['sessionId']).toBe('session-1');
    });
});

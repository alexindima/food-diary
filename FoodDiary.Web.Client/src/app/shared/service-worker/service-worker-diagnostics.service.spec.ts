import { DOCUMENT } from '@angular/common';
import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { FrontendObservabilityService } from '../../services/frontend-observability.service';
import { ServiceWorkerDiagnosticsService } from './service-worker-diagnostics.service';

const FIVE_MINUTES_MS = 300_000;
const VERSION_HASH_LENGTH = 40;
const DISTINCT_FAILURE_COUNT = 3;
const RETRIED_FAILURE_COUNT = 4;
const VERSION = 'a'.repeat(VERSION_HASH_LENGTH);
let navigator: { onLine: boolean };
let record: ReturnType<typeof vi.fn>;

beforeEach(() => {
    vi.useFakeTimers();
    navigator = { onLine: true };
    record = vi.fn();
    TestBed.configureTestingModule({
        providers: [
            { provide: PLATFORM_ID, useValue: 'browser' },
            { provide: DOCUMENT, useValue: { defaultView: { navigator } } },
            { provide: FrontendObservabilityService, useValue: { recordServiceWorkerFailure: record } },
        ],
    });
});

afterEach(() => {
    TestBed.resetTestingModule();
    vi.useRealTimers();
});

describe('Service worker failure diagnostics', () => {
    it('suppresses identical failures for five minutes while keeping other stages and versions observable', () => {
        const service = TestBed.inject(ServiceWorkerDiagnosticsService);
        service.report('installation', VERSION);
        service.report('installation', VERSION);
        service.report('installation', 'b'.repeat(VERSION_HASH_LENGTH));
        service.report('check');
        expect(record).toHaveBeenCalledTimes(DISTINCT_FAILURE_COUNT);
        vi.advanceTimersByTime(FIVE_MINUTES_MS - 1);
        service.report('installation', VERSION);
        expect(record).toHaveBeenCalledTimes(DISTINCT_FAILURE_COUNT);
        vi.advanceTimersByTime(1);
        service.report('installation', VERSION);
        expect(record).toHaveBeenCalledTimes(RETRIED_FAILURE_COUNT);
    });

    it('drops non-hash version data rather than sending URLs or credentials', () => {
        const service = TestBed.inject(ServiceWorkerDiagnosticsService);
        service.report('installation', 'https://private.test/chunk.js?token=secret');
        expect(record).toHaveBeenCalledExactlyOnceWith('installation', undefined);
    });

    it('ignores known offline checks/download failures but keeps unrecoverable failures visible', () => {
        navigator.onLine = false;
        const service = TestBed.inject(ServiceWorkerDiagnosticsService);
        service.report('check');
        service.report('installation', VERSION);
        service.report('recovery');
        expect(record).toHaveBeenCalledExactlyOnceWith('recovery', undefined);
        navigator.onLine = true;
        service.report('check');
        expect(record).toHaveBeenLastCalledWith('check', undefined);
    });

    it('does not create telemetry dependencies on the server', () => {
        TestBed.overrideProvider(PLATFORM_ID, { useValue: 'server' });
        TestBed.inject(ServiceWorkerDiagnosticsService).report('recovery');
        expect(record).not.toHaveBeenCalled();
    });

    it('contains telemetry failures without breaking the worker event handler', () => {
        record.mockImplementation(() => {
            throw new Error('Telemetry unavailable');
        });
        expect(() => {
            TestBed.inject(ServiceWorkerDiagnosticsService).report('recovery');
        }).not.toThrow();
    });
});

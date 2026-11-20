import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it, vi } from 'vitest';
import * as vitals from 'web-vitals';

import { BrowserPerformanceService } from './browser-performance.service';

vi.mock('web-vitals', () => ({ onCLS: vi.fn(), onINP: vi.fn(), onLCP: vi.fn(), onFCP: vi.fn(), onTTFB: vi.fn() }));
afterEach(() => {
    vi.clearAllMocks();
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
});

describe('BrowserPerformanceService', () => {
    it('registers each official metric once for the whole page lifetime', async () => {
        const service = createService();
        const callback = vi.fn();
        await service.observeWebVitalsAsync(callback);
        await service.observeWebVitalsAsync(callback);
        for (const observe of [vitals.onCLS, vitals.onINP, vitals.onLCP, vitals.onFCP, vitals.onTTFB]) {
            expect(observe).toHaveBeenCalledExactlyOnceWith(callback);
        }
    });

    it('does not register browser measurements during SSR', async () => {
        await createService('server').observeWebVitalsAsync(vi.fn());
        expect(vitals.onCLS).not.toHaveBeenCalled();
        expect(vitals.onINP).not.toHaveBeenCalled();
    });
    it('returns a monotonic browser timestamp when available', () => {
        const service = createService();

        expect(service.now()).toBeGreaterThanOrEqual(0);
    });

    it('returns null when navigation timing is unavailable', () => {
        const service = createService();

        expect(service.getNavigationResponseStart()).toBeNull();
    });

    it('continues without web-vitals when a browser metric API throws on registration', async () => {
        const service = createService();
        vi.mocked(vitals.onCLS).mockImplementationOnce(() => {
            throw new Error('Performance metric is unavailable');
        });

        await expect(service.observeWebVitalsAsync(vi.fn())).resolves.toBeUndefined();
        await expect(service.observeWebVitalsAsync(vi.fn())).resolves.toBeUndefined();
        expect(vitals.onCLS).toHaveBeenCalledOnce();
    });
});

describe('BrowserPerformanceService navigation timing', () => {
    it('uses the server clock and never queries browser navigation entries under SSR', () => {
        const timestamp = 1_800_000_000_000;
        vi.spyOn(Date, 'now').mockReturnValue(timestamp);
        const entries = vi.spyOn(performance, 'getEntriesByType');
        const service = createService('server');

        expect(service.now()).toBe(timestamp);
        expect(service.getNavigationResponseStart()).toBeNull();
        expect(entries).not.toHaveBeenCalled();
    });

    it('only accepts a positive numeric response start from the first navigation entry', () => {
        const responseStart = 125;
        const entry = timingEntry('navigation', 'document');
        const malformed = Object.assign(timingEntry('navigation', 'document'), { responseStart: String(responseStart) });
        const receivePending = Object.assign(timingEntry('navigation', 'document'), { responseStart: 0 });
        const received = Object.assign(timingEntry('navigation', 'document'), { responseStart });
        const browserTiming: { getEntriesByType: (type: string) => PerformanceEntry[] } = performance;
        const entries = vi
            .spyOn(browserTiming, 'getEntriesByType')
            .mockReturnValueOnce([entry])
            .mockReturnValueOnce([malformed])
            .mockReturnValueOnce([receivePending])
            .mockReturnValue([received]);
        const service = createService();

        expect(service.getNavigationResponseStart()).toBeNull();
        expect(service.getNavigationResponseStart()).toBeNull();
        expect(service.getNavigationResponseStart()).toBeNull();
        expect(service.getNavigationResponseStart()).toBe(responseStart);
        expect(entries).toHaveBeenLastCalledWith('navigation');
    });
});

describe('BrowserPerformanceService performance observers', () => {
    it('forwards buffered paint entries and picks the last entry for other metric batches', () => {
        const { observe, publish } = installObserver();
        const service = createService();
        const paint = vi.fn();
        const longTask = vi.fn();
        const firstPaint = timingEntry('paint', 'first-paint');
        const contentfulPaint = timingEntry('paint', 'first-contentful-paint');
        const firstTask = timingEntry('longtask', 'first');
        const lastTask = timingEntry('longtask', 'last');

        expect(service.observePaintMetric(paint)).not.toBeNull();
        expect(service.observeLatestEntry('longtask', longTask)).not.toBeNull();
        expect(observe.mock.calls).toEqual([[{ type: 'paint', buffered: true }], [{ type: 'longtask', buffered: true }]]);
        publish[0]?.([firstPaint, contentfulPaint]);
        publish[1]?.([]);
        expect(longTask).not.toHaveBeenCalled();
        publish[1]?.([firstTask, lastTask]);

        expect(paint.mock.calls).toEqual([[firstPaint], [contentfulPaint]]);
        expect(longTask).toHaveBeenCalledExactlyOnceWith(lastTask);
    });

    it('does not create observers in SSR even if the global constructor exists', () => {
        const { publish } = installObserver();
        const service = createService('server');

        expect(service.observePaintMetric(vi.fn())).toBeNull();
        expect(publish).toEqual([]);
    });

    it('disables the unsupported measurement without interrupting the client', () => {
        const { observe } = installObserver();
        observe.mockImplementation(() => {
            throw new Error('This entry type is unsupported');
        });
        const callback = vi.fn();

        expect(createService().observeLatestEntry('invalid-entry-type', callback)).toBeNull();
        expect(callback).not.toHaveBeenCalled();
    });
});

function createService(platform = 'browser'): BrowserPerformanceService {
    TestBed.configureTestingModule({
        providers: [BrowserPerformanceService, { provide: PLATFORM_ID, useValue: platform }],
    });

    return TestBed.inject(BrowserPerformanceService);
}

function timingEntry(entryType: string, name: string): PerformanceEntry {
    return { entryType, name, duration: 0, startTime: 0, toJSON: () => ({ entryType, name }) };
}

function installObserver(): {
    observe: ReturnType<typeof vi.fn<PerformanceObserver['observe']>>;
    publish: Array<(entries: PerformanceEntry[]) => void>;
} {
    const publish: Array<(entries: PerformanceEntry[]) => void> = [];
    const observe = vi.fn<PerformanceObserver['observe']>();
    class TestObserver implements PerformanceObserver {
        public readonly observe = observe;
        public readonly disconnect = vi.fn<PerformanceObserver['disconnect']>();
        public readonly takeRecords = vi.fn<PerformanceObserver['takeRecords']>(() => []);

        public constructor(callback: PerformanceObserverCallback) {
            publish.push(entries => {
                callback(
                    {
                        getEntries: () => entries,
                        getEntriesByType: type => entries.filter(entry => entry.entryType === type),
                        getEntriesByName: name => entries.filter(entry => entry.name === name),
                    },
                    this,
                );
            });
        }
    }
    vi.stubGlobal('PerformanceObserver', TestObserver);
    return { observe, publish };
}

import { isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID, Service } from '@angular/core';
import type { Metric } from 'web-vitals';

export type BrowserWebVital = Pick<Metric, 'name' | 'value' | 'id' | 'delta' | 'rating' | 'navigationType'>;

const FIRST_NAVIGATION_ENTRY_INDEX = 0;

type NavigationTimingLike = {
    responseStart: number;
};

@Service()
export class BrowserPerformanceService {
    private readonly platformId = inject(PLATFORM_ID);
    private readonly isBrowser = isPlatformBrowser(this.platformId);
    private observingVitals = false;

    public async observeWebVitalsAsync(callback: (metric: BrowserWebVital) => void): Promise<void> {
        if (!this.isBrowser || this.observingVitals) {
            return;
        }
        this.observingVitals = true;
        try {
            const { onCLS, onINP, onLCP, onFCP, onTTFB } = await import('web-vitals');
            // Default reporting preserves final values across visibility changes and
            // bfcache restores without emitting every intermediate layout shift.
            for (const observe of [onCLS, onINP, onLCP, onFCP, onTTFB]) {
                observe(callback);
            }
        } catch {
            // Unsupported performance APIs or a failed optional chunk cannot block the app.
        }
    }

    public now(): number {
        return this.isBrowser ? performance.now() : Date.now();
    }

    public getNavigationResponseStart(): number | null {
        if (!this.isBrowser) {
            return null;
        }

        const entry = performance.getEntriesByType('navigation')[FIRST_NAVIGATION_ENTRY_INDEX];
        if (!isNavigationTimingLike(entry) || entry.responseStart <= 0) {
            return null;
        }

        return entry.responseStart;
    }

    public observePaintMetric(callback: (entry: PerformanceEntry) => void): PerformanceObserver | null {
        return this.observePerformanceEntry('paint', entries => {
            for (const entry of entries) {
                callback(entry);
            }
        });
    }

    public observeLatestEntry(type: string, callback: (entry: PerformanceEntry) => void): PerformanceObserver | null {
        return this.observePerformanceEntry(type, entries => {
            const latestEntry = entries.at(-1);
            if (latestEntry !== undefined) {
                callback(latestEntry);
            }
        });
    }

    private observePerformanceEntry(type: string, callback: (entries: PerformanceEntry[]) => void): PerformanceObserver | null {
        if (typeof PerformanceObserver === 'undefined' || !this.isBrowser) {
            return null;
        }

        try {
            const observer = new PerformanceObserver(list => {
                callback(list.getEntries());
            });

            observer.observe({ type, buffered: true });
            return observer;
        } catch {
            return null;
        }
    }
}

function isNavigationTimingLike(entry: PerformanceEntry | undefined): entry is PerformanceEntry & NavigationTimingLike {
    return entry !== undefined && 'responseStart' in entry && typeof entry.responseStart === 'number';
}

import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it, vi } from 'vitest';
import * as vitals from 'web-vitals';

import { BrowserPerformanceService } from './browser-performance.service';

vi.mock('web-vitals', () => ({ onCLS: vi.fn(), onINP: vi.fn(), onLCP: vi.fn(), onFCP: vi.fn(), onTTFB: vi.fn() }));
afterEach(() => vi.clearAllMocks());

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
        TestBed.configureTestingModule({ providers: [{ provide: PLATFORM_ID, useValue: 'server' }] });
        await TestBed.inject(BrowserPerformanceService).observeWebVitalsAsync(vi.fn());
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
});

function createService(): BrowserPerformanceService {
    TestBed.configureTestingModule({
        providers: [BrowserPerformanceService],
    });

    return TestBed.inject(BrowserPerformanceService);
}

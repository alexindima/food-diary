import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { BrowserPerformanceService } from './browser-performance.service';

describe('BrowserPerformanceService', () => {
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

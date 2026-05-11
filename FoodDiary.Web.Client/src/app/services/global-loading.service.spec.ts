import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { GlobalLoadingService } from './global-loading.service';

const SHOW_DELAY_MS = 500;
const BEFORE_SHOW_DELAY_MS = SHOW_DELAY_MS - 1;
const MIN_VISIBLE_DURATION_MS = 300;
const BEFORE_MIN_VISIBLE_DURATION_MS = MIN_VISIBLE_DURATION_MS - 1;

describe('GlobalLoadingService', () => {
    let service: GlobalLoadingService;

    beforeEach(() => {
        vi.useFakeTimers();

        TestBed.configureTestingModule({
            providers: [GlobalLoadingService],
        });

        service = TestBed.inject(GlobalLoadingService);
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('should not show loader for fast requests', () => {
        const complete = service.trackRequest();

        vi.advanceTimersByTime(BEFORE_SHOW_DELAY_MS);
        expect(service.isVisible()).toBe(false);

        complete();
        vi.runOnlyPendingTimers();

        expect(service.isVisible()).toBe(false);
    });

    it('should show loader after delay for long requests', () => {
        service.trackRequest();

        vi.advanceTimersByTime(SHOW_DELAY_MS);

        expect(service.isVisible()).toBe(true);
    });

    it('should keep loader visible for minimum duration after showing', () => {
        const complete = service.trackRequest();

        vi.advanceTimersByTime(SHOW_DELAY_MS);
        expect(service.isVisible()).toBe(true);

        complete();
        vi.advanceTimersByTime(BEFORE_MIN_VISIBLE_DURATION_MS);
        expect(service.isVisible()).toBe(true);

        vi.advanceTimersByTime(1);
        expect(service.isVisible()).toBe(false);
    });

    it('should remain visible while another tracked request is active', () => {
        const firstComplete = service.trackRequest();
        const secondComplete = service.trackRequest();

        vi.advanceTimersByTime(SHOW_DELAY_MS);
        expect(service.isVisible()).toBe(true);

        firstComplete();
        vi.runOnlyPendingTimers();
        expect(service.isVisible()).toBe(true);

        secondComplete();
        vi.advanceTimersByTime(MIN_VISIBLE_DURATION_MS);
        expect(service.isVisible()).toBe(false);
    });
});

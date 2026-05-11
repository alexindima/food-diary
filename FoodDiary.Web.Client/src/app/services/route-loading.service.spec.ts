import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { RouteLoadingService } from './route-loading.service';

const SHOW_DELAY_MS = 150;
const BEFORE_SHOW_DELAY_MS = SHOW_DELAY_MS - 1;
const MIN_VISIBLE_DURATION_MS = 250;
const BEFORE_MIN_VISIBLE_DURATION_MS = MIN_VISIBLE_DURATION_MS - 1;

describe('RouteLoadingService', () => {
    let service: RouteLoadingService;

    beforeEach(() => {
        vi.useFakeTimers();

        TestBed.configureTestingModule({
            providers: [RouteLoadingService],
        });

        service = TestBed.inject(RouteLoadingService);
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('should not show loader for short chunk loads', () => {
        service.beginLoad();

        vi.advanceTimersByTime(BEFORE_SHOW_DELAY_MS);
        expect(service.isVisible()).toBe(false);

        service.endLoad();
        vi.runOnlyPendingTimers();

        expect(service.isVisible()).toBe(false);
    });

    it('should show loader after delay for long chunk loads', () => {
        service.beginLoad();

        vi.advanceTimersByTime(SHOW_DELAY_MS);

        expect(service.isVisible()).toBe(true);
    });

    it('should keep loader visible for minimum duration after showing', () => {
        service.beginLoad();

        vi.advanceTimersByTime(SHOW_DELAY_MS);
        expect(service.isVisible()).toBe(true);

        service.endLoad();
        vi.advanceTimersByTime(BEFORE_MIN_VISIBLE_DURATION_MS);
        expect(service.isVisible()).toBe(true);

        vi.advanceTimersByTime(1);
        expect(service.isVisible()).toBe(false);
    });

    it('should remain visible while another route chunk is still loading', () => {
        service.beginLoad();
        service.beginLoad();

        vi.advanceTimersByTime(SHOW_DELAY_MS);
        expect(service.isVisible()).toBe(true);

        service.endLoad();
        vi.runOnlyPendingTimers();
        expect(service.isVisible()).toBe(true);

        service.endLoad();
        vi.advanceTimersByTime(MIN_VISIBLE_DURATION_MS);
        expect(service.isVisible()).toBe(false);
    });
});

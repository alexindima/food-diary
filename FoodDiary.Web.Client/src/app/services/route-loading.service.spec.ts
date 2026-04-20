import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { RouteLoadingService } from './route-loading.service';

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

        vi.advanceTimersByTime(149);
        expect(service.isVisible()).toBe(false);

        service.endLoad();
        vi.runOnlyPendingTimers();

        expect(service.isVisible()).toBe(false);
    });

    it('should show loader after delay for long chunk loads', () => {
        service.beginLoad();

        vi.advanceTimersByTime(150);

        expect(service.isVisible()).toBe(true);
    });

    it('should keep loader visible for minimum duration after showing', () => {
        service.beginLoad();

        vi.advanceTimersByTime(150);
        expect(service.isVisible()).toBe(true);

        service.endLoad();
        vi.advanceTimersByTime(249);
        expect(service.isVisible()).toBe(true);

        vi.advanceTimersByTime(1);
        expect(service.isVisible()).toBe(false);
    });

    it('should remain visible while another route chunk is still loading', () => {
        service.beginLoad();
        service.beginLoad();

        vi.advanceTimersByTime(150);
        expect(service.isVisible()).toBe(true);

        service.endLoad();
        vi.runOnlyPendingTimers();
        expect(service.isVisible()).toBe(true);

        service.endLoad();
        vi.advanceTimersByTime(250);
        expect(service.isVisible()).toBe(false);
    });
});

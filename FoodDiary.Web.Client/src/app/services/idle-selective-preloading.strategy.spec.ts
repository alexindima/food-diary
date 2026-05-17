import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { IDLE_PRELOADING_TIMING_CONFIG } from '../config/runtime-ui.tokens';
import { AuthService } from './auth.service';
import { IdleSelectivePreloadingStrategy } from './idle-selective-preloading.strategy';

const WAIT_MS = 10;

describe('IdleSelectivePreloadingStrategy', () => {
    afterEach(() => {
        vi.useRealTimers();
    });

    it('skips routes without preload flag', () => {
        const { strategy } = setup(true);
        const load = vi.fn().mockReturnValue(of('loaded'));
        const values: unknown[] = [];

        strategy.preload({ data: {} }, load).subscribe(value => values.push(value));

        expect(load).not.toHaveBeenCalled();
        expect(values).toEqual([]);
    });

    it('skips protected routes when user is not authenticated', () => {
        const { strategy } = setup(false);
        const load = vi.fn().mockReturnValue(of('loaded'));

        strategy.preload({ data: { preload: true }, canActivate: [vi.fn()] }, load).subscribe();

        expect(load).not.toHaveBeenCalled();
    });

    it('preloads eligible routes after page ready and idle fallback', async () => {
        vi.useFakeTimers();
        const { strategy } = setup(true);
        const load = vi.fn().mockReturnValue(of('loaded'));
        const values: unknown[] = [];

        strategy.preload({ data: { preload: true } }, load).subscribe(value => values.push(value));

        await vi.runAllTimersAsync();

        expect(load).toHaveBeenCalledTimes(1);
        expect(values).toEqual(['loaded']);
    });
});

function setup(isAuthenticated: boolean): { strategy: IdleSelectivePreloadingStrategy } {
    TestBed.configureTestingModule({
        providers: [
            IdleSelectivePreloadingStrategy,
            { provide: AuthService, useValue: { isAuthenticated: vi.fn(() => isAuthenticated) } },
            {
                provide: IDLE_PRELOADING_TIMING_CONFIG,
                useValue: {
                    pageReadyFallbackMs: WAIT_MS,
                    loadEventFallbackMs: WAIT_MS,
                    idleTimeoutMs: WAIT_MS,
                    idleFallbackMs: WAIT_MS,
                },
            },
        ],
    });

    return { strategy: TestBed.inject(IdleSelectivePreloadingStrategy) };
}

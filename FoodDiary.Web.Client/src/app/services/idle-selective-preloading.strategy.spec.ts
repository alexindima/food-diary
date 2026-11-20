import { signal, type WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { IDLE_PRELOADING_TIMING_CONFIG } from '../config/runtime-ui.tokens';
import { AuthService } from './auth.service';
import { IdleSelectivePreloadingStrategy } from './idle-selective-preloading.strategy';

const WAIT_MS = 10;

afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
});

describe('IdleSelectivePreloadingStrategy', () => {
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

describe('IdleSelectivePreloadingStrategy browser scheduling', () => {
    it('waits for load and a native idle slot before loading a public route', async () => {
        vi.useFakeTimers();
        vi.spyOn(document, 'readyState', 'get').mockReturnValue('loading');
        const idle = vi.fn<(callback: () => void, options: { timeout: number }) => number>().mockReturnValue(1);
        vi.stubGlobal('requestIdleCallback', idle);
        const { strategy } = setup(false);
        const load = vi.fn(() => of('route chunk'));

        const completed = firstValueFrom(strategy.preload({ data: { preload: true } }, load));
        expect(idle).not.toHaveBeenCalled();
        window.dispatchEvent(new Event('load'));
        await vi.advanceTimersByTimeAsync(0);

        expect(idle).toHaveBeenCalledExactlyOnceWith(expect.any(Function), { timeout: WAIT_MS });
        expect(load).not.toHaveBeenCalled();
        idle.mock.calls[0]?.[0]();
        await expect(completed).resolves.toBe('route chunk');
        expect(load).toHaveBeenCalledOnce();
    });

    it('skips a protected preload when the user signs out before the idle callback', async () => {
        vi.useFakeTimers();
        vi.spyOn(document, 'readyState', 'get').mockReturnValue('complete');
        const idle = vi.fn<(callback: () => void, options: { timeout: number }) => number>().mockReturnValue(1);
        vi.stubGlobal('requestIdleCallback', idle);
        const { strategy, authenticated } = setup(true);
        const load = vi.fn(() => of('private route chunk'));
        const route = { data: { preload: true }, canActivate: [vi.fn(() => true)] };

        const completed = firstValueFrom(strategy.preload(route, load), { defaultValue: null });
        await vi.advanceTimersByTimeAsync(0);
        expect(idle).toHaveBeenCalledOnce();
        authenticated.set(false);
        idle.mock.calls[0]?.[0]();

        await expect(completed).resolves.toBeNull();
        expect(load).not.toHaveBeenCalled();
    });

    it('never starts a route request after its preload has been unsubscribed', async () => {
        vi.useFakeTimers();
        vi.spyOn(document, 'readyState', 'get').mockReturnValue('complete');
        vi.stubGlobal('requestIdleCallback', undefined);
        const { strategy } = setup(true);
        const load = vi.fn(() => of('route chunk'));
        const subscription = strategy.preload({ data: { preload: true } }, load).subscribe();
        await vi.advanceTimersByTimeAsync(0);
        subscription.unsubscribe();

        await vi.runAllTimersAsync();

        expect(load).not.toHaveBeenCalled();
    });
});

describe('IdleSelectivePreloadingStrategy page readiness fallback', () => {
    it.each([false, true])('still preloads if the load event does not arrive, listeners available=%s', async listenersAvailable => {
        vi.useFakeTimers();
        vi.spyOn(document, 'readyState', 'get').mockReturnValue('loading');
        vi.stubGlobal('requestIdleCallback', undefined);
        const { strategy } = setup(true);
        if (!listenersAvailable) {
            vi.stubGlobal('addEventListener', undefined);
        }
        const load = vi.fn(() => of('route chunk'));
        const completed = firstValueFrom(strategy.preload({ data: { preload: true } }, load));

        expect(load).not.toHaveBeenCalled();
        await vi.advanceTimersByTimeAsync(WAIT_MS);
        expect(load).not.toHaveBeenCalled();
        await vi.advanceTimersByTimeAsync(WAIT_MS);

        await expect(completed).resolves.toBe('route chunk');
        expect(load).toHaveBeenCalledOnce();
    });
});

function setup(isAuthenticated: boolean): { strategy: IdleSelectivePreloadingStrategy; authenticated: WritableSignal<boolean> } {
    const authenticated = signal(isAuthenticated);
    TestBed.configureTestingModule({
        providers: [
            IdleSelectivePreloadingStrategy,
            { provide: AuthService, useValue: { isAuthenticated: authenticated } },
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

    return { strategy: TestBed.inject(IdleSelectivePreloadingStrategy), authenticated };
}

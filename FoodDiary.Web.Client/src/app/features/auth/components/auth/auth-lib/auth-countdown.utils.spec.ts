import { signal } from '@angular/core';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { MS_PER_SECOND } from '../../../../../shared/lib/time.constants';
import { startSecondsCountdown } from './auth-countdown.utils';

const STOP_TEST_SECONDS = 3;

afterEach(() => {
    vi.useRealTimers();
});

describe('startSecondsCountdown', () => {
    it('should decrement seconds until zero', () => {
        vi.useFakeTimers();
        const target = signal(0);
        const destroyRef = { destroyed: false, onDestroy: vi.fn() };

        startSecondsCountdown(target, 2, destroyRef, createTimerAdapter());

        expect(target()).toBe(2);
        vi.advanceTimersByTime(MS_PER_SECOND);
        expect(target()).toBe(1);
        vi.advanceTimersByTime(MS_PER_SECOND);
        expect(target()).toBe(0);
    });

    it('should stop ticking when stopped manually', () => {
        vi.useFakeTimers();
        const target = signal(0);
        const destroyRef = { destroyed: false, onDestroy: vi.fn() };

        const stop = startSecondsCountdown(target, STOP_TEST_SECONDS, destroyRef, createTimerAdapter());
        stop();
        vi.advanceTimersByTime(MS_PER_SECOND);

        expect(target()).toBe(STOP_TEST_SECONDS);
    });
});

function createTimerAdapter(): { setInterval: (callback: () => void, delayMs: number) => number; clearInterval: (id: number) => void } {
    return {
        setInterval: (callback, delayMs) => window.setInterval(callback, delayMs),
        clearInterval: (id): void => {
            window.clearInterval(id);
        },
    };
}

import type { DestroyRef } from '@angular/core';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { AutosaveQueue } from './autosave-queue';

const DEBOUNCE_MS = 100;

function createDestroyRef(): DestroyRef {
    return { onDestroy: vi.fn(() => vi.fn()) } as unknown as DestroyRef;
}

describe('AutosaveQueue', () => {
    afterEach(() => {
        vi.useRealTimers();
    });

    it('persists the latest scheduled value after debounce', () => {
        vi.useFakeTimers();
        const persist = vi.fn();
        const queue = new AutosaveQueue<string>(createDestroyRef(), DEBOUNCE_MS, () => false, persist);

        queue.schedule('first');
        queue.schedule('latest');
        vi.advanceTimersByTime(DEBOUNCE_MS);

        expect(persist).toHaveBeenCalledTimes(1);
        expect(persist).toHaveBeenCalledWith('latest');
        expect(queue.hasPending()).toBe(false);
    });

    it('keeps pending value while busy and flushes it when scheduled again', () => {
        vi.useFakeTimers();
        let isBusy = true;
        const persist = vi.fn();
        const queue = new AutosaveQueue<string>(createDestroyRef(), DEBOUNCE_MS, () => isBusy, persist);

        queue.schedule('pending');
        vi.advanceTimersByTime(DEBOUNCE_MS);

        expect(persist).not.toHaveBeenCalled();
        expect(queue.hasPending()).toBe(true);

        isBusy = false;
        queue.scheduleIfPending();
        vi.advanceTimersByTime(DEBOUNCE_MS);

        expect(persist).toHaveBeenCalledWith('pending');
        expect(queue.hasPending()).toBe(false);
    });

    it('flushes immediate value and clears pending state', () => {
        const persist = vi.fn();
        const queue = new AutosaveQueue<string>(createDestroyRef(), DEBOUNCE_MS, () => false, persist);

        queue.schedule('pending');
        queue.flushNow('current');

        expect(persist).toHaveBeenCalledWith('current');
        expect(queue.hasPending()).toBe(false);
    });

    it('restores pending value only when queue is empty', () => {
        vi.useFakeTimers();
        const persist = vi.fn();
        const queue = new AutosaveQueue<string>(createDestroyRef(), DEBOUNCE_MS, () => false, persist);

        queue.restore('restored');
        queue.restore('ignored');
        queue.scheduleIfPending();
        vi.advanceTimersByTime(DEBOUNCE_MS);

        expect(persist).toHaveBeenCalledWith('restored');
    });
});

import type { DestroyRef, WritableSignal } from '@angular/core';

import { MS_PER_SECOND } from '../../../../../shared/lib/time.constants';

export function startSecondsCountdown(target: WritableSignal<number>, seconds: number, destroyRef: DestroyRef): () => void {
    target.set(seconds);

    let intervalId: number | null = window.setInterval(() => {
        const remaining = target();
        if (remaining <= 1) {
            target.set(0);
            stop();
            return;
        }
        target.set(remaining - 1);
    }, MS_PER_SECOND);

    const stop = (): void => {
        if (intervalId !== null) {
            window.clearInterval(intervalId);
            intervalId = null;
        }
    };

    destroyRef.onDestroy(stop);
    return stop;
}

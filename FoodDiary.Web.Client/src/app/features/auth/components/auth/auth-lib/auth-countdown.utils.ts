import type { DestroyRef, WritableSignal } from '@angular/core';

import { MS_PER_SECOND } from '../../../../../shared/lib/time.constants';
import type { BrowserWindowService } from '../../../../../shared/platform/browser-window.service';

export function startSecondsCountdown(
    target: WritableSignal<number>,
    seconds: number,
    destroyRef: DestroyRef,
    browserWindow: Pick<BrowserWindowService, 'setInterval' | 'clearInterval'>,
): () => void {
    target.set(seconds);

    let intervalId: number | null = browserWindow.setInterval(() => {
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
            browserWindow.clearInterval(intervalId);
            intervalId = null;
        }
    };

    destroyRef.onDestroy(stop);
    return stop;
}

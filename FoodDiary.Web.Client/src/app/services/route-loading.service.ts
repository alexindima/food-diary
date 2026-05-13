import { inject, Injectable, signal } from '@angular/core';

import { ROUTE_LOADING_TIMING } from '../config/runtime-ui.tokens';

@Injectable({ providedIn: 'root' })
export class RouteLoadingService {
    private readonly timing = inject(ROUTE_LOADING_TIMING);

    private activeLoads = 0;
    private visibleSince = 0;
    private showTimer: ReturnType<typeof setTimeout> | null = null;
    private hideTimer: ReturnType<typeof setTimeout> | null = null;

    public readonly isVisible = signal(false);

    public beginLoad(): void {
        this.activeLoads += 1;
        this.cancelHideTimer();

        if (!this.isVisible()) {
            this.scheduleShow();
        }
    }

    public endLoad(): void {
        this.activeLoads = Math.max(0, this.activeLoads - 1);

        if (this.activeLoads > 0) {
            return;
        }

        this.cancelShowTimer();

        if (!this.isVisible()) {
            return;
        }

        const remainingVisibleMs = Math.max(0, this.timing.minVisibleMs - (Date.now() - this.visibleSince));
        if (remainingVisibleMs === 0) {
            this.hideNow();
            return;
        }

        this.hideTimer = setTimeout(() => {
            this.hideTimer = null;
            this.hideNow();
        }, remainingVisibleMs);
    }

    private scheduleShow(): void {
        if (this.showTimer !== null) {
            return;
        }

        this.showTimer = setTimeout(() => {
            this.showTimer = null;

            if (this.activeLoads === 0 || this.isVisible()) {
                return;
            }

            this.visibleSince = Date.now();
            this.isVisible.set(true);
        }, this.timing.showDelayMs);
    }

    private hideNow(): void {
        this.cancelHideTimer();
        this.visibleSince = 0;
        this.isVisible.set(false);
    }

    private cancelShowTimer(): void {
        if (this.showTimer === null) {
            return;
        }

        clearTimeout(this.showTimer);
        this.showTimer = null;
    }

    private cancelHideTimer(): void {
        if (this.hideTimer === null) {
            return;
        }

        clearTimeout(this.hideTimer);
        this.hideTimer = null;
    }
}

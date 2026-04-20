import { Injectable, signal } from '@angular/core';

@Injectable({
    providedIn: 'root',
})
export class GlobalLoadingService {
    private static readonly showDelayMs = 500;
    private static readonly minVisibleMs = 300;

    private activeRequests = 0;
    private visibleSince = 0;
    private showTimer: ReturnType<typeof setTimeout> | null = null;
    private hideTimer: ReturnType<typeof setTimeout> | null = null;

    public readonly isVisible = signal(false);

    public trackRequest(): () => void {
        this.activeRequests += 1;
        this.cancelHideTimer();

        if (!this.isVisible()) {
            this.scheduleShow();
        }

        let completed = false;

        return () => {
            if (completed) {
                return;
            }

            completed = true;
            this.completeRequest();
        };
    }

    private completeRequest(): void {
        this.activeRequests = Math.max(0, this.activeRequests - 1);

        if (this.activeRequests > 0) {
            return;
        }

        this.cancelShowTimer();

        if (!this.isVisible()) {
            return;
        }

        const remainingVisibleMs = Math.max(0, GlobalLoadingService.minVisibleMs - (Date.now() - this.visibleSince));
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

            if (this.activeRequests === 0 || this.isVisible()) {
                return;
            }

            this.visibleSince = Date.now();
            this.isVisible.set(true);
        }, GlobalLoadingService.showDelayMs);
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

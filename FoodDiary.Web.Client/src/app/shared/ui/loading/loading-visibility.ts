import { type DestroyRef, signal } from '@angular/core';

type LoadingTiming = { showDelayMs: number; minVisibleMs: number };

export class LoadingVisibility {
    public readonly isVisible = signal(false);
    private activeLoads = 0;
    private visibleSince = 0;
    private showTimer: ReturnType<typeof setTimeout> | null = null;
    private hideTimer: ReturnType<typeof setTimeout> | null = null;

    public constructor(
        private readonly timing: LoadingTiming,
        destroyRef: DestroyRef,
    ) {
        destroyRef.onDestroy(() => {
            this.cancelShow();
            this.cancelHide();
        });
    }

    public begin(): void {
        this.activeLoads += 1;
        this.cancelHide();
        if (this.isVisible() || this.showTimer !== null) {
            return;
        }
        this.showTimer = setTimeout(() => {
            this.showTimer = null;
            if (this.activeLoads > 0) {
                this.visibleSince = Date.now();
                this.isVisible.set(true);
            }
        }, this.timing.showDelayMs);
    }

    public end(): void {
        this.activeLoads = Math.max(0, this.activeLoads - 1);
        if (this.activeLoads > 0) {
            return;
        }
        this.cancelShow();
        if (!this.isVisible()) {
            return;
        }
        this.cancelHide();
        const remaining = Math.max(0, this.timing.minVisibleMs - (Date.now() - this.visibleSince));
        if (remaining === 0) {
            this.hide();
            return;
        }
        this.hideTimer = setTimeout(() => {
            this.hide();
        }, remaining);
    }

    private hide(): void {
        this.cancelHide();
        this.visibleSince = 0;
        this.isVisible.set(false);
    }

    private cancelShow(): void {
        if (this.showTimer !== null) {
            clearTimeout(this.showTimer);
            this.showTimer = null;
        }
    }

    private cancelHide(): void {
        if (this.hideTimer !== null) {
            clearTimeout(this.hideTimer);
            this.hideTimer = null;
        }
    }
}

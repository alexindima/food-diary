import { DOCUMENT } from '@angular/common';
import { DestroyRef, inject, Service, signal } from '@angular/core';

import { BrowserWindowService } from './browser-window.service';

/** Keep calendar views current across midnight and a suspended browser tab. */
@Service()
export class CurrentLocalDayService {
    private readonly browserWindow = inject(BrowserWindowService);
    private readonly document = inject(DOCUMENT);
    private readonly currentDay = signal(new Date());
    private timeoutId: number | null = null;
    public readonly today = this.currentDay.asReadonly();

    public constructor() {
        if (!this.browserWindow.isAvailable()) {
            return;
        }
        const resume = (): void => {
            this.refresh();
        };
        this.document.addEventListener('visibilitychange', resume);
        this.document.defaultView?.addEventListener('focus', resume);
        this.scheduleNextDay();
        inject(DestroyRef).onDestroy(() => {
            this.browserWindow.clearTimeout(this.timeoutId);
            this.document.removeEventListener('visibilitychange', resume);
            this.document.defaultView?.removeEventListener('focus', resume);
        });
    }

    private refresh(): void {
        const now = new Date();
        if (now.toDateString() !== this.currentDay().toDateString()) {
            this.currentDay.set(now);
        }
        this.scheduleNextDay();
    }

    private scheduleNextDay(): void {
        this.browserWindow.clearTimeout(this.timeoutId);
        const now = new Date();
        const nextDay = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1);
        this.timeoutId = this.browserWindow.setTimeout(() => {
            this.refresh();
        }, nextDay.getTime() - now.getTime());
    }
}

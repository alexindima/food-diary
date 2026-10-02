import { DOCUMENT } from '@angular/common';
import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { fromEvent, Subject } from 'rxjs';

import { BrowserWindowService } from '../../../shared/platform/browser-window.service';
import { normalizeDate } from './dashboard-date.utils';

@Injectable()
export class DashboardLocalDayFacade {
    private readonly browserWindow = inject(BrowserWindowService);
    private readonly document = inject(DOCUMENT);
    private readonly destroyRef = inject(DestroyRef);
    private readonly dayChanges = new Subject<Date>();
    private readonly currentDay = signal(normalizeDate(new Date()));
    private midnightTimer: number | null = null;
    public readonly today = this.currentDay.asReadonly();
    public readonly changes = this.dayChanges.asObservable();

    public constructor() {
        if (!this.browserWindow.isAvailable()) {
            return;
        }
        fromEvent(this.document, 'visibilitychange')
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                if (this.document.visibilityState === 'visible') {
                    this.refresh();
                }
            });
        if (this.document.defaultView !== null) {
            fromEvent(this.document.defaultView, 'focus')
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe(() => this.refresh());
        }
        this.scheduleMidnight();
        this.destroyRef.onDestroy(() => {
            this.browserWindow.clearTimeout(this.midnightTimer);
            this.dayChanges.complete();
        });
    }

    public refresh(): Date {
        const today = normalizeDate(new Date());
        if (today.getTime() !== this.currentDay().getTime()) {
            this.currentDay.set(today);
            this.dayChanges.next(today);
        }
        this.scheduleMidnight();
        return today;
    }

    private scheduleMidnight(): void {
        this.browserWindow.clearTimeout(this.midnightTimer);
        const now = new Date();
        const nextMidnight = normalizeDate(now);
        nextMidnight.setDate(nextMidnight.getDate() + 1);
        this.midnightTimer = this.browserWindow.setTimeout(() => this.refresh(), nextMidnight.getTime() - now.getTime());
    }
}

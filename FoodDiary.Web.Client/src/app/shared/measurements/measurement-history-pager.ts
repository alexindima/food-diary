import { computed, DestroyRef, inject, signal } from '@angular/core';
import { finalize, type Observable } from 'rxjs';

import type { CalendarDate } from '../models/semantics/date-value';
import { calendarDate } from '../models/semantics/date-value';
import { MEASUREMENT_HISTORY_PAGE_SIZE } from './measurement-history.constants';

type DatedMeasurement = { id: string; date: CalendarDate };

/** Date is unique per user in both measurement tables. Keep pagination independent of local time. */
export function previousMeasurementDay(value: CalendarDate): CalendarDate {
    const date = new Date(`${value.split('T')[0]}T00:00:00.000Z`);
    date.setUTCDate(date.getUTCDate() - 1);
    return calendarDate(date.toISOString().split('T')[0] ?? '');
}

/** Dialog-owned rows include one hidden lookahead for the final visible row's delta. */
export class MeasurementHistoryPager<T extends DatedMeasurement> {
    public readonly entries = signal<T[]>([]);
    public readonly loading = signal(false);
    public readonly failed = signal(false);
    public readonly loaded = signal(false);
    public readonly hasMore = signal(true);
    public readonly visibleCount = computed(() => this.entries().length - (this.loaded() && this.hasMore() ? 1 : 0));
    public readonly message = computed(() => {
        if (this.failed()) {
            return 'MEASUREMENT_HISTORY_PAGING.ERROR';
        }
        return this.loaded() && this.entries().length === 0 ? 'MEASUREMENT_HISTORY_PAGING.EMPTY' : null;
    });
    private readonly destroyRef = inject(DestroyRef);
    private dateTo: CalendarDate | undefined;

    public constructor(private readonly fetchPage: (dateTo?: CalendarDate) => Observable<T[]>) {
        this.loadMore();
    }

    public loadMore(): void {
        if (this.loading() || !this.hasMore()) {
            return;
        }
        this.loading.set(true);
        this.failed.set(false);
        const request = this.fetchPage(this.dateTo)
            .pipe(
                finalize(() => {
                    this.loading.set(false);
                }),
            )
            .subscribe({
                next: page => {
                    // Refetch the hidden lookahead: it may have been changed or deleted since the last page.
                    const visible = this.entries().slice(0, this.visibleCount());
                    this.entries.set([...visible, ...page]);
                    this.hasMore.set(page.length > MEASUREMENT_HISTORY_PAGE_SIZE);
                    const lastVisible = page.at(MEASUREMENT_HISTORY_PAGE_SIZE - 1);
                    this.dateTo = lastVisible === undefined ? undefined : previousMeasurementDay(lastVisible.date);
                    this.loaded.set(true);
                },
                error: () => {
                    this.failed.set(true);
                },
            });
        this.destroyRef.onDestroy(() => {
            request.unsubscribe();
        });
    }
}

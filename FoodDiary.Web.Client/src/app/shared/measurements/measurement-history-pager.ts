import { computed, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { type Observable, tap } from 'rxjs';

import { measurementDateInputValue } from '../lib/measurement-date.utils';
import type { CalendarDate } from '../models/semantics/date-value';
import { calendarDate } from '../models/semantics/date-value';
import { MEASUREMENT_HISTORY_PAGE_SIZE } from './measurement-history.constants';

type DatedMeasurement = { id: string; date: CalendarDate };

/** Date is unique per user in both measurement tables. Keep pagination independent of local time. */
export function previousMeasurementDay(value: CalendarDate): CalendarDate {
    const date = new Date(`${measurementDateInputValue(value)}T00:00:00.000Z`);
    date.setUTCDate(date.getUTCDate() - 1);
    return calendarDate(measurementDateInputValue(date.toISOString()));
}

/** Dialog-owned rows include one hidden lookahead for the final visible row's delta. */
export class MeasurementHistoryPager<T extends DatedMeasurement> {
    public readonly entries = signal<T[]>([]);
    private readonly requestedPage = signal<{ dateTo: CalendarDate | undefined } | undefined>(undefined);
    private readonly pageResource = rxResource({
        params: this.requestedPage,
        stream: ({ params, abortSignal }) =>
            this.fetchPage(params.dateTo).pipe(
                tap(page => {
                    if (!abortSignal.aborted && this.requestedPage() === params) {
                        this.appendPage(page);
                    }
                }),
            ),
    });
    public readonly loading = this.pageResource.isLoading;
    public readonly failed = computed(() => this.pageResource.error() !== undefined);
    public readonly loaded = signal(false);
    public readonly hasMore = signal(true);
    public readonly visibleCount = computed(() => this.entries().length - (this.loaded() && this.hasMore() ? 1 : 0));
    public readonly message = computed(() => {
        if (this.failed()) {
            return 'MEASUREMENT_HISTORY_PAGING.ERROR';
        }
        return this.loaded() && this.entries().length === 0 ? 'MEASUREMENT_HISTORY_PAGING.EMPTY' : null;
    });
    private dateTo: CalendarDate | undefined;

    public constructor(private readonly fetchPage: (dateTo?: CalendarDate) => Observable<T[]>) {
        this.loadMore();
    }

    public loadMore(): void {
        if (this.loading() || !this.hasMore()) {
            return;
        }
        this.requestedPage.set({ dateTo: this.dateTo });
    }

    private appendPage(page: T[]): void {
        // Refetch the hidden lookahead: it may have been changed or deleted since the last page.
        const visible = this.entries().slice(0, this.visibleCount());
        this.entries.set([...visible, ...page]);
        this.hasMore.set(page.length > MEASUREMENT_HISTORY_PAGE_SIZE);
        const lastVisible = page.at(MEASUREMENT_HISTORY_PAGE_SIZE - 1);
        this.dateTo = lastVisible === undefined ? undefined : previousMeasurementDay(lastVisible.date);
        this.loaded.set(true);
    }
}

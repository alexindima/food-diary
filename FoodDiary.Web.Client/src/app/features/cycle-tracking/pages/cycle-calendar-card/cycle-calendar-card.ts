import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiCalendarComponent, type FdUiCalendarMarker } from 'fd-ui-kit';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import { toCycleDateKey } from '../../lib/cycle-tracking.mapper';
import type { CycleResponse } from '../../models/cycle.data';

const DAY_MILLISECONDS = 86_400_000;
const DATE_PART_LENGTH = 2;
type MarkerRange = {
    start: string;
    end: string;
    tone: FdUiCalendarMarker['tone'];
    labelKey: string;
};

@Component({
    selector: 'fd-cycle-calendar-card',
    imports: [TranslatePipe, FdUiCardComponent, FdUiCalendarComponent],
    templateUrl: './cycle-calendar-card.html',
    styleUrl: './cycle-calendar-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleCalendarCardComponent {
    private readonly translateService = inject(TranslateService);

    public readonly cycle = input.required<CycleResponse>();
    public readonly locale = input.required<string>();
    public readonly dateSelected = output<string>();

    protected readonly today = new Date();
    protected readonly trackingStartDate = computed(() => new Date(this.cycle().trackingStartDate));
    protected readonly markers = computed<readonly FdUiCalendarMarker[]>(() => {
        this.locale();
        return this.buildMarkers();
    });

    protected selectDate(date: Date | null): void {
        if (date !== null) {
            this.dateSelected.emit(this.toLocalDateKey(date));
        }
    }

    private toLocalDateKey(date: Date): string {
        const month = String(date.getMonth() + 1).padStart(DATE_PART_LENGTH, '0');
        const day = String(date.getDate()).padStart(DATE_PART_LENGTH, '0');
        return `${date.getFullYear()}-${month}-${day}`;
    }

    private buildMarkers(): readonly FdUiCalendarMarker[] {
        const cycle = this.cycle();
        const markers: FdUiCalendarMarker[] = cycle.bleedingEntries.map(entry => ({
            date: toCycleDateKey(entry.date),
            tone: 'danger',
            label: this.translateService.instant('CYCLE_TRACKING.CALENDAR_LOGGED_BLEEDING'),
        }));

        for (const episode of cycle.menstrualEpisodes ?? []) {
            if (episode.status === 1) {
                this.addRangeMarkers(markers, {
                    start: episode.startDate,
                    end: episode.endDate ?? episode.startDate,
                    tone: 'brand',
                    labelKey: 'CYCLE_TRACKING.CALENDAR_CONFIRMED_PERIOD',
                });
            }
        }

        const prediction = cycle.predictions;
        if (prediction?.nextPeriodStartFrom !== null && prediction?.nextPeriodStartFrom !== undefined) {
            this.addRangeMarkers(markers, {
                start: prediction.nextPeriodStartFrom,
                end: prediction.nextPeriodStartTo ?? prediction.nextPeriodStartFrom,
                tone: 'warning',
                labelKey: 'CYCLE_TRACKING.CALENDAR_PREDICTED_PERIOD',
            });
        }

        return markers;
    }

    private addRangeMarkers(markers: FdUiCalendarMarker[], range: MarkerRange): void {
        const startTime = new Date(range.start).getTime();
        const endTime = new Date(range.end).getTime();
        const label = this.translateService.instant(range.labelKey);
        for (let value = startTime; value <= endTime; value += DAY_MILLISECONDS) {
            markers.push({ date: toCycleDateKey(new Date(value).toISOString()), tone: range.tone, label });
        }
    }
}

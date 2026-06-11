import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { DashboardWidgetFrameComponent } from '../../../../components/shared/dashboard-widget-frame/dashboard-widget-frame';
import { NoticeBannerComponent } from '../../../../components/shared/notice-banner/notice-banner';
import { normalizeStartOfUtcDay, parseDateValue } from '../../../../shared/lib/local-date.utils';
import { MS_PER_DAY } from '../../../../shared/lib/time.constants';
import type { CyclePredictions } from '../../../cycle-tracking/models/cycle.data';

@Component({
    selector: 'fd-cycle-summary-card',
    imports: [CommonModule, TranslatePipe, NoticeBannerComponent, DashboardWidgetFrameComponent],
    templateUrl: './cycle-summary-card.html',
    styleUrl: './cycle-summary-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleSummaryCardComponent {
    public readonly startDate = input.required<string | null>();
    public readonly predictions = input.required<CyclePredictions | null>();
    public readonly referenceDate = input.required<Date | string | null>();
    public readonly isLoading = input.required<boolean>();
    public readonly setupAction = output();

    private readonly normalizedStart = computed(() => this.normalizeDate(this.startDate()));
    private readonly normalizedReference = computed(() => this.normalizeDate(this.referenceDate()));

    protected readonly cycleDay = computed(() => {
        const start = this.normalizedStart();
        const reference = this.normalizedReference();
        if (start === null || reference === null) {
            return null;
        }
        const diff = Math.floor((reference.getTime() - start.getTime()) / MS_PER_DAY);
        return Math.max(1, diff + 1);
    });

    protected readonly statusKey = computed(() => {
        const reference = this.normalizedReference();
        if (reference === null) {
            return null;
        }

        const predictions = this.predictions();
        const ovulation = this.normalizeDate(predictions?.ovulationFrom ?? null);
        const nextPeriod = this.normalizeDate(predictions?.nextPeriodStartFrom ?? null);

        if (ovulation !== null) {
            const days = this.daysBetween(reference, ovulation);
            if (days >= 0) {
                return 'CYCLE_CARD.OVULATION_IN';
            }
        }

        if (nextPeriod !== null) {
            const days = this.daysBetween(reference, nextPeriod);
            if (days <= 0) {
                return 'CYCLE_CARD.NEXT_PERIOD_TODAY';
            }
            return 'CYCLE_CARD.NEXT_PERIOD_IN';
        }

        return null;
    });

    protected readonly statusDays = computed(() => {
        const reference = this.normalizedReference();
        if (reference === null) {
            return null;
        }

        const predictions = this.predictions();
        const ovulation = this.normalizeDate(predictions?.ovulationFrom ?? null);
        const nextPeriod = this.normalizeDate(predictions?.nextPeriodStartFrom ?? null);

        if (ovulation !== null) {
            const days = this.daysBetween(reference, ovulation);
            if (days >= 0) {
                return days;
            }
        }

        if (nextPeriod !== null) {
            const days = this.daysBetween(reference, nextPeriod);
            if (days > 0) {
                return days;
            }
        }

        return null;
    });

    protected readonly hasCycle = computed(() => this.normalizedStart() !== null);

    protected onSetup(): void {
        this.setupAction.emit();
    }

    private normalizeDate(value: Date | string | null | undefined): Date | null {
        const date = parseDateValue(value);
        return date !== null ? normalizeStartOfUtcDay(date) : null;
    }

    private daysBetween(from: Date, to: Date): number {
        return Math.round((to.getTime() - from.getTime()) / MS_PER_DAY);
    }
}

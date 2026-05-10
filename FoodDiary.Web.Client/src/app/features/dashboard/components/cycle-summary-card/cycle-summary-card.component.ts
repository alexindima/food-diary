import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { DashboardWidgetFrameComponent } from '../../../../components/shared/dashboard-widget-frame/dashboard-widget-frame.component';
import { NoticeBannerComponent } from '../../../../components/shared/notice-banner/notice-banner.component';
import type { CyclePredictions } from '../../../cycle-tracking/models/cycle.data';

const MILLISECONDS_PER_DAY = 86_400_000;

@Component({
    selector: 'fd-cycle-summary-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe, NoticeBannerComponent, DashboardWidgetFrameComponent],
    templateUrl: './cycle-summary-card.component.html',
    styleUrl: './cycle-summary-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleSummaryCardComponent {
    public readonly startDate = input.required<string | null>();
    public readonly predictions = input.required<CyclePredictions | null>();
    public readonly referenceDate = input.required<Date | string | null>();
    public readonly isLoading = input.required<boolean>();
    public readonly setupAction = output<void>();

    private readonly normalizedStart = computed(() => this.normalizeDate(this.startDate()));
    private readonly normalizedReference = computed(() => this.normalizeDate(this.referenceDate()));

    public readonly cycleDay = computed(() => {
        const start = this.normalizedStart();
        const reference = this.normalizedReference();
        if (start === null || reference === null) {
            return null;
        }
        const diff = Math.floor((reference.getTime() - start.getTime()) / MILLISECONDS_PER_DAY);
        return Math.max(1, diff + 1);
    });

    public readonly statusKey = computed(() => {
        const reference = this.normalizedReference();
        if (reference === null) {
            return null;
        }

        const predictions = this.predictions();
        const ovulation = this.normalizeDate(predictions?.ovulationDate ?? null);
        const nextPeriod = this.normalizeDate(predictions?.nextPeriodStart ?? null);

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

    public readonly statusDays = computed(() => {
        const reference = this.normalizedReference();
        if (reference === null) {
            return null;
        }

        const predictions = this.predictions();
        const ovulation = this.normalizeDate(predictions?.ovulationDate ?? null);
        const nextPeriod = this.normalizeDate(predictions?.nextPeriodStart ?? null);

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

    public readonly hasCycle = computed(() => this.normalizedStart() !== null);

    public onSetup(): void {
        this.setupAction.emit();
    }

    private normalizeDate(value: Date | string | null | undefined): Date | null {
        if (value === null || value === undefined || value === '') {
            return null;
        }
        const date = value instanceof Date ? value : new Date(value);
        if (Number.isNaN(date.getTime())) {
            return null;
        }
        return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
    }

    private daysBetween(from: Date, to: Date): number {
        return Math.round((to.getTime() - from.getTime()) / MILLISECONDS_PER_DAY);
    }
}

import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { DashboardWidgetFrameComponent } from '../../../../components/shared/dashboard-widget-frame/dashboard-widget-frame';
import { NoticeBannerComponent } from '../../../../components/shared/notice-banner/notice-banner';
import { normalizeStartOfUtcDay, parseDateValue } from '../../../../shared/lib/local-date.utils';
import { MS_PER_DAY } from '../../../../shared/lib/time.constants';
import {
    CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
    CYCLE_FACTOR_TYPE_LACTATION,
    CYCLE_FACTOR_TYPE_NO_PERIOD,
    CYCLE_FACTOR_TYPE_NON_HORMONAL_CONTRACEPTION,
    CYCLE_FACTOR_TYPE_PERIMENOPAUSE,
    CYCLE_FACTOR_TYPE_POSTPARTUM,
    CYCLE_FACTOR_TYPE_PREGNANCY,
    CYCLE_TRACKING_MODE_NO_PERIOD,
    CYCLE_TRACKING_MODE_PERIMENOPAUSE,
    CYCLE_TRACKING_MODE_PERIOD_TRACKING,
    CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION,
    CYCLE_TRACKING_MODE_PREGNANCY,
    CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE,
    type CycleFactor,
    type CycleFactorType,
    type CyclePredictions,
    type CycleResponse,
    type CycleTrackingMode,
} from '../../../cycle-tracking/models/cycle.data';

type CycleSummaryPill = {
    id: string;
    labelKey: string;
    params?: Record<string, number | string>;
};

@Component({
    selector: 'fd-cycle-summary-card',
    imports: [CommonModule, TranslatePipe, NoticeBannerComponent, DashboardWidgetFrameComponent],
    templateUrl: './cycle-summary-card.html',
    styleUrl: './cycle-summary-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleSummaryCardComponent {
    public readonly cycle = input.required<CycleResponse | null>();
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

    protected readonly modeKey = computed(() => {
        const cycle = this.cycle();
        return cycle === null ? null : this.getModeLabelKey(cycle.mode);
    });
    protected readonly confidence = computed(() => this.predictions()?.confidence ?? null);
    protected readonly activeFactorPills = computed<CycleSummaryPill[]>(() => {
        const cycle = this.cycle();
        if (cycle === null) {
            return [];
        }

        return cycle.factors
            .filter(factor => factor.endDate === null || factor.endDate === undefined)
            .sort((a, b) => b.startDate.localeCompare(a.startDate))
            .slice(0, 2)
            .map(factor => this.buildFactorPill(factor));
    });
    protected readonly metaPills = computed<CycleSummaryPill[]>(() => {
        const pills: CycleSummaryPill[] = [];
        const modeKey = this.modeKey();
        if (modeKey !== null) {
            pills.push({ id: 'mode', labelKey: modeKey });
        }

        const confidence = this.confidence();
        if (confidence !== null) {
            pills.push({ id: 'confidence', labelKey: 'CYCLE_CARD.CONFIDENCE', params: { value: confidence } });
        }

        return [...pills, ...this.activeFactorPills()];
    });
    protected readonly hasMeta = computed(() => this.metaPills().length > 0);
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

    private buildFactorPill(factor: CycleFactor): CycleSummaryPill {
        return {
            id: `factor-${factor.id}`,
            labelKey: this.getFactorLabelKey(factor.type),
        };
    }

    private getModeLabelKey(mode: CycleTrackingMode): string {
        switch (mode) {
            case CYCLE_TRACKING_MODE_PERIOD_TRACKING: {
                return 'CYCLE_TRACKING.MODE_PERIOD_TRACKING';
            }
            case CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE: {
                return 'CYCLE_TRACKING.MODE_TRYING_TO_CONCEIVE';
            }
            case CYCLE_TRACKING_MODE_PREGNANCY: {
                return 'CYCLE_TRACKING.MODE_PREGNANCY';
            }
            case CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION: {
                return 'CYCLE_TRACKING.MODE_POSTPARTUM_LACTATION';
            }
            case CYCLE_TRACKING_MODE_PERIMENOPAUSE: {
                return 'CYCLE_TRACKING.MODE_PERIMENOPAUSE';
            }
            case CYCLE_TRACKING_MODE_NO_PERIOD: {
                return 'CYCLE_TRACKING.MODE_NO_PERIOD';
            }
        }
    }

    private getFactorLabelKey(type: CycleFactorType): string {
        switch (type) {
            case CYCLE_FACTOR_TYPE_PREGNANCY: {
                return 'CYCLE_TRACKING.FACTOR_PREGNANCY';
            }
            case CYCLE_FACTOR_TYPE_LACTATION: {
                return 'CYCLE_TRACKING.FACTOR_LACTATION';
            }
            case CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION: {
                return 'CYCLE_TRACKING.FACTOR_HORMONAL_CONTRACEPTION';
            }
            case CYCLE_FACTOR_TYPE_NON_HORMONAL_CONTRACEPTION: {
                return 'CYCLE_TRACKING.FACTOR_NON_HORMONAL_CONTRACEPTION';
            }
            case CYCLE_FACTOR_TYPE_POSTPARTUM: {
                return 'CYCLE_TRACKING.FACTOR_POSTPARTUM';
            }
            case CYCLE_FACTOR_TYPE_PERIMENOPAUSE: {
                return 'CYCLE_TRACKING.FACTOR_PERIMENOPAUSE';
            }
            case CYCLE_FACTOR_TYPE_NO_PERIOD: {
                return 'CYCLE_TRACKING.FACTOR_NO_PERIOD';
            }
        }
    }
}

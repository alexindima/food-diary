import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiLineChartComponent, type FdUiLineChartPoint } from 'fd-ui-kit';

import { DashboardWidgetFrameComponent } from '../../../../components/shared/dashboard-widget-frame/dashboard-widget-frame';
import { compareDatesAsc } from '../../../../shared/lib/local-date.utils';
import {
    WEIGHT_TREND_DISPLAY_FRACTION_DIGITS,
    WEIGHT_TREND_EPSILON,
    WEIGHT_TREND_FILL_COLOR_PERCENT,
    WEIGHT_TREND_ISO_DATE_LENGTH,
    WEIGHT_TREND_ROUNDING_FACTOR,
} from './weight-trend-card.config';

export type WeightTrendPoint = {
    date: string | Date;
    value: number | null;
};

@Component({
    selector: 'fd-weight-trend-card',
    imports: [CommonModule, TranslatePipe, FdUiLineChartComponent, DashboardWidgetFrameComponent],
    templateUrl: './weight-trend-card.html',
    styleUrl: './weight-trend-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightTrendCardComponent {
    public readonly title = input<string>('WEIGHT_CARD.TITLE');
    public readonly currentWeight = input.required<number | null>();
    public readonly change = input.required<number | null>();
    public readonly timeframeLabel = input.required<string>();
    public readonly points = input.required<WeightTrendPoint[]>();
    public readonly isLoading = input.required<boolean>();
    public readonly unitKey = input<string>('WEIGHT_CARD.KG');
    public readonly iconName = input<string | null>('monitor_weight');
    public readonly accentColor = input<string>('var(--fd-color-blue-500)');

    protected readonly chartPoints = computed<readonly FdUiLineChartPoint[]>(() =>
        [...this.points()]
            .sort((a, b) => compareDatesAsc(a.date, b.date))
            .map(point => ({
                label: this.formatPointLabel(point.date),
                value: point.value ?? null,
            })),
    );

    protected readonly hasChartData = computed(() =>
        this.chartPoints().some(point => typeof point.value === 'number' && Number.isFinite(point.value)),
    );

    protected readonly chartFillColor = computed(
        () => `color-mix(in srgb, ${this.accentColor()} ${WEIGHT_TREND_FILL_COLOR_PERCENT}%, transparent)`,
    );

    protected readonly changeTone = computed<'positive' | 'negative' | 'neutral'>(() => {
        const value = this.change();
        if (value === null) {
            return 'neutral';
        }
        if (value < -WEIGHT_TREND_EPSILON) {
            return 'positive';
        }
        if (value > WEIGHT_TREND_EPSILON) {
            return 'negative';
        }
        return 'neutral';
    });

    protected readonly formattedChangeValue = computed(() => {
        const delta = this.change();
        if (delta === null) {
            return null;
        }
        const rounded = Math.round(delta * WEIGHT_TREND_ROUNDING_FACTOR) / WEIGHT_TREND_ROUNDING_FACTOR;
        const sign = rounded > 0 ? '+' : '';
        return `${sign}${rounded.toFixed(WEIGHT_TREND_DISPLAY_FRACTION_DIGITS)}`;
    });

    protected readonly hasValue = computed(() => {
        const value = this.currentWeight();
        return value !== null;
    });

    private formatPointLabel(date: string | Date): string {
        return date instanceof Date ? date.toISOString().slice(0, WEIGHT_TREND_ISO_DATE_LENGTH) : date;
    }
}

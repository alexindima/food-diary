import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiLineChartComponent, type FdUiLineChartPoint, type FdUiLineChartReferenceLine } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { DashboardWidgetFrameComponent } from '../../../../components/shared/dashboard-widget-frame/dashboard-widget-frame';
import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { compareDatesAsc, formatDateValue, parseDateValue } from '../../../../shared/lib/local-date.utils';
import {
    WEIGHT_TREND_CHART_MINIMUM_PADDING,
    WEIGHT_TREND_CHART_RANGE_PADDING_RATIO,
    WEIGHT_TREND_DISPLAY_FRACTION_DIGITS,
    WEIGHT_TREND_EPSILON,
    WEIGHT_TREND_FILL_COLOR_PERCENT,
    WEIGHT_TREND_ISO_DATE_LENGTH,
    WEIGHT_TREND_MINIMUM_CHART_POINTS,
    WEIGHT_TREND_ROUNDING_FACTOR,
} from './weight-trend-card.config';

export type WeightTrendPoint = {
    date: string | Date;
    value: number | null;
};

@Component({
    selector: 'fd-weight-trend-card',
    imports: [CommonModule, RouterLink, TranslatePipe, FdUiButtonComponent, FdUiLineChartComponent, DashboardWidgetFrameComponent],
    templateUrl: './weight-trend-card.html',
    styleUrl: './weight-trend-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightTrendCardComponent {
    private readonly translateService = inject(TranslateService);
    public readonly title = input<string>('WEIGHT_CARD.TITLE');
    public readonly currentWeight = input.required<number | null>();
    public readonly change = input.required<number | null>();
    public readonly timeframeLabel = input.required<string>();
    public readonly actionLabelKey = input<string>('WEIGHT_TREND_CARD.ADD_WEIGHT');
    public readonly actionRoute = input<string>('/weight-history');
    public readonly firstMeasurementKey = input<string>('WEIGHT_TREND_CARD.FIRST_MEASUREMENT');
    public readonly twoMeasurementsKey = input<string>('WEIGHT_TREND_CARD.TWO_MEASUREMENTS');
    public readonly stableTrendKey = input<string>('WEIGHT_TREND_CARD.STABLE_TREND');
    public readonly points = input.required<WeightTrendPoint[]>();
    public readonly isLoading = input.required<boolean>();
    public readonly unitKey = input<string>('WEIGHT_CARD.KG');
    public readonly emptyStateKey = input<string>('WEIGHT_TREND_CARD.NO_DATA');
    public readonly iconName = input<string | null>('monitor_weight');
    public readonly accentColor = input<string>('var(--fd-color-blue-500)');
    public readonly targetValue = input<number | null>(null);

    protected readonly measurementPoints = computed<ReadonlyArray<FdUiLineChartPoint & { value: number }>>(() => {
        const points = [...this.points()]
            .sort((a, b) => compareDatesAsc(a.date, b.date))
            .filter(
                (point): point is WeightTrendPoint & { value: number } => typeof point.value === 'number' && Number.isFinite(point.value),
            );
        const timestamps = points.map(point => this.toTimestamp(point.date));
        const firstTimestamp = timestamps[0] ?? 0;
        const lastTimestamp = timestamps.at(-1) ?? firstTimestamp;
        const range = lastTimestamp - firstTimestamp;

        return points.map((point, index) => ({
            label: this.formatPointLabel(point.date),
            value: point.value,
            xPosition: range > 0 ? ((timestamps[index] ?? firstTimestamp) - firstTimestamp) / range : undefined,
        }));
    });
    protected readonly measurementCount = computed(() => this.measurementPoints().length);
    protected readonly hasMeasurements = computed(() => this.measurementCount() > 0);
    protected readonly hasEnoughMeasurements = computed(() => this.measurementCount() >= WEIGHT_TREND_MINIMUM_CHART_POINTS);
    protected readonly hasChartVariance = computed(() => {
        const values = this.measurementPoints().map(point => point.value);
        return values.length > 0 && Math.max(...values) - Math.min(...values) > WEIGHT_TREND_EPSILON;
    });
    protected readonly showChart = computed(() => this.hasEnoughMeasurements() && this.hasChartVariance());
    protected readonly chartBounds = computed(() => {
        const values = this.measurementPoints().map(point => point.value);
        const minimum = Math.min(...values);
        const maximum = Math.max(...values);
        const padding = Math.max((maximum - minimum) * WEIGHT_TREND_CHART_RANGE_PADDING_RATIO, WEIGHT_TREND_CHART_MINIMUM_PADDING);
        return { minimum: minimum - padding, maximum: maximum + padding };
    });

    protected readonly chartFillColor = computed(
        () => `color-mix(in srgb, ${this.accentColor()} ${WEIGHT_TREND_FILL_COLOR_PERCENT}%, transparent)`,
    );
    protected readonly referenceLines = computed<readonly FdUiLineChartReferenceLine[]>(() => {
        const targetValue = this.targetValue();
        return targetValue === null ? [] : [{ value: targetValue, color: this.accentColor() }];
    });

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
    protected readonly hasMeaningfulChange = computed(() => Math.abs(this.change() ?? 0) > WEIGHT_TREND_EPSILON);

    protected readonly hasValue = computed(() => this.hasMeasurements() && this.currentWeight() !== null);

    private formatPointLabel(date: string | Date): string {
        return (
            formatDateValue(date, resolveTranslateLanguage(this.translateService), {
                day: 'numeric',
                month: 'long',
                timeZone: 'UTC',
            }) ?? (date instanceof Date ? date.toISOString().slice(0, WEIGHT_TREND_ISO_DATE_LENGTH) : date)
        );
    }

    private toTimestamp(date: string | Date): number {
        return parseDateValue(date)?.getTime() ?? 0;
    }
}

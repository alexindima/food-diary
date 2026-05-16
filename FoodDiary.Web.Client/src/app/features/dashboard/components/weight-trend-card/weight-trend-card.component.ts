import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import type { ChartConfiguration, ScaleOptionsByType } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';

import { DashboardWidgetFrameComponent } from '../../../../components/shared/dashboard-widget-frame/dashboard-widget-frame.component';
import {
    WEIGHT_TREND_ACTIVE_POINT_HOVER_RADIUS,
    WEIGHT_TREND_ACTIVE_POINT_RADIUS,
    WEIGHT_TREND_CHART_BORDER_WIDTH,
    WEIGHT_TREND_CHART_TENSION,
    WEIGHT_TREND_DISPLAY_FRACTION_DIGITS,
    WEIGHT_TREND_EPSILON,
    WEIGHT_TREND_FILL_COLOR_PERCENT,
    WEIGHT_TREND_INACTIVE_POINT_RADIUS,
    WEIGHT_TREND_POINT_BORDER_WIDTH,
    WEIGHT_TREND_POINT_HIT_RADIUS,
    WEIGHT_TREND_ROUNDING_FACTOR,
    WEIGHT_TREND_TOOLTIP_PADDING,
    WEIGHT_TREND_Y_AXIS_PADDING_MIN,
    WEIGHT_TREND_Y_AXIS_PADDING_RATIO,
} from './weight-trend-card.config';

export type WeightTrendPoint = {
    date: string | Date;
    value: number | null;
};

@Component({
    selector: 'fd-weight-trend-card',
    imports: [CommonModule, BaseChartDirective, TranslatePipe, DashboardWidgetFrameComponent],
    templateUrl: './weight-trend-card.component.html',
    styleUrl: './weight-trend-card.component.scss',
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

    private readonly chartValues = computed(() => this.getOrderedValues());
    private readonly baseYAxisOptions = {
        display: false,
        grid: { display: false },
    } satisfies Partial<ScaleOptionsByType<'linear'>>;

    public readonly chartData = computed<ChartConfiguration<'line'>['data'] | null>(() => {
        const values = this.chartValues();
        const hasValue = values.some(value => value !== null && !Number.isNaN(value));
        if (!hasValue) {
            return null;
        }

        const labels = values.map(() => '');
        const lastIndexFromEnd = [...values].reverse().findIndex(value => value !== null && !Number.isNaN(value));
        const lastIndex = lastIndexFromEnd === -1 ? -1 : values.length - 1 - lastIndexFromEnd;

        return {
            labels,
            datasets: [
                {
                    data: values,
                    borderColor: this.accentColor(),
                    backgroundColor: this.getFillColor(),
                    borderWidth: WEIGHT_TREND_CHART_BORDER_WIDTH,
                    tension: WEIGHT_TREND_CHART_TENSION,
                    fill: true,
                    spanGaps: true,
                    pointRadius: values.map((_, index) =>
                        index === lastIndex ? WEIGHT_TREND_ACTIVE_POINT_RADIUS : WEIGHT_TREND_INACTIVE_POINT_RADIUS,
                    ),
                    pointHoverRadius: values.map((_, index) =>
                        index === lastIndex ? WEIGHT_TREND_ACTIVE_POINT_HOVER_RADIUS : WEIGHT_TREND_INACTIVE_POINT_RADIUS,
                    ),
                    pointBackgroundColor: this.accentColor(),
                    pointBorderColor: 'var(--fd-color-white)',
                    pointBorderWidth: WEIGHT_TREND_POINT_BORDER_WIDTH,
                },
            ],
        };
    });

    private readonly baseChartOptions: ChartConfiguration<'line'>['options'] = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: { display: false },
            tooltip: {
                displayColors: false,
                callbacks: {
                    label: context => {
                        const value = context.raw;
                        if (typeof value !== 'number') {
                            return '';
                        }
                        return `${value.toFixed(WEIGHT_TREND_DISPLAY_FRACTION_DIGITS)} kg`;
                    },
                },
                padding: WEIGHT_TREND_TOOLTIP_PADDING,
                backgroundColor: 'var(--fd-color-slate-900)',
                titleColor: 'var(--fd-color-slate-200)',
                bodyColor: 'var(--fd-color-slate-200)',
            },
        },
        scales: {
            x: {
                display: false,
                grid: { display: false },
                ticks: { display: false },
            },
            y: {
                ...this.baseYAxisOptions,
            },
        },
        elements: {
            line: { borderCapStyle: 'round' },
            point: { hitRadius: WEIGHT_TREND_POINT_HIT_RADIUS },
        },
    };

    public readonly changeTone = computed<'positive' | 'negative' | 'neutral'>(() => {
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

    public readonly formattedChangeValue = computed(() => {
        const delta = this.change();
        if (delta === null) {
            return null;
        }
        const rounded = Math.round(delta * WEIGHT_TREND_ROUNDING_FACTOR) / WEIGHT_TREND_ROUNDING_FACTOR;
        const sign = rounded > 0 ? '+' : '';
        return `${sign}${rounded.toFixed(WEIGHT_TREND_DISPLAY_FRACTION_DIGITS)}`;
    });

    public readonly hasValue = computed(() => {
        const value = this.currentWeight();
        return value !== null;
    });

    public readonly dynamicChartOptions = computed<ChartConfiguration<'line'>['options']>(() => {
        const values = this.chartValues();
        const numeric = values.filter(v => typeof v === 'number');
        const minVal = numeric.length > 0 ? Math.min(...numeric) : 0;
        const maxVal = numeric.length > 0 ? Math.max(...numeric) : 1;
        const padding = Math.max(WEIGHT_TREND_Y_AXIS_PADDING_MIN, (maxVal - minVal) * WEIGHT_TREND_Y_AXIS_PADDING_RATIO);
        const suggestedMin = Math.max(0, minVal - padding);
        const baseOptions = this.baseChartOptions;
        const baseScales = baseOptions?.scales;

        return {
            ...baseOptions,
            scales: {
                ...baseScales,
                y: {
                    ...this.baseYAxisOptions,
                    min: suggestedMin,
                },
            },
        };
    });

    private getOrderedValues(): Array<number | null> {
        return [...this.points()].sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime()).map(point => point.value ?? null);
    }

    private getFillColor(): string {
        return `color-mix(in srgb, ${this.accentColor()} ${WEIGHT_TREND_FILL_COLOR_PERCENT}%, transparent)`;
    }
}

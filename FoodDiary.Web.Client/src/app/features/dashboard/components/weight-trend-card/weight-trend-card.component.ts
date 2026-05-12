import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import type { ChartConfiguration, ScaleOptionsByType } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';

import { DashboardWidgetFrameComponent } from '../../../../components/shared/dashboard-widget-frame/dashboard-widget-frame.component';

const CHART_BORDER_WIDTH = 4;
const CHART_TENSION = 0.48;
const ACTIVE_POINT_RADIUS = 5;
const INACTIVE_POINT_RADIUS = 0;
const ACTIVE_POINT_HOVER_RADIUS = 6;
const POINT_BORDER_WIDTH = 2;
const TOOLTIP_PADDING = 8;
const POINT_HIT_RADIUS = 16;
const TREND_EPSILON = 0.01;
const DISPLAY_FRACTION_DIGITS = 1;
const ROUNDING_FACTOR = 10;
const FILL_COLOR_PERCENT = 15;
const Y_AXIS_PADDING_MIN = 0.5;
const Y_AXIS_PADDING_RATIO = 0.08;

export type WeightTrendPoint = {
    date: string | Date;
    value: number | null;
};

@Component({
    selector: 'fd-weight-trend-card',
    standalone: true,
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
                    borderWidth: CHART_BORDER_WIDTH,
                    tension: CHART_TENSION,
                    fill: true,
                    spanGaps: true,
                    pointRadius: values.map((_, index) => (index === lastIndex ? ACTIVE_POINT_RADIUS : INACTIVE_POINT_RADIUS)),
                    pointHoverRadius: values.map((_, index) => (index === lastIndex ? ACTIVE_POINT_HOVER_RADIUS : INACTIVE_POINT_RADIUS)),
                    pointBackgroundColor: this.accentColor(),
                    pointBorderColor: 'var(--fd-color-white)',
                    pointBorderWidth: POINT_BORDER_WIDTH,
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
                        return `${value.toFixed(DISPLAY_FRACTION_DIGITS)} kg`;
                    },
                },
                padding: TOOLTIP_PADDING,
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
            point: { hitRadius: POINT_HIT_RADIUS },
        },
    };

    public readonly changeTone = computed<'positive' | 'negative' | 'neutral'>(() => {
        const value = this.change();
        if (value === null) {
            return 'neutral';
        }
        if (value < -TREND_EPSILON) {
            return 'positive';
        }
        if (value > TREND_EPSILON) {
            return 'negative';
        }
        return 'neutral';
    });

    public readonly formattedChangeValue = computed(() => {
        const delta = this.change();
        if (delta === null) {
            return null;
        }
        const rounded = Math.round(delta * ROUNDING_FACTOR) / ROUNDING_FACTOR;
        const sign = rounded > 0 ? '+' : '';
        return `${sign}${rounded.toFixed(DISPLAY_FRACTION_DIGITS)}`;
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
        const padding = Math.max(Y_AXIS_PADDING_MIN, (maxVal - minVal) * Y_AXIS_PADDING_RATIO);
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
        return `color-mix(in srgb, ${this.accentColor()} ${FILL_COLOR_PERCENT}%, transparent)`;
    }
}

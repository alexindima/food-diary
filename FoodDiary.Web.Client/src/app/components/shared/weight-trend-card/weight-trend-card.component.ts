import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';

export interface WeightTrendPoint {
    date: string | Date;
    value: number | null;
}

@Component({
    selector: 'fd-weight-trend-card',
    standalone: true,
    imports: [CommonModule, BaseChartDirective, TranslatePipe],
    templateUrl: './weight-trend-card.component.html',
    styleUrl: './weight-trend-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightTrendCardComponent {
    public readonly title = input<string>('WEIGHT_CARD.TITLE');
    public readonly currentWeight = input<number | null>(null);
    public readonly change = input<number | null>(null);
    public readonly timeframeLabel = input<string>('');
    public readonly points = input<WeightTrendPoint[]>([]);
    public readonly isLoading = input<boolean>(false);
    public readonly unitKey = input<string>('WEIGHT_CARD.KG');
    public readonly iconLabel = input<string>('WT');
    public readonly accentColor = input<string>('#1b84ff');
    public readonly iconGradient = input<string>('linear-gradient(135deg, #ff4f85, #ff7e9c)');

    public readonly chartData = computed<ChartConfiguration<'line'>['data'] | null>(() => {
        const ordered = [...this.points()].sort(
            (a, b) => new Date(a.date).getTime() - new Date(b.date).getTime(),
        );
        const values = ordered.map(point => (point.value ?? null));
        const hasValue = values.some(value => value !== null && !isNaN(value as number));
        if (!hasValue) {
            return null;
        }

        const labels = ordered.map(() => '');
        const lastIndexFromEnd = [...values]
            .reverse()
            .findIndex(value => value !== null && !isNaN(value as number));
        const lastIndex = lastIndexFromEnd === -1 ? -1 : values.length - 1 - lastIndexFromEnd;

        return {
            labels,
            datasets: [
                {
                    data: values,
                    borderColor: this.accentColor(),
                    backgroundColor: this.getFillColor(),
                    borderWidth: 4,
                    tension: 0.48,
                    fill: true,
                    spanGaps: true,
                    pointRadius: values.map((_, index) => (index === lastIndex ? 5 : 0)),
                    pointHoverRadius: values.map((_, index) => (index === lastIndex ? 6 : 0)),
                    pointBackgroundColor: this.accentColor(),
                    pointBorderColor: '#ffffff',
                    pointBorderWidth: 2,
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
                        const value = context.raw as number | null;
                        if (value === null || value === undefined) {
                            return '';
                        }
                        return `${value.toFixed(1)} kg`;
                    },
                },
                padding: 8,
                backgroundColor: '#0f172a',
                titleColor: '#e2e8f0',
                bodyColor: '#e2e8f0',
            },
        },
        scales: {
            x: {
                display: false,
                grid: { display: false },
                ticks: { display: false },
            },
            y: {
                display: false,
                grid: { display: false },
            },
        },
        elements: {
            line: { borderCapStyle: 'round' },
            point: { hitRadius: 16 },
        },
    };

    public readonly changeTone = computed<'positive' | 'negative' | 'neutral'>(() => {
        const value = this.change();
        if (value === null || value === undefined) {
            return 'neutral';
        }
        if (value < -0.01) {
            return 'positive';
        }
        if (value > 0.01) {
            return 'negative';
        }
        return 'neutral';
    });

    public readonly formattedChangeValue = computed(() => {
        const delta = this.change();
        if (delta === null || delta === undefined) {
            return null;
        }
        const rounded = Math.round(delta * 10) / 10;
        const sign = rounded > 0 ? '+' : '';
        return `${sign}${rounded.toFixed(1)}`;
    });

    public readonly hasValue = computed(() => {
        const value = this.currentWeight();
        return value !== null && value !== undefined;
    });

    public readonly dynamicChartOptions = computed<ChartConfiguration<'line'>['options']>(() => {
        const values = (this.chartData()?.datasets[0].data as (number | null)[] | undefined) ?? [];
        const numeric = values.filter(v => typeof v === 'number') as number[];
        const minVal = numeric.length ? Math.min(...numeric) : 0;
        const maxVal = numeric.length ? Math.max(...numeric) : 1;
        const padding = Math.max(0.5, (maxVal - minVal) * 0.08);
        const suggestedMin = Math.max(0, minVal - padding);
        const baseOptions = this.baseChartOptions as ChartConfiguration<'line'>['options'];
        const baseScales = ((baseOptions as any)?.scales ?? {}) as Record<string, unknown>;
        const baseY = (baseScales as any)?.y as Record<string, unknown> | undefined;

        return {
            ...baseOptions,
            scales: {
                ...baseScales,
                y: {
                    ...(baseY ?? {}),
                    min: suggestedMin,
                },
            },
        };
    });

    private getFillColor(): string {
        const color = this.accentColor();
        const rgba = this.hexToRgba(color, 0.15);
        return rgba ?? 'rgba(27, 132, 255, 0.14)';
    }

    private hexToRgba(hex: string, alpha: number): string | null {
        const match = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
        if (!match) {
            return null;
        }
        const r = parseInt(match[1], 16);
        const g = parseInt(match[2], 16);
        const b = parseInt(match[3], 16);
        return `rgba(${r}, ${g}, ${b}, ${alpha})`;
    }
}

import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type FdUiLineChartPoint = {
    label: string;
    value: number | null;
};

type FdUiLineChartPointViewModel = {
    label: string;
    value: number;
    x: number;
    y: number;
    tooltip: string;
};

type FdUiLineChartGridLine = {
    y: number;
    label: string;
};

const LINE_CHART_VIEWBOX_WIDTH = 100;
const LINE_CHART_VIEWBOX_HEIGHT = 64;
const LINE_CHART_PADDING = 6;
const LINE_CHART_GRID_LINE_COUNT = 3;
const DEFAULT_AXIS_DECIMAL_PLACES = 1;
const FLAT_RANGE_PADDING_RATIO = 0.05;
const DEFAULT_LINE_COLOR = 'var(--fd-color-primary-500)';
const DEFAULT_FILL_COLOR = 'color-mix(in srgb, var(--fd-color-primary-500) 14%, transparent)';

@Component({
    selector: 'fd-ui-line-chart',
    imports: [],
    templateUrl: './fd-ui-line-chart.component.html',
    styleUrl: './fd-ui-line-chart.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiLineChartComponent {
    public readonly title = input<string>();
    public readonly points = input<readonly FdUiLineChartPoint[]>([]);
    public readonly emptyLabel = input('No data');
    public readonly lineColor = input(DEFAULT_LINE_COLOR);
    public readonly fillColor = input(DEFAULT_FILL_COLOR);
    public readonly showArea = input(false);
    public readonly showPoints = input(true);
    public readonly minValue = input<number | null>(null);
    public readonly maxValue = input<number | null>(null);
    public readonly defaultMaxValue = input<number | null>(null);
    public readonly showAxisLabels = input(false);
    public readonly showGrid = input(false);
    public readonly valueSuffix = input('');
    public readonly axisDecimalPlaces = input(DEFAULT_AXIS_DECIMAL_PLACES);

    protected readonly viewBox = `0 0 ${LINE_CHART_VIEWBOX_WIDTH} ${LINE_CHART_VIEWBOX_HEIGHT}`;
    protected readonly chartBottom = LINE_CHART_VIEWBOX_HEIGHT - LINE_CHART_PADDING;

    private readonly numericValues = computed(() =>
        this.points()
            .map(point => point.value)
            .filter((value): value is number => typeof value === 'number' && Number.isFinite(value)),
    );
    private readonly resolvedMinValue = computed(() => {
        const minValue = this.minValue();
        if (minValue !== null) {
            return minValue;
        }

        const numeric = this.numericValues();
        const actualMinValue = Math.min(...numeric);
        const actualMaxValue = Math.max(...numeric);
        if (numeric.length > 0 && actualMinValue === actualMaxValue && actualMinValue !== 0) {
            return Math.max(0, actualMinValue - this.getFlatRangePadding(actualMinValue));
        }

        return actualMinValue;
    });
    private readonly resolvedMaxValue = computed(() => {
        const minValue = this.resolvedMinValue();
        const numeric = this.numericValues();
        const maxValue = this.maxValue() ?? Math.max(...this.numericValues());
        if (this.shouldPadFlatRange(numeric, maxValue)) {
            return maxValue + this.getFlatRangePadding(maxValue);
        }

        if (maxValue > minValue) {
            return maxValue;
        }

        const defaultMaxValue = this.defaultMaxValue();
        if (defaultMaxValue !== null && defaultMaxValue > minValue) {
            return defaultMaxValue;
        }

        if (minValue !== 0) {
            return minValue + this.getFlatRangePadding(minValue);
        }

        return minValue + 1;
    });
    protected readonly axisStartLabel = computed(() => this.points().find(point => point.label.trim().length > 0)?.label ?? '');
    protected readonly axisEndLabel = computed(
        () => [...this.points()].reverse().find(point => point.label.trim().length > 0)?.label ?? '',
    );
    protected readonly axisMinLabel = computed(() => this.formatAxisValue(this.resolvedMinValue()));
    protected readonly axisMaxLabel = computed(() => this.formatAxisValue(this.resolvedMaxValue()));
    protected readonly gridLines = computed<readonly FdUiLineChartGridLine[]>(() => {
        const minValue = this.resolvedMinValue();
        const maxValue = this.resolvedMaxValue();
        const availableHeight = LINE_CHART_VIEWBOX_HEIGHT - LINE_CHART_PADDING * 2;
        const step = availableHeight / (LINE_CHART_GRID_LINE_COUNT - 1);
        const valueStep = (maxValue - minValue) / (LINE_CHART_GRID_LINE_COUNT - 1);

        return Array.from({ length: LINE_CHART_GRID_LINE_COUNT }, (_, index) => {
            const reversedIndex = LINE_CHART_GRID_LINE_COUNT - 1 - index;

            return {
                y: LINE_CHART_PADDING + step * index,
                label: this.formatAxisValue(minValue + valueStep * reversedIndex),
            };
        });
    });

    public readonly pointViews = computed<readonly FdUiLineChartPointViewModel[]>(() => {
        const points = this.points();
        const numeric = this.numericValues();

        if (points.length === 0 || numeric.length === 0) {
            return [];
        }

        const minValue = this.resolvedMinValue();
        const maxValue = this.resolvedMaxValue();
        const valueRange = maxValue - minValue;
        const effectiveRange = valueRange > 0 ? valueRange : 1;
        const xStep = points.length > 1 ? (LINE_CHART_VIEWBOX_WIDTH - LINE_CHART_PADDING * 2) / (points.length - 1) : 0;
        const availableHeight = LINE_CHART_VIEWBOX_HEIGHT - LINE_CHART_PADDING * 2;

        return points.flatMap((point, index) => {
            const value = point.value;
            if (typeof value !== 'number' || !Number.isFinite(value)) {
                return [];
            }

            const x = points.length > 1 ? LINE_CHART_PADDING + index * xStep : LINE_CHART_VIEWBOX_WIDTH / 2;
            const y = this.chartBottom - ((value - minValue) / effectiveRange) * availableHeight;
            const label = point.label.trim().length > 0 ? point.label : this.emptyLabel();

            return [
                {
                    label,
                    value,
                    x,
                    y,
                    tooltip: `${label}: ${value}`,
                },
            ];
        });
    });

    public readonly linePath = computed(() => {
        const points = this.pointViews();
        return points.map((point, index) => `${index === 0 ? 'M' : 'L'} ${point.x} ${point.y}`).join(' ');
    });

    public readonly areaPath = computed(() => {
        const points = this.pointViews();
        if (points.length === 0) {
            return '';
        }

        const first = points[0];
        const last = points[points.length - 1];
        return `${this.linePath()} L ${last.x} ${this.chartBottom} L ${first.x} ${this.chartBottom} Z`;
    });

    public readonly ariaLabel = computed(() => {
        const title = this.title();
        const hasTitle = title !== undefined && title.trim().length > 0;
        if (this.pointViews().length === 0) {
            return hasTitle ? `${title}: ${this.emptyLabel()}` : this.emptyLabel();
        }

        const details = this.pointViews()
            .map(point => `${point.label} ${point.value}`)
            .join(', ');
        return hasTitle ? `${title}: ${details}` : details;
    });

    private formatAxisValue(value: number): string {
        if (!Number.isFinite(value)) {
            return '';
        }

        const decimals = Math.max(0, this.axisDecimalPlaces());
        const formatted = Number.isInteger(value) ? String(value) : value.toFixed(decimals).replace(/\.?0+$/, '');
        const suffix = this.valueSuffix().trim();

        return suffix.length > 0 ? `${formatted} ${suffix}` : formatted;
    }

    private getFlatRangePadding(value: number): number {
        return Math.max(1, Math.abs(value) * FLAT_RANGE_PADDING_RATIO);
    }

    private shouldPadFlatRange(values: readonly number[], maxValue: number): boolean {
        return (
            this.minValue() === null && this.maxValue() === null && values.length > 0 && Math.min(...values) === maxValue && maxValue !== 0
        );
    }
}

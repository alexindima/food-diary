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
    xPercent: string;
    yPercent: string;
    tooltip: string;
};

type FdUiLineChartGridLine = {
    y: number;
    yPercent: string;
    label: string;
};

type FdUiLineChartVerticalGridLine = {
    x: number;
};

type FdUiLineChartXAxisLabel = {
    label: string;
    xPercent: string;
    position: 'start' | 'middle' | 'end';
};

const LINE_CHART_VIEWBOX_WIDTH = 100;
const LINE_CHART_VIEWBOX_HEIGHT = 64;
const LINE_CHART_PADDING = 6;
const LINE_CHART_HORIZONTAL_PADDING = 2;
const LINE_CHART_LEFT_X = LINE_CHART_HORIZONTAL_PADDING;
const LINE_CHART_RIGHT_X = LINE_CHART_VIEWBOX_WIDTH - LINE_CHART_HORIZONTAL_PADDING;
const LINE_CHART_GRID_LINE_COUNT = 5;
const LINE_CHART_X_AXIS_LABEL_COUNT = 14;
const LINE_CHART_PERCENTAGE_SCALE = 100;
const LINE_CHART_CURVE_CONTROL_DIVISOR = 6;
const NICE_STEP_ONE = 1;
const NICE_STEP_ONE_AND_QUARTER = 1.25;
const NICE_STEP_ONE_AND_HALF = 1.5;
const NICE_STEP_TWO = 2;
const NICE_STEP_TWO_AND_HALF = 2.5;
const NICE_STEP_FIVE = 5;
const NICE_STEP_TEN = 10;
const LINE_CHART_NICE_STEP_MULTIPLIERS = [
    NICE_STEP_ONE,
    NICE_STEP_ONE_AND_QUARTER,
    NICE_STEP_ONE_AND_HALF,
    NICE_STEP_TWO,
    NICE_STEP_TWO_AND_HALF,
    NICE_STEP_FIVE,
    NICE_STEP_TEN,
] as const;
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
    protected readonly chartLeft = LINE_CHART_LEFT_X;
    protected readonly chartRight = LINE_CHART_RIGHT_X;
    protected readonly chartTop = LINE_CHART_PADDING;
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
        const explicitMaxValue = this.maxValue();
        if (explicitMaxValue !== null && explicitMaxValue > minValue) {
            return explicitMaxValue;
        }

        const numeric = this.numericValues();
        const actualMaxValue = Math.max(...numeric);
        const paddedMaxValue = this.shouldPadFlatRange(numeric, actualMaxValue)
            ? actualMaxValue + this.getFlatRangePadding(actualMaxValue)
            : actualMaxValue;
        const defaultMaxValue = this.defaultMaxValue();
        const maxValue = defaultMaxValue !== null ? Math.max(paddedMaxValue, defaultMaxValue) : paddedMaxValue;

        if (maxValue > minValue) {
            return this.getNiceMaxValue(minValue, maxValue);
        }

        if (minValue !== 0) {
            return this.getNiceMaxValue(minValue, minValue + this.getFlatRangePadding(minValue));
        }

        return this.getNiceMaxValue(minValue, minValue + 1);
    });
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
                yPercent: `${((LINE_CHART_PADDING + step * index) / LINE_CHART_VIEWBOX_HEIGHT) * LINE_CHART_PERCENTAGE_SCALE}%`,
                label: this.formatAxisValue(minValue + valueStep * reversedIndex),
            };
        });
    });
    protected readonly xAxisLabels = computed<readonly FdUiLineChartXAxisLabel[]>(() => {
        const points = this.points();
        if (points.length === 0) {
            return [];
        }

        if (points.length === 1) {
            const label = points[0]?.label.trim() ?? '';
            return label.length > 0 ? [{ label, xPercent: '50%', position: 'middle' }] : [];
        }

        const labelCount = Math.min(LINE_CHART_X_AXIS_LABEL_COUNT, points.length);
        const xStep = (LINE_CHART_RIGHT_X - LINE_CHART_LEFT_X) / (points.length - 1);
        const indexes = Array.from({ length: labelCount }, (_, index) => Math.round((index * (points.length - 1)) / (labelCount - 1)));
        const uniqueIndexes = [...new Set(indexes)];

        return uniqueIndexes.flatMap(index => {
            const label = points[index]?.label.trim() ?? '';
            if (label.length === 0) {
                return [];
            }

            const x = LINE_CHART_LEFT_X + index * xStep;
            const position = index === 0 ? 'start' : index === points.length - 1 ? 'end' : 'middle';

            return [
                {
                    label,
                    xPercent: `${x}%`,
                    position,
                },
            ];
        });
    });
    protected readonly verticalGridLines = computed<readonly FdUiLineChartVerticalGridLine[]>(() => {
        const points = this.points();
        if (points.length === 0) {
            return [];
        }

        if (points.length === 1) {
            return [{ x: LINE_CHART_VIEWBOX_WIDTH / 2 }];
        }

        const xStep = (LINE_CHART_RIGHT_X - LINE_CHART_LEFT_X) / (points.length - 1);

        return Array.from({ length: points.length }, (_, index) => ({
            x: LINE_CHART_LEFT_X + index * xStep,
        }));
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
        const xStep = points.length > 1 ? (LINE_CHART_RIGHT_X - LINE_CHART_LEFT_X) / (points.length - 1) : 0;
        const availableHeight = LINE_CHART_VIEWBOX_HEIGHT - LINE_CHART_PADDING * 2;

        return points.flatMap((point, index) => {
            const value = point.value;
            if (typeof value !== 'number' || !Number.isFinite(value)) {
                return [];
            }

            const x = points.length > 1 ? LINE_CHART_LEFT_X + index * xStep : LINE_CHART_VIEWBOX_WIDTH / 2;
            const y = this.chartBottom - ((value - minValue) / effectiveRange) * availableHeight;
            const label = point.label.trim().length > 0 ? point.label : this.emptyLabel();

            return [
                {
                    label,
                    value,
                    x,
                    y,
                    xPercent: `${x}%`,
                    yPercent: `${(y / LINE_CHART_VIEWBOX_HEIGHT) * LINE_CHART_PERCENTAGE_SCALE}%`,
                    tooltip: `${label}: ${value}`,
                },
            ];
        });
    });

    public readonly linePath = computed(() => {
        const points = this.pointViews();
        if (points.length === 0) {
            return '';
        }

        if (points.length === 1) {
            const point = points[0];
            return `M ${point.x} ${point.y}`;
        }

        const [firstPoint, ...remainingPoints] = points;
        const segments = remainingPoints.map((point, index) => {
            const currentIndex = index + 1;
            const previousPoint = points[currentIndex - 1];
            const beforePreviousPoint = points[currentIndex - 2] ?? previousPoint;
            const nextPoint = points[currentIndex + 1] ?? point;
            const [minY, maxY] = previousPoint.y < point.y ? [previousPoint.y, point.y] : [point.y, previousPoint.y];
            const controlStartX = this.clamp(
                previousPoint.x + (point.x - beforePreviousPoint.x) / LINE_CHART_CURVE_CONTROL_DIVISOR,
                previousPoint.x,
                point.x,
            );
            const controlStartY = this.clamp(
                previousPoint.y + (point.y - beforePreviousPoint.y) / LINE_CHART_CURVE_CONTROL_DIVISOR,
                minY,
                maxY,
            );
            const controlEndX = this.clamp(
                point.x - (nextPoint.x - previousPoint.x) / LINE_CHART_CURVE_CONTROL_DIVISOR,
                previousPoint.x,
                point.x,
            );
            const controlEndY = this.clamp(point.y - (nextPoint.y - previousPoint.y) / LINE_CHART_CURVE_CONTROL_DIVISOR, minY, maxY);

            return `C ${controlStartX} ${controlStartY}, ${controlEndX} ${controlEndY}, ${point.x} ${point.y}`;
        });

        return [`M ${firstPoint.x} ${firstPoint.y}`, ...segments].join(' ');
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

    private getNiceMaxValue(minValue: number, maxValue: number): number {
        const tickCount = LINE_CHART_GRID_LINE_COUNT - 1;
        const rawStep = (maxValue - minValue) / tickCount;
        const step = this.getNiceStep(rawStep);

        return minValue + step * tickCount;
    }

    private getNiceStep(rawStep: number): number {
        if (!Number.isFinite(rawStep) || rawStep <= 0) {
            return 1;
        }

        const magnitude = NICE_STEP_TEN ** Math.floor(Math.log10(rawStep));
        const normalizedStep = rawStep / magnitude;
        const multiplier = LINE_CHART_NICE_STEP_MULTIPLIERS.find(value => normalizedStep <= value) ?? NICE_STEP_TEN;

        return multiplier * magnitude;
    }

    private shouldPadFlatRange(values: readonly number[], maxValue: number): boolean {
        return (
            this.minValue() === null && this.maxValue() === null && values.length > 0 && Math.min(...values) === maxValue && maxValue !== 0
        );
    }

    private clamp(value: number, minValue: number, maxValue: number): number {
        return Math.min(Math.max(value, minValue), maxValue);
    }
}

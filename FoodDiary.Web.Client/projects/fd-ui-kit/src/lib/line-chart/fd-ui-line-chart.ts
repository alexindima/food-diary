import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type FdUiLineChartPoint = {
    label: string;
    value: number | null;
};

export type FdUiLineChartSeries = {
    label: string;
    points: readonly FdUiLineChartPoint[];
    color?: string;
    fillColor?: string;
};

export type FdUiLineChartDensity = 'default' | 'sparkline';

type FdUiLineChartPointViewModel = {
    label: string;
    seriesLabel: string;
    value: number;
    x: number;
    y: number;
    xPercent: string;
    yPercent: string;
    color: string;
    tooltip: string;
};

type FdUiLineChartSeriesViewModel = {
    label: string;
    color: string;
    fillColor: string;
    points: readonly FdUiLineChartPointViewModel[];
    path: string;
    areaPath: string;
    paths: readonly string[];
    areaPaths: readonly string[];
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

type FdUiLineChartPointGroupOptions = {
    minValue: number;
    effectiveRange: number;
    availableHeight: number;
    shouldPrefixTooltip: boolean;
};

const LINE_CHART_VIEWBOX_WIDTH = 100;
const LINE_CHART_VIEWBOX_HEIGHT = 64;
const LINE_CHART_PADDING = 6;
const LINE_CHART_SPARKLINE_PADDING = 2;
const LINE_CHART_LINE_STROKE_WIDTH = 2.4;
const LINE_CHART_SPARKLINE_AREA_BASELINE_OFFSET = LINE_CHART_LINE_STROKE_WIDTH / 2;
const LINE_CHART_HORIZONTAL_PADDING = 2;
const LINE_CHART_LEFT_X = LINE_CHART_HORIZONTAL_PADDING;
const LINE_CHART_RIGHT_X = LINE_CHART_VIEWBOX_WIDTH - LINE_CHART_HORIZONTAL_PADDING;
const LINE_CHART_SPARKLINE_LEFT_X = 0;
const LINE_CHART_SPARKLINE_RIGHT_X = LINE_CHART_VIEWBOX_WIDTH;
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
    templateUrl: './fd-ui-line-chart.html',
    styleUrl: './fd-ui-line-chart.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiLineChartComponent {
    public readonly title = input<string>();
    public readonly points = input<readonly FdUiLineChartPoint[]>([]);
    public readonly series = input<readonly FdUiLineChartSeries[]>([]);
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
    public readonly density = input<FdUiLineChartDensity>('default');

    protected readonly viewBox = `0 0 ${LINE_CHART_VIEWBOX_WIDTH} ${LINE_CHART_VIEWBOX_HEIGHT}`;
    protected readonly chartLeft = computed(() => (this.density() === 'sparkline' ? LINE_CHART_SPARKLINE_LEFT_X : LINE_CHART_LEFT_X));
    protected readonly chartRight = computed(() => (this.density() === 'sparkline' ? LINE_CHART_SPARKLINE_RIGHT_X : LINE_CHART_RIGHT_X));
    protected readonly chartTop = computed(() => this.chartPadding());
    protected readonly chartBottom = computed(() => LINE_CHART_VIEWBOX_HEIGHT - this.chartPadding());
    private readonly areaBaseline = computed(() =>
        this.density() === 'sparkline' ? this.chartBottom() + LINE_CHART_SPARKLINE_AREA_BASELINE_OFFSET : this.chartBottom(),
    );
    private readonly chartPadding = computed(() => (this.density() === 'sparkline' ? LINE_CHART_SPARKLINE_PADDING : LINE_CHART_PADDING));

    private readonly resolvedSeries = computed<ReadonlyArray<Required<FdUiLineChartSeries>>>(() => {
        const series = this.series();
        if (series.length > 0) {
            return series.map(item => ({
                label: item.label,
                points: item.points,
                color: item.color ?? DEFAULT_LINE_COLOR,
                fillColor: item.fillColor ?? DEFAULT_FILL_COLOR,
            }));
        }

        return [
            {
                label: this.title() ?? '',
                points: this.points(),
                color: this.lineColor(),
                fillColor: this.fillColor(),
            },
        ];
    });
    private readonly xAxisPoints = computed(() => this.resolvedSeries().find(item => item.points.length > 0)?.points ?? []);
    private readonly numericValues = computed(() =>
        this.resolvedSeries()
            .flatMap(item => item.points.map(point => point.value))
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

        if (paddedMaxValue > minValue) {
            return this.getNiceMaxValue(minValue, paddedMaxValue);
        }

        const defaultMaxValue = this.defaultMaxValue();
        if (defaultMaxValue !== null && defaultMaxValue > minValue) {
            return defaultMaxValue;
        }

        if (minValue !== 0) {
            return this.getNiceMaxValue(minValue, minValue + this.getFlatRangePadding(minValue));
        }

        return this.getNiceMaxValue(minValue, minValue + 1);
    });
    protected readonly gridLines = computed<readonly FdUiLineChartGridLine[]>(() => {
        const minValue = this.resolvedMinValue();
        const maxValue = this.resolvedMaxValue();
        const padding = this.chartPadding();
        const availableHeight = LINE_CHART_VIEWBOX_HEIGHT - padding * 2;
        const step = availableHeight / (LINE_CHART_GRID_LINE_COUNT - 1);
        const valueStep = (maxValue - minValue) / (LINE_CHART_GRID_LINE_COUNT - 1);

        return Array.from({ length: LINE_CHART_GRID_LINE_COUNT }, (_, index) => {
            const reversedIndex = LINE_CHART_GRID_LINE_COUNT - 1 - index;
            const y = padding + step * index;

            return {
                y,
                yPercent: `${(y / LINE_CHART_VIEWBOX_HEIGHT) * LINE_CHART_PERCENTAGE_SCALE}%`,
                label: this.formatAxisValue(minValue + valueStep * reversedIndex),
            };
        });
    });
    protected readonly xAxisLabels = computed<readonly FdUiLineChartXAxisLabel[]>(() => {
        const points = this.xAxisPoints();
        if (points.length === 0) {
            return [];
        }

        if (points.length === 1) {
            const label = points[0]?.label.trim() ?? '';
            return label.length > 0 ? [{ label, xPercent: '50%', position: 'middle' }] : [];
        }

        const labelCount = Math.min(LINE_CHART_X_AXIS_LABEL_COUNT, points.length);
        const xStep = (this.chartRight() - this.chartLeft()) / (points.length - 1);
        const indexes = Array.from({ length: labelCount }, (_, index) => Math.round((index * (points.length - 1)) / (labelCount - 1)));
        const uniqueIndexes = [...new Set(indexes)];

        return uniqueIndexes.flatMap(index => {
            const label = points[index]?.label.trim() ?? '';
            if (label.length === 0) {
                return [];
            }

            const x = this.chartLeft() + index * xStep;
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
        const points = this.xAxisPoints();
        if (points.length === 0) {
            return [];
        }

        if (points.length === 1) {
            return [{ x: LINE_CHART_VIEWBOX_WIDTH / 2 }];
        }

        const xStep = (this.chartRight() - this.chartLeft()) / (points.length - 1);

        return Array.from({ length: points.length }, (_, index) => ({
            x: this.chartLeft() + index * xStep,
        }));
    });

    protected readonly seriesViews = computed<readonly FdUiLineChartSeriesViewModel[]>(() => {
        const numeric = this.numericValues();

        if (numeric.length === 0) {
            return [];
        }

        const minValue = this.resolvedMinValue();
        const maxValue = this.resolvedMaxValue();
        const valueRange = maxValue - minValue;
        const effectiveRange = valueRange > 0 ? valueRange : 1;
        const availableHeight = LINE_CHART_VIEWBOX_HEIGHT - this.chartPadding() * 2;

        return this.resolvedSeries().flatMap(series => {
            const shouldPrefixTooltip = this.series().length > 0 && series.label.trim().length > 0;
            const pointGroups = this.buildPointGroups(series, {
                minValue,
                effectiveRange,
                availableHeight,
                shouldPrefixTooltip,
            });
            const points = pointGroups.flat();

            if (points.length === 0) {
                return [];
            }

            const paths = pointGroups.map(group => this.buildLinePath(group)).filter(path => path.length > 0);
            const areaPaths = pointGroups.map(group => this.buildAreaPath(this.buildLinePath(group), group));
            const path = paths.join(' ');

            return [
                {
                    label: series.label,
                    color: series.color,
                    fillColor: series.fillColor,
                    points,
                    path,
                    areaPath: areaPaths.join(' '),
                    paths,
                    areaPaths,
                },
            ];
        });
    });
    protected readonly pointViews = computed<readonly FdUiLineChartPointViewModel[]>(() =>
        this.seriesViews().flatMap(series => series.points),
    );

    protected readonly linePath = computed(() => {
        return this.seriesViews()[0]?.path ?? '';
    });

    protected readonly areaPath = computed(() => {
        return this.seriesViews()[0]?.areaPath ?? '';
    });

    protected readonly shouldShowSeriesLegend = computed(() => this.seriesViews().length > 1);

    protected readonly ariaLabel = computed(() => {
        const title = this.title();
        const hasTitle = title !== undefined && title.trim().length > 0;
        if (this.pointViews().length === 0) {
            return hasTitle ? `${title}: ${this.emptyLabel()}` : this.emptyLabel();
        }

        const shouldPrefixSeriesLabel = this.series().length > 0;
        const details = this.seriesViews()
            .flatMap(series =>
                series.points.map(point => {
                    const seriesLabel = shouldPrefixSeriesLabel && series.label.trim().length > 0 ? `${series.label} ` : '';

                    return `${seriesLabel}${point.label} ${point.value}`;
                }),
            )
            .join(', ');
        return hasTitle ? `${title}: ${details}` : details;
    });

    private buildLinePath(points: readonly FdUiLineChartPointViewModel[]): string {
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
    }

    private buildPointGroups(
        series: Required<FdUiLineChartSeries>,
        options: FdUiLineChartPointGroupOptions,
    ): FdUiLineChartPointViewModel[][] {
        const groups: FdUiLineChartPointViewModel[][] = [];
        let currentGroup: FdUiLineChartPointViewModel[] = [];

        series.points.forEach((point, index) => {
            const value = point.value;
            if (typeof value !== 'number' || !Number.isFinite(value)) {
                if (currentGroup.length > 0) {
                    groups.push(currentGroup);
                    currentGroup = [];
                }

                return;
            }

            const xStep = series.points.length > 1 ? (this.chartRight() - this.chartLeft()) / (series.points.length - 1) : 0;
            const x = series.points.length > 1 ? this.chartLeft() + index * xStep : LINE_CHART_VIEWBOX_WIDTH / 2;
            const y = this.chartBottom() - ((value - options.minValue) / options.effectiveRange) * options.availableHeight;
            const label = point.label.trim().length > 0 ? point.label : this.emptyLabel();
            const tooltipPrefix = options.shouldPrefixTooltip ? `${series.label}, ` : '';

            currentGroup.push({
                label,
                seriesLabel: series.label,
                value,
                x,
                y,
                xPercent: `${x}%`,
                yPercent: `${(y / LINE_CHART_VIEWBOX_HEIGHT) * LINE_CHART_PERCENTAGE_SCALE}%`,
                color: series.color,
                tooltip: `${tooltipPrefix}${label}: ${value}`,
            });
        });

        if (currentGroup.length > 0) {
            groups.push(currentGroup);
        }

        return groups;
    }

    private buildAreaPath(path: string, points: readonly FdUiLineChartPointViewModel[]): string {
        const first = points[0];
        const last = points.at(-1);
        if (last === undefined) {
            return path;
        }

        const baseline = this.areaBaseline();

        return `${path} L ${last.x} ${baseline} L ${first.x} ${baseline} Z`;
    }

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

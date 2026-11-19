import { ChangeDetectionStrategy, Component, computed, inject, input, LOCALE_ID } from '@angular/core';

import type { FdUiChartSize } from '../chart/fd-ui-chart.types';
import type { FdUiLineChartReferenceLine, FdUiLineChartXAxisLabelLayout } from './fd-ui-line-chart.types';
import { FdUiLineChartLegendComponent } from './fd-ui-line-chart-legend';
import { FdUiLineChartXAxisComponent } from './fd-ui-line-chart-x-axis';
import { resolveXAxisTicks } from './fd-ui-line-chart-x-axis-layout';
import { FdUiLineChartYAxisComponent } from './fd-ui-line-chart-y-axis';

export type { FdUiLineChartReferenceLine, FdUiLineChartXAxisLabelLayout } from './fd-ui-line-chart.types';

export type FdUiLineChartPoint = {
    label: string;
    value: number | null;
    xPosition?: number;
};

export type FdUiLineChartSeries = {
    label: string;
    points: readonly FdUiLineChartPoint[];
    color?: string;
    fillColor?: string;
};

export type FdUiLineChartDensity = 'default' | 'sparkline';
export type FdUiLineChartHorizontalEdgeInset = 'default' | 'none';
export type FdUiLineChartVerticalEdgeInset = 'default' | 'none';

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

type FdUiLineChartPointGroup = [FdUiLineChartPointViewModel, ...FdUiLineChartPointViewModel[]];

type FdUiLineChartSeriesViewModel = {
    label: string;
    color: string;
    fillColor: string;
    points: readonly FdUiLineChartPointViewModel[];
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

type FdUiLineChartGridSegment = {
    x1: number;
    y1: number;
    x2: number;
    y2: number;
};

type FdUiLineChartAreaSegment = {
    path: string;
    fill: string;
};

type FdUiLineChartLineSegment = {
    path: string;
    color: string;
};

type FdUiLineChartReferenceLineViewModel = {
    label: string | null;
    color: string;
    lineStyle: 'solid' | 'dashed';
    y: number;
    yPercent: string;
    edge: 'top' | 'inside' | 'bottom';
    outsideRange: boolean;
};

type FdUiLineChartReferenceScale = {
    minValue: number;
    maxValue: number;
    top: number;
    bottom: number;
};

const LINE_CHART_VIEWBOX_WIDTH = 100;
const LINE_CHART_VIEWBOX_HEIGHT = 64;
const LINE_CHART_PADDING = 6;
const LINE_CHART_STACKED_AXIS_BOTTOM_PADDING = 1;
const LINE_CHART_SPARKLINE_PADDING = 2;
const LINE_CHART_LINE_STROKE_WIDTH = 2.4;
const LINE_CHART_SPARKLINE_AREA_BASELINE_OFFSET = LINE_CHART_LINE_STROKE_WIDTH / 2;
const LINE_CHART_HORIZONTAL_PADDING = 2;
const LINE_CHART_LEFT_X = LINE_CHART_HORIZONTAL_PADDING;
const LINE_CHART_RIGHT_X = LINE_CHART_VIEWBOX_WIDTH - LINE_CHART_HORIZONTAL_PADDING;
const LINE_CHART_SPARKLINE_LEFT_X = 0;
const LINE_CHART_SPARKLINE_RIGHT_X = LINE_CHART_VIEWBOX_WIDTH;
const LINE_CHART_GRID_LINE_COUNT = 5;
const LINE_CHART_PERCENTAGE_SCALE = 100;
const REFERENCE_LABEL_TOP_THRESHOLD_PERCENT = 8;
const REFERENCE_LABEL_TOP_THRESHOLD_Y = (LINE_CHART_VIEWBOX_HEIGHT * REFERENCE_LABEL_TOP_THRESHOLD_PERCENT) / LINE_CHART_PERCENTAGE_SCALE;
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
const DEFAULT_REFERENCE_LINE_COLOR = 'var(--fd-color-primary-500)';
const DEFAULT_REFERENCE_LINE_EDGE_PADDING_RATIO = 0.15;

@Component({
    selector: 'fd-ui-line-chart',
    imports: [FdUiLineChartYAxisComponent, FdUiLineChartXAxisComponent, FdUiLineChartLegendComponent],
    templateUrl: './fd-ui-line-chart.html',
    styleUrl: './fd-ui-line-chart.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: { '[class.fd-ui-chart--large]': 'size() === "large"' },
})
export class FdUiLineChartComponent {
    public readonly title = input<string>();
    public readonly size = input<FdUiChartSize>('default');
    public readonly showLegend = input(true);
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
    public readonly connectNulls = input(true);
    public readonly referenceLines = input<readonly FdUiLineChartReferenceLine[]>([]);
    public readonly xAxisLabelLayout = input<FdUiLineChartXAxisLabelLayout>('angled');
    public readonly valueSuffix = input('');
    public readonly axisValueSuffix = input<string | null>(null);
    public readonly axisUnit = input<string | null>(null);
    public readonly locale = input(inject(LOCALE_ID));
    protected readonly resolvedAxisUnit = computed(() => this.axisUnit() ?? (this.axisValueSuffix() === null ? this.valueSuffix() : ''));
    public readonly axisDecimalPlaces = input(DEFAULT_AXIS_DECIMAL_PLACES);
    public readonly axisValueFormatter = input<((value: number) => string) | null>(null);
    public readonly density = input<FdUiLineChartDensity>('default');
    public readonly horizontalEdgeInset = input<FdUiLineChartHorizontalEdgeInset>('default');
    public readonly verticalEdgeInset = input<FdUiLineChartVerticalEdgeInset>('default');

    protected readonly viewBox = `0 0 ${LINE_CHART_VIEWBOX_WIDTH} ${LINE_CHART_VIEWBOX_HEIGHT}`;
    protected readonly chartLeft = computed(() =>
        this.density() === 'sparkline' || this.horizontalEdgeInset() === 'none' || this.showAxisLabels()
            ? LINE_CHART_SPARKLINE_LEFT_X
            : LINE_CHART_LEFT_X,
    );
    protected readonly chartRight = computed(() =>
        this.density() === 'sparkline' || this.horizontalEdgeInset() === 'none' || this.showAxisLabels()
            ? LINE_CHART_SPARKLINE_RIGHT_X
            : LINE_CHART_RIGHT_X,
    );
    protected readonly chartTop = computed(() => (this.verticalEdgeInset() === 'none' ? 0 : this.chartPadding()));
    protected readonly chartBottom = computed(() =>
        this.verticalEdgeInset() === 'none'
            ? LINE_CHART_VIEWBOX_HEIGHT
            : this.xAxisLabelLayout() === 'stacked'
              ? LINE_CHART_VIEWBOX_HEIGHT - LINE_CHART_STACKED_AXIS_BOTTOM_PADDING
              : LINE_CHART_VIEWBOX_HEIGHT - this.chartPadding(),
    );
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
        if (this.hasClampedReferenceBelow(actualMinValue)) {
            return Math.max(0, actualMinValue - this.getReferenceEdgePadding(actualMinValue, actualMaxValue, 'below'));
        }
        if (this.hasClampedReferenceAbove(actualMaxValue)) {
            return actualMinValue;
        }

        if (actualMinValue !== 0 && actualMinValue === actualMaxValue && numeric.length > 0) {
            return Math.max(0, actualMinValue - this.getFlatRangePadding(actualMinValue));
        }

        return actualMinValue;
    });
    private readonly resolvedMaxValue = computed(() => this.resolveMaxValue());

    private resolveMaxValue(): number {
        const minValue = this.resolvedMinValue();
        const explicitMaxValue = this.maxValue();
        if (explicitMaxValue !== null && explicitMaxValue > minValue) {
            return explicitMaxValue;
        }

        const numeric = this.numericValues();
        const actualMaxValue = Math.max(...numeric);
        const actualMinValue = Math.min(...numeric);
        if (this.hasClampedReferenceBelow(actualMinValue) && !this.hasClampedReferenceAbove(actualMaxValue)) {
            return actualMaxValue;
        }
        const paddedMaxValue = this.resolvePaddedMaxValue(numeric, actualMinValue, actualMaxValue);

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
    }

    private resolvePaddedMaxValue(numeric: readonly number[], actualMinValue: number, actualMaxValue: number): number {
        if (this.hasClampedReferenceAbove(actualMaxValue)) {
            return actualMaxValue + this.getReferenceEdgePadding(actualMinValue, actualMaxValue, 'above');
        }

        return this.shouldPadFlatRange(numeric, actualMaxValue)
            ? actualMaxValue + this.getFlatRangePadding(actualMaxValue)
            : actualMaxValue;
    }
    protected readonly gridLines = computed<readonly FdUiLineChartGridLine[]>(() => {
        const minValue = this.resolvedMinValue();
        const maxValue = this.resolvedMaxValue();
        const availableHeight = this.chartBottom() - this.chartTop();
        const step = availableHeight / (LINE_CHART_GRID_LINE_COUNT - 1);
        const valueStep = (maxValue - minValue) / (LINE_CHART_GRID_LINE_COUNT - 1);

        return Array.from({ length: LINE_CHART_GRID_LINE_COUNT }, (_, index) => {
            const reversedIndex = LINE_CHART_GRID_LINE_COUNT - 1 - index;
            const y = this.chartTop() + step * index;

            return {
                y,
                yPercent: `${(y / LINE_CHART_VIEWBOX_HEIGHT) * LINE_CHART_PERCENTAGE_SCALE}%`,
                label: this.formatAxisValue(minValue + valueStep * reversedIndex),
            };
        });
    });
    private readonly xAxisTicks = computed(() =>
        resolveXAxisTicks(
            this.resolvedSeries(),
            (point, index, count) => this.resolvePointX(point, index, count),
            LINE_CHART_VIEWBOX_WIDTH / 2,
        ),
    );
    protected readonly xAxisLabels = computed<readonly FdUiLineChartXAxisLabel[]>(() =>
        this.xAxisTicks()
            .filter(tick => tick.label.length > 0)
            .map(tick => ({ label: tick.label, xPercent: `${tick.x}%`, position: tick.position })),
    );
    protected readonly verticalGridLines = computed<readonly FdUiLineChartVerticalGridLine[]>(() =>
        this.xAxisTicks().map(({ x }) => ({ x })),
    );

    protected readonly seriesViews = computed<readonly FdUiLineChartSeriesViewModel[]>(() => {
        const numeric = this.numericValues();

        if (numeric.length === 0) {
            return [];
        }

        const minValue = this.resolvedMinValue();
        const maxValue = this.resolvedMaxValue();
        const valueRange = maxValue - minValue;
        const effectiveRange = valueRange > 0 ? valueRange : 1;
        const availableHeight = this.chartBottom() - this.chartTop();

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

            const paths = pointGroups.map(group => this.buildLinePath(group));
            const areaPaths = pointGroups.map((group, index) => this.buildAreaPath(paths[index], group));

            return [
                {
                    label: series.label,
                    color: series.color,
                    fillColor: series.fillColor,
                    points,
                    paths,
                    areaPaths,
                },
            ];
        });
    });
    protected readonly pointViews = computed<readonly FdUiLineChartPointViewModel[]>(() =>
        this.seriesViews().flatMap(series => series.points),
    );

    protected readonly gridSegments = computed<readonly FdUiLineChartGridSegment[]>(() => {
        if (!this.showGrid()) {
            return [];
        }

        const left = this.chartLeft();
        const right = this.chartRight();
        const top = this.chartTop();
        const bottom = this.chartBottom();
        const horizontal = this.gridLines().map(line => ({ x1: left, y1: line.y, x2: right, y2: line.y }));
        const vertical = this.verticalGridLines().map(line => ({ x1: line.x, y1: top, x2: line.x, y2: bottom }));

        return [...horizontal, ...vertical];
    });
    protected readonly areaSegments = computed<readonly FdUiLineChartAreaSegment[]>(() => {
        if (!this.showArea()) {
            return [];
        }

        return this.seriesViews().flatMap(series => series.areaPaths.map(path => ({ path, fill: series.fillColor })));
    });
    protected readonly lineSegments = computed<readonly FdUiLineChartLineSegment[]>(() =>
        this.seriesViews().flatMap(series => series.paths.map(path => ({ path, color: series.color }))),
    );
    protected readonly referenceLineViews = computed<readonly FdUiLineChartReferenceLineViewModel[]>(() => {
        const scale: FdUiLineChartReferenceScale = {
            minValue: this.resolvedMinValue(),
            maxValue: this.resolvedMaxValue(),
            top: this.chartTop(),
            bottom: this.chartBottom(),
        };

        return this.referenceLines()
            .filter(referenceLine => this.shouldRenderReferenceLine(referenceLine, scale))
            .map(referenceLine => this.buildReferenceLineView(referenceLine, scale));
    });
    protected readonly inRangeReferenceLines = computed(() =>
        this.referenceLineViews().filter(line => !this.showAxisLabels() || !line.outsideRange),
    );
    protected readonly visiblePoints = computed<readonly FdUiLineChartPointViewModel[]>(() => (this.showPoints() ? this.pointViews() : []));

    private buildReferenceLineView(
        referenceLine: FdUiLineChartReferenceLine,
        scale: FdUiLineChartReferenceScale,
    ): FdUiLineChartReferenceLineViewModel {
        const belowRange = referenceLine.value < scale.minValue;
        const aboveRange = referenceLine.value > scale.maxValue;
        const range = scale.maxValue - scale.minValue;
        const clampedValue = this.clamp(referenceLine.value, scale.minValue, scale.maxValue);
        const y = scale.bottom - ((clampedValue - scale.minValue) / (range > 0 ? range : 1)) * (scale.bottom - scale.top);
        const label = referenceLine.label?.trim();

        return {
            label: this.formatReferenceLabel(label, aboveRange, belowRange),
            outsideRange: aboveRange || belowRange,
            color: referenceLine.color ?? DEFAULT_REFERENCE_LINE_COLOR,
            lineStyle: referenceLine.lineStyle ?? 'dashed',
            y,
            yPercent: `${(y / LINE_CHART_VIEWBOX_HEIGHT) * LINE_CHART_PERCENTAGE_SCALE}%`,
            edge: aboveRange || y <= REFERENCE_LABEL_TOP_THRESHOLD_Y ? 'top' : belowRange ? 'bottom' : 'inside',
        };
    }

    private formatReferenceLabel(label: string | undefined, aboveRange: boolean, belowRange: boolean): string | null {
        if (label === undefined || label.length === 0) {
            return null;
        }
        if (aboveRange) {
            return `↑ ${label}`;
        }
        return belowRange ? `↓ ${label}` : label;
    }

    private shouldRenderReferenceLine(referenceLine: FdUiLineChartReferenceLine, scale: FdUiLineChartReferenceScale): boolean {
        if (!Number.isFinite(referenceLine.value)) {
            return false;
        }

        const isOutsideRange = referenceLine.value < scale.minValue || referenceLine.value > scale.maxValue;
        return !isOutsideRange || referenceLine.outOfRangeBehavior !== 'hide';
    }

    private getReferenceEdgePadding(actualMinValue: number, actualMaxValue: number, edge: 'above' | 'below'): number {
        const range = actualMaxValue - actualMinValue;
        const fallbackPadding = range === 0 ? this.getFlatRangePadding(edge === 'below' ? actualMinValue : actualMaxValue) : 0;
        const referenceLines = this.referenceLines().filter(referenceLine => {
            const isOutside = edge === 'below' ? referenceLine.value < actualMinValue : referenceLine.value > actualMaxValue;
            return isOutside && referenceLine.outOfRangeBehavior !== 'hide';
        });
        const ratio = Math.max(...referenceLines.map(line => line.edgePaddingRatio ?? DEFAULT_REFERENCE_LINE_EDGE_PADDING_RATIO));

        return Math.max(range * ratio, fallbackPadding);
    }

    private hasClampedReferenceAbove(actualMaxValue: number): boolean {
        return this.referenceLines().some(
            referenceLine => referenceLine.value > actualMaxValue && referenceLine.outOfRangeBehavior !== 'hide',
        );
    }

    private hasClampedReferenceBelow(actualMinValue: number): boolean {
        return this.referenceLines().some(
            referenceLine => referenceLine.value < actualMinValue && referenceLine.outOfRangeBehavior !== 'hide',
        );
    }

    protected readonly shouldShowSeriesLegend = computed(() => this.showLegend() && this.seriesViews().length > 1);

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

                    return `${seriesLabel}${point.label} ${this.formatDataValue(point.value)}`;
                }),
            )
            .join(', ');
        return hasTitle ? `${title}: ${details}` : details;
    });

    private buildLinePath(points: FdUiLineChartPointGroup): string {
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

    private buildPointGroups(series: Required<FdUiLineChartSeries>, options: FdUiLineChartPointGroupOptions): FdUiLineChartPointGroup[] {
        const groups: FdUiLineChartPointGroup[] = [];
        let currentGroup: FdUiLineChartPointGroup | null = null;

        series.points.forEach((point, index) => {
            const value = point.value;
            if (typeof value !== 'number' || !Number.isFinite(value)) {
                if (!this.connectNulls()) {
                    currentGroup = null;
                }

                return;
            }

            const x = this.resolvePointX(point, index, series.points.length);
            const y = this.chartBottom() - ((value - options.minValue) / options.effectiveRange) * options.availableHeight;
            const label = point.label.trim().length > 0 ? point.label : this.emptyLabel();
            const tooltipPrefix = options.shouldPrefixTooltip ? `${series.label}, ` : '';

            const view: FdUiLineChartPointViewModel = {
                label,
                seriesLabel: series.label,
                value,
                x,
                y,
                xPercent: `${x}%`,
                yPercent: `${(y / LINE_CHART_VIEWBOX_HEIGHT) * LINE_CHART_PERCENTAGE_SCALE}%`,
                color: series.color,
                tooltip: `${tooltipPrefix}${label}: ${this.formatDataValue(value)}`,
            };
            if (currentGroup === null) {
                currentGroup = [view];
                groups.push(currentGroup);
            } else {
                currentGroup.push(view);
            }
        });

        return groups;
    }

    private resolvePointX(point: Pick<FdUiLineChartPoint, 'xPosition'> | undefined, index: number, pointCount: number): number {
        const explicitPosition = point?.xPosition;
        if (explicitPosition !== undefined && Number.isFinite(explicitPosition)) {
            const normalizedPosition = this.clamp(explicitPosition, 0, 1);
            return this.chartLeft() + normalizedPosition * (this.chartRight() - this.chartLeft());
        }

        const xStep = pointCount > 1 ? (this.chartRight() - this.chartLeft()) / (pointCount - 1) : 0;
        return pointCount > 1 ? this.chartLeft() + index * xStep : LINE_CHART_VIEWBOX_WIDTH / 2;
    }

    private buildAreaPath(path: string, points: FdUiLineChartPointGroup): string {
        const first = points[0];
        // eslint-disable-next-line unicorn/prefer-at -- The point group is nonempty and direct indexing preserves its point type.
        const last = points[points.length - 1];
        const baseline = this.areaBaseline();

        return `${path} L ${last.x} ${baseline} L ${first.x} ${baseline} Z`;
    }

    private formatAxisValue(value: number): string {
        const formatter = this.axisValueFormatter();
        const formatted = formatter === null ? this.formatNumericValue(value) : formatter(value);
        return this.withSuffix(formatted, this.axisValueSuffix() ?? '');
    }

    private formatDataValue(value: number): string {
        return this.withSuffix(this.formatNumericValue(value), this.valueSuffix());
    }

    private formatNumericValue(value: number): string {
        const decimals = Math.max(0, this.axisDecimalPlaces());
        return new Intl.NumberFormat(this.locale(), { maximumFractionDigits: decimals }).format(value).replace('-', '−');
    }

    private withSuffix(formatted: string, valueSuffix: string): string {
        const suffix = valueSuffix.trim();

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

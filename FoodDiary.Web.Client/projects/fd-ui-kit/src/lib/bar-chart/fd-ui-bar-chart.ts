import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type FdUiBarChartItem = {
    label: string;
    value: number;
    color?: string;
};

export type FdUiBarChartValue = {
    label: string;
    value: number | null;
    color?: string;
};

export type FdUiBarChartCategory = {
    label: string;
    ariaLabel?: string;
    highlighted?: boolean;
    values: readonly FdUiBarChartValue[];
};

export type FdUiBarChartReferenceLine = {
    value: number;
    label?: string;
    color?: string;
    labelPlacement?: 'inside' | 'outside';
};

export type FdUiBarChartLayout = 'single' | 'grouped' | 'stacked';
export type FdUiBarChartHorizontalEdgeInset = 'default' | 'none';

type FdUiBarChartItemViewModel = {
    label: string;
    value: number;
    color: string;
    height: number;
    x: number;
    y: number;
    width: number;
    tooltip: string;
};

type FdUiBarChartGridLine = {
    y: number;
};

type FdUiBarChartCategoryView = FdUiBarChartCategory & {
    ariaLabel: string;
    values: ReadonlyArray<FdUiBarChartValue & { height: number; color: string }>;
};

type FdUiBarChartReferenceLineView = FdUiBarChartReferenceLine & {
    color: string;
    top: number;
    labelAtTop: boolean;
};

const BAR_CHART_VIEWBOX_WIDTH = 100;
const BAR_CHART_VIEWBOX_HEIGHT = 64;
const BAR_CHART_PADDING_TOP = 6;
const BAR_CHART_PADDING_BOTTOM = 8;
const REFERENCE_LABEL_TOP_THRESHOLD_PERCENT = 8;
const BAR_CHART_GAP = 4;
const BAR_CHART_GRID_LINE_COUNT = 5;
const PERCENTAGE_SCALE = 100;
const DEFAULT_BAR_COLOR = 'var(--fd-color-primary-500)';
const DEFAULT_REFERENCE_LINE_COLOR = 'var(--fd-color-text-subtle)';

@Component({
    selector: 'fd-ui-bar-chart',
    imports: [],
    templateUrl: './fd-ui-bar-chart.html',
    styleUrl: './fd-ui-bar-chart.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiBarChartComponent {
    public readonly title = input<string>();
    public readonly items = input<readonly FdUiBarChartItem[]>([]);
    public readonly emptyLabel = input('No data');
    public readonly showLabels = input(true);
    public readonly categories = input<readonly FdUiBarChartCategory[]>([]);
    public readonly layout = input<FdUiBarChartLayout>('single');
    public readonly horizontalEdgeInset = input<FdUiBarChartHorizontalEdgeInset>('none');
    public readonly axisUnit = input('');
    public readonly axisTicks = input<readonly number[]>([]);
    public readonly scaleMaximum = input<number>();
    public readonly referenceLines = input<readonly FdUiBarChartReferenceLine[]>([]);
    public readonly axisValueFormatter = input<(value: number) => string>(value => String(value));

    protected readonly viewBox = `0 0 ${BAR_CHART_VIEWBOX_WIDTH} ${BAR_CHART_VIEWBOX_HEIGHT}`;
    protected readonly chartBottom = BAR_CHART_VIEWBOX_HEIGHT - BAR_CHART_PADDING_BOTTOM;
    protected readonly gridLines = computed<readonly FdUiBarChartGridLine[]>(() => {
        const availableHeight = BAR_CHART_VIEWBOX_HEIGHT - BAR_CHART_PADDING_TOP - BAR_CHART_PADDING_BOTTOM;
        const step = availableHeight / (BAR_CHART_GRID_LINE_COUNT - 1);

        return Array.from({ length: BAR_CHART_GRID_LINE_COUNT }, (_, index) => ({
            y: BAR_CHART_PADDING_TOP + step * index,
        }));
    });

    protected readonly maxValue = computed(() => Math.max(0, ...this.normalizedItems().map(item => item.value)));
    protected readonly itemViews = computed<readonly FdUiBarChartItemViewModel[]>(() => {
        const items = this.normalizedItems();
        const maxValue = this.maxValue();

        if (items.length === 0) {
            return [];
        }

        const effectiveMaxValue = maxValue > 0 ? maxValue : 1;
        const totalGap = Math.max(0, items.length - 1) * BAR_CHART_GAP;
        const width = Math.max(2, (BAR_CHART_VIEWBOX_WIDTH - totalGap) / items.length);
        const availableHeight = BAR_CHART_VIEWBOX_HEIGHT - BAR_CHART_PADDING_TOP - BAR_CHART_PADDING_BOTTOM;

        return items.map((item, index) => {
            const height = (item.value / effectiveMaxValue) * availableHeight;
            const x = index * (width + BAR_CHART_GAP);
            const y = this.chartBottom - height;

            return {
                label: item.label,
                value: item.value,
                color: item.color ?? DEFAULT_BAR_COLOR,
                height,
                x,
                y,
                width,
                tooltip: `${item.label}: ${item.value}`,
            };
        });
    });

    protected readonly ariaLabel = computed(() => {
        const title = this.title();
        const hasTitle = title !== undefined && title.trim().length > 0;
        if (this.itemViews().length === 0) {
            return hasTitle ? `${title}: ${this.emptyLabel()}` : this.emptyLabel();
        }

        const details = this.itemViews()
            .map(item => `${item.label} ${item.value}`)
            .join(', ');
        return hasTitle ? `${title}: ${details}` : details;
    });

    protected readonly usesCategoricalLayout = computed(() => this.categories().length > 0);
    protected readonly categoricalMaximum = computed(() => {
        const configuredMaximum = this.scaleMaximum();
        if (configuredMaximum !== undefined && Number.isFinite(configuredMaximum) && configuredMaximum > 0) {
            return configuredMaximum;
        }

        const values = this.categories().map(category => {
            const finiteValues = category.values
                .filter(item => item.value !== null && Number.isFinite(item.value))
                .map(item => item.value ?? 0);
            return this.layout() === 'stacked'
                ? finiteValues.reduce((sum, value) => sum + Math.max(0, value), 0)
                : Math.max(0, ...finiteValues);
        });
        return Math.max(1, ...values);
    });
    protected readonly categoricalTicks = computed(() => {
        const configuredTicks = this.axisTicks().filter(Number.isFinite);
        if (configuredTicks.length > 0) {
            return configuredTicks;
        }

        return Array.from({ length: BAR_CHART_GRID_LINE_COUNT }, (_, index) =>
            Math.round(this.categoricalMaximum() * ((BAR_CHART_GRID_LINE_COUNT - index - 1) / (BAR_CHART_GRID_LINE_COUNT - 1))),
        );
    });
    protected readonly categoryViews = computed<readonly FdUiBarChartCategoryView[]>(() => {
        const maximum = this.categoricalMaximum();
        return this.categories().map(category => ({
            ...category,
            ariaLabel:
                category.ariaLabel ??
                `${category.label}: ${category.values
                    .filter(item => item.value !== null)
                    .map(item => `${item.label} ${item.value}`)
                    .join(', ')}`,
            values: category.values
                .filter(item => item.value !== null && Number.isFinite(item.value))
                .map(item => ({
                    ...item,
                    value: Math.max(0, item.value ?? 0),
                    height: (Math.max(0, item.value ?? 0) / maximum) * PERCENTAGE_SCALE,
                    color: item.color ?? DEFAULT_BAR_COLOR,
                })),
        }));
    });
    protected readonly referenceLineViews = computed<readonly FdUiBarChartReferenceLineView[]>(() =>
        this.referenceLines()
            .filter(line => Number.isFinite(line.value) && line.value >= 0 && line.value <= this.categoricalMaximum())
            .map(line => ({
                ...line,
                color: line.color ?? DEFAULT_REFERENCE_LINE_COLOR,
                top: PERCENTAGE_SCALE - (line.value / this.categoricalMaximum()) * PERCENTAGE_SCALE,
                labelAtTop:
                    PERCENTAGE_SCALE - (line.value / this.categoricalMaximum()) * PERCENTAGE_SCALE <= REFERENCE_LABEL_TOP_THRESHOLD_PERCENT,
            })),
    );
    protected readonly categoricalAriaLabel = computed(() => {
        const title = this.title()?.trim();
        const details = this.categoryViews()
            .map(category => category.ariaLabel)
            .join(', ');
        return title === undefined || title.length === 0 ? details : `${title}: ${details}`;
    });

    protected categoryGridTemplate(valueCount: number): string | null {
        return this.layout() === 'grouped' ? `repeat(${valueCount}, minmax(0, 1fr))` : null;
    }

    private readonly normalizedItems = computed(() =>
        this.items()
            .filter(item => Number.isFinite(item.value))
            .map(item => ({
                ...item,
                label: item.label.trim().length > 0 ? item.label : this.emptyLabel(),
                value: Math.max(item.value, 0),
            })),
    );
}

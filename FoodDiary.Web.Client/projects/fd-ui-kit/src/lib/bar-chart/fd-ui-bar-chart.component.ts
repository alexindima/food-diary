import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type FdUiBarChartItem = {
    label: string;
    value: number;
    color?: string;
};

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

const BAR_CHART_VIEWBOX_WIDTH = 100;
const BAR_CHART_VIEWBOX_HEIGHT = 64;
const BAR_CHART_PADDING_TOP = 6;
const BAR_CHART_PADDING_BOTTOM = 8;
const BAR_CHART_GAP = 4;
const BAR_CHART_GRID_LINE_COUNT = 5;
const DEFAULT_BAR_COLOR = 'var(--fd-color-primary-500)';

@Component({
    selector: 'fd-ui-bar-chart',
    imports: [],
    templateUrl: './fd-ui-bar-chart.component.html',
    styleUrl: './fd-ui-bar-chart.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiBarChartComponent {
    public readonly title = input<string>();
    public readonly items = input<readonly FdUiBarChartItem[]>([]);
    public readonly emptyLabel = input('No data');
    public readonly showLabels = input(true);

    protected readonly viewBox = `0 0 ${BAR_CHART_VIEWBOX_WIDTH} ${BAR_CHART_VIEWBOX_HEIGHT}`;
    protected readonly chartBottom = BAR_CHART_VIEWBOX_HEIGHT - BAR_CHART_PADDING_BOTTOM;
    protected readonly gridLines = computed<readonly FdUiBarChartGridLine[]>(() => {
        const availableHeight = BAR_CHART_VIEWBOX_HEIGHT - BAR_CHART_PADDING_TOP - BAR_CHART_PADDING_BOTTOM;
        const step = availableHeight / (BAR_CHART_GRID_LINE_COUNT - 1);

        return Array.from({ length: BAR_CHART_GRID_LINE_COUNT }, (_, index) => ({
            y: BAR_CHART_PADDING_TOP + step * index,
        }));
    });

    public readonly maxValue = computed(() => Math.max(0, ...this.normalizedItems().map(item => item.value)));
    public readonly itemViews = computed<readonly FdUiBarChartItemViewModel[]>(() => {
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

    public readonly ariaLabel = computed(() => {
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

    private readonly normalizedItems = computed(() =>
        this.items()
            .filter(item => Number.isFinite(item.value))
            .map(item => ({
                ...item,
                label: item.label.trim().length > 0 ? item.label : this.emptyLabel(),
                value: item.value > 0 ? item.value : 0,
            })),
    );
}

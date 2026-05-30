import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type FdUiPieChartSegment = {
    label: string;
    value: number;
    color?: string;
};

export type FdUiPieChartVariant = 'donut' | 'pie';

type FdUiPieChartSegmentViewModel = {
    label: string;
    value: number;
    color: string;
    percent: number;
    dashArray: string;
    dashOffset: number;
    tooltip: string;
};

const CHART_CIRCUMFERENCE = 100;
const DONUT_CHART_RADIUS = 15.9155;
const PIE_CHART_RADIUS = 7.95775;
const DEFAULT_SEGMENT_COLORS = [
    'var(--fd-color-blue-500)',
    'var(--fd-color-emerald-500)',
    'var(--fd-color-orange-500)',
    'var(--fd-color-purple-500)',
    'var(--fd-color-rose-500)',
    'var(--fd-color-slate-500)',
] as const;

@Component({
    selector: 'fd-ui-pie-chart',
    imports: [],
    templateUrl: './fd-ui-pie-chart.html',
    styleUrl: './fd-ui-pie-chart.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiPieChartComponent {
    public readonly title = input<string>();
    public readonly segments = input<readonly FdUiPieChartSegment[]>([]);
    public readonly emptyLabel = input('No data');
    public readonly showLegend = input(true);
    public readonly variant = input<FdUiPieChartVariant>('donut');

    protected readonly total = computed(() => this.normalizedSegments().reduce((sum, segment) => sum + segment.value, 0));

    protected readonly radius = computed(() => (this.variant() === 'pie' ? PIE_CHART_RADIUS : DONUT_CHART_RADIUS));

    protected readonly segmentViews = computed<readonly FdUiPieChartSegmentViewModel[]>(() => {
        const total = this.total();
        let offset = 0;

        if (total <= 0) {
            return [];
        }

        return this.normalizedSegments().map((segment, index) => {
            const percent = (segment.value / total) * CHART_CIRCUMFERENCE;
            const segmentView = {
                label: segment.label,
                value: segment.value,
                color: segment.color ?? DEFAULT_SEGMENT_COLORS[index % DEFAULT_SEGMENT_COLORS.length],
                percent,
                dashArray: `${percent} ${CHART_CIRCUMFERENCE - percent}`,
                dashOffset: -offset,
                tooltip: `${segment.label}: ${segment.value}`,
            };
            offset += percent;
            return segmentView;
        });
    });

    protected readonly ariaLabel = computed(() => {
        const title = this.title();
        const total = this.total();
        const hasTitle = title !== undefined && title.trim().length > 0;
        if (total <= 0) {
            return hasTitle ? `${title}: ${this.emptyLabel()}` : this.emptyLabel();
        }

        const details = this.segmentViews()
            .map(segment => `${segment.label} ${segment.value}`)
            .join(', ');
        return hasTitle ? `${title}: ${details}` : details;
    });

    private readonly normalizedSegments = computed(() =>
        this.segments()
            .map(segment => ({
                ...segment,
                label: segment.label.trim().length > 0 ? segment.label : this.emptyLabel(),
                value: Number.isFinite(segment.value) && segment.value > 0 ? segment.value : 0,
            }))
            .filter(segment => segment.value > 0),
    );
}

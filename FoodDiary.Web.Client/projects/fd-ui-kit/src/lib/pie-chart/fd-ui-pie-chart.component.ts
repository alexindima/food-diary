import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type FdUiPieChartSegment = {
    label: string;
    value: number;
    color?: string;
};

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
    templateUrl: './fd-ui-pie-chart.component.html',
    styleUrl: './fd-ui-pie-chart.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiPieChartComponent {
    public readonly title = input<string>();
    public readonly segments = input<readonly FdUiPieChartSegment[]>([]);
    public readonly emptyLabel = input('No data');

    public readonly total = computed(() => this.normalizedSegments().reduce((sum, segment) => sum + segment.value, 0));

    public readonly segmentViews = computed<readonly FdUiPieChartSegmentViewModel[]>(() => {
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

    public readonly ariaLabel = computed(() => {
        const title = this.title();
        const total = this.total();
        if (total <= 0) {
            return title ? `${title}: ${this.emptyLabel()}` : this.emptyLabel();
        }

        const details = this.segmentViews()
            .map(segment => `${segment.label} ${segment.value}`)
            .join(', ');
        return title ? `${title}: ${details}` : details;
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

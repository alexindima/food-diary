import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import type { FdUiLineChartXAxisLabelLayout } from './fd-ui-line-chart.types';

export type FdUiLineChartXAxisLabelView = {
    label: string;
    xPercent: string;
    position: 'start' | 'middle' | 'end';
};

@Component({
    selector: 'fd-ui-line-chart-x-axis',
    imports: [],
    templateUrl: './fd-ui-line-chart-x-axis.html',
    styleUrl: './fd-ui-line-chart-x-axis.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiLineChartXAxisComponent {
    public readonly show = input(false);
    public readonly labels = input<readonly FdUiLineChartXAxisLabelView[]>([]);
    public readonly layout = input<FdUiLineChartXAxisLabelLayout>('angled');

    protected readonly stackedLineCount = computed(() => Math.max(1, ...this.labels().map(label => label.label.split('\n').length)));
}

import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { FdUiChartLegendComponent, type FdUiChartLegendItem } from '../chart-legend/fd-ui-chart-legend';

export type FdUiLineChartLegendSeries = FdUiChartLegendItem;

@Component({
    selector: 'fd-ui-line-chart-legend',
    imports: [FdUiChartLegendComponent],
    templateUrl: './fd-ui-line-chart-legend.html',
    styleUrl: './fd-ui-line-chart-legend.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiLineChartLegendComponent {
    public readonly show = input(false);
    public readonly series = input<readonly FdUiLineChartLegendSeries[]>([]);
}

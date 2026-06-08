import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type FdUiLineChartLegendSeries = {
    label: string;
    color: string;
};

@Component({
    selector: 'fd-ui-line-chart-legend',
    imports: [],
    templateUrl: './fd-ui-line-chart-legend.html',
    styleUrl: './fd-ui-line-chart-legend.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiLineChartLegendComponent {
    public readonly show = input(false);
    public readonly series = input<readonly FdUiLineChartLegendSeries[]>([]);
}

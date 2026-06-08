import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type FdUiLineChartYAxisGridLine = {
    label: string;
    yPercent: string;
};

@Component({
    selector: 'fd-ui-line-chart-y-axis',
    imports: [],
    templateUrl: './fd-ui-line-chart-y-axis.html',
    styleUrl: './fd-ui-line-chart-y-axis.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiLineChartYAxisComponent {
    public readonly show = input(false);
    public readonly gridLines = input<readonly FdUiLineChartYAxisGridLine[]>([]);
}

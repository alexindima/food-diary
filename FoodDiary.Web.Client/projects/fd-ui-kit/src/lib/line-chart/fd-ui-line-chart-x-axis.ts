import { ChangeDetectionStrategy, Component, input } from '@angular/core';

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
}

import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FdUiBarChartComponent, type FdUiBarChartItem, FdUiPieChartComponent, type FdUiPieChartSegment } from 'fd-ui-kit';

@Component({
    selector: 'fd-nutrients-summary-charts',
    imports: [FdUiPieChartComponent, FdUiBarChartComponent],
    templateUrl: './nutrients-summary-charts.html',
    styleUrl: '../nutrients-summary.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NutrientsSummaryChartsComponent {
    public readonly showPieChart = input.required<boolean>();
    public readonly showBarChart = input.required<boolean>();
    public readonly isColumnLayout = input.required<boolean>();
    public readonly chartsBlockSize = input.required<number>();
    public readonly chartsWrapperStyles = input.required<Record<string, string>>();
    public readonly chartStyles = input.required<Record<string, string>>();
    public readonly pieSegments = input.required<readonly FdUiPieChartSegment[]>();
    public readonly barItems = input.required<readonly FdUiBarChartItem[]>();
}

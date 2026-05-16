import { NgStyle } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import type { ChartData, ChartOptions } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';

@Component({
    selector: 'fd-nutrients-summary-charts',
    imports: [BaseChartDirective, NgStyle],
    templateUrl: './nutrients-summary-charts.component.html',
    styleUrl: '../nutrients-summary.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NutrientsSummaryChartsComponent {
    public readonly showPieChart = input.required<boolean>();
    public readonly showBarChart = input.required<boolean>();
    public readonly isColumnLayout = input.required<boolean>();
    public readonly chartsBlockSize = input.required<number>();
    public readonly chartsWrapperStyles = input.required<Record<string, string>>();
    public readonly chartStyles = input.required<Record<string, string>>();
    public readonly chartCanvasStyles = input.required<Record<string, string>>();
    public readonly pieChartData = input.required<ChartData<'pie', number[], string>>();
    public readonly barChartData = input.required<ChartData<'bar', number[], string>>();
    public readonly pieChartOptions = input.required<ChartOptions<'pie'>>();
    public readonly barChartOptions = input.required<ChartOptions<'bar'>>();
}

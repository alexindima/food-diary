import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import type { ChartConfiguration, ChartOptions } from 'chart.js';
import { FdUiSectionStateComponent } from 'fd-ui-kit';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { type FdUiTab, FdUiTabsComponent } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { BaseChartDirective } from 'ng2-charts';

@Component({
    selector: 'fd-statistics-nutrition',
    standalone: true,
    imports: [CommonModule, TranslateModule, FdUiSectionStateComponent, FdUiCardComponent, FdUiTabsComponent, BaseChartDirective],
    templateUrl: './statistics-nutrition.component.html',
    styleUrls: ['./statistics-nutrition.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsNutritionComponent {
    protected readonly emptyLineChartData: ChartConfiguration<'line'>['data'] = { labels: [], datasets: [] };
    protected readonly emptyPieChartData: ChartConfiguration<'pie'>['data'] = { labels: [], datasets: [] };
    protected readonly emptyRadarChartData: ChartConfiguration<'radar'>['data'] = { labels: [], datasets: [] };
    protected readonly emptyBarChartData: ChartConfiguration<'bar'>['data'] = { labels: [], datasets: [] };

    public readonly tabs = input.required<FdUiTab[]>();
    public readonly selectedTab = input.required<string>();
    public readonly hasData = input.required<boolean>();

    public readonly caloriesLineChartData = input.required<ChartConfiguration<'line'>['data'] | null>();
    public readonly caloriesLineChartOptions = input.required<ChartConfiguration['options'] | null>();

    public readonly nutrientsLineChartData = input.required<ChartConfiguration<'line'>['data'] | null>();
    public readonly nutrientsLineChartOptions = input.required<ChartConfiguration['options'] | null>();

    public readonly nutrientsPieChartData = input.required<ChartConfiguration<'pie'>['data'] | null>();
    public readonly pieChartOptions = input.required<ChartOptions<'pie'> | null>();

    public readonly nutrientsRadarChartData = input.required<ChartConfiguration<'radar'>['data'] | null>();
    public readonly radarChartOptions = input.required<ChartOptions<'radar'> | null>();

    public readonly nutrientsBarChartData = input.required<ChartConfiguration<'bar'>['data'] | null>();
    public readonly barChartOptions = input.required<ChartOptions<'bar'> | null>();

    public readonly selectedTabChange = output<string>();

    public onTabChange(value: string): void {
        this.selectedTabChange.emit(value);
    }
}

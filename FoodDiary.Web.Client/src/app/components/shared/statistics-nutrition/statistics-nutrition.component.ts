import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiTabsComponent, FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartOptions } from 'chart.js';

@Component({
    selector: 'fd-statistics-nutrition',
    standalone: true,
    imports: [CommonModule, TranslateModule, FdUiCardComponent, FdUiTabsComponent, BaseChartDirective],
    templateUrl: './statistics-nutrition.component.html',
    styleUrls: ['./statistics-nutrition.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsNutritionComponent {
    public readonly tabs = input.required<FdUiTab[]>();
    public readonly selectedTab = input<string>('calories');
    public readonly hasData = input<boolean>(false);

    public readonly caloriesLineChartData = input<ChartConfiguration<'line'>['data'] | null>(null);
    public readonly caloriesLineChartOptions = input<ChartConfiguration['options'] | null>(null);

    public readonly nutrientsLineChartData = input<ChartConfiguration<'line'>['data'] | null>(null);
    public readonly nutrientsLineChartOptions = input<ChartConfiguration['options'] | null>(null);

    public readonly nutrientsPieChartData = input<ChartConfiguration<'pie'>['data'] | null>(null);
    public readonly pieChartOptions = input<ChartOptions<'pie'> | null>(null);

    public readonly nutrientsRadarChartData = input<ChartConfiguration<'radar'>['data'] | null>(null);
    public readonly radarChartOptions = input<ChartOptions<'radar'> | null>(null);

    public readonly nutrientsBarChartData = input<ChartConfiguration<'bar'>['data'] | null>(null);
    public readonly barChartOptions = input<ChartOptions<'bar'> | null>(null);

    public readonly selectedTabChange = output<string>();

    public onTabChange(value: string): void {
        this.selectedTabChange.emit(value);
    }
}

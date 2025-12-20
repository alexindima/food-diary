import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiTabsComponent, FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';

@Component({
    selector: 'fd-statistics-body',
    standalone: true,
    imports: [CommonModule, TranslateModule, FdUiCardComponent, FdUiTabsComponent, BaseChartDirective],
    templateUrl: './statistics-body.component.html',
    styleUrls: ['./statistics-body.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsBodyComponent {
    public readonly tabs = input.required<FdUiTab[]>();
    public readonly selectedTab = input<string>('weight');
    public readonly isLoading = input<boolean>(false);
    public readonly bodyChartData = input<ChartConfiguration<'line'>['data'] | null>(null);
    public readonly bodyChartOptions = input<ChartConfiguration['options'] | null>(null);
    public readonly hasBodyData = input<boolean>(false);
    public readonly noDataKey = input<string>('STATISTICS.BODY_NO_DATA');

    public readonly selectedTabChange = output<string>();

    public onTabChange(value: string): void {
        this.selectedTabChange.emit(value);
    }
}

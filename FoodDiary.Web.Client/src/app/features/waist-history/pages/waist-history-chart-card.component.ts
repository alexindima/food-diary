import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import type { ChartConfiguration } from 'chart.js';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { BaseChartDirective } from 'ng2-charts';

@Component({
    selector: 'fd-waist-history-chart-card',
    imports: [BaseChartDirective, FdUiCardComponent, FdUiLoaderComponent, TranslatePipe],
    templateUrl: './waist-history-chart-card.component.html',
    styleUrl: './waist-history-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistHistoryChartCardComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly hasPoints = input.required<boolean>();
    public readonly chartData = input.required<ChartConfiguration<'line'>['data']>();
    public readonly chartOptions = input.required<ChartConfiguration<'line'>['options']>();
}

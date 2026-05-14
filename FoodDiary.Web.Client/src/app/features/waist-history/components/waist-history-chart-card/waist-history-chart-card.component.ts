import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import type { ChartConfiguration } from 'chart.js';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { BaseChartDirective } from 'ng2-charts';

import { WAIST_HISTORY_CHART_OPTIONS } from '../../lib/waist-history-chart.mapper';

@Component({
    selector: 'fd-waist-history-chart-card',
    imports: [BaseChartDirective, FdUiCardComponent, FdUiLoaderComponent, TranslatePipe],
    templateUrl: './waist-history-chart-card.component.html',
    styleUrl: '../../pages/waist-history-page/waist-history-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistHistoryChartCardComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly chartData = input.required<ChartConfiguration<'line'>['data']>();
    public readonly hasPoints = computed(() => (this.chartData().labels?.length ?? 0) > 0);
    public readonly chartOptions = WAIST_HISTORY_CHART_OPTIONS;
}

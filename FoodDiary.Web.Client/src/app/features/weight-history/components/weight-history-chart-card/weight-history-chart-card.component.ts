import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import type { ChartConfiguration } from 'chart.js';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { BaseChartDirective } from 'ng2-charts';

import { WEIGHT_HISTORY_CHART_OPTIONS } from '../../lib/weight-history-chart.mapper';

@Component({
    selector: 'fd-weight-history-chart-card',
    imports: [BaseChartDirective, FdUiCardComponent, FdUiLoaderComponent, TranslatePipe],
    templateUrl: './weight-history-chart-card.component.html',
    styleUrl: '../../pages/weight-history-page/weight-history-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryChartCardComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly chartData = input.required<ChartConfiguration<'line'>['data']>();
    public readonly hasPoints = computed(() => (this.chartData().labels?.length ?? 0) > 0);
    public readonly chartOptions = WEIGHT_HISTORY_CHART_OPTIONS;
}

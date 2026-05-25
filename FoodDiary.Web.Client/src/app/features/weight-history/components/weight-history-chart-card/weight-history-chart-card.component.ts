import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent, FdUiLineChartComponent } from 'fd-ui-kit';

import type { WeightHistoryChartPoint } from '../../lib/weight-history-chart.mapper';

@Component({
    selector: 'fd-weight-history-chart-card',
    imports: [FdUiCardComponent, FdUiLineChartComponent, TranslatePipe],
    templateUrl: './weight-history-chart-card.component.html',
    styleUrl: '../../pages/weight-history-page/weight-history-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryChartCardComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly chartPoints = input.required<readonly WeightHistoryChartPoint[]>();
    public readonly hasPoints = computed(() => this.chartPoints().some(point => point.value !== null));
}

import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent, FdUiLineChartComponent } from 'fd-ui-kit';

import type { WaistHistoryChartPoint } from '../../lib/waist-history-chart.mapper';

@Component({
    selector: 'fd-waist-history-chart-card',
    imports: [FdUiCardComponent, FdUiLineChartComponent, TranslatePipe],
    templateUrl: './waist-history-chart-card.component.html',
    styleUrl: '../../pages/waist-history-page/waist-history-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistHistoryChartCardComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly chartPoints = input.required<readonly WaistHistoryChartPoint[]>();
    public readonly hasPoints = computed(() => this.chartPoints().some(point => point.value !== null));
}

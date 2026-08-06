import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent, FdUiLineChartComponent, type FdUiLineChartReferenceLine } from 'fd-ui-kit';

import type { WaistHistoryChartPoint } from '../../lib/waist-history-chart.mapper';

@Component({
    selector: 'fd-waist-history-chart-card',
    imports: [FdUiCardComponent, FdUiLineChartComponent, TranslatePipe],
    templateUrl: './waist-history-chart-card.html',
    styleUrl: '../../pages/waist-history-page/waist-history-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistHistoryChartCardComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly chartPoints = input.required<readonly WaistHistoryChartPoint[]>();
    public readonly desiredWaist = input.required<number | null>();
    public readonly goalLabel = input.required<string>();
    protected readonly hasPoints = computed(() => this.chartPoints().some(point => point.value !== null));
    protected readonly referenceLines = computed<readonly FdUiLineChartReferenceLine[]>(() => {
        const desiredWaist = this.desiredWaist();
        return desiredWaist === null ? [] : [{ value: desiredWaist, label: this.goalLabel() }];
    });
}

import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent, FdUiLineChartComponent, type FdUiLineChartReferenceLine } from 'fd-ui-kit';

import type { WeightHistoryChartPoint } from '../../lib/weight-history-chart.mapper';

@Component({
    selector: 'fd-weight-history-chart-card',
    imports: [FdUiCardComponent, FdUiLineChartComponent, TranslatePipe],
    templateUrl: './weight-history-chart-card.html',
    styleUrl: '../../pages/weight-history-page/weight-history-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryChartCardComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly chartPoints = input.required<readonly WeightHistoryChartPoint[]>();
    public readonly desiredWeightKg = input.required<number | null>();
    public readonly goalLabel = input.required<string>();
    protected readonly hasPoints = computed(() => this.chartPoints().some(point => point.value !== null));
    protected readonly referenceLines = computed<readonly FdUiLineChartReferenceLine[]>(() => {
        const desiredWeightKg = this.desiredWeightKg();
        return desiredWeightKg === null ? [] : [{ value: desiredWeightKg, label: this.goalLabel() }];
    });
}

import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent, FdUiLineChartComponent, type FdUiLineChartReferenceLine } from 'fd-ui-kit';

import { MeasurementUnitPipe } from '../../../../shared/measurements/measurement-display.pipe';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import type { WeightHistoryChartPoint } from '../../lib/weight-history-chart.mapper';

@Component({
    selector: 'fd-weight-history-chart-card',
    imports: [FdUiCardComponent, FdUiLineChartComponent, MeasurementUnitPipe, TranslatePipe],
    templateUrl: './weight-history-chart-card.html',
    styleUrl: '../../pages/weight-history-page/weight-history-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryChartCardComponent {
    protected readonly measurements = inject(MeasurementSystemService);
    public readonly isLoading = input.required<boolean>();
    public readonly chartPoints = input.required<readonly WeightHistoryChartPoint[]>();
    public readonly desiredWeightKg = input.required<number | null>();
    public readonly goalLabel = input.required<string>();
    protected readonly displayChartPoints = computed(() =>
        this.chartPoints().map(point => ({
            ...point,
            value: point.value === null ? null : this.measurements.displayWeight(point.value),
        })),
    );
    protected readonly hasPoints = computed(() => this.displayChartPoints().some(point => point.value !== null));
    protected readonly referenceLines = computed<readonly FdUiLineChartReferenceLine[]>(() => {
        const desiredWeightKg = this.desiredWeightKg();
        return desiredWeightKg === null ? [] : [{ value: this.measurements.displayWeight(desiredWeightKg), label: this.goalLabel() }];
    });
}

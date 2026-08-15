import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { MeasurementSystemService } from '../../../../../shared/measurements/measurement-system.service';
import type { TdeeInsight } from '../../../models/tdee-insight.data';

@Component({
    selector: 'fd-tdee-insight-dialog-metrics',
    imports: [DecimalPipe, TranslatePipe],
    templateUrl: './tdee-insight-dialog-metrics.html',
    styleUrl: '../tdee-insight-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TdeeInsightDialogMetricsComponent {
    protected readonly measurements = inject(MeasurementSystemService);
    public readonly insight = input.required<TdeeInsight>();
    public readonly weightTrendFormatted = input.required<string | null>();
    protected readonly displayWeightTrend = computed(() => {
        const value = Number(this.weightTrendFormatted());
        return Number.isFinite(value) ? this.measurements.displayWeight(value).toFixed(1) : null;
    });
}

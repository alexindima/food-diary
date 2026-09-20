import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import {
    FdUiButtonComponent,
    FdUiCardComponent,
    FdUiEmptyStateComponent,
    FdUiLineChartComponent,
    type FdUiLineChartReferenceLine,
} from 'fd-ui-kit';

import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedDatePipe } from '../../../../shared/i18n/localized-date.pipe';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import { MeasurementUnitPipe, MeasurementValuePipe } from '../../../../shared/measurements/measurement-display.pipe';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import type { WeightHistoryChartPoint } from '../../lib/weight-history-chart.mapper';

@Component({
    selector: 'fd-weight-history-chart-card',
    imports: [
        FdUiCardComponent,
        FdUiLineChartComponent,
        FdUiButtonComponent,
        FdUiEmptyStateComponent,
        MeasurementUnitPipe,
        MeasurementValuePipe,
        LocalizedNumberPipe,
        LocalizedDatePipe,
        TranslatePipe,
    ],
    templateUrl: './weight-history-chart-card.html',
    styleUrl: '../../pages/weight-history-page/weight-history-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryChartCardComponent {
    protected readonly locale = injectCurrentLanguage();
    public readonly latestValue = input<number | null>(null);
    public readonly latestDate = input<string | null>(null);
    public readonly addEntry = output();
    public readonly showLatest = output();
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

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
import type { WaistHistoryChartPoint } from '../../lib/waist-history-chart.mapper';

@Component({
    selector: 'fd-waist-history-chart-card',
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
    templateUrl: './waist-history-chart-card.html',
    styleUrl: '../../pages/waist-history-page/waist-history-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistHistoryChartCardComponent {
    protected readonly locale = injectCurrentLanguage();
    public readonly latestValue = input<number | null>(null);
    public readonly latestDate = input<string | null>(null);
    public readonly addEntry = output();
    public readonly showLatest = output();
    protected readonly measurements = inject(MeasurementSystemService);
    public readonly isLoading = input.required<boolean>();
    public readonly chartPoints = input.required<readonly WaistHistoryChartPoint[]>();
    public readonly desiredWaistCm = input.required<number | null>();
    public readonly goalLabel = input.required<string>();
    protected readonly displayChartPoints = computed(() =>
        this.chartPoints().map(point => ({
            ...point,
            value: point.value === null ? null : this.measurements.displayLength(point.value),
        })),
    );
    protected readonly hasPoints = computed(() => this.displayChartPoints().some(point => point.value !== null));
    protected readonly referenceLines = computed<readonly FdUiLineChartReferenceLine[]>(() => {
        const desiredWaistCm = this.desiredWaistCm();
        return desiredWaistCm === null ? [] : [{ value: this.measurements.displayLength(desiredWaistCm), label: this.goalLabel() }];
    });
}

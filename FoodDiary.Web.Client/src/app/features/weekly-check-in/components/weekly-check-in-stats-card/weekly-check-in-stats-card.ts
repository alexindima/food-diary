import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import type { WeekSummary } from '../../models/weekly-check-in.data';

@Component({
    selector: 'fd-weekly-check-in-stats-card',
    imports: [DecimalPipe, TranslatePipe, FdUiCardComponent],
    templateUrl: './weekly-check-in-stats-card.html',
    styleUrl: '../../pages/weekly-check-in-page/weekly-check-in-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeeklyCheckInStatsCardComponent {
    protected readonly measurements = inject(MeasurementSystemService);
    public readonly week = input<WeekSummary | undefined>();
    protected readonly displayWeight = computed(() => {
        const weightKg = this.week()?.weightEnd;
        return weightKg === null || weightKg === undefined ? null : this.measurements.displayWeight(weightKg);
    });
}

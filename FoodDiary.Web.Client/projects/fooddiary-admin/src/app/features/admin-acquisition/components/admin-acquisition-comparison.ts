import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent, FdUiLineChartComponent } from 'fd-ui-kit';

import { acquisitionTrend } from '../lib/admin-acquisition-trend';
import type { MarketingAttributionRange } from '../models/admin-acquisition-range';

@Component({
    selector: 'fd-admin-acquisition-comparison',
    imports: [DatePipe, DecimalPipe, TranslatePipe, FdUiCardComponent, FdUiLineChartComponent],
    templateUrl: './admin-acquisition-comparison.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: { class: 'fd-grid fd-gap-md' },
})
export class AdminAcquisitionComparisonComponent {
    public readonly report = input.required<MarketingAttributionRange | null>();
    private readonly trend = computed(() => {
        const report = this.report();
        return report === null ? [] : acquisitionTrend(report);
    });
    protected readonly metrics = computed(() => {
        const report = this.report();
        if (report === null) {
            return [];
        }
        return (['visits', 'signups', 'premiumStarts'] as const).map(key => ({
            key,
            current: report.current[key],
            previous: report.previous[key],
            delta: report.current[key] - report.previous[key],
            points: this.trend().map(day => ({ label: day.date, value: day[key] })),
        }));
    });
}

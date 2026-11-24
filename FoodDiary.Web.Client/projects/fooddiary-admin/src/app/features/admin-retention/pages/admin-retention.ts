import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { adminPeriod, adminPeriodParams } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { AdminRetentionFacade } from '../lib/admin-retention.facade';

const PERCENT_SCALE = 100;

@Component({
    selector: 'fd-admin-retention',
    imports: [DatePipe, TranslatePipe, FdUiCardComponent, AdminPeriodControlComponent, AdminLoadErrorComponent],
    templateUrl: './admin-retention.html',
    styleUrl: './admin-retention.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: { class: 'fd-grid fd-gap-md' },
})
export class AdminRetentionComponent {
    private readonly api = inject(AdminRetentionFacade);
    private readonly translate = inject(TranslateService);
    private readonly query = toSignal(inject(ActivatedRoute).queryParamMap, { requireSync: true });
    private readonly retryVersion = signal(0);
    private readonly reportResource = rxResource({
        params: () => {
            this.retryVersion();
            const query = this.query();
            const range = adminPeriod(adminPeriodParams(query, 'activity_'), '30d');
            const cohorts = adminPeriod(query, '90d');
            if (range === null || cohorts === null) {
                return;
            }
            return {
                ...range,
                from: range.from ?? '1970-01-01',
                cohortFrom: cohorts.from ?? '1970-01-01',
                cohortTo: cohorts.to,
            };
        },
        stream: ({ params }) => this.api.getReport(params),
    });

    protected readonly report = computed(() => (this.reportResource.hasValue() ? this.reportResource.value() : null));
    protected readonly loading = this.reportResource.isLoading;
    protected readonly failed = computed(() => this.reportResource.error() !== undefined);

    protected retry(): void {
        this.retryVersion.update(version => version + 1);
    }

    protected ratio(value: number | null, total: number): string {
        if (value === null) {
            return String(this.translate.instant('ADMIN_RETENTION.IMMATURE'));
        }
        return total === 0 ? '—' : `${((value / total) * PERCENT_SCALE).toFixed(1)}% (${value}/${total})`;
    }
}

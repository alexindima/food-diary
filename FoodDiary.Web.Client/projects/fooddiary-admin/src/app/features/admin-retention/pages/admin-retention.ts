import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { convertToParamMap } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { adminPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { firstAdminQueryValue } from '../../../shared/period/admin-query';
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
    public readonly period = input(undefined, { transform: firstAdminQueryValue });
    public readonly from = input(undefined, { transform: firstAdminQueryValue });
    public readonly to = input(undefined, { transform: firstAdminQueryValue });
    public readonly activity_period = input(undefined, { transform: firstAdminQueryValue });
    public readonly activity_from = input(undefined, { transform: firstAdminQueryValue });
    public readonly activity_to = input(undefined, { transform: firstAdminQueryValue });

    private readonly api = inject(AdminRetentionFacade);
    private readonly translate = inject(TranslateService);
    private readonly activityPeriod = computed(() =>
        adminPeriod(
            convertToParamMap({
                period: this.activity_period() ?? null,
                from: this.activity_from() ?? null,
                to: this.activity_to() ?? null,
            }),
            '30d',
        ),
    );
    private readonly cohortPeriod = computed(() =>
        adminPeriod(convertToParamMap({ period: this.period() ?? null, from: this.from() ?? null, to: this.to() ?? null }), '90d'),
    );
    private readonly retryVersion = signal(0);
    private readonly reportResource = rxResource({
        params: () => {
            this.retryVersion();
            const range = this.activityPeriod();
            const cohorts = this.cohortPeriod();
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

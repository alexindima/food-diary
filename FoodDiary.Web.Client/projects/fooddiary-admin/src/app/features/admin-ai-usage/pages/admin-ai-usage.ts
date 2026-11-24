import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { convertToParamMap, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCardComponent, FdUiLineChartComponent } from 'fd-ui-kit';

import { adminExclusiveDatePeriod, adminPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { firstAdminQueryValue } from '../../../shared/period/admin-query';
import { AdminAiUsageFacade } from '../lib/admin-ai-usage.facade';

@Component({
    selector: 'fd-admin-ai-usage',
    imports: [
        CommonModule,
        FdUiCardComponent,
        FdUiButtonComponent,
        FdUiLineChartComponent,
        AdminPeriodControlComponent,
        TranslatePipe,
        RouterLink,
    ],
    templateUrl: './admin-ai-usage.html',
    styleUrl: './admin-ai-usage.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminAiUsageComponent {
    public readonly userId = input(undefined, { transform: firstAdminQueryValue });
    public readonly period = input(undefined, { transform: firstAdminQueryValue });
    public readonly from = input(undefined, { transform: firstAdminQueryValue });
    public readonly to = input(undefined, { transform: firstAdminQueryValue });

    private readonly aiUsageFacade = inject(AdminAiUsageFacade);
    private readonly retryVersion = signal(0);
    private readonly usageResource = rxResource({
        params: () => {
            this.retryVersion();
            const range = adminPeriod(
                convertToParamMap({ period: this.period() ?? null, from: this.from() ?? null, to: this.to() ?? null }),
            );
            if (range === null) {
                return;
            }
            const userId = this.userId();
            return { ...adminExclusiveDatePeriod(range), ...(userId === undefined ? {} : { userId }) };
        },
        stream: ({ params }) => this.aiUsageFacade.getSummary(params),
    });

    protected readonly isLoading = this.usageResource.isLoading;
    protected readonly failed = computed(() => this.usageResource.error() !== undefined);
    protected readonly report = computed(() => {
        if (!this.usageResource.hasValue()) {
            return null;
        }
        const usage = this.usageResource.value();
        return {
            ...usage,
            points: usage.byDay.map(day => ({ label: day.date, value: day.totalTokens })),
            breakdowns: [
                { title: 'ADMIN_AI.OPERATIONS', rows: usage.byOperation },
                { title: 'ADMIN_AI.MODELS', rows: usage.byModel },
            ],
        };
    });

    protected loadUsage(): void {
        this.retryVersion.update(version => version + 1);
    }
}

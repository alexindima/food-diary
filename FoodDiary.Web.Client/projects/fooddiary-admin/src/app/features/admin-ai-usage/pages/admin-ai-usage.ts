import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCardComponent, FdUiLineChartComponent } from 'fd-ui-kit';

import { adminExclusiveDatePeriod, adminPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
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
    private readonly aiUsageFacade = inject(AdminAiUsageFacade);
    private readonly query = toSignal(inject(ActivatedRoute).queryParamMap, { requireSync: true });
    private readonly retryVersion = signal(0);
    protected readonly userId = computed(() => this.query().get('userId'));
    private readonly usageResource = rxResource({
        params: () => {
            this.retryVersion();
            const range = adminPeriod(this.query());
            if (range === null) {
                return;
            }
            const userId = this.userId();
            return { ...adminExclusiveDatePeriod(range), ...(userId === null ? {} : { userId }) };
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

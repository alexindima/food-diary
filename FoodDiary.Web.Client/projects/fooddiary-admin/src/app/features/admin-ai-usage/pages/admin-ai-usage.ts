import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCardComponent, FdUiLineChartComponent } from 'fd-ui-kit';

import { adminExclusiveDatePeriod, adminPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { AdminAiUsageFacade } from '../lib/admin-ai-usage.facade';
import type { AdminAiUsageSummary } from '../models/admin-ai-usage.data';

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
    private readonly destroyRef = inject(DestroyRef);
    private readonly route = inject(ActivatedRoute);
    private requestId = 0;

    protected readonly usage = signal<AdminAiUsageSummary | null>(null);
    protected readonly isLoading = signal(false);
    protected readonly userId = signal<string | null>(null);
    protected readonly failed = signal(false);
    protected readonly points = computed(() => (this.usage()?.byDay ?? []).map(day => ({ label: day.date, value: day.totalTokens })));
    protected readonly breakdowns = computed(() => [
        { title: 'ADMIN_AI.OPERATIONS', rows: this.usage()?.byOperation ?? [] },
        { title: 'ADMIN_AI.MODELS', rows: this.usage()?.byModel ?? [] },
    ]);

    public constructor() {
        this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(() => {
            this.loadUsage();
        });
    }

    protected loadUsage(): void {
        const requestId = ++this.requestId;
        const userId = this.route.snapshot.queryParamMap.get('userId');
        this.userId.set(userId);
        const range = adminPeriod(this.route.snapshot.queryParamMap);
        this.failed.set(false);
        if (range === null) {
            this.usage.set(null);
            this.isLoading.set(false);
            return;
        }
        this.isLoading.set(true);
        this.aiUsageFacade
            .getSummary({ ...adminExclusiveDatePeriod(range), ...(userId === null ? {} : { userId }) })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    if (requestId !== this.requestId) {
                        return;
                    }
                    this.usage.set(response);
                    this.isLoading.set(false);
                },
                error: () => {
                    if (requestId !== this.requestId) {
                        return;
                    }
                    this.failed.set(true);
                    this.usage.set(null);
                    this.isLoading.set(false);
                },
            });
    }
}

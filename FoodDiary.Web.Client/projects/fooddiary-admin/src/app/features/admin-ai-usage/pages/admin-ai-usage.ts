import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import { AdminAiUsageFacade } from '../lib/admin-ai-usage.facade';
import type { AdminAiUsageSummary } from '../models/admin-ai-usage.data';

@Component({
    selector: 'fd-admin-ai-usage',
    imports: [CommonModule, FdUiCardComponent],
    templateUrl: './admin-ai-usage.html',
    styleUrl: './admin-ai-usage.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminAiUsageComponent {
    private readonly aiUsageFacade = inject(AdminAiUsageFacade);
    private readonly destroyRef = inject(DestroyRef);

    protected readonly usage = signal<AdminAiUsageSummary | null>(null);
    protected readonly isLoading = signal(false);

    public constructor() {
        this.loadUsage();
    }

    protected loadUsage(): void {
        this.isLoading.set(true);
        this.aiUsageFacade
            .getSummary()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.usage.set(response);
                    this.isLoading.set(false);
                },
                error: () => {
                    this.usage.set(null);
                    this.isLoading.set(false);
                },
            });
    }
}

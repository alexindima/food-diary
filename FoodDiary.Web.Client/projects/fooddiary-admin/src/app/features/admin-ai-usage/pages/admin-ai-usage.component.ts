import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import { AdminAiUsageService } from '../api/admin-ai-usage.service';
import { AdminAiUsageSummary } from '../models/admin-ai-usage.data';

@Component({
    selector: 'fd-admin-ai-usage',
    standalone: true,
    imports: [CommonModule, FdUiCardComponent],
    templateUrl: './admin-ai-usage.component.html',
    styleUrl: './admin-ai-usage.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminAiUsageComponent {
    private readonly aiUsageService = inject(AdminAiUsageService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly usage = signal<AdminAiUsageSummary | null>(null);
    public readonly isLoading = signal(false);

    public constructor() {
        this.loadUsage();
    }

    public loadUsage(): void {
        this.isLoading.set(true);
        this.aiUsageService
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

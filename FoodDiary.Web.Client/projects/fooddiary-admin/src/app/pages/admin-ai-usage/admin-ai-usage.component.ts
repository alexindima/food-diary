import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { AdminDashboardService, AdminAiUsageSummary } from '../admin-dashboard/admin-dashboard.service';

@Component({
  selector: 'fd-admin-ai-usage',
  standalone: true,
  imports: [CommonModule, FdUiCardComponent],
  templateUrl: './admin-ai-usage.component.html',
  styleUrl: './admin-ai-usage.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminAiUsageComponent {
  private readonly dashboardService = inject(AdminDashboardService);
  private readonly destroyRef = inject(DestroyRef);

  public readonly usage = signal<AdminAiUsageSummary | null>(null);
  public readonly isLoading = signal(false);

  public constructor() {
    this.loadUsage();
  }

  public loadUsage(): void {
    this.isLoading.set(true);
    this.dashboardService
      .getAiUsageSummary()
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

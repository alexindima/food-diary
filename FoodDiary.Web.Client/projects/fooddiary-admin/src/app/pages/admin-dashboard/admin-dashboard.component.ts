import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { AdminDashboardService, AdminDashboardSummary } from './admin-dashboard.service';

@Component({
  selector: 'fd-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FdUiCardComponent],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminDashboardComponent {
  private readonly dashboardService = inject(AdminDashboardService);
  private readonly destroyRef = inject(DestroyRef);

  public readonly summary = signal<AdminDashboardSummary | null>(null);
  public readonly isLoading = signal(false);

  public constructor() {
    this.loadSummary();
  }

  public loadSummary(): void {
    this.isLoading.set(true);
    this.dashboardService
      .getSummary()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: response => {
          this.summary.set(response);
          this.isLoading.set(false);
        },
        error: () => {
          this.summary.set(null);
          this.isLoading.set(false);
        },
      });
  }
}

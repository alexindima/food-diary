import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { Subscription } from 'rxjs';

import { AdminDashboardService } from '../api/admin-dashboard.service';
import type { AdminDashboardOverview, DashboardRange } from '../models/admin-dashboard-overview.data';

@Injectable()
export class AdminDashboardFacade {
    private readonly service = inject(AdminDashboardService);
    private readonly destroyRef = inject(DestroyRef);
    private request?: Subscription;
    public readonly overview = signal<AdminDashboardOverview | null>(null);
    public readonly isLoading = signal(false);
    public readonly failed = signal(false);

    public clear(): void {
        this.request?.unsubscribe();
        this.overview.set(null);
        this.isLoading.set(false);
        this.failed.set(false);
    }

    public load(range: DashboardRange = {}): void {
        this.request?.unsubscribe();
        this.overview.set(null);
        this.isLoading.set(true);
        this.failed.set(false);
        this.request = this.service
            .getOverview(range)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: data => {
                    this.overview.set(data);
                    this.isLoading.set(false);
                },
                error: () => {
                    this.failed.set(true);
                    this.isLoading.set(false);
                },
            });
    }
}

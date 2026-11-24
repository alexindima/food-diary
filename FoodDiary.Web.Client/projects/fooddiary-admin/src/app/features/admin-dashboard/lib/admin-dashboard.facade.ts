import { computed, inject, Injectable, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';

import { AdminDashboardService } from '../api/admin-dashboard.service';
import type { DashboardRange } from '../models/admin-dashboard-overview.data';

@Injectable()
export class AdminDashboardFacade {
    private readonly service = inject(AdminDashboardService);
    private readonly requestedRange = signal<DashboardRange | undefined>(undefined);
    private readonly overviewResource = rxResource({
        params: this.requestedRange,
        stream: ({ params }) => this.service.getOverview(params),
    });

    public readonly overview = computed(() => (this.overviewResource.hasValue() ? this.overviewResource.value() : null));
    public readonly isLoading = this.overviewResource.isLoading;
    public readonly failed = computed(() => this.overviewResource.error() !== undefined);

    public clear(): void {
        this.requestedRange.set(undefined);
    }

    public load(range: DashboardRange = {}): void {
        // Refresh starts a new request for the applied range even while it is loading.
        this.requestedRange.set({ ...range });
    }
}

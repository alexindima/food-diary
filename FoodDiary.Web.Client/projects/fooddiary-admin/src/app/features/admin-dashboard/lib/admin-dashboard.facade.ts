import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminDashboardService } from '../api/admin-dashboard.service';
import type { AdminDashboardOverview, DashboardRange } from '../models/admin-dashboard-overview.data';

@Injectable()
export class AdminDashboardFacade {
    private readonly api = inject(AdminDashboardService);

    public getOverview(range: DashboardRange): Observable<AdminDashboardOverview> {
        return this.api.getOverview(range);
    }
}

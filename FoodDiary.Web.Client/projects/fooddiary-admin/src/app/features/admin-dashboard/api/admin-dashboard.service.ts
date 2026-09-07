import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type { AdminDashboardSummary } from '../models/admin-dashboard.data';
import type { AdminDashboardOverview, DashboardRange } from '../models/admin-dashboard-overview.data';

@Service()
export class AdminDashboardService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/dashboard`;

    public getSummary(): Observable<AdminDashboardSummary> {
        return this.http.get<AdminDashboardSummary>(this.baseUrl);
    }

    public getOverview(range: DashboardRange): Observable<AdminDashboardOverview> {
        const params: Record<string, string> = {};
        if (range.allTime === true) {
            params['allTime'] = 'true';
        } else {
            if (range.from !== undefined) {
                params['from'] = range.from;
            }
            if (range.to !== undefined) {
                params['to'] = range.to;
            }
        }
        return this.http.get<AdminDashboardOverview>(`${this.baseUrl}/overview`, { params });
    }
}

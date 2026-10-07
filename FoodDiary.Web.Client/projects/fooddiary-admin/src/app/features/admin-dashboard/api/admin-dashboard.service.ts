import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';
import { map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AdminDashboardSdk } from '../../../shared/api/sdk/generated/api/admin-dashboard.service';
import { createSdkConnection, sdkRequestOptions } from '../../../shared/api/sdk/sdk-connection';
import type { AdminDashboardSummary } from '../models/admin-dashboard.data';
import type { AdminDashboardOverview, DashboardRange } from '../models/admin-dashboard-overview.data';
import { adminDashboardOverviewFromSdk, adminDashboardSummaryFromSdk } from './admin-dashboard-sdk.mapper';

@Service()
export class AdminDashboardService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/dashboard`;
    private readonly sdk = createSdkConnection(AdminDashboardSdk, this.baseUrl, this.http);

    public getSummary(): Observable<AdminDashboardSummary> {
        return this.sdk.client.getAdminDashboard({ version: this.sdk.version }).pipe(map(adminDashboardSummaryFromSdk));
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
        return this.sdk.client
            .getAdminDashboardOverview({ version: this.sdk.version }, 'body', false, sdkRequestOptions(undefined, params))
            .pipe(map(adminDashboardOverviewFromSdk));
    }
}

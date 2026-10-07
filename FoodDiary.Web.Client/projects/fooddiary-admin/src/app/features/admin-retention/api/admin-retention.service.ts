import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';
import { map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AdminRetentionSdk } from '../../../shared/api/sdk/generated/api/admin-retention.service';
import { createSdkConnection, sdkRequestOptions } from '../../../shared/api/sdk/sdk-connection';
import type { AdminRetentionReport } from '../models/admin-retention';
import { adminRetentionReportFromSdk } from './admin-retention-sdk.mapper';

@Service()
export class AdminRetentionService {
    private readonly http = inject(HttpClient);
    private readonly url = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/analytics/retention`;
    private readonly sdk = createSdkConnection(AdminRetentionSdk, this.url, this.http);

    public getReport(params: { from?: string; to?: string; cohortFrom?: string; cohortTo?: string }): Observable<AdminRetentionReport> {
        let queryParams = new HttpParams();
        for (const key of ['from', 'to', 'cohortFrom', 'cohortTo'] as const) {
            const value = params[key];
            if (value !== undefined) {
                queryParams = queryParams.set(key, value);
            }
        }
        return this.sdk.client
            .getAdminAnalyticsRetention({ version: this.sdk.version }, 'body', false, sdkRequestOptions(undefined, queryParams))
            .pipe(map(adminRetentionReportFromSdk));
    }
}

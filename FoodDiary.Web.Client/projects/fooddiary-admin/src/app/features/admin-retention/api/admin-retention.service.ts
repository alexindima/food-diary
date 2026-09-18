import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type { AdminRetentionReport } from '../models/admin-retention';

@Service()
export class AdminRetentionService {
    private readonly http = inject(HttpClient);
    private readonly url = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/analytics/retention`;

    public getReport(params: { from?: string; to?: string; cohortFrom?: string; cohortTo?: string }): Observable<AdminRetentionReport> {
        let queryParams = new HttpParams();
        for (const key of ['from', 'to', 'cohortFrom', 'cohortTo'] as const) {
            const value = params[key];
            if (value !== undefined) {
                queryParams = queryParams.set(key, value);
            }
        }
        return this.http.get<AdminRetentionReport>(this.url, { params: queryParams });
    }
}

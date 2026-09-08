import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type { AdminRetentionReport } from '../models/admin-retention';

@Service()
export class AdminRetentionService {
    private readonly http = inject(HttpClient);
    private readonly url = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/analytics/retention`;

    public getReport(params: { from?: string; to?: string }): Observable<AdminRetentionReport> {
        return this.http.get<AdminRetentionReport>(this.url, { params });
    }
}

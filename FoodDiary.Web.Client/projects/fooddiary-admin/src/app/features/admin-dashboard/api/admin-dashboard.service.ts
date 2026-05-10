import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type { AdminDashboardSummary } from '../models/admin-dashboard.data';

@Injectable({ providedIn: 'root' })
export class AdminDashboardService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/dashboard`;

    public getSummary(): Observable<AdminDashboardSummary> {
        return this.http.get<AdminDashboardSummary>(this.baseUrl);
    }
}

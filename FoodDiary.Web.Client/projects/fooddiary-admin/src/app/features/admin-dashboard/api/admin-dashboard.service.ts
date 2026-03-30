import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AdminDashboardSummary } from '../models/admin-dashboard.data';

@Injectable({ providedIn: 'root' })
export class AdminDashboardService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/dashboard`;

    public getSummary(): Observable<AdminDashboardSummary> {
        return this.http.get<AdminDashboardSummary>(this.baseUrl);
    }
}

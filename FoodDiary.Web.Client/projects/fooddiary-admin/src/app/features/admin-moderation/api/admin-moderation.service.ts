import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type { AdminContentReport, AdminReportAction } from '../models/admin-moderation.data';

export type PagedResponse<T> = {
    items: T[];
    totalPages: number;
    totalItems: number;
};

type ApiPagedResponse<T> = {
    data: T[];
    page: number;
    limit: number;
    totalPages: number;
    totalItems: number;
};

@Injectable({ providedIn: 'root' })
export class AdminModerationService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/moderation`;

    public getReports(page: number, limit: number, status?: string | null): Observable<PagedResponse<AdminContentReport>> {
        let params = new HttpParams().set('page', page).set('limit', limit);
        if (status) {
            params = params.set('status', status);
        }

        return this.http.get<ApiPagedResponse<AdminContentReport>>(this.baseUrl, { params }).pipe(
            map(response => ({
                items: response.data,
                totalPages: response.totalPages,
                totalItems: response.totalItems,
            })),
        );
    }

    public reviewReport(reportId: string, action: AdminReportAction): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${reportId}/review`, action);
    }

    public dismissReport(reportId: string, action: AdminReportAction): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${reportId}/dismiss`, action);
    }
}

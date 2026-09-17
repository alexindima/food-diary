import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type {
    AdminDailyAdvice,
    AdminDailyAdvicesImportRequest,
    AdminDailyAdvicesImportResponse,
} from '../models/admin-daily-advice.models';

@Service()
export class AdminDailyAdvicesService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/daily-advices`;

    public getAll(): Observable<AdminDailyAdvice[]> {
        return this.http.get<AdminDailyAdvice[]>(this.baseUrl);
    }

    public importAdvices(request: AdminDailyAdvicesImportRequest): Observable<AdminDailyAdvicesImportResponse> {
        const headers = new HttpHeaders({ 'Idempotency-Key': crypto.randomUUID() });
        return this.http.post<AdminDailyAdvicesImportResponse>(`${this.baseUrl}/import`, request, { headers });
    }
}

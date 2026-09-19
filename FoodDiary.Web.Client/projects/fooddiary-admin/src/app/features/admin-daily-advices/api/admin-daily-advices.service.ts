import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type {
    AdminDailyAdvice,
    AdminDailyAdvicesImportRequest,
    AdminDailyAdvicesImportResponse,
    AdminDailyAdviceUpdate,
} from '../models/admin-daily-advice.models';

@Service()
export class AdminDailyAdvicesService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/daily-advices`;

    public getAll(): Observable<AdminDailyAdvice[]> {
        return this.http.get<AdminDailyAdvice[]>(`${this.baseUrl}/groups`);
    }

    public importAdvices(request: AdminDailyAdvicesImportRequest): Observable<AdminDailyAdvicesImportResponse> {
        const headers = new HttpHeaders({ 'Idempotency-Key': crypto.randomUUID() });
        return this.http.post<AdminDailyAdvicesImportResponse>(
            `${this.baseUrl}/${request.version === 2 ? 'groups/import' : 'import'}`,
            request,
            { headers },
        );
    }

    public update(id: string, request: AdminDailyAdviceUpdate): Observable<AdminDailyAdvice> {
        return this.http.put<AdminDailyAdvice>(`${this.baseUrl}/groups/${id}`, request);
    }

    public delete(id: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/groups/${id}`);
    }
}

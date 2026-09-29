import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { loadPagedCollection } from '../../../shared/api/load-paged-collection';
import type {
    AdminEmailTemplate,
    AdminEmailTemplateTestRequest,
    AdminEmailTemplateUpsertRequest,
} from '../models/admin-email-template.data';

@Service()
export class AdminEmailTemplatesService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/email-templates`;

    public getAll(): Observable<AdminEmailTemplate[]> {
        return loadPagedCollection((page, limit) => this.http.get<AdminEmailTemplate[]>(this.baseUrl, { params: { page, limit } }));
    }

    public upsert(key: string, locale: string, request: AdminEmailTemplateUpsertRequest): Observable<AdminEmailTemplate> {
        return this.http.put<AdminEmailTemplate>(`${this.baseUrl}/${key}/${locale}`, request);
    }

    public sendTest(request: AdminEmailTemplateTestRequest): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/test`, request);
    }
}

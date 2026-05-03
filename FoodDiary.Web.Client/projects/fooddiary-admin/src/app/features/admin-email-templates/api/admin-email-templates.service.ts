import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
    type AdminEmailTemplate,
    type AdminEmailTemplateTestRequest,
    type AdminEmailTemplateUpsertRequest,
} from '../models/admin-email-template.data';

@Injectable({ providedIn: 'root' })
export class AdminEmailTemplatesService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/email-templates`;

    public getAll(): Observable<AdminEmailTemplate[]> {
        return this.http.get<AdminEmailTemplate[]>(this.baseUrl);
    }

    public upsert(key: string, locale: string, request: AdminEmailTemplateUpsertRequest): Observable<AdminEmailTemplate> {
        return this.http.put<AdminEmailTemplate>(`${this.baseUrl}/${key}/${locale}`, request);
    }

    public sendTest(request: AdminEmailTemplateTestRequest): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/test`, request);
    }
}

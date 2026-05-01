import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AdminMailInboxMessageDetails, AdminMailInboxMessageSummary } from '../models/admin-mail-inbox.data';

@Injectable({ providedIn: 'root' })
export class AdminMailInboxService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/mail-inbox/messages`;

    public getMessages(limit: number): Observable<AdminMailInboxMessageSummary[]> {
        const params = new HttpParams().set('limit', limit);
        return this.http.get<AdminMailInboxMessageSummary[]>(this.baseUrl, { params });
    }

    public getMessage(id: string): Observable<AdminMailInboxMessageDetails> {
        return this.http.get<AdminMailInboxMessageDetails>(`${this.baseUrl}/${id}`);
    }
}

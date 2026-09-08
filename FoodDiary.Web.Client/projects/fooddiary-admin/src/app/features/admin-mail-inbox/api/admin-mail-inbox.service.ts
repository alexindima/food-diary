import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type {
    AdminMailInboxFilters,
    AdminMailInboxMessageDetails,
    AdminMailInboxMessagePage,
    AdminMailInboxMessageSummary,
} from '../models/admin-mail-inbox.data';

@Service()
export class AdminMailInboxService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/mail-inbox/messages`;

    public getMessagePage(page: number, limit: number, filters: AdminMailInboxFilters = {}): Observable<AdminMailInboxMessagePage> {
        const { recipient = '', category = '', unread } = filters;
        let params = new HttpParams().set('page', page).set('limit', limit);
        if (recipient.trim().length > 0) {
            params = params.set('recipient', recipient.trim());
        }
        if (category.length > 0) {
            params = params.set('category', category);
        }
        if (unread !== undefined) {
            params = params.set('unread', unread);
        }
        return this.http.get<AdminMailInboxMessagePage>(`${this.baseUrl}/page`, { params });
    }

    public getMessages(limit: number, recipient = '', category = '', unread?: boolean): Observable<AdminMailInboxMessageSummary[]> {
        let params = new HttpParams().set('limit', limit);
        if (recipient.trim().length > 0) {
            params = params.set('recipient', recipient.trim());
        }
        if (category.length > 0) {
            params = params.set('category', category);
        }
        if (unread !== undefined) {
            params = params.set('unread', unread);
        }
        return this.http.get<AdminMailInboxMessageSummary[]>(this.baseUrl, { params });
    }

    public getMessage(id: string): Observable<AdminMailInboxMessageDetails> {
        return this.http.get<AdminMailInboxMessageDetails>(`${this.baseUrl}/${id}`);
    }

    public markMessageRead(id: string): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${id}/read`, null);
    }
}

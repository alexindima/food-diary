import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type { OutgoingEmailPage } from '../models/outgoing-email';

const PAGE_SIZE = 50;

@Service()
export class AdminOutgoingEmailsService {
    private readonly http = inject(HttpClient);
    private readonly url = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/outgoing-emails`;

    public getPage(page: number, purpose: string, status: string, filters: Record<string, string> = {}): Observable<OutgoingEmailPage> {
        let params = new HttpParams()
            .set('page', page)
            .set('limit', PAGE_SIZE)
            .set('purpose', purpose)
            .set('status', status)
            .set('recipient', (filters['recipient'] ?? '').trim());
        for (const [key, value] of Object.entries(filters)) {
            if (value.length > 0) {
                params = params.set(key, value);
            }
        }
        return this.http.get<OutgoingEmailPage>(this.url, { params });
    }
}

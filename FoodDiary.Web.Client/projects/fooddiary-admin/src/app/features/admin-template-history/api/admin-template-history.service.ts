import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type { AdminTemplateRevision } from '../models/admin-template-revision';

@Service()
export class AdminTemplateHistoryService {
    private readonly http = inject(HttpClient);
    private readonly url = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin`;
    public getRevisions(kind: 'email-templates' | 'ai-prompts', key: string, locale: string): Observable<AdminTemplateRevision[]> {
        return this.http.get<AdminTemplateRevision[]>(
            `${this.url}/${kind}/${encodeURIComponent(key)}/${encodeURIComponent(locale)}/revisions`,
        );
    }
}

import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type { AdminAuditPageResult } from '../models/admin-audit';

@Service()
export class AdminAuditService {
    private readonly http = inject(HttpClient);
    private readonly url = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/audit`;

    public getPage(params: Record<string, string | number>): Observable<AdminAuditPageResult> {
        return this.http.get<AdminAuditPageResult>(this.url, { params });
    }
}

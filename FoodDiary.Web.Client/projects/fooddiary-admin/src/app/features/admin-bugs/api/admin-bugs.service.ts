import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type { AdminBugReportPage } from '../models/admin-bug-report';

@Service()
export class AdminBugsService {
    private readonly http = inject(HttpClient);
    private readonly url = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/bugs`;

    public getPage(params: Record<string, string | number>): Observable<AdminBugReportPage> {
        return this.http.get<AdminBugReportPage>(this.url, { params });
    }
}

import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { type AdminAiUsageSummary } from '../models/admin-ai-usage.data';

@Injectable({ providedIn: 'root' })
export class AdminAiUsageService {
    private readonly http = inject(HttpClient);
    private readonly aiUsageUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/ai-usage/summary`;

    public getSummary(): Observable<AdminAiUsageSummary> {
        return this.http.get<AdminAiUsageSummary>(this.aiUsageUrl);
    }
}

import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type { MarketingAttributionSummary } from '../models/admin-acquisition.data';

const DEFAULT_ACQUISITION_WINDOW_HOURS = 720;

@Service()
export class AdminAcquisitionService {
    private readonly http = inject(HttpClient);
    private readonly summaryUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/acquisition/summary`;

    public getSummary(hours: number = DEFAULT_ACQUISITION_WINDOW_HOURS): Observable<MarketingAttributionSummary> {
        return this.http.get<MarketingAttributionSummary>(this.summaryUrl, { params: { hours } });
    }
}

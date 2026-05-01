import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { FastingTelemetrySummary } from '../models/admin-telemetry.data';

@Injectable({ providedIn: 'root' })
export class AdminTelemetryService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/telemetry`;

    public getFastingSummary(hours: number = 24): Observable<FastingTelemetrySummary> {
        return this.http.get<FastingTelemetrySummary>(`${this.baseUrl}/fasting`, { params: { hours } });
    }
}

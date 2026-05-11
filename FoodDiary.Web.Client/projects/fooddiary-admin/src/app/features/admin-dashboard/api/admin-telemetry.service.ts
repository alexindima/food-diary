import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type { FastingTelemetrySummary } from '../models/admin-telemetry.data';

const DEFAULT_FASTING_SUMMARY_HOURS = 24;

@Injectable({ providedIn: 'root' })
export class AdminTelemetryService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/telemetry`;

    public getFastingSummary(hours: number = DEFAULT_FASTING_SUMMARY_HOURS): Observable<FastingTelemetrySummary> {
        return this.http.get<FastingTelemetrySummary>(`${this.baseUrl}/fasting`, { params: { hours } });
    }
}

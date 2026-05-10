import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { fallbackApiError } from '../shared/lib/api-error.utils';
import type { FastingTelemetrySummary } from '../shared/models/admin-telemetry.data';
import { ApiService } from './api.service';

@Injectable({
    providedIn: 'root',
})
export class AdminTelemetryService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.auth.replace('/auth', '/admin/telemetry');

    public getFastingSummary(hours: number = 24): Observable<FastingTelemetrySummary | null> {
        return this.get<FastingTelemetrySummary>('fasting', { hours }).pipe(
            catchError(error => fallbackApiError('Get fasting telemetry summary error', error, null)),
        );
    }
}

import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { SKIP_AUTH } from '../constants/http-context.tokens';
import { SKIP_OBSERVABILITY } from '../constants/observability-context.tokens';

export type ClientTelemetryEvent = {
    category: 'client_error' | 'route_timing' | 'http_request' | 'web_vital' | 'user_action';
    name: string;
    level: 'info' | 'warning' | 'error';
    timestamp: string;
    message?: string;
    location?: string;
    route?: string;
    httpMethod?: string;
    outcome?: string;
    durationMs?: number;
    value?: number;
    statusCode?: number;
    unit?: string;
    buildVersion?: string;
    stack?: string;
    details?: Record<string, unknown>;
};

@Injectable({ providedIn: 'root' })
export class LoggingApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = environment.apiUrls.logs;
    private readonly telemetryContext = new HttpContext().set(SKIP_AUTH, true).set(SKIP_OBSERVABILITY, true);

    public logEvent(payload: ClientTelemetryEvent): Observable<void> {
        return this.http.post<void>(this.baseUrl, payload, {
            context: this.telemetryContext,
        });
    }

    public logError(payload: ClientTelemetryEvent): Observable<void> {
        return this.logEvent(payload);
    }
}

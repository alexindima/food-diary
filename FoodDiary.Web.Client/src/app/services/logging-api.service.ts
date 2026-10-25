import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { SKIP_AUTH } from '../constants/http-context.tokens';
import { SKIP_OBSERVABILITY } from '../constants/observability-context.tokens';
import { LoggingSdk } from '../shared/api/sdk/generated/api/logging.service';
import { createSdkConnection, sdkRequestOptions } from '../shared/api/sdk/sdk-connection';
import { redactTelemetryDetails, redactTelemetryText } from '../shared/observability/telemetry-redaction';

export type ClientTelemetryEvent = {
    category: 'client_error' | 'route_timing' | 'http_request' | 'web_vital' | 'user_action';
    name: string;
    level: 'info' | 'warning' | 'error';
    timestamp: string;
    message?: string;
    location?: string;
    route?: string;
    pageRoute?: string;
    sessionId?: string;
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
const API_MESSAGE_LIMIT = 512;
const API_LOCATION_LIMIT = 512;
const API_ROUTE_LIMIT = 256;

@Service()
export class LoggingApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = environment.apiUrls.logs;
    private readonly sdk = createSdkConnection(LoggingSdk, this.baseUrl, this.http, {
        withCredentials: false,
        keepalive: true,
        priority: 'low',
    });
    private readonly telemetryContext = new HttpContext().set(SKIP_AUTH, true).set(SKIP_OBSERVABILITY, true);

    public logEvent(payload: ClientTelemetryEvent): Observable<void> {
        const safePayload = {
            ...payload,
            ...(payload.message === undefined ? {} : { message: redactTelemetryText(payload.message).slice(0, API_MESSAGE_LIMIT) }),
            ...(payload.stack === undefined ? {} : { stack: redactTelemetryText(payload.stack) }),
            ...(payload.location === undefined
                ? {}
                : { location: redactTelemetryText(payload.location).split(/[?#]/u, 1)[0].slice(0, API_LOCATION_LIMIT) }),
            ...(payload.route === undefined
                ? {}
                : { route: redactTelemetryText(payload.route).split(/[?#]/u, 1)[0].slice(0, API_ROUTE_LIMIT) }),
            ...(payload.pageRoute === undefined
                ? {}
                : { pageRoute: redactTelemetryText(payload.pageRoute).split(/[?#]/u, 1)[0].slice(0, API_ROUTE_LIMIT) }),
            ...(payload.details === undefined ? {} : { details: redactTelemetryDetails(payload.details) }),
        };
        return this.sdk.client.postLogs(
            { version: this.sdk.version, clientTelemetryLogHttpRequest: safePayload },
            'body',
            false,
            sdkRequestOptions(undefined, this.telemetryContext),
        );
    }

    public logError(payload: ClientTelemetryEvent): Observable<void> {
        return this.logEvent(payload);
    }
}

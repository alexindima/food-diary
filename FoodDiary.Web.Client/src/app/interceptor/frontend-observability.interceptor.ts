import {
    HttpErrorResponse,
    type HttpEvent,
    type HttpHandler,
    type HttpHeaders,
    type HttpInterceptor,
    type HttpRequest,
    HttpResponse,
    HttpStatusCode,
} from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { finalize, type Observable, tap } from 'rxjs';

import { environment } from '../../environments/environment';
import { SKIP_OBSERVABILITY } from '../constants/observability-context.tokens';
import { FrontendObservabilityService } from '../services/frontend-observability.service';
import { DEVELOPMENT_REQUEST_SCOPE } from '../shared/development/development-request-context';
import { DEVELOPMENT_REQUESTS, type DevelopmentResponseMetadata } from '../shared/development/development-tools';

@Injectable()
export class FrontendObservabilityInterceptor implements HttpInterceptor {
    private readonly frontendObservabilityService = inject(FrontendObservabilityService);
    private readonly developmentRequests = inject(DEVELOPMENT_REQUESTS, { optional: true });

    public intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
        if (this.shouldSkip(req)) {
            return next.handle(req);
        }

        const startedAt = performance.now();
        const completeDevelopmentRequest = this.developmentRequests?.observe(req.method, req.url);
        if (completeDevelopmentRequest?.requestId !== undefined) {
            req.context.get(DEVELOPMENT_REQUEST_SCOPE)?.(completeDevelopmentRequest.requestId);
        }
        let developmentStatus: number | undefined;
        let developmentMetadata: DevelopmentResponseMetadata | undefined;

        return next.handle(req).pipe(
            tap({
                next: event => {
                    if (event instanceof HttpResponse) {
                        developmentStatus = event.status;
                        developmentMetadata = this.runtimeMetadata(event.headers);
                        this.frontendObservabilityService.recordHttpRequest({
                            url: req.url,
                            method: req.method,
                            statusCode: event.status,
                            durationMs: performance.now() - startedAt,
                            outcome: 'success',
                        });
                    }
                },
                error: (error: unknown) => {
                    const statusCode = error instanceof HttpErrorResponse ? error.status : 0;
                    developmentStatus = statusCode;
                    developmentMetadata = error instanceof HttpErrorResponse ? this.runtimeMetadata(error.headers) : undefined;
                    this.frontendObservabilityService.recordHttpRequest({
                        url: req.url,
                        method: req.method,
                        statusCode,
                        durationMs: performance.now() - startedAt,
                        outcome: this.resolveOutcome(error),
                    });
                },
            }),
            finalize(() => {
                if (developmentMetadata === undefined) {
                    completeDevelopmentRequest?.(developmentStatus);
                } else {
                    completeDevelopmentRequest?.(developmentStatus, developmentMetadata);
                }
            }),
        );
    }

    private runtimeMetadata(headers: HttpHeaders): DevelopmentResponseMetadata | undefined {
        const apiBuild = headers.get('X-FoodDiary-Api-Build');
        const apiInstance = headers.get('X-FoodDiary-Api-Instance');
        return apiBuild === null && apiInstance === null ? undefined : { apiBuild, apiInstance };
    }

    private shouldSkip(req: HttpRequest<unknown>): boolean {
        if (req.context.get(SKIP_OBSERVABILITY)) {
            return true;
        }

        const url = req.url.toLowerCase();
        const logsPath = new URL(environment.apiUrls.logs, 'http://localhost').pathname.toLowerCase();
        return !url.includes('/api/') || url.includes(logsPath);
    }

    private resolveOutcome(error: unknown): 'client_error' | 'server_error' | 'network_error' {
        if (!(error instanceof HttpErrorResponse)) {
            return 'network_error';
        }

        if (error.status >= Number(HttpStatusCode.InternalServerError)) {
            return 'server_error';
        }

        if (error.status >= Number(HttpStatusCode.BadRequest)) {
            return 'client_error';
        }

        return 'network_error';
    }
}

import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest, HttpResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../environments/environment';
import { SKIP_OBSERVABILITY } from '../constants/observability-context.tokens';
import { FrontendObservabilityService } from '../services/frontend-observability.service';

@Injectable()
export class FrontendObservabilityInterceptor implements HttpInterceptor {
    private readonly frontendObservabilityService = inject(FrontendObservabilityService);

    public intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
        if (this.shouldSkip(req)) {
            return next.handle(req);
        }

        const startedAt = performance.now();

        return next.handle(req).pipe(
            tap({
                next: event => {
                    if (event instanceof HttpResponse) {
                        this.frontendObservabilityService.recordHttpRequest({
                            url: req.url,
                            method: req.method,
                            statusCode: event.status,
                            durationMs: performance.now() - startedAt,
                            outcome: 'success',
                        });
                    }
                },
                error: error => {
                    const statusCode = error instanceof HttpErrorResponse ? error.status : 0;
                    this.frontendObservabilityService.recordHttpRequest({
                        url: req.url,
                        method: req.method,
                        statusCode,
                        durationMs: performance.now() - startedAt,
                        outcome: this.resolveOutcome(error),
                    });
                },
            }),
        );
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

        if (error.status >= 500) {
            return 'server_error';
        }

        if (error.status >= 400) {
            return 'client_error';
        }

        return 'network_error';
    }
}

import {
    HttpErrorResponse,
    type HttpEvent,
    type HttpHandler,
    type HttpInterceptor,
    type HttpRequest,
    HttpStatusCode,
} from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { type Observable, retry, throwError, timer } from 'rxjs';

import { RETRY_TIMING_CONFIG } from '../config/runtime-ui.tokens';

const HTTP_CLIENT_ERROR_MIN: number = HttpStatusCode.BadRequest;
const HTTP_SERVER_ERROR_MIN: number = HttpStatusCode.InternalServerError;

@Injectable()
export class RetryInterceptor implements HttpInterceptor {
    private readonly timingConfig = inject(RETRY_TIMING_CONFIG);

    public intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
        if (req.method !== 'GET') {
            return next.handle(req);
        }

        return next.handle(req).pipe(
            retry({
                count: this.timingConfig.attemptCount,
                delay: (error, retryCount) => {
                    if (
                        error instanceof HttpErrorResponse &&
                        error.status >= HTTP_CLIENT_ERROR_MIN &&
                        error.status < HTTP_SERVER_ERROR_MIN
                    ) {
                        return throwError(() => error);
                    }

                    const delayMs = Math.pow(2, retryCount - 1) * this.timingConfig.baseDelayMs;
                    return timer(delayMs);
                },
            }),
        );
    }
}

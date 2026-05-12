import {
    HttpErrorResponse,
    type HttpEvent,
    type HttpHandler,
    type HttpInterceptor,
    type HttpRequest,
    HttpStatusCode,
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { type Observable, retry, throwError, timer } from 'rxjs';

const RETRY_ATTEMPT_COUNT = 3;
const HTTP_CLIENT_ERROR_MIN: number = HttpStatusCode.BadRequest;
const HTTP_SERVER_ERROR_MIN: number = HttpStatusCode.InternalServerError;
const RETRY_BASE_DELAY_MS = 1000;

@Injectable()
export class RetryInterceptor implements HttpInterceptor {
    public intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
        if (req.method !== 'GET') {
            return next.handle(req);
        }

        return next.handle(req).pipe(
            retry({
                count: RETRY_ATTEMPT_COUNT,
                delay: (error, retryCount) => {
                    if (
                        error instanceof HttpErrorResponse &&
                        error.status >= HTTP_CLIENT_ERROR_MIN &&
                        error.status < HTTP_SERVER_ERROR_MIN
                    ) {
                        return throwError(() => error);
                    }

                    const delayMs = Math.pow(2, retryCount - 1) * RETRY_BASE_DELAY_MS;
                    return timer(delayMs);
                },
            }),
        );
    }
}

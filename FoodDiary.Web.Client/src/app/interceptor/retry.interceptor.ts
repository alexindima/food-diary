import { HttpErrorResponse, type HttpEvent, type HttpHandler, type HttpInterceptor, type HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { type Observable, retry, throwError, timer } from 'rxjs';

@Injectable()
export class RetryInterceptor implements HttpInterceptor {
    public intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
        if (req.method !== 'GET') {
            return next.handle(req);
        }

        return next.handle(req).pipe(
            retry({
                count: 3,
                delay: (error, retryCount) => {
                    if (error instanceof HttpErrorResponse && error.status >= 400 && error.status < 500) {
                        return throwError(() => error);
                    }

                    const delayMs = Math.pow(2, retryCount - 1) * 1000;
                    return timer(delayMs);
                },
            }),
        );
    }
}

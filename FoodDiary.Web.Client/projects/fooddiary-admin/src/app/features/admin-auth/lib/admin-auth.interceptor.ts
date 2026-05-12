import { HttpErrorResponse, type HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

const HTTP_UNAUTHORIZED = 401;
const HTTP_FORBIDDEN = 403;

export const adminAuthInterceptor: HttpInterceptorFn = (req, next) => {
    const router = inject(Router);
    const token = localStorage.getItem('authToken') ?? sessionStorage.getItem('authToken');
    if (token === null || token.trim().length === 0) {
        return next(req);
    }

    return next(
        req.clone({
            setHeaders: {
                Authorization: `Bearer ${token}`,
            },
        }),
    ).pipe(
        catchError((error: unknown) => {
            if (req.url.includes('/admin-sso/exchange')) {
                return throwError(() => error);
            }

            const status = error instanceof HttpErrorResponse ? error.status : undefined;

            if (status === HTTP_UNAUTHORIZED || status === HTTP_FORBIDDEN) {
                localStorage.removeItem('authToken');
                localStorage.removeItem('refreshToken');
                sessionStorage.removeItem('authToken');

                const reason = status === HTTP_FORBIDDEN ? 'forbidden' : 'unauthenticated';
                const returnUrl = router.url;
                void router.navigate(['/unauthorized'], {
                    queryParams: { reason, returnUrl },
                });
            }

            return throwError(() => error);
        }),
    );
};

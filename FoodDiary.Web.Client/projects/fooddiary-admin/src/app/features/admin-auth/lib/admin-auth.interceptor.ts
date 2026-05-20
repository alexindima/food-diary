import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { HttpErrorResponse, type HttpInterceptorFn, HttpStatusCode } from '@angular/common/http';
import { inject, PLATFORM_ID } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const adminAuthInterceptor: HttpInterceptorFn = (req, next) => {
    const router = inject(Router);
    const documentRef = inject(DOCUMENT);
    const isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
    const localStorageRef = getBrowserStorage(documentRef, isBrowser, 'localStorage');
    const sessionStorageRef = getBrowserStorage(documentRef, isBrowser, 'sessionStorage');
    const token = localStorageRef?.getItem('authToken') ?? sessionStorageRef?.getItem('authToken') ?? null;
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

            if (status === HttpStatusCode.Unauthorized || status === HttpStatusCode.Forbidden) {
                localStorageRef?.removeItem('authToken');
                localStorageRef?.removeItem('refreshToken');
                sessionStorageRef?.removeItem('authToken');

                const reason = status === HttpStatusCode.Forbidden ? 'forbidden' : 'unauthenticated';
                const returnUrl = router.url;
                void router.navigate(['/unauthorized'], {
                    queryParams: { reason, returnUrl },
                });
            }

            return throwError(() => error);
        }),
    );
};

function getBrowserStorage(documentRef: Document, isBrowser: boolean, storageName: 'localStorage' | 'sessionStorage'): Storage | null {
    if (!isBrowser) {
        return null;
    }

    try {
        return documentRef.defaultView?.[storageName] ?? null;
    } catch {
        return null;
    }
}

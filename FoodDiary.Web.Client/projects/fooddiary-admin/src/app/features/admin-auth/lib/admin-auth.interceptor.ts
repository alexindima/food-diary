import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { HttpErrorResponse, type HttpInterceptorFn, HttpStatusCode } from '@angular/common/http';
import { inject, PLATFORM_ID } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { adminApiErrorCode } from '../../../shared/api/admin-api-error';
import { AdminAuthService } from './admin-auth.service';

export const adminAuthInterceptor: HttpInterceptorFn = (req, next) => {
    const apiRoot = environment.apiUrls.auth.replace(/\/auth$/, '');
    if (!req.url.startsWith(`${apiRoot}/`)) {
        return next(req);
    }
    const router = inject(Router);
    const documentRef = inject(DOCUMENT);
    const isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
    const localStorageRef = getBrowserStorage(documentRef, isBrowser, 'localStorage');
    const sessionStorageRef = getBrowserStorage(documentRef, isBrowser, 'sessionStorage');
    const auth = inject(AdminAuthService);
    let headers = req.headers.set('X-FoodDiary-Client', 'admin').set('X-Requested-With', 'XMLHttpRequest');
    const sessionId = auth.getSessionId();
    if (sessionId !== null && !req.url.endsWith('/auth/session') && !req.url.endsWith('/admin-sso/exchange')) {
        headers = headers.set('X-FoodDiary-Session', sessionId);
    }

    return next(
        req.clone({
            headers,
            withCredentials: true,
        }),
    ).pipe(
        catchError((error: unknown) => {
            if (isSessionHandshake(req.url)) {
                return throwError(() => error);
            }

            if (auth.getSessionId() !== sessionId) {
                return throwError(() => error);
            }

            const status = error instanceof HttpErrorResponse ? error.status : undefined;

            if (status === HttpStatusCode.Unauthorized) {
                auth.invalidateSession();
                localStorageRef?.removeItem('authToken');
                localStorageRef?.removeItem('refreshToken');
                sessionStorageRef?.removeItem('authToken');

                const returnUrl = router.url;
                void router.navigate(['/unauthorized'], {
                    queryParams: { reason: 'unauthenticated', returnUrl },
                });
            }

            if (isAccessDenied(error)) {
                void router.navigate(['/unauthorized'], {
                    queryParams: { reason: 'forbidden', returnUrl: router.url },
                });
            }

            return throwError(() => error);
        }),
    );
};

function isSessionHandshake(url: string): boolean {
    return url.endsWith('/auth/session') || url.includes('/admin-sso/exchange');
}

function isAccessDenied(error: unknown): boolean {
    return (
        error instanceof HttpErrorResponse &&
        error.status === Number(HttpStatusCode.Forbidden) &&
        adminApiErrorCode(error) !== 'Ai.ConsentRequired'
    );
}

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

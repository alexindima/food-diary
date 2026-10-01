import { type HttpEvent, type HttpHandler, type HttpInterceptor, type HttpRequest, HttpStatusCode } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, from, map, type Observable, of, switchMap, throwError } from 'rxjs';

import { SKIP_AUTH } from '../constants/http-context.tokens';
import { AuthService } from '../services/auth.service';
import { getRecordProperty, getStringProperty } from '../shared/lib/unknown-value.utils';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
    private readonly authService = inject(AuthService);
    private readonly refreshExcludedAuthPaths = [
        '/auth/register',
        '/auth/login',
        '/auth/google',
        '/auth/refresh',
        '/auth/restore',
        '/auth/verify-email',
        '/auth/password-reset/request',
        '/auth/password-reset/confirm',
        '/auth/telegram/verify',
        '/auth/telegram/login-widget',
        '/auth/telegram/bot/auth',
        '/auth/admin-sso/exchange',
    ];

    public intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
        const skipAuth = req.context.get(SKIP_AUTH);
        if (skipAuth) {
            return next.handle(req);
        }

        const token = this.authService.getToken();
        let request = req;

        if (token !== null && token.trim().length > 0) {
            request = req.clone({
                headers: req.headers.set('Authorization', `Bearer ${token}`),
            });
        }

        return next.handle(request).pipe(
            catchError((error: unknown) => {
                if (!this.isUnauthorizedError(error) || this.shouldSkipRefresh(req.url)) {
                    return throwError(() => error);
                }

                return this.isCredentialVerificationFailure(error).pipe(
                    switchMap(isCredentialFailure => {
                        if (isCredentialFailure) {
                            return throwError(() => error);
                        }
                        return this.refreshRequest(req, next, error);
                    }),
                );
            }),
        );
    }

    private isCredentialVerificationFailure(error: unknown): Observable<boolean> {
        const body = getRecordProperty(error, 'error');
        if (body instanceof Blob) {
            return from(body.text()).pipe(
                map(text => {
                    const payload: unknown = JSON.parse(text);
                    return getStringProperty(payload, 'error') === 'User.InvalidPassword';
                }),
                catchError(() => of(false)),
            );
        }
        return of(getStringProperty(body, 'error') === 'User.InvalidPassword');
    }

    private refreshRequest(req: HttpRequest<unknown>, next: HttpHandler, error: unknown): Observable<HttpEvent<unknown>> {
        return this.authService.refreshToken().pipe(
            catchError((refreshError: unknown) => {
                void this.authService.onLogoutAsync(true);
                return throwError(() => refreshError);
            }),
            switchMap(accessToken => {
                if (accessToken !== null && accessToken.trim().length > 0) {
                    const newRequest = req.clone({ headers: req.headers.set('Authorization', `Bearer ${accessToken}`) });
                    return next.handle(newRequest);
                }
                void this.authService.onLogoutAsync(true);
                return throwError(() => error);
            }),
        );
    }

    private shouldSkipRefresh(url: string): boolean {
        const path = url.split('?')[0].toLowerCase();
        return this.refreshExcludedAuthPaths.some(endpoint => path.endsWith(endpoint));
    }

    private isUnauthorizedError(error: unknown): boolean {
        return typeof error === 'object' && error !== null && 'status' in error && error.status === HttpStatusCode.Unauthorized;
    }
}

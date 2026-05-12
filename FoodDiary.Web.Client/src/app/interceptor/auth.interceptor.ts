import type { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, type Observable, switchMap, throwError } from 'rxjs';

import { SKIP_AUTH } from '../constants/http-context.tokens';
import { AuthService } from '../services/auth.service';

const HTTP_UNAUTHORIZED = 401;

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
    private readonly authService = inject(AuthService);

    public intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
        const skipAuth = req.context.get(SKIP_AUTH);
        if (skipAuth === true) {
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
                if (!this.isUnauthorizedError(error) || this.isAuthRequest(req.url)) {
                    return throwError(() => error);
                }

                return this.authService.refreshToken().pipe(
                    switchMap(accessToken => {
                        if (accessToken !== null && accessToken.trim().length > 0) {
                            const newRequest = req.clone({
                                headers: req.headers.set('Authorization', `Bearer ${accessToken}`),
                            });
                            return next.handle(newRequest);
                        }

                        void this.authService.onLogoutAsync(true);
                        return throwError(() => error);
                    }),
                    catchError((refreshError: unknown) => {
                        void this.authService.onLogoutAsync(true);
                        return throwError(() => refreshError);
                    }),
                );
            }),
        );
    }

    private isAuthRequest(url: string): boolean {
        return url.toLowerCase().includes('/auth/');
    }

    private isUnauthorizedError(error: unknown): boolean {
        return typeof error === 'object' && error !== null && 'status' in error && error.status === HTTP_UNAUTHORIZED;
    }
}

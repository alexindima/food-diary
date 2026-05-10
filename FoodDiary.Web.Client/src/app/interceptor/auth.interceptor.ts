import type { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, type Observable, switchMap, throwError } from 'rxjs';

import { SKIP_AUTH } from '../constants/http-context.tokens';
import { AuthService } from '../services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
    private readonly authService = inject(AuthService);

    public intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
        const skipAuth = req.context.get(SKIP_AUTH);
        if (skipAuth) {
            return next.handle(req);
        }

        const token = this.authService.getToken();
        let request = req;

        if (token) {
            request = req.clone({
                headers: req.headers.set('Authorization', `Bearer ${token}`),
            });
        }

        return next.handle(request).pipe(
            catchError((error: HttpErrorResponse) => {
                if (error.status !== 401 || this.isAuthRequest(req.url)) {
                    return throwError(() => error);
                }

                return this.authService.refreshToken().pipe(
                    switchMap(accessToken => {
                        if (accessToken) {
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
}

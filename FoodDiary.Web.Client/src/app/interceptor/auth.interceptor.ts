import { type HttpEvent, type HttpHandler, type HttpInterceptor, type HttpRequest, HttpStatusCode } from '@angular/common/http';
import { DestroyRef, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, from, map, merge, type Observable, of, switchMap, throwError } from 'rxjs';

import { SKIP_AUTH } from '../constants/http-context.tokens';
import { AuthService } from '../services/auth.service';
import { SessionEventsService } from '../shared/auth/session-events.service';
import { getRecordProperty, getStringProperty } from '../shared/lib/unknown-value.utils';

type RequestSession = {
    generation: number;
    userId: string | null;
    impersonating: boolean;
};

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
    private readonly authService = inject(AuthService);
    private sessionGeneration = 0;
    private readonly refreshExcludedAuthPaths = [
        '/auth/register',
        '/auth/login',
        '/auth/google',
        '/auth/refresh',
        '/auth/logout',
        '/auth/restore',
        '/auth/verify-email',
        '/auth/password-reset/request',
        '/auth/password-reset/confirm',
        '/auth/telegram/verify',
        '/auth/telegram/login-widget',
        '/auth/telegram/oidc/exchange',
        '/auth/telegram/bot/auth',
        '/auth/telegram/complete',
        '/auth/telegram/complete-link',
        '/auth/telegram/link',
        '/auth/admin-sso/exchange',
    ];

    public constructor() {
        const sessions = inject(SessionEventsService);
        merge(sessions.authenticated$, sessions.sessionEnded$)
            .pipe(takeUntilDestroyed(inject(DestroyRef)))
            .subscribe(() => {
                this.sessionGeneration++;
            });
    }

    public intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
        const skipAuth = req.context.get(SKIP_AUTH);
        if (skipAuth) {
            return next.handle(req);
        }

        const token = this.authService.getToken();
        const session: RequestSession = {
            generation: this.sessionGeneration,
            userId: this.authService.getUserId(),
            impersonating: this.authService.isImpersonating(),
        };
        let request = req;

        if (token !== null && token.trim().length > 0) {
            request = req.clone({
                headers: req.headers.set('Authorization', `Bearer ${token}`),
            });
        }

        return next.handle(request).pipe(
            catchError((error: unknown) => {
                if (!this.isUnauthorizedError(error) || this.shouldSkipRefresh(req.url) || !this.isCurrentSession(session)) {
                    return throwError(() => error);
                }

                return this.isCredentialVerificationFailure(error).pipe(
                    switchMap(isCredentialFailure => {
                        if (isCredentialFailure) {
                            return throwError(() => error);
                        }
                        return this.refreshRequest(req, next, error, session);
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

    private refreshRequest(
        req: HttpRequest<unknown>,
        next: HttpHandler,
        error: unknown,
        session: RequestSession,
    ): Observable<HttpEvent<unknown>> {
        if (!this.isCurrentSession(session)) {
            return throwError(() => error);
        }
        return this.authService.refreshToken().pipe(
            switchMap(accessToken => {
                if (accessToken !== null && accessToken.trim().length > 0 && this.isCurrentSession(session)) {
                    const newRequest = req.clone({ headers: req.headers.set('Authorization', `Bearer ${accessToken}`) });
                    return next.handle(newRequest);
                }
                return throwError(() => error);
            }),
        );
    }

    private isCurrentSession(session: RequestSession): boolean {
        return (
            session.generation === this.sessionGeneration &&
            session.userId === this.authService.getUserId() &&
            session.impersonating === this.authService.isImpersonating()
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

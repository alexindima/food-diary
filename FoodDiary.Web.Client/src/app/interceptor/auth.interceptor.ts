import { type HttpEvent, type HttpHandler, type HttpInterceptor, type HttpRequest, HttpStatusCode } from '@angular/common/http';
import { DestroyRef, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, from, map, merge, type Observable, of, switchMap, throwError } from 'rxjs';

import { environment } from '../../environments/environment';
import { SKIP_AUTH } from '../constants/http-context.tokens';
import { AuthService } from '../services/auth.service';
import { BrowserSessionStateService } from '../shared/auth/browser-session-state.service';
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
    private readonly browserSession = inject(BrowserSessionStateService);
    private sessionGeneration = 0;
    private readonly refreshExcludedAuthPaths = [
        '/auth/register',
        '/auth/login',
        '/auth/google',
        '/auth/refresh',
        '/auth/session',
        '/auth/logout',
        '/auth/restore',
        '/auth/verify-email',
        '/auth/password-reset/request',
        '/auth/password-reset/confirm',
        '/auth/telegram/verify',
        '/auth/telegram/mini-app/begin',
        '/auth/telegram/mini-app/begin-link',
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
        const apiRoot = environment.apiUrls.auth.replace(/\/auth$/, '');
        if (!req.url.startsWith(`${apiRoot}/`)) {
            return next.handle(req);
        }
        req = this.withSessionTransport(req);
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
            request = req.clone({ headers: req.headers.set('Authorization', `Bearer ${token}`) });
        }

        return next.handle(request).pipe(
            catchError((error: unknown) => {
                if (this.shouldRevalidatePermissions(error, req.url, session)) {
                    return this.authService.refreshSession().pipe(switchMap(() => throwError(() => error)));
                }
                if (!this.isUnauthorizedError(error) || this.shouldSkipRefresh(req.url) || !this.isCurrentSession(session)) {
                    return throwError(() => error);
                }
                return this.isCredentialVerificationFailure(error).pipe(
                    switchMap(isCredentialFailure =>
                        isCredentialFailure ? throwError(() => error) : this.refreshRequest(req, next, error, session),
                    ),
                );
            }),
        );
    }

    private withSessionTransport(req: HttpRequest<unknown>): HttpRequest<unknown> {
        let headers = req.headers.set('X-Requested-With', 'XMLHttpRequest');
        if (!headers.has('X-FoodDiary-Client')) {
            headers = headers.set('X-FoodDiary-Client', this.browserSession.telegram ? 'telegram' : 'web');
        }
        const context = this.browserSession.contextId();
        if (context !== null && !headers.has('X-FoodDiary-Context')) {
            headers = headers.set('X-FoodDiary-Context', context);
        }
        const expected = this.browserSession.sessionId();
        const changesIdentity = [
            '/auth/session',
            '/auth/login',
            '/auth/register',
            '/auth/google',
            '/auth/restore',
            '/auth/password-reset/confirm',
            '/auth/impersonation/exchange',
        ].some(path => req.url.endsWith(path));
        if (expected !== null && !changesIdentity && !headers.has('X-FoodDiary-Session')) {
            headers = headers.set('X-FoodDiary-Session', expected);
        }
        return req.clone({ headers, withCredentials: true });
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
        return this.authService.refreshSession().pipe(
            switchMap(restored => {
                if (restored && this.isCurrentSession(session)) {
                    const token = this.authService.getToken();
                    const newRequest = token === null ? req : req.clone({ headers: req.headers.set('Authorization', `Bearer ${token}`) });
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

    private shouldRevalidatePermissions(error: unknown, url: string, session: RequestSession): boolean {
        return (
            !session.impersonating &&
            this.authService.getToken() === null &&
            !this.shouldSkipRefresh(url) &&
            this.isCurrentSession(session) &&
            typeof error === 'object' &&
            error !== null &&
            'status' in error &&
            error.status === HttpStatusCode.Forbidden &&
            getStringProperty(getRecordProperty(error, 'error'), 'error') === 'Authentication.Forbidden'
        );
    }
}

import type { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, finalize, firstValueFrom, map, type Observable, of, shareReplay, tap } from 'rxjs';

import { environment } from '../../environments/environment';
import type {
    AuthResponse,
    ConfirmPasswordResetRequest,
    LoginRequest,
    PasswordResetRequest,
    RegisterRequest,
    RestoreAccountRequest,
    TelegramAuthRequest,
    TelegramLoginWidgetRequest,
} from '../features/auth/models/auth.data';
import type { GoogleLoginRequest } from '../features/auth/models/google-auth.data';
import { QuickMealService } from '../features/meals/lib/quick-meal.service';
import { fallbackApiError, rethrowApiError } from '../shared/lib/api-error.utils';
import { ApiService } from './api.service';
import { FrontendLoggerService } from './frontend-logger.service';
import { JwtDecoderService } from './jwt-decoder.service';
import { LocalizationService } from './localization.service';
import { NavigationService } from './navigation.service';
import { ThemeService } from './theme.service';
import { TokenStorageService } from './token-storage.service';

const TOKEN_EXPIRATION_LEEWAY_SECONDS = 30;

@Injectable({
    providedIn: 'root',
})
export class AuthService extends ApiService {
    private readonly navigationService = inject(NavigationService);
    private readonly quickConsumptionService = inject(QuickMealService);
    private readonly localizationService = inject(LocalizationService);
    private readonly themeService = inject(ThemeService);
    private readonly tokenStorage = inject(TokenStorageService);
    private readonly jwtDecoder = inject(JwtDecoderService);
    private readonly logger = inject(FrontendLoggerService);
    protected readonly baseUrl = environment.apiUrls.auth;

    private readonly authTokenSignal = signal<string | null>(this.tokenStorage.getToken());
    private readonly userSignal = signal<string | null>(this.tokenStorage.loadUserId());
    private readonly emailConfirmedSignal = signal<boolean | null>(this.tokenStorage.loadEmailConfirmed());
    private refreshInFlight$: Observable<string | null> | null = null;
    private sessionRestorePromise: Promise<void> | null = null;
    private readonly authReadySignal = signal(false);

    public readonly isAuthenticated = computed(() => this.authTokenSignal() !== null);
    public readonly isEmailConfirmed = computed(() => this.emailConfirmedSignal() ?? true);
    public readonly isAdmin = computed(() => this.hasRole('Admin'));
    public readonly isPremium = computed(() => this.hasRole('Premium'));
    public readonly isDietologist = computed(() => this.hasRole('Dietologist'));
    public readonly isImpersonating = computed(() => this.jwtDecoder.isImpersonation(this.authTokenSignal()));
    public readonly impersonationActorId = computed(() => this.jwtDecoder.extractImpersonationActorId(this.authTokenSignal()));
    public readonly impersonationReason = computed(() => this.jwtDecoder.extractImpersonationReason(this.authTokenSignal()));
    public readonly isAuthReady = this.authReadySignal.asReadonly();

    public initializeAuth(): void {
        let token = this.tokenStorage.getToken();
        if (token === null || token.length === 0) {
            this.clearStoredIdentity();
            return;
        }

        if (this.clearExpiredToken(token)) {
            token = null;
        }

        if (token === null || token.length === 0) {
            return;
        }

        this.authTokenSignal.set(token);
        this.restoreUserIdFromToken(token);
        if (this.emailConfirmedSignal() === null) {
            const stored = this.tokenStorage.loadEmailConfirmed();
            this.emailConfirmedSignal.set(stored ?? true);
        }

        this.linkTelegramIfAvailable();
    }

    private restoreUserIdFromToken(token: string): void {
        if (this.userSignal() !== null) {
            return;
        }

        const resolvedUserId = this.jwtDecoder.extractUserId(token);
        if (resolvedUserId !== null && resolvedUserId.length > 0) {
            this.tokenStorage.setUserId(resolvedUserId);
            this.userSignal.set(resolvedUserId);
        }
    }

    public async restoreSessionAsync(): Promise<void> {
        if (this.authReadySignal()) {
            return;
        }

        if (this.sessionRestorePromise !== null) {
            return this.sessionRestorePromise;
        }

        this.sessionRestorePromise = this.restoreSessionInternalAsync().finally(() => {
            this.authReadySignal.set(true);
            this.sessionRestorePromise = null;
        });

        return this.sessionRestorePromise;
    }

    public async ensureSessionReadyAsync(): Promise<void> {
        await this.restoreSessionAsync();
    }

    public login(data: LoginRequest): Observable<AuthResponse> {
        const loginData = {
            email: data.email,
            password: data.password,
            rememberMe: undefined,
        };
        return this.post<AuthResponse>('login', loginData).pipe(
            tap(response => {
                this.onLogin(response, data.rememberMe || false);
            }),
            catchError((error: HttpErrorResponse) => rethrowApiError('Login error', error)),
        );
    }

    public register(data: RegisterRequest): Observable<AuthResponse> {
        return this.post<AuthResponse>('register', {
            email: data.email,
            password: data.password,
            language: data.language,
            clientOrigin: this.getClientOrigin(),
        }).pipe(
            tap(response => {
                this.onLogin(response, false);
            }),
            catchError((error: HttpErrorResponse) => rethrowApiError('Register error', error)),
        );
    }

    public verifyEmail(userId: string, token: string): Observable<void> {
        return this.post<void>('verify-email', { userId, token }).pipe(
            tap(() => {
                this.setEmailConfirmed(true);
            }),
            catchError((error: HttpErrorResponse) => rethrowApiError('Verify email error', error)),
        );
    }

    public resendEmailVerification(): Observable<void> {
        return this.post<void>('verify-email/resend', { clientOrigin: this.getClientOrigin() }).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Resend email verification error', error)),
        );
    }

    public restoreAccount(data: RestoreAccountRequest, rememberMe = false): Observable<AuthResponse> {
        return this.post<AuthResponse>('restore', data).pipe(
            tap(response => {
                this.onLogin(response, rememberMe);
            }),
            catchError((error: HttpErrorResponse) => rethrowApiError('Restore account error', error)),
        );
    }

    public loginWithGoogle(data: GoogleLoginRequest): Observable<AuthResponse> {
        return this.post<AuthResponse>('google', data).pipe(
            tap(response => {
                this.onLogin(response, data.rememberMe ?? false);
            }),
            catchError((error: HttpErrorResponse) => rethrowApiError('Google login error', error)),
        );
    }

    public requestPasswordReset(data: PasswordResetRequest): Observable<void> {
        return this.post<void>('password-reset/request', {
            email: data.email,
            clientOrigin: this.getClientOrigin(),
        }).pipe(catchError((error: HttpErrorResponse) => rethrowApiError('Password reset request error', error)));
    }

    public confirmPasswordReset(data: ConfirmPasswordResetRequest): Observable<AuthResponse> {
        return this.post<AuthResponse>('password-reset/confirm', data).pipe(
            tap(response => {
                this.onLogin(response, false);
            }),
            catchError((error: HttpErrorResponse) => rethrowApiError('Password reset confirm error', error)),
        );
    }

    public loginWithTelegramWidget(data: TelegramLoginWidgetRequest, rememberMe: boolean): Observable<AuthResponse> {
        return this.post<AuthResponse>('telegram/login-widget', data).pipe(
            tap(response => {
                this.onLogin(response, rememberMe);
            }),
            catchError((error: HttpErrorResponse) => rethrowApiError('Telegram login error', error)),
        );
    }

    public startAdminSso(): Observable<AdminSsoStartResponse> {
        return this.post<AdminSsoStartResponse>('admin-sso/start', {});
    }

    public refreshToken(): Observable<string | null> {
        if (this.refreshInFlight$ !== null) {
            return this.refreshInFlight$;
        }

        const refreshToken = this.tokenStorage.getRefreshToken();
        if (refreshToken === null || refreshToken.length === 0) {
            void this.onLogoutAsync(true);
            return of(null);
        }

        const refreshRequest$ = this.post<AuthResponse>('refresh', { refreshToken }).pipe(
            map(response => {
                const accessToken = response.accessToken;
                if (accessToken.length > 0) {
                    this.applyAuthenticatedSession(response);
                }
                return accessToken;
            }),
            catchError(error => {
                void this.onLogoutAsync(true);
                return fallbackApiError('refreshToken error', error, null);
            }),
            finalize(() => {
                this.refreshInFlight$ = null;
            }),
            shareReplay(1),
        );

        this.refreshInFlight$ = refreshRequest$;
        return refreshRequest$;
    }

    public async onLogoutAsync(redirectToAuth = false): Promise<void> {
        this.authTokenSignal.set(null);
        this.userSignal.set(null);
        this.emailConfirmedSignal.set(null);
        this.tokenStorage.clearAll();
        this.localizationService.clearStoredLanguage();
        if (redirectToAuth) {
            await this.navigationService.navigateToAuthAsync('login');
            return;
        }
        await this.navigationService.navigateToLandingAsync();
    }

    public getToken(): string | null {
        return this.tokenStorage.getToken();
    }

    public getUserId(): string | null {
        return this.userSignal();
    }

    public setEmailConfirmed(value: boolean): void {
        this.emailConfirmedSignal.set(value);
        this.tokenStorage.setEmailConfirmed(value);
    }

    private onLogin(authResponse: AuthResponse, rememberMe: boolean): void {
        this.quickConsumptionService.exitPreview();
        this.applyAuthenticatedSession(authResponse, rememberMe);
        this.linkTelegramIfAvailable();
    }

    private applyAuthenticatedSession(authResponse: AuthResponse, rememberMe?: boolean): void {
        this.tokenStorage.setToken(authResponse.accessToken, rememberMe);
        this.tokenStorage.setRefreshToken(authResponse.refreshToken);
        this.authTokenSignal.set(authResponse.accessToken);

        const preferredLanguage = authResponse.user.language;
        if (preferredLanguage !== undefined && preferredLanguage.length > 0) {
            void this.localizationService.applyLanguagePreferenceAsync(preferredLanguage);
        }

        this.themeService.syncWithUserPreferences(authResponse.user.theme, authResponse.user.uiStyle);

        if (typeof authResponse.user.isEmailConfirmed === 'boolean') {
            this.setEmailConfirmed(authResponse.user.isEmailConfirmed);
        } else {
            this.setEmailConfirmed(true);
        }

        const userId = authResponse.user.id;
        if (userId.length > 0) {
            this.tokenStorage.setUserId(userId);
            this.userSignal.set(userId);
        } else {
            this.logger.warn('Auth response did not include user ID');
            this.tokenStorage.clearUserId();
            this.userSignal.set(null);
        }
    }

    private clearExpiredToken(token: string): boolean {
        if (!this.jwtDecoder.isExpired(token, TOKEN_EXPIRATION_LEEWAY_SECONDS)) {
            return false;
        }

        this.authTokenSignal.set(null);
        this.tokenStorage.clearToken();
        return true;
    }

    private async restoreSessionInternalAsync(): Promise<void> {
        this.captureImpersonationTokenFromQuery();
        this.initializeAuth();
        if (this.isAuthenticated()) {
            return;
        }

        const refreshToken = this.tokenStorage.getRefreshToken();
        if (refreshToken === null || refreshToken.length === 0) {
            this.clearStoredIdentity();
            return;
        }

        await firstValueFrom(this.refreshToken());
    }

    private clearStoredIdentity(): void {
        this.authTokenSignal.set(null);
        this.userSignal.set(null);
        this.emailConfirmedSignal.set(null);
        this.tokenStorage.clearUserId();
        this.tokenStorage.clearEmailConfirmed();
    }

    private linkTelegramIfAvailable(): void {
        const initData = this.getTelegramInitData();
        if (initData === null || initData.length === 0) {
            return;
        }

        const request: TelegramAuthRequest = { initData };
        this.post<unknown>('telegram/link', request)
            .pipe(
                catchError(error => {
                    this.logger.warn('Telegram link failed', error);
                    return of(null);
                }),
            )
            .subscribe();
    }

    private getTelegramInitData(): string | null {
        const telegram = (window as { Telegram?: { WebApp?: { initData?: string } } }).Telegram;
        const initData = telegram?.WebApp?.initData;
        if (typeof initData !== 'string' || initData.length === 0) {
            return null;
        }

        const trimmed = initData.trim();
        return trimmed.length > 0 ? trimmed : null;
    }

    private hasRole(role: string): boolean {
        const token = this.authTokenSignal();
        if (token === null || token.length === 0) {
            return false;
        }

        return this.jwtDecoder.extractRoles(token).includes(role);
    }

    private getClientOrigin(): string | undefined {
        return typeof window === 'undefined' ? undefined : window.location.origin;
    }

    private captureImpersonationTokenFromQuery(): void {
        if (typeof window === 'undefined') {
            return;
        }

        const url = new URL(window.location.href);
        const token = url.searchParams.get('impersonationToken');
        if (token === null || token.length === 0) {
            return;
        }

        this.tokenStorage.clearAll();
        this.tokenStorage.setToken(token, false);
        this.authTokenSignal.set(token);

        url.searchParams.delete('impersonationToken');
        const nextUrl = `${url.pathname}${url.search}${url.hash}`;
        window.history.replaceState({}, '', nextUrl);
    }
}

type AdminSsoStartResponse = {
    code: string;
    expiresAtUtc: string;
};

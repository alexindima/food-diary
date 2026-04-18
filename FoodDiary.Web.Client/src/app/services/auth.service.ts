import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, map, Observable, of, tap } from 'rxjs';
import {
    AuthResponse,
    ConfirmPasswordResetRequest,
    LoginRequest,
    PasswordResetRequest,
    RegisterRequest,
    RestoreAccountRequest,
    TelegramAuthRequest,
    TelegramLoginWidgetRequest,
} from '../features/auth/models/auth.data';
import { environment } from '../../environments/environment';
import { ApiService } from './api.service';
import { NavigationService } from './navigation.service';
import { HttpErrorResponse } from '@angular/common/http';
import { GoogleLoginRequest } from '../features/auth/models/google-auth.data';
import { QuickMealService } from '../features/meals/lib/quick-meal.service';
import { LocalizationService } from './localization.service';
import { TokenStorageService } from './token-storage.service';
import { JwtDecoderService } from './jwt-decoder.service';
import { fallbackApiError, rethrowApiError } from '../shared/lib/api-error.utils';
import { ThemeService } from './theme.service';

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
    protected readonly baseUrl = environment.apiUrls.auth;

    private authTokenSignal = signal<string | null>(this.tokenStorage.getToken());
    private userSignal = signal<string | null>(this.tokenStorage.loadUserId());
    private emailConfirmedSignal = signal<boolean | null>(this.tokenStorage.loadEmailConfirmed());

    public readonly isAuthenticated = computed(() => this.authTokenSignal() !== null);
    public readonly isEmailConfirmed = computed(() => this.emailConfirmedSignal() ?? true);
    public readonly isAdmin = computed(() => this.hasRole('Admin'));
    public readonly isPremium = computed(() => this.hasRole('Premium'));
    public readonly isDietologist = computed(() => this.hasRole('Dietologist'));

    public initializeAuth(): void {
        const token = this.tokenStorage.getToken();
        if (!token) {
            this.tokenStorage.clearUserId();
            return;
        }

        this.authTokenSignal.set(token);
        if (!this.userSignal()) {
            const resolvedUserId = this.jwtDecoder.extractUserId(token);
            if (resolvedUserId) {
                this.tokenStorage.setUserId(resolvedUserId);
                this.userSignal.set(resolvedUserId);
            }
        }
        if (this.emailConfirmedSignal() === null) {
            const stored = this.tokenStorage.loadEmailConfirmed();
            this.emailConfirmedSignal.set(stored ?? true);
        }

        this.linkTelegramIfAvailable();
    }

    public login(data: LoginRequest): Observable<AuthResponse> {
        const loginData = { ...data, rememberMe: undefined };
        return this.post<AuthResponse>('login', loginData).pipe(
            tap(response => {
                this.onLogin(response, data.rememberMe || false);
            }),
            catchError((error: HttpErrorResponse) => rethrowApiError('Login error', error)),
        );
    }

    public register(data: RegisterRequest): Observable<AuthResponse> {
        return this.post<AuthResponse>('register', data).pipe(
            tap(response => {
                this.onLogin(response, false);
            }),
            catchError((error: HttpErrorResponse) => rethrowApiError('Register error', error)),
        );
    }

    public verifyEmail(userId: string, token: string): Observable<boolean> {
        return this.post<boolean>('verify-email', { userId, token }).pipe(
            tap(success => {
                if (success) {
                    this.setEmailConfirmed(true);
                }
            }),
            catchError((error: HttpErrorResponse) => rethrowApiError('Verify email error', error)),
        );
    }

    public resendEmailVerification(): Observable<boolean> {
        return this.post<boolean>('verify-email/resend', {}).pipe(
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

    public requestPasswordReset(data: PasswordResetRequest): Observable<boolean> {
        return this.post<boolean>('password-reset/request', data).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Password reset request error', error)),
        );
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
        const refreshToken = this.tokenStorage.getRefreshToken();
        if (!refreshToken) {
            void this.onLogout(true);
            return of(null);
        }

        return this.post<AuthResponse>('refresh', { refreshToken }).pipe(
            map(response => {
                const accessToken = response?.accessToken ?? null;
                if (accessToken) {
                    this.tokenStorage.setToken(accessToken);
                    this.tokenStorage.setRefreshToken(response.refreshToken);
                    this.authTokenSignal.set(accessToken);
                }
                return accessToken;
            }),
            catchError(error => {
                void this.onLogout(true);
                return fallbackApiError('refreshToken error', error, null);
            }),
        );
    }

    public async onLogout(redirectToAuth = false): Promise<void> {
        this.authTokenSignal.set(null);
        this.userSignal.set(null);
        this.emailConfirmedSignal.set(null);
        this.tokenStorage.clearAll();
        this.localizationService.clearStoredLanguage();
        if (redirectToAuth) {
            await this.navigationService.navigateToAuth('login');
            return;
        }
        await this.navigationService.navigateToHome();
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
        this.tokenStorage.setToken(authResponse.accessToken, rememberMe);
        this.tokenStorage.setRefreshToken(authResponse.refreshToken);
        this.authTokenSignal.set(authResponse.accessToken);

        const preferredLanguage = authResponse.user?.language;
        if (preferredLanguage) {
            void this.localizationService.applyLanguagePreference(preferredLanguage);
        }

        this.themeService.syncWithUserPreferences(authResponse.user?.theme, authResponse.user?.uiStyle);

        if (typeof authResponse.user?.isEmailConfirmed === 'boolean') {
            this.setEmailConfirmed(authResponse.user.isEmailConfirmed);
        } else {
            this.setEmailConfirmed(true);
        }

        const userId = authResponse.user?.id ?? this.jwtDecoder.extractUserId(authResponse.accessToken);
        if (userId) {
            this.tokenStorage.setUserId(userId);
            this.userSignal.set(userId);
        } else {
            console.warn('Auth response did not include user ID');
            this.tokenStorage.clearUserId();
            this.userSignal.set(null);
        }

        this.linkTelegramIfAvailable();
    }

    private linkTelegramIfAvailable(): void {
        const initData = this.getTelegramInitData();
        if (!initData) {
            return;
        }

        const request: TelegramAuthRequest = { initData };
        this.post<unknown>('telegram/link', request)
            .pipe(
                catchError(error => {
                    console.warn('Telegram link failed', error);
                    return of(null);
                }),
            )
            .subscribe();
    }

    private getTelegramInitData(): string | null {
        const telegram = (window as { Telegram?: { WebApp?: { initData?: string } } }).Telegram;
        const initData = telegram?.WebApp?.initData;
        if (!initData || typeof initData !== 'string') {
            return null;
        }

        const trimmed = initData.trim();
        return trimmed.length > 0 ? trimmed : null;
    }

    private hasRole(role: string): boolean {
        const token = this.authTokenSignal();
        if (!token) {
            return false;
        }

        return this.jwtDecoder.extractRoles(token).includes(role);
    }
}

type AdminSsoStartResponse = {
    code: string;
    expiresAtUtc: string;
};

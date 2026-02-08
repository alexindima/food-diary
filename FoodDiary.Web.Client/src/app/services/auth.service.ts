import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, map, Observable, of, tap, throwError } from 'rxjs';
import {
    AuthResponse,
    LoginRequest,
    RegisterRequest,
    RestoreAccountRequest,
    TelegramLoginWidgetRequest,
    TelegramAuthRequest,
    PasswordResetRequest,
    ConfirmPasswordResetRequest,
} from '../types/auth.data';
import { environment } from '../../environments/environment';
import { ApiService } from './api.service';
import { NavigationService } from './navigation.service';
import { HttpErrorResponse } from '@angular/common/http';
import { GoogleLoginRequest } from '../types/google-auth.data';
import { QuickConsumptionService } from './quick-consumption.service';
import { LocalizationService } from './localization.service';

@Injectable({
    providedIn: 'root',
})
export class AuthService extends ApiService {
    private readonly navigationService = inject(NavigationService);
    private readonly quickConsumptionService = inject(QuickConsumptionService);
    private readonly localizationService = inject(LocalizationService);
    protected readonly baseUrl = environment.apiUrls.auth;

    private authTokenSignal = signal<string | null>(this.getToken());
    private userSignal = signal<string | null>(this.loadUserId());
    private emailConfirmedSignal = signal<boolean | null>(this.loadEmailConfirmed());

    public readonly isAuthenticated = computed(() => this.authTokenSignal() !== null);
    public readonly isEmailConfirmed = computed(() => this.emailConfirmedSignal() ?? true);
    public readonly isAdmin = computed(() => this.hasRole('Admin'));

    public initializeAuth(): void {
        const token = this.getToken();
        if (!token) {
            this.clearUserId();
            return;
        }

        this.authTokenSignal.set(token);
        if (!this.userSignal()) {
            const resolvedUserId = this.extractUserIdFromToken(token);
            if (resolvedUserId) {
                this.setUserId(resolvedUserId);
                this.userSignal.set(resolvedUserId);
            }
        }
        if (this.emailConfirmedSignal() === null) {
            const stored = this.loadEmailConfirmed();
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
            catchError((error: HttpErrorResponse) => {
                console.error('Login error', error);
                return throwError(() => error);
            }),
        );
    }

    public register(data: RegisterRequest): Observable<AuthResponse> {
        return this.post<AuthResponse>('register', data).pipe(
            tap(response => {
                this.onLogin(response, false);
            }),
            catchError((error: HttpErrorResponse) => {
                console.error('Register error', error);
                return throwError(() => error);
            }),
        );
    }

    public verifyEmail(userId: string, token: string): Observable<boolean> {
        return this.post<boolean>('verify-email', { userId, token }).pipe(
            tap(success => {
                if (success) {
                    this.setEmailConfirmed(true);
                }
            }),
            catchError((error: HttpErrorResponse) => {
                console.error('Verify email error', error);
                return throwError(() => error);
            }),
        );
    }

    public resendEmailVerification(): Observable<boolean> {
        return this.post<boolean>('verify-email/resend', {}).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Resend email verification error', error);
                return throwError(() => error);
            }),
        );
    }

    public restoreAccount(data: RestoreAccountRequest, rememberMe = false): Observable<AuthResponse> {
        return this.post<AuthResponse>('restore', data).pipe(
            tap(response => {
                this.onLogin(response, rememberMe);
            }),
            catchError((error: HttpErrorResponse) => {
                console.error('Restore account error', error);
                return throwError(() => error);
            }),
        );
    }

    public loginWithGoogle(data: GoogleLoginRequest): Observable<AuthResponse> {
        return this.post<AuthResponse>('google', data).pipe(
            tap(response => {
                this.onLogin(response, data.rememberMe ?? false);
            }),
            catchError((error: HttpErrorResponse) => {
                console.error('Google login error', error);
                return throwError(() => error);
            }),
        );
    }

    public requestPasswordReset(data: PasswordResetRequest): Observable<boolean> {
        return this.post<boolean>('password-reset/request', data).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Password reset request error', error);
                return throwError(() => error);
            }),
        );
    }

    public confirmPasswordReset(data: ConfirmPasswordResetRequest): Observable<AuthResponse> {
        return this.post<AuthResponse>('password-reset/confirm', data).pipe(
            tap(response => {
                this.onLogin(response, false);
            }),
            catchError((error: HttpErrorResponse) => {
                console.error('Password reset confirm error', error);
                return throwError(() => error);
            }),
        );
    }

    public loginWithTelegramWidget(data: TelegramLoginWidgetRequest, rememberMe: boolean): Observable<AuthResponse> {
        return this.post<AuthResponse>('telegram/login-widget', data).pipe(
            tap(response => {
                this.onLogin(response, rememberMe);
            }),
            catchError((error: HttpErrorResponse) => {
                console.error('Telegram login error', error);
                return throwError(() => error);
            }),
        );
    }

    public startAdminSso(): Observable<AdminSsoStartResponse> {
        return this.post<AdminSsoStartResponse>('admin-sso/start', {});
    }

    public refreshToken(): Observable<string | null> {
        const refreshToken = this.getRefreshToken();
        if (!refreshToken) {
            void this.onLogout(true);
            return of(null);
        }

        return this.post<{ accessToken: string }>('refresh', { refreshToken }).pipe(
            map(response => {
                const accessToken = response?.accessToken ?? null;
                if (accessToken) {
                    this.setToken(accessToken);
                    this.authTokenSignal.set(accessToken);
                }
                return accessToken;
            }),
            catchError(error => {
                console.error('refreshToken error', error);
                void this.onLogout(true);
                return of(null);
            }),
        );
    }

    public async onLogout(redirectToAuth = false): Promise<void> {
        this.authTokenSignal.set(null);
        this.userSignal.set(null);
        this.emailConfirmedSignal.set(null);
        this.clearToken();
        this.clearUserId();
        this.clearRefreshToken();
        this.clearEmailConfirmed();
        this.localizationService.clearStoredLanguage();
        if (redirectToAuth) {
            await this.navigationService.navigateToAuth('login');
            return;
        }
        await this.navigationService.navigateToHome();
    }

    private onLogin(authResponse: AuthResponse, rememberMe: boolean): void {
        this.quickConsumptionService.exitPreview();
        this.setToken(authResponse.accessToken, rememberMe);
        this.setRefreshToken(authResponse.refreshToken);
        this.authTokenSignal.set(authResponse.accessToken);

        const preferredLanguage = authResponse.user?.language;
        if (preferredLanguage) {
            void this.localizationService.applyLanguagePreference(preferredLanguage);
        }

        if (typeof authResponse.user?.isEmailConfirmed === 'boolean') {
            this.setEmailConfirmed(authResponse.user.isEmailConfirmed);
        } else {
            this.setEmailConfirmed(true);
        }

        const userId = authResponse.user?.id ?? this.extractUserIdFromToken(authResponse.accessToken);
        if (userId) {
            this.setUserId(userId);
            this.userSignal.set(userId);
        } else {
            console.warn('Auth response did not include user ID');
            this.clearUserId();
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

    public getToken(): string | null {
        return localStorage.getItem('authToken') || sessionStorage.getItem('authToken');
    }

    private setToken(token: string, rememberMe?: boolean): void {
        if (rememberMe !== undefined) {
            if (rememberMe) {
                localStorage.setItem('authToken', token);
                sessionStorage.removeItem('authToken');
            } else {
                sessionStorage.setItem('authToken', token);
                localStorage.removeItem('authToken');
            }
        } else {
            if (localStorage.getItem('authToken') !== null) {
                localStorage.setItem('authToken', token);
            } else {
                sessionStorage.setItem('authToken', token);
            }
        }
    }

    private clearToken(): void {
        localStorage.removeItem('authToken');
        sessionStorage.removeItem('authToken');
    }

    private setRefreshToken(token: string | null | undefined): void {
        if (!token) {
            this.clearRefreshToken();
            return;
        }
        localStorage.setItem('refreshToken', token);
    }

    private getRefreshToken(): string | null {
        const token = localStorage.getItem('refreshToken');
        if (!token || token === 'undefined' || token === 'null') {
            this.clearRefreshToken();
            return null;
        }
        return token;
    }

    private clearRefreshToken(): void {
        localStorage.removeItem('refreshToken');
    }

    public getUserId(): string | null {
        return this.userSignal();
    }

    private setUserId(userId: string | null): void {
        if (userId) {
            localStorage.setItem('userId', userId);
        } else {
            localStorage.removeItem('userId');
        }
    }

    private loadUserId(): string | null {
        const storedId = localStorage.getItem('userId');
        if (!storedId || storedId === 'undefined') {
            return null;
        }
        return storedId;
    }

    private clearUserId(): void {
        localStorage.removeItem('userId');
        sessionStorage.removeItem('userId');
    }

    public setEmailConfirmed(value: boolean): void {
        this.emailConfirmedSignal.set(value);
        localStorage.setItem('emailConfirmed', value ? 'true' : 'false');
    }

    private loadEmailConfirmed(): boolean | null {
        const stored = localStorage.getItem('emailConfirmed');
        if (stored === 'true') {
            return true;
        }
        if (stored === 'false') {
            return false;
        }
        return null;
    }

    private clearEmailConfirmed(): void {
        localStorage.removeItem('emailConfirmed');
    }

    private extractUserIdFromToken(token: string | null): string | null {
        if (!token) {
            return null;
        }

        const [, payloadSegment] = token.split('.');
        if (!payloadSegment) {
            return null;
        }

        try {
            const normalized = payloadSegment.replace(/-/g, '+').replace(/_/g, '/');
            const padLength = (4 - (normalized.length % 4 || 4)) % 4;
            const padded = normalized.padEnd(normalized.length + padLength, '=');
            const decoded = atob(padded);
            const payload = JSON.parse(decoded);
            return (
                payload['nameid'] ||
                payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ||
                payload['sub'] ||
                null
            );
        } catch {
            return null;
        }
    }

    private hasRole(role: string): boolean {
        const token = this.authTokenSignal();
        if (!token) {
            return false;
        }

        return this.extractRolesFromToken(token).includes(role);
    }

    private extractRolesFromToken(token: string): string[] {
        const payload = this.decodePayload(token);
        if (!payload) {
            return [];
        }

        const roleClaim =
            payload['role'] ??
            payload['roles'] ??
            payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
            payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role'];

        if (Array.isArray(roleClaim)) {
            return roleClaim.filter((value): value is string => typeof value === 'string');
        }

        if (typeof roleClaim === 'string') {
            return [roleClaim];
        }

        return [];
    }

    private decodePayload(token: string): Record<string, unknown> | null {
        const [, payloadSegment] = token.split('.');
        if (!payloadSegment) {
            return null;
        }

        try {
            const normalized = payloadSegment.replace(/-/g, '+').replace(/_/g, '/');
            const padLength = (4 - (normalized.length % 4 || 4)) % 4;
            const padded = normalized.padEnd(normalized.length + padLength, '=');
            const decoded = atob(padded);
            return JSON.parse(decoded) as Record<string, unknown>;
        } catch {
            return null;
        }
    }
}

type AdminSsoStartResponse = {
    code: string;
    expiresAtUtc: string;
};

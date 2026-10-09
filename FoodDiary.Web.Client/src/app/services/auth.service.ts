import { HttpClient, HttpStatusCode } from '@angular/common/http';
import { computed, DestroyRef, inject, Service, signal } from '@angular/core';
import { catchError, defer, finalize, firstValueFrom, from, map, type Observable, of, shareReplay, tap } from 'rxjs';

import { environment } from '../../environments/environment';
import { authResponseFromSdk } from '../shared/api/sdk/auth-sdk.mapper';
import { AuthSdk } from '../shared/api/sdk/generated/api/auth.service';
import { TelegramAuthSdk } from '../shared/api/sdk/generated/api/telegram-auth.service';
import { createSdkConnection } from '../shared/api/sdk/sdk-connection';
import { requireSdkFields } from '../shared/api/sdk/sdk-response';
import { userFromSdk } from '../shared/api/sdk/user-sdk.mapper';
import type {
    AuthResponse,
    ConfirmPasswordResetRequest,
    LoginRequest,
    PasswordResetRequest,
    RegisterRequest,
    RestoreAccountRequest,
} from '../shared/auth/auth.data';
import type { GoogleLoginRequest } from '../shared/auth/google-auth.data';
import { SessionEventsService } from '../shared/auth/session-events.service';
import type { TelegramOAuthCode, TelegramOAuthState } from '../shared/auth/telegram-oidc-values';
import { LocalizationService } from '../shared/i18n/localization.service';
import { fallbackApiError, rethrowApiError } from '../shared/lib/api-error.utils';
import { getNumberProperty } from '../shared/lib/unknown-value.utils';
import type { User } from '../shared/models/user.data';
import { BrowserWindowService } from '../shared/platform/browser-window.service';
import { ThemeService } from '../shared/theme/theme.service';
import { FrontendLoggerService } from './frontend-logger.service';
import { JwtDecoderService } from './jwt-decoder.service';
import { NavigationService } from './navigation.service';
import { TokenStorageService } from './token-storage.service';

const TOKEN_EXPIRATION_LEEWAY_SECONDS = 30;
const AUTH_REFRESH_LOCK_NAME = 'fooddiary.auth.refresh';

@Service()
export class AuthService {
    private readonly navigationService = inject(NavigationService);
    private readonly sessionEvents = inject(SessionEventsService);
    private readonly browserWindow = inject(BrowserWindowService);
    private readonly localizationService = inject(LocalizationService);
    private readonly themeService = inject(ThemeService);
    private readonly tokenStorage = inject(TokenStorageService);
    private readonly jwtDecoder = inject(JwtDecoderService);
    private readonly logger = inject(FrontendLoggerService);
    protected readonly baseUrl = environment.apiUrls.auth;
    private readonly http = inject(HttpClient);
    private readonly sdk = createSdkConnection(AuthSdk, this.baseUrl, this.http);
    private readonly telegram = createSdkConnection(TelegramAuthSdk, this.baseUrl, this.http);

    private readonly authTokenSignal = signal<string | null>(this.tokenStorage.getToken());
    private readonly userSignal = signal<string | null>(this.tokenStorage.loadUserId());
    private readonly emailConfirmedSignal = signal<boolean | null>(this.tokenStorage.loadEmailConfirmed());
    private readonly mustChangePasswordSignal = signal<boolean>(this.tokenStorage.loadMustChangePassword() ?? false);
    private refreshInFlight$: Observable<string | null> | null = null;
    private pendingLegacyRefreshToken: string | null = null;
    private sessionRestorePromise: Promise<void> | null = null;
    private readonly authReadySignal = signal(false);
    private sessionVersion = 0;

    public readonly isAuthenticated = computed(() => this.authTokenSignal() !== null);
    public readonly isEmailConfirmed = computed(() => this.emailConfirmedSignal() ?? true);
    public readonly requiresEmailVerification = computed(
        () => !this.isEmailConfirmed() && !this.jwtDecoder.isEmailOptional(this.authTokenSignal()),
    );
    public readonly mustChangePassword = this.mustChangePasswordSignal.asReadonly();
    public readonly isAdmin = computed(() => this.hasRole('Admin'));
    public readonly isPremium = computed(() => this.hasRole('Premium'));
    public readonly isDietologist = computed(() => this.hasRole('Dietologist'));
    public readonly isImpersonating = computed(() => this.jwtDecoder.isImpersonation(this.authTokenSignal()));
    public readonly impersonationReason = computed(() => this.jwtDecoder.extractImpersonationReason(this.authTokenSignal()));
    public readonly isAuthReady = this.authReadySignal.asReadonly();

    public constructor() {
        const stopListening = this.browserWindow.onLocalStorageChange(event => {
            const sessionRemoved = event.key === 'refreshSession' && event.newValue === null;
            const storageCleared = event.key === null;
            if ((sessionRemoved || storageCleared) && this.isAuthenticated() && !this.tokenStorage.hasRefreshSession()) {
                void this.onLogoutAsync(true);
            }
        });
        inject(DestroyRef).onDestroy(stopListening);
    }

    public initializeAuth(): void {
        let token = this.tokenStorage.getToken();
        if (token === null || token.length === 0) {
            this.clearStoredIdentity();
            return;
        }

        if (this.jwtDecoder.decodePayload(token) === null) {
            this.authTokenSignal.set(null);
            this.tokenStorage.clearToken();
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
            rememberMe: data.rememberMe,
        };
        return this.sdk.client.postAuthLogin({ version: this.sdk.version, loginHttpRequest: loginData }).pipe(
            map(authResponseFromSdk),
            tap(response => {
                this.onLogin(response, data.rememberMe || false);
            }),
            catchError((error: unknown) => rethrowApiError('Login error', error)),
        );
    }

    public register(data: RegisterRequest): Observable<AuthResponse> {
        return this.sdk.client
            .postAuthRegister({
                version: this.sdk.version,
                registerHttpRequest: {
                    email: data.email,
                    password: data.password,
                    language: data.language,
                    clientOrigin: this.getClientOrigin(),
                },
            })
            .pipe(
                map(authResponseFromSdk),
                tap(response => {
                    this.onLogin(response, false);
                }),
                catchError((error: unknown) => rethrowApiError('Register error', error)),
            );
    }

    public verifyEmail(userId: string, token: string): Observable<void> {
        return this.sdk.client.postAuthVerifyEmail({ version: this.sdk.version, verifyEmailHttpRequest: { userId, token } }).pipe(
            tap(() => {
                this.setEmailConfirmed(true);
            }),
            catchError((error: unknown) => rethrowApiError('Verify email error', error)),
        );
    }

    public resendEmailVerification(): Observable<void> {
        return this.sdk.client
            .postAuthVerifyEmailResend({
                version: this.sdk.version,
                resendEmailVerificationHttpRequest: { clientOrigin: this.getClientOrigin() },
            })
            .pipe(catchError((error: unknown) => rethrowApiError('Resend email verification error', error)));
    }

    public restoreAccount(data: RestoreAccountRequest, rememberMe = false): Observable<AuthResponse> {
        return this.sdk.client
            .postAuthRestore({
                version: this.sdk.version,
                restoreAccountHttpRequest: {
                    email: data.email,
                    password: data.password,
                    rememberMe,
                },
            })
            .pipe(
                map(authResponseFromSdk),
                tap(response => {
                    this.onLogin(response, rememberMe);
                }),
                catchError((error: unknown) => rethrowApiError('Restore account error', error)),
            );
    }

    public loginWithGoogle(data: GoogleLoginRequest): Observable<AuthResponse> {
        return this.sdk.client.postAuthGoogle({ version: this.sdk.version, googleLoginHttpRequest: data }).pipe(
            map(authResponseFromSdk),
            tap(response => {
                this.onLogin(response, data.rememberMe ?? false);
            }),
            catchError((error: unknown) => rethrowApiError('Google login error', error)),
        );
    }

    public linkGoogle(credential: string): Observable<User> {
        return this.sdk.client.postAuthGoogleLink({ version: this.sdk.version, googleLoginHttpRequest: { credential } }).pipe(
            map(userFromSdk),
            catchError((error: unknown) => rethrowApiError('Google link error', error)),
        );
    }

    public unlinkTelegram(initData: string): Observable<void> {
        return this.telegram.client.postAuthTelegramUnlink({ version: this.telegram.version, telegramAuthHttpRequest: { initData } });
    }

    public requestTelegramBackupEmail(email: string, initData: string): Observable<void> {
        return this.telegram.client.postAuthTelegramBackupEmail({
            version: this.telegram.version,
            telegramBackupEmailHttpRequest: { email, initData },
        });
    }

    public startTelegramBackupEmail(email: string): Observable<{ authorizationUrl: string }> {
        return this.telegram.client
            .postAuthTelegramBackupEmailOidcStart({ version: this.telegram.version, startTelegramBackupEmailHttpRequest: { email } })
            .pipe(map(value => requireSdkFields(value, ['authorizationUrl'])));
    }

    public completeTelegramBackupEmail(code: TelegramOAuthCode, state: TelegramOAuthState): Observable<void> {
        return this.telegram.client.postAuthTelegramBackupEmailOidcComplete({
            version: this.telegram.version,
            exchangeTelegramOidcHttpRequest: { code, state },
        });
    }

    public requestPasswordReset(data: PasswordResetRequest): Observable<void> {
        return this.sdk.client
            .postAuthPasswordResetRequest({
                version: this.sdk.version,
                requestPasswordResetHttpRequest: {
                    email: data.email,
                    clientOrigin: this.getClientOrigin(),
                },
            })
            .pipe(catchError((error: unknown) => rethrowApiError('Password reset request error', error)));
    }

    public confirmPasswordReset(data: ConfirmPasswordResetRequest): Observable<AuthResponse> {
        return this.sdk.client.postAuthPasswordResetConfirm({ version: this.sdk.version, confirmPasswordResetHttpRequest: data }).pipe(
            map(authResponseFromSdk),
            tap(response => {
                this.onLogin(response, false);
            }),
            catchError((error: unknown) => rethrowApiError('Password reset confirm error', error)),
        );
    }

    public startAdminSso(): Observable<AdminSsoStartResponse> {
        return this.sdk.client
            .postAuthAdminSsoStart({ version: this.sdk.version })
            .pipe(map(value => requireSdkFields(value, ['code', 'expiresAtUtc'])));
    }

    public refreshToken(): Observable<string | null> {
        if (this.refreshInFlight$ !== null) {
            return this.refreshInFlight$;
        }

        if (!this.tokenStorage.hasRefreshSession()) {
            void this.onLogoutAsync(true);
            return of(null);
        }

        const refreshVersion = this.sessionVersion;
        // Refresh cookies are shared by tabs, so their rotation must also be shared.
        const refreshRequest$ = defer(() =>
            from(this.browserWindow.runWithLockAsync(AUTH_REFRESH_LOCK_NAME, async () => this.refreshSessionAsync(refreshVersion))),
        ).pipe(
            finalize(() => {
                if (this.refreshInFlight$ === refreshRequest$) {
                    this.refreshInFlight$ = null;
                }
            }),
            shareReplay(1),
        );

        this.refreshInFlight$ = refreshRequest$;
        return refreshRequest$;
    }

    private async refreshSessionAsync(refreshVersion: number): Promise<string | null> {
        if (refreshVersion !== this.sessionVersion || !this.tokenStorage.hasRefreshSession()) {
            return null;
        }

        this.pendingLegacyRefreshToken ??= this.tokenStorage.consumeLegacyRefreshToken();
        const legacyRefreshToken = this.pendingLegacyRefreshToken;
        if (legacyRefreshToken !== null) {
            this.tokenStorage.markRefreshSession();
        }
        const request = legacyRefreshToken === null ? {} : { refreshToken: legacyRefreshToken };
        return firstValueFrom(
            this.sdk.client.postAuthRefresh({ version: this.sdk.version, refreshTokenHttpRequest: request }).pipe(
                map(authResponseFromSdk),
                map(response => {
                    if (refreshVersion !== this.sessionVersion) {
                        return null;
                    }
                    const accessToken = response.accessToken;
                    if (accessToken.length > 0) {
                        this.applyAuthenticatedSession(response);
                    }
                    return accessToken;
                }),
                catchError((error: unknown) => {
                    if (refreshVersion === this.sessionVersion && getNumberProperty(error, 'status') === HttpStatusCode.Unauthorized) {
                        void this.onLogoutAsync(true);
                    }
                    return fallbackApiError('refreshToken error', error, null);
                }),
            ),
        );
    }

    public async onLogoutAsync(redirectToAuth = false): Promise<void> {
        this.sessionVersion++;
        const hasRefreshSession = this.tokenStorage.hasRefreshSession();
        const legacyRefreshToken = this.pendingLegacyRefreshToken ?? this.tokenStorage.consumeLegacyRefreshToken();
        this.pendingLegacyRefreshToken = null;
        if (hasRefreshSession) {
            const request = legacyRefreshToken === null ? {} : { refreshToken: legacyRefreshToken };
            await firstValueFrom(
                this.sdk.client
                    .postAuthLogout({ version: this.sdk.version, refreshTokenHttpRequest: request })
                    .pipe(catchError(() => of(undefined))),
            );
        }
        this.sessionEvents.notifySessionEnded();
        this.authTokenSignal.set(null);
        this.userSignal.set(null);
        this.emailConfirmedSignal.set(null);
        this.mustChangePasswordSignal.set(false);
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

    public async completeRequiredPasswordChangeAsync(newPassword: string): Promise<void> {
        // Password changes revoke every old session. Acquire a fresh session before navigating.
        const payload = this.jwtDecoder.decodePayload(this.getToken() ?? '');
        const email = payload?.['email'] ?? payload?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'];
        if (typeof email !== 'string' || email.length === 0) {
            throw new Error('Missing session email');
        }
        await firstValueFrom(this.login({ email, password: newPassword, rememberMe: this.tokenStorage.isRemembered() }));
    }

    private onLogin(authResponse: AuthResponse, rememberMe: boolean): void {
        this.sessionVersion++;
        this.refreshInFlight$ = null;
        this.sessionEvents.notifyAuthenticated();
        this.applyAuthenticatedSession(authResponse, rememberMe);
    }

    public acceptExternalAuthentication(response: AuthResponse): void {
        this.onLogin(response, false);
    }

    private applyAuthenticatedSession(authResponse: AuthResponse, rememberMe?: boolean): void {
        this.pendingLegacyRefreshToken = null;
        this.tokenStorage.setToken(authResponse.accessToken, rememberMe);
        this.tokenStorage.markRefreshSession();
        this.authTokenSignal.set(authResponse.accessToken);

        const preferredLanguage = authResponse.user.language;
        if (preferredLanguage !== undefined && preferredLanguage.length > 0) {
            void this.localizationService.applyLanguagePreferenceAsync(preferredLanguage);
        }

        this.themeService.syncWithUserPreferences(authResponse.user.theme, authResponse.user.uiStyle, authResponse.user.surfaceStyle);

        if (typeof authResponse.user.isEmailConfirmed === 'boolean') {
            this.setEmailConfirmed(authResponse.user.isEmailConfirmed);
        } else {
            this.setEmailConfirmed(true);
        }

        this.mustChangePasswordSignal.set(authResponse.user.mustChangePassword === true);
        this.tokenStorage.setMustChangePassword(authResponse.user.mustChangePassword === true);

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
        const impersonationExchange = this.captureImpersonationCodeFromQueryAsync();
        if (impersonationExchange !== null) {
            await impersonationExchange;
        }
        this.initializeAuth();
        if (this.isAuthenticated()) {
            return;
        }

        if (!this.tokenStorage.hasRefreshSession()) {
            this.clearStoredIdentity();
            return;
        }

        await firstValueFrom(this.refreshToken());
    }

    private clearStoredIdentity(): void {
        this.authTokenSignal.set(null);
        this.userSignal.set(null);
        this.emailConfirmedSignal.set(null);
        this.mustChangePasswordSignal.set(false);
        this.tokenStorage.clearUserId();
        this.tokenStorage.clearEmailConfirmed();
        this.tokenStorage.clearMustChangePassword();
    }

    private hasRole(role: string): boolean {
        const token = this.authTokenSignal();
        if (token === null || token.length === 0) {
            return false;
        }

        return this.jwtDecoder.extractRoles(token).includes(role);
    }

    private getClientOrigin(): string | undefined {
        return this.browserWindow.getOrigin();
    }

    private captureImpersonationCodeFromQueryAsync(): Promise<void> | null {
        const currentHref = this.browserWindow.getHref();
        if (currentHref === null) {
            return null;
        }

        const url = new URL(currentHref);
        const code = url.searchParams.get('impersonationCode');
        if (code === null || code.length === 0) {
            return null;
        }

        url.searchParams.delete('impersonationCode');
        const nextUrl = `${url.pathname}${url.search}${url.hash}`;
        this.browserWindow.replaceCurrentUrl(nextUrl);

        return firstValueFrom(
            this.sdk.client.postAuthImpersonationExchange({ version: this.sdk.version, exchangeImpersonationHttpRequest: { code } }).pipe(
                map(value => requireSdkFields(value, ['accessToken'])),
                catchError((error: unknown) => fallbackApiError('impersonation exchange error', error, null)),
            ),
        ).then(response => {
            const token = response?.accessToken;
            if (token === undefined || token.length === 0) {
                return;
            }

            this.tokenStorage.clearAll();
            this.tokenStorage.setToken(token, false);
            this.authTokenSignal.set(token);
        });
    }
}

type AdminSsoStartResponse = {
    code: string;
    expiresAtUtc: string;
};

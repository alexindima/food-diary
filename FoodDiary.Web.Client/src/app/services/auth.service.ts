import { HttpClient, HttpStatusCode } from '@angular/common/http';
import { computed, DestroyRef, inject, Service, signal } from '@angular/core';
import { catchError, defer, finalize, firstValueFrom, map, type Observable, of, shareReplay, tap } from 'rxjs';

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
import { AuthCookieSessionService } from '../shared/auth/auth-cookie-session.service';
import { EmailVerificationCooldownService } from '../shared/auth/email-verification-cooldown.service';
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
import { ACCOUNT_ACCESS_STORAGE_KEY, TokenStorageService } from './token-storage.service';

const TOKEN_EXPIRATION_LEEWAY_SECONDS = 30;
type SessionIdentity = { userId: string; impersonating: boolean };

@Service()
export class AuthService {
    private readonly navigationService = inject(NavigationService);
    private readonly emailVerificationCooldown = inject(EmailVerificationCooldownService);
    private readonly sessionEvents = inject(SessionEventsService);
    private readonly cookieSessions = inject(AuthCookieSessionService);
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
    private sessionIdentity = this.readSessionIdentity(this.authTokenSignal());
    private initialized = false;
    private restoredPolicyUnknown = false;
    private readonly userSignal = signal<string | null>(this.sessionIdentity?.userId ?? null);
    private readonly emailConfirmedSignal = signal<boolean | null>(
        this.tokenStorage.loadEmailConfirmed(this.sessionIdentity?.userId ?? null),
    );
    private readonly mustChangePasswordSignal = signal<boolean>(
        this.tokenStorage.loadMustChangePassword(this.sessionIdentity?.userId ?? null) ?? false,
    );
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
        const token = this.authTokenSignal();
        if (token !== null && this.sessionIdentity !== null) {
            this.tokenStorage.pinToken(token, this.sessionIdentity.userId);
        }
        const stopListening = this.browserWindow.onLocalStorageChange(event => {
            this.onSharedStorageChange(event);
        });
        inject(DestroyRef).onDestroy(stopListening);
    }

    private onSharedStorageChange(event: StorageEvent): void {
        if (this.sessionIdentity?.impersonating === true) {
            return;
        }
        const owner = this.sessionIdentity?.userId;
        if (owner !== undefined && this.tokenStorage.hasForeignSession(owner)) {
            this.invalidateLocalSession();
            return;
        }
        if (this.isSharedSessionRemoved(event)) {
            void this.onLogoutAsync(true);
        }
        if (event.key === ACCOUNT_ACCESS_STORAGE_KEY && this.hasUpdatedAccountAccess()) {
            // The snapshot is only an invalidation hint; roles always come from the server.
            void firstValueFrom(this.refreshToken());
        }
    }

    private isSharedSessionRemoved(event: StorageEvent): boolean {
        const sessionRemoved = event.key === 'refreshSession' && event.newValue === null;
        return (sessionRemoved || event.key === null) && this.isAuthenticated() && !this.tokenStorage.hasRefreshSession();
    }

    public initializeAuth(): void {
        let token = this.authTokenSignal() ?? (this.initialized ? null : this.tokenStorage.getToken());
        this.initialized = true;
        if (token === null || token.length === 0) {
            if (this.sessionIdentity === null) {
                this.clearStoredIdentity();
            }
            return;
        }

        const identity = this.readSessionIdentity(token);
        if (identity === null) {
            this.authTokenSignal.set(null);
            this.tokenStorage.clearToken();
            this.clearStoredIdentity();
            return;
        }

        this.restoreTokenIdentity(token, identity);

        if (this.clearExpiredToken(token)) {
            token = null;
        }

        if (token === null || token.length === 0) {
            return;
        }

        this.authTokenSignal.set(token);
    }

    private restoreTokenIdentity(token: string, identity: SessionIdentity): void {
        this.sessionIdentity = identity;
        this.tokenStorage.pinToken(token, identity.userId);
        const emailConfirmed = this.tokenStorage.loadEmailConfirmed(identity.userId);
        const mustChangePassword = this.tokenStorage.loadMustChangePassword(identity.userId);
        this.restoredPolicyUnknown = !identity.impersonating && (emailConfirmed === null || mustChangePassword === null);
        this.emailConfirmedSignal.set(emailConfirmed ?? identity.impersonating);
        this.mustChangePasswordSignal.set(mustChangePassword ?? !identity.impersonating);
        this.restoreUserIdFromToken(token, !this.restoredPolicyUnknown);
    }

    private restoreUserIdFromToken(token: string, persistMetadata: boolean): void {
        const resolvedUserId = this.jwtDecoder.extractUserId(token);
        if (resolvedUserId !== null && resolvedUserId.length > 0) {
            if (persistMetadata) {
                this.tokenStorage.setUserId(resolvedUserId);
            }
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
        return this.cookieSessions.run(() =>
            this.sdk.client.postAuthLogin({ version: this.sdk.version, loginHttpRequest: loginData }).pipe(
                map(authResponseFromSdk),
                tap(response => {
                    this.onLogin(response, data.rememberMe || false);
                }),
                catchError((error: unknown) => rethrowApiError('Login error', error)),
            ),
        );
    }

    public register(data: RegisterRequest): Observable<AuthResponse> {
        return this.cookieSessions.run(() =>
            this.sdk.client
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
                        this.emailVerificationCooldown.markSent(response.user.id);
                    }),
                    catchError((error: unknown) => rethrowApiError('Register error', error)),
                ),
        );
    }

    public verifyEmail(userId: string, token: string): Observable<void> {
        return defer(() => {
            const verificationVersion = this.sessionVersion;
            const owner = this.getUserId();
            return this.sdk.client.postAuthVerifyEmail({ version: this.sdk.version, verifyEmailHttpRequest: { userId, token } }).pipe(
                tap(() => {
                    if (
                        owner === userId &&
                        verificationVersion === this.sessionVersion &&
                        this.getUserId() === userId &&
                        this.isAuthenticated()
                    ) {
                        this.setEmailConfirmed(true);
                    }
                }),
                catchError((error: unknown) => rethrowApiError('Verify email error', error)),
            );
        });
    }

    public resendEmailVerification(): Observable<void> {
        return this.sdk.client
            .postAuthVerifyEmailResend({
                version: this.sdk.version,
                resendEmailVerificationHttpRequest: { clientOrigin: this.getClientOrigin() },
            })
            .pipe(
                tap(() => {
                    this.emailVerificationCooldown.markSent(this.getUserId());
                }),
                catchError((error: unknown) => rethrowApiError('Resend email verification error', error)),
            );
    }

    public restoreAccount(data: RestoreAccountRequest, rememberMe = false): Observable<AuthResponse> {
        return this.cookieSessions.run(() =>
            this.sdk.client
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
                ),
        );
    }

    public loginWithGoogle(data: GoogleLoginRequest): Observable<AuthResponse> {
        return this.cookieSessions.run(() =>
            this.sdk.client.postAuthGoogle({ version: this.sdk.version, googleLoginHttpRequest: data }).pipe(
                map(authResponseFromSdk),
                tap(response => {
                    this.onLogin(response, data.rememberMe ?? false);
                }),
                catchError((error: unknown) => rethrowApiError('Google login error', error)),
            ),
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
        return this.cookieSessions.run(() =>
            this.sdk.client.postAuthPasswordResetConfirm({ version: this.sdk.version, confirmPasswordResetHttpRequest: data }).pipe(
                map(authResponseFromSdk),
                tap(response => {
                    this.onLogin(response, false);
                }),
                catchError((error: unknown) => rethrowApiError('Password reset confirm error', error)),
            ),
        );
    }

    public startAdminSso(): Observable<AdminSsoStartResponse> {
        return this.sdk.client
            .postAuthAdminSsoStart({ version: this.sdk.version })
            .pipe(map(value => requireSdkFields(value, ['code', 'expiresAtUtc'])));
    }

    public refreshToken(): Observable<string | null> {
        if (this.sessionEvents.isLocalSessionInvalidated) {
            return of(null);
        }
        if (this.sessionIdentity?.impersonating === true) {
            this.invalidateLocalSession();
            return of(null);
        }
        if (this.refreshInFlight$ !== null) {
            return this.refreshInFlight$;
        }

        if (!this.tokenStorage.hasRefreshSession()) {
            void this.onLogoutAsync(true);
            return of(null);
        }

        const refreshVersion = this.sessionVersion;
        const expectedOwner = this.sessionIdentity?.userId ?? null;
        const refreshRequest$ = this.cookieSessions
            .run(() => this.createRefreshRequest(refreshVersion, expectedOwner))
            .pipe(
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

    private createRefreshRequest(refreshVersion: number, expectedOwner: string | null): Observable<string | null> {
        if (refreshVersion !== this.sessionVersion || !this.tokenStorage.hasRefreshSession()) {
            return of(null);
        }

        if (expectedOwner !== null && this.tokenStorage.hasForeignSession(expectedOwner)) {
            this.invalidateLocalSession();
            return of(null);
        }

        this.pendingLegacyRefreshToken ??=
            expectedOwner === null ? this.tokenStorage.peekLegacyRefreshTokenForBootstrap() : this.tokenStorage.consumeLegacyRefreshToken();
        const legacyRefreshToken = this.pendingLegacyRefreshToken;
        if (legacyRefreshToken !== null && expectedOwner !== null) {
            this.tokenStorage.markRefreshSession();
        }
        const request = legacyRefreshToken === null ? {} : { refreshToken: legacyRefreshToken };
        return this.sdk.client.postAuthRefresh({ version: this.sdk.version, refreshTokenHttpRequest: request }).pipe(
            map(authResponseFromSdk),
            map(response => {
                if (refreshVersion !== this.sessionVersion) {
                    return null;
                }
                if (!this.isValidSessionResponse(response, expectedOwner)) {
                    this.invalidateLocalSession();
                    return null;
                }
                const accessToken = response.accessToken;
                if (accessToken.length > 0) {
                    if (expectedOwner === null) {
                        this.sessionVersion++;
                        this.sessionEvents.notifyAuthenticated();
                    }
                    this.applyAuthenticatedSession(response, expectedOwner === null ? this.tokenStorage.isRemembered() : undefined);
                }
                return accessToken;
            }),
            catchError((error: unknown) => {
                if (refreshVersion === this.sessionVersion && getNumberProperty(error, 'status') === HttpStatusCode.Unauthorized) {
                    void this.onLogoutAsync(true);
                }
                return fallbackApiError('refreshToken error', error, null);
            }),
        );
    }

    public async onLogoutAsync(redirectToAuth = false): Promise<void> {
        if (this.sessionEvents.isLocalSessionInvalidated) {
            await this.navigateAfterSessionEndAsync(redirectToAuth);
            return;
        }
        if (this.requiresLocalLogout()) {
            this.invalidateLocalSession(redirectToAuth);
            return;
        }
        const logoutVersion = ++this.sessionVersion;
        const logoutOwner = this.sessionIdentity === null ? null : this.sessionIdentity.userId;
        const hasRefreshSession = this.tokenStorage.hasRefreshSession();
        const legacyRefreshToken = this.pendingLegacyRefreshToken ?? this.tokenStorage.consumeLegacyRefreshToken();
        this.pendingLegacyRefreshToken = null;
        const cleanup = this.sessionEvents.prepareSessionEnd();
        if (cleanup !== null) {
            await cleanup.catch(() => {});
        }
        if (logoutVersion !== this.sessionVersion) {
            return;
        }
        if (hasRefreshSession) {
            await firstValueFrom(
                this.cookieSessions.run(() => this.createLogoutRequest(logoutVersion, logoutOwner, legacyRefreshToken, redirectToAuth)),
            );
        } else {
            this.completeSessionEnd(logoutVersion, legacyRefreshToken);
        }
        if (logoutVersion !== this.sessionVersion) {
            return;
        }
        await this.navigateAfterSessionEndAsync(redirectToAuth);
    }

    private requiresLocalLogout(): boolean {
        return (
            this.sessionIdentity !== null &&
            (this.sessionIdentity.impersonating || this.tokenStorage.hasForeignSession(this.sessionIdentity.userId))
        );
    }

    private createLogoutRequest(
        logoutVersion: number,
        owner: string | null,
        legacyToken: string | null,
        redirectToAuth: boolean,
    ): Observable<void> {
        if (logoutVersion !== this.sessionVersion) {
            return of(undefined);
        }
        if (owner !== null && this.tokenStorage.hasForeignSession(owner)) {
            this.invalidateLocalSession(redirectToAuth);
            return of(undefined);
        }
        const request = legacyToken === null ? {} : { refreshToken: legacyToken };
        return this.sdk.client.postAuthLogout({ version: this.sdk.version, refreshTokenHttpRequest: request }).pipe(
            catchError(() => of(undefined)),
            tap(() => {
                this.completeSessionEnd(logoutVersion, legacyToken);
            }),
        );
    }

    private completeSessionEnd(logoutVersion: number, legacyToken: string | null): void {
        if (logoutVersion !== this.sessionVersion) {
            return;
        }
        if (legacyToken !== null) {
            this.tokenStorage.clearLegacyRefreshTokenIfMatches(legacyToken);
        }
        this.sessionEvents.notifySessionEnded();
        this.sessionIdentity = null;
        this.restoredPolicyUnknown = false;
        this.authTokenSignal.set(null);
        this.userSignal.set(null);
        this.emailConfirmedSignal.set(null);
        this.mustChangePasswordSignal.set(false);
        this.tokenStorage.clearAll();
        this.localizationService.clearStoredLanguage();
    }

    public getToken(): string | null {
        return this.authTokenSignal();
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
        if (!this.isValidSessionResponse(authResponse, null)) {
            this.invalidateLocalSession();
            throw new Error('Authentication response identity mismatch');
        }
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
        this.restoredPolicyUnknown = false;
        this.initialized = true;
        this.sessionIdentity = this.readSessionIdentity(authResponse.accessToken);
        this.tokenStorage.bindOwner(authResponse.user.id);
        this.tokenStorage.setToken(authResponse.accessToken, rememberMe);
        this.tokenStorage.saveAccountAccess(
            authResponse.user.id,
            this.jwtDecoder.extractRoles(authResponse.accessToken),
            rememberMe !== undefined,
        );
        this.tokenStorage.markRefreshSession(rememberMe !== undefined);
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
            this.tokenStorage.saveAccountAccess(userId, this.jwtDecoder.extractRoles(authResponse.accessToken), rememberMe !== undefined);
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
            if (this.restoredPolicyUnknown && !this.tokenStorage.hasRefreshSession()) {
                this.invalidateLocalSession();
            } else if (this.restoredPolicyUnknown || this.hasUpdatedAccountAccess()) {
                await firstValueFrom(this.refreshToken());
                if (this.restoredPolicyUnknown) {
                    this.invalidateLocalSession();
                }
            }
            return;
        }

        if (!this.tokenStorage.hasRefreshSession()) {
            this.clearStoredIdentity();
            return;
        }

        await firstValueFrom(this.refreshToken());
    }

    private hasUpdatedAccountAccess(): boolean {
        const token = this.authTokenSignal();
        const userId = this.userSignal();
        return (
            token !== null &&
            userId !== null &&
            !this.isImpersonating() &&
            this.tokenStorage.hasRefreshSession() &&
            this.tokenStorage.hasUpdatedAccountAccess(userId, this.jwtDecoder.extractRoles(token))
        );
    }

    private clearStoredIdentity(): void {
        // A guest may still restore the shared HttpOnly cookie. Clearing local
        // identity must not erase its marker or another tab's persisted owner.
        this.tokenStorage.clearTabSession();
        this.sessionIdentity = null;
        this.restoredPolicyUnknown = false;
        this.authTokenSignal.set(null);
        this.userSignal.set(null);
        this.emailConfirmedSignal.set(null);
        this.mustChangePasswordSignal.set(false);
    }

    private readSessionIdentity(token: string | null): SessionIdentity | null {
        const userId = this.jwtDecoder.extractUserId(token);
        return userId === null ? null : { userId, impersonating: this.jwtDecoder.isImpersonation(token) };
    }

    private isValidSessionResponse(response: AuthResponse, expectedOwner: string | null): boolean {
        const identity = this.readSessionIdentity(response.accessToken);
        return (
            identity !== null &&
            !identity.impersonating &&
            identity.userId === response.user.id &&
            (expectedOwner === null || identity.userId === expectedOwner)
        );
    }

    private invalidateLocalSession(redirectToAuth = true): void {
        this.sessionEvents.notifyLocalSessionInvalidating();
        this.sessionVersion++;
        this.initialized = true;
        this.refreshInFlight$ = null;
        this.pendingLegacyRefreshToken = null;
        this.tokenStorage.clearTabSession();
        this.sessionIdentity = null;
        this.restoredPolicyUnknown = false;
        this.authTokenSignal.set(null);
        this.userSignal.set(null);
        this.emailConfirmedSignal.set(null);
        this.mustChangePasswordSignal.set(false);
        this.sessionEvents.notifyLocalSessionInvalidated();
        void this.navigateAfterSessionEndAsync(redirectToAuth);
    }

    private async navigateAfterSessionEndAsync(redirectToAuth: boolean): Promise<void> {
        await this.sessionEvents.runSessionEndNavigationAsync(async () =>
            redirectToAuth ? this.navigationService.navigateToAuthAsync('login') : this.navigationService.navigateToLandingAsync(),
        );
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

            this.sessionVersion++;
            this.tokenStorage.clearTabSession();
            this.sessionIdentity = this.readSessionIdentity(token);
            this.tokenStorage.bindOwner(this.sessionIdentity?.userId ?? null);
            this.tokenStorage.setToken(token, false, false);
            this.authTokenSignal.set(token);
            this.userSignal.set(this.sessionIdentity?.userId ?? null);
            this.emailConfirmedSignal.set(null);
            this.mustChangePasswordSignal.set(false);
            this.sessionEvents.notifyAuthenticated();
        });
    }
}

type AdminSsoStartResponse = {
    code: string;
    expiresAtUtc: string;
};

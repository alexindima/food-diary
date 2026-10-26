import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { HttpClient, HttpErrorResponse, HttpHeaders, HttpStatusCode } from '@angular/common/http';
import { computed, DestroyRef, inject, PLATFORM_ID, Service, signal } from '@angular/core';
import { catchError, defer, finalize, firstValueFrom, map, type Observable, of, shareReplay, tap, timeout } from 'rxjs';

import { environment } from '../../environments/environment';
import { authResponseFromSdk } from '../shared/api/sdk/auth-sdk.mapper';
import { AuthSdk } from '../shared/api/sdk/generated/api/auth.service';
import { TelegramAuthSdk } from '../shared/api/sdk/generated/api/telegram-auth.service';
import { createSdkConnection, sdkRequestOptions } from '../shared/api/sdk/sdk-connection';
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
import { ACCOUNT_ACCESS_STORAGE_KEY, AuthMetadataStorageService } from '../shared/auth/auth-metadata-storage.service';
import { AuthRecoveryStateService, type PendingLogout } from '../shared/auth/auth-recovery-state.service';
import { BrowserSessionStateService } from '../shared/auth/browser-session-state.service';
import { EmailVerificationCooldownService } from '../shared/auth/email-verification-cooldown.service';
import type { GoogleLoginRequest } from '../shared/auth/google-auth.data';
import { SessionEventsService } from '../shared/auth/session-events.service';
import type { TelegramOAuthCode, TelegramOAuthState } from '../shared/auth/telegram-oidc-values';
import { LocalizationService } from '../shared/i18n/localization.service';
import { fallbackApiError, rethrowApiError } from '../shared/lib/api-error.utils';
import { getStringProperty } from '../shared/lib/unknown-value.utils';
import type { User } from '../shared/models/user.data';
import { BrowserWindowService } from '../shared/platform/browser-window.service';
import { ThemeService } from '../shared/theme/theme.service';
import { JwtDecoderService } from './jwt-decoder.service';
import { NavigationService } from './navigation.service';

const REJECTED_SESSION_STATUSES: ReadonlySet<number> = new Set([HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden]);
const SESSION_ACTIVITY_REVALIDATION_MS = 3_600_000;
const SESSION_RESTORATION_TIMEOUT_MS = 30_000;

@Service()
export class AuthService {
    private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
    private readonly document = inject(DOCUMENT);
    private readonly navigationService = inject(NavigationService);
    private readonly emailVerificationCooldown = inject(EmailVerificationCooldownService);
    private readonly sessionEvents = inject(SessionEventsService);
    private readonly cookieSessions = inject(AuthCookieSessionService);
    private readonly browserWindow = inject(BrowserWindowService);
    private readonly browserSession = inject(BrowserSessionStateService);
    private readonly localizationService = inject(LocalizationService);
    private readonly themeService = inject(ThemeService);
    private readonly authMetadata = inject(AuthMetadataStorageService);
    private readonly recovery = inject(AuthRecoveryStateService);
    private readonly jwtDecoder = inject(JwtDecoderService);
    protected readonly baseUrl = environment.apiUrls.auth;
    private readonly http = inject(HttpClient);
    private readonly sdk = createSdkConnection(AuthSdk, this.baseUrl, this.http);
    private readonly telegram = createSdkConnection(TelegramAuthSdk, this.baseUrl, this.http);
    private readonly session = signal<AuthResponse | null>(null);
    private readonly authReadySignal = signal(false);
    private readonly emailConfirmedSignal = signal<boolean | null>(null);
    private readonly mustChangePasswordSignal = signal(false);
    private refreshInFlight$: Observable<boolean> | null = null;
    private sessionRestorePromise: Promise<void> | null = null;
    private sessionVersion = 0;
    private lastRenewalAttemptAt = Date.now();

    public readonly isAuthenticated = computed(() => this.session() !== null);
    public readonly isEmailConfirmed = computed(() => this.emailConfirmedSignal() ?? true);
    public readonly requiresEmailVerification = computed(
        () => !this.isEmailConfirmed() && this.session()?.user.email !== null && this.session()?.user.email !== undefined,
    );
    public readonly mustChangePassword = this.mustChangePasswordSignal.asReadonly();
    public readonly isAdmin = computed(() => this.hasRole('Admin'));
    public readonly isPremium = computed(() => this.hasRole('Premium'));
    public readonly isDietologist = computed(() => this.hasRole('Dietologist'));
    public readonly isImpersonating = computed(
        () => this.session()?.impersonationContext !== null && this.session()?.impersonationContext !== undefined,
    );
    public readonly impersonationReason = computed(() => this.session()?.impersonationReason ?? null);
    public readonly isAuthReady = this.authReadySignal.asReadonly();

    public constructor() {
        // Previous browser bearer credentials are deliberately not restored.
        this.authMetadata.clearTabSession();
        const stop = this.browserWindow.onLocalStorageChange(event => {
            if (
                this.recovery.isLogoutStorageKey(event.key) &&
                this.recovery.pendingLogout() !== null &&
                this.recovery.pendingLogout()?.sessionId === this.browserSession.sessionId()
            ) {
                this.invalidateLocalSession(false);
                return;
            }
            if (this.isImpersonating() || event.key !== ACCOUNT_ACCESS_STORAGE_KEY) {
                return;
            }
            const owner = this.getUserId();
            if (owner !== null && this.authMetadata.hasForeignSession(owner)) {
                this.invalidateLocalSession();
            } else if (owner !== null) {
                void firstValueFrom(this.refreshSession());
            }
        });
        inject(DestroyRef).onDestroy(stop);
        this.observeSessionActivity();
    }

    public initializeAuth(): void {
        // The HttpOnly session is restored by restoreSessionAsync before protected routing.
    }

    private observeSessionActivity(): void {
        if (!this.isBrowser) {
            return;
        }
        const renew = (): void => {
            if (
                this.document.visibilityState === 'hidden' ||
                !this.isAuthenticated() ||
                this.browserSession.telegram ||
                this.browserSession.contextId() !== null ||
                this.recovery.pendingLogout() !== null
            ) {
                return;
            }
            const now = Date.now();
            if (now - this.lastRenewalAttemptAt < SESSION_ACTIVITY_REVALIDATION_MS) {
                return;
            }
            this.lastRenewalAttemptAt = now;
            void firstValueFrom(this.refreshSession());
        };
        const events = ['pointerdown', 'keydown', 'visibilitychange'] as const;
        events.forEach(event => {
            this.document.addEventListener(event, renew, { passive: true });
        });
        inject(DestroyRef).onDestroy(() => {
            events.forEach(event => {
                this.document.removeEventListener(event, renew);
            });
        });
    }

    public async restoreSessionAsync(): Promise<void> {
        if (!this.isBrowser) {
            this.authReadySignal.set(true);
            return;
        }
        if (this.authReadySignal()) {
            return;
        }
        this.sessionRestorePromise ??= this.restoreSessionInternalAsync().finally(() => {
            this.authReadySignal.set(this.recovery.issue() === null);
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

    public refreshSession(): Observable<boolean> {
        if (this.recovery.pendingLogout() !== null || !this.recovery.canRetry()) {
            return of(false);
        }
        if (this.refreshInFlight$ !== null) {
            return this.refreshInFlight$;
        }
        const version = this.sessionVersion;
        const owner = this.getUserId();
        const sessionId = this.browserSession.sessionId();
        const contextId = this.browserSession.contextId();
        const request =
            this.browserSession.telegram && this.browserSession.contextId() === null
                ? this.sdk.client.postAuthRefresh({ version: this.sdk.version, refreshTokenHttpRequest: {} })
                : this.sdk.client.postAuthSession(
                      { version: this.sdk.version },
                      'body',
                      false,
                      sdkRequestOptions(this.pendingLogoutHeaders({ sessionId, contextId, telegram: this.browserSession.telegram })),
                  );
        const pending = this.cookieSessions
            .run(() =>
                defer(() => {
                    if (version !== this.sessionVersion || this.recovery.pendingLogout() !== null) {
                        return of(false);
                    }
                    return request.pipe(
                        timeout(SESSION_RESTORATION_TIMEOUT_MS),
                        map(authResponseFromSdk),
                        map(response => {
                            if (version !== this.sessionVersion || this.recovery.pendingLogout() !== null) {
                                return false;
                            }
                            if (
                                (owner !== null && response.user.id !== owner) ||
                                (sessionId !== null && response.sessionId !== sessionId) ||
                                (contextId !== null && response.impersonationContext !== contextId)
                            ) {
                                this.invalidateLocalSession();
                                return false;
                            }
                            this.applyAuthenticatedSession(response);
                            this.recovery.restorationSucceeded();
                            this.authReadySignal.set(true);
                            return true;
                        }),
                        catchError((error: unknown) => {
                            if (version === this.sessionVersion) {
                                if (error instanceof HttpErrorResponse && REJECTED_SESSION_STATUSES.has(error.status)) {
                                    this.recovery.restorationSucceeded();
                                    this.authReadySignal.set(true);
                                    if (owner === null) {
                                        this.clearSession();
                                    } else {
                                        this.invalidateLocalSession();
                                    }
                                } else {
                                    this.recovery.restorationFailed(
                                        error instanceof HttpErrorResponse ? error.headers.get('Retry-After') : null,
                                    );
                                }
                            }
                            return of(false);
                        }),
                    );
                }),
            )
            .pipe(
                finalize(() => {
                    if (this.refreshInFlight$ === pending) {
                        this.refreshInFlight$ = null;
                    }
                }),
                shareReplay({ bufferSize: 1, refCount: true }),
            );
        this.refreshInFlight$ = pending;
        return pending;
    }

    public async onLogoutAsync(redirectToAuth = false): Promise<void> {
        if (this.recovery.pendingLogout() !== null && !this.isAuthenticated()) {
            await this.retryPendingLogoutAsync();
            return;
        }
        const pending = this.currentLogoutSelection();
        const impersonating = this.isImpersonating();
        this.recovery.beginLogout(pending);
        const cleanup = this.sessionEvents.prepareSessionEnd();
        this.clearSession();
        const version = this.sessionVersion;
        if (!impersonating) {
            this.authMetadata.clearAll();
            this.localizationService.clearStoredLanguage();
        }
        if (cleanup !== null) {
            await cleanup.catch(() => {});
        }
        if (version !== this.sessionVersion) {
            return;
        }
        await this.retryPendingLogoutAsync();
        if (version !== this.sessionVersion) {
            return;
        }
        await this.navigateAfterSessionEndAsync(redirectToAuth);
    }

    public async retryPendingLogoutAsync(): Promise<void> {
        const pending = this.recovery.pendingLogout();
        if (pending === null || this.recovery.busy() || !this.recovery.canRetry()) {
            return;
        }
        this.recovery.busy.set(true);
        try {
            const headers = this.pendingLogoutHeaders(pending);
            await firstValueFrom(
                this.cookieSessions.run(() =>
                    this.sdk.client.postAuthLogout(
                        { version: this.sdk.version, refreshTokenHttpRequest: {} },
                        'body',
                        false,
                        sdkRequestOptions(headers),
                    ),
                ),
            );
            this.recovery.confirmLogout(pending);
        } catch (error: unknown) {
            this.handleLogoutFailure(error, pending);
        } finally {
            this.recovery.busy.set(false);
        }
        if (this.recovery.issue() === null && !this.isAuthenticated()) {
            this.authReadySignal.set(true);
        }
    }

    private currentLogoutSelection(): PendingLogout {
        const tokenSession = this.jwtDecoder.decodePayload(this.getToken() ?? '')?.['refresh_session_id'];
        return {
            sessionId: this.browserSession.sessionId() ?? (typeof tokenSession === 'string' ? tokenSession : null),
            contextId: this.browserSession.contextId(),
            telegram: this.browserSession.telegram,
        };
    }

    private pendingLogoutHeaders(pending: PendingLogout): HttpHeaders {
        let headers = new HttpHeaders({ 'X-FoodDiary-Client': pending.telegram ? 'telegram' : 'web' });
        if (pending.sessionId !== null) {
            headers = headers.set('X-FoodDiary-Session', pending.sessionId);
        }
        if (pending.contextId !== null) {
            headers = headers.set('X-FoodDiary-Context', pending.contextId);
        }
        return headers;
    }

    private handleLogoutFailure(error: unknown, pending: PendingLogout): void {
        if (this.recovery.pendingLogout() !== pending) {
            return;
        }
        if (
            error instanceof HttpErrorResponse &&
            error.status === Number(HttpStatusCode.Unauthorized) &&
            getStringProperty(error.error, 'error') === 'Authentication.SessionChanged'
        ) {
            this.recovery.confirmLogout(pending);
        } else {
            this.recovery.restorationFailed(error instanceof HttpErrorResponse ? error.headers.get('Retry-After') : null);
        }
    }

    /** Only the explicitly supported Telegram token transport has an access token, held in memory. */
    public getToken(): string | null {
        return this.session()?.accessToken ?? null;
    }

    public getSessionKey(): string | null {
        const current = this.session();
        return current?.sessionId ?? current?.impersonationContext ?? current?.accessToken ?? null;
    }

    public sessionHubUrl(url: string): string {
        return this.browserSession.hubUrl(url);
    }

    public getUserId(): string | null {
        return this.session()?.user.id ?? null;
    }

    public setEmailConfirmed(value: boolean): void {
        this.emailConfirmedSignal.set(value);
        this.authMetadata.setEmailConfirmed(value);
    }

    public async completeRequiredPasswordChangeAsync(newPassword: string): Promise<void> {
        const email = this.session()?.user.email;
        if (email === null || email === undefined || email.length === 0) {
            throw new Error('Missing session email');
        }
        await firstValueFrom(this.login({ email, password: newPassword, rememberMe: this.authMetadata.isRemembered() }));
    }

    private onLogin(response: AuthResponse, rememberMe: boolean): void {
        this.sessionVersion++;
        this.refreshInFlight$ = null;
        this.recovery.clear();
        this.browserSession.setContext(null);
        this.applyAuthenticatedSession(response, rememberMe);
        this.sessionEvents.notifyAuthenticated();
    }

    public acceptExternalAuthentication(response: AuthResponse): void {
        this.onLogin(response, false);
    }

    private applyAuthenticatedSession(response: AuthResponse, rememberMe?: boolean): void {
        if (!this.browserSession.telegram && response.accessToken !== undefined) {
            throw new Error('Unexpected bearer credential in a browser session response');
        }
        this.session.set(response);
        this.browserSession.sessionId.set(response.sessionId ?? null);
        this.browserSession.setContext(response.impersonationContext ?? null);
        this.authMetadata.bindOwner(response.user.id);
        this.authMetadata.setRemembered(rememberMe ?? response.rememberMe ?? false);
        if (!this.isImpersonating()) {
            this.authMetadata.setUserId(response.user.id);
            this.authMetadata.saveAccountAccess(response.user.id, this.roles(), rememberMe !== undefined);
        }
        if (response.user.language !== undefined && response.user.language.length > 0) {
            void this.localizationService.applyLanguagePreferenceAsync(response.user.language);
        }
        this.themeService.syncWithUserPreferences(response.user.theme, response.user.uiStyle, response.user.surfaceStyle);
        this.setEmailConfirmed(response.user.isEmailConfirmed);
        this.mustChangePasswordSignal.set(response.user.mustChangePassword === true);
        this.authMetadata.setMustChangePassword(response.user.mustChangePassword === true);
    }

    private async restoreSessionInternalAsync(): Promise<void> {
        if (this.recovery.pendingLogout() !== null) {
            await this.retryPendingLogoutAsync();
            return;
        }
        const href = this.browserWindow.getHref();
        if (href !== null) {
            const url = new URL(href);
            const code = url.searchParams.get('impersonationCode');
            if (code !== null) {
                url.searchParams.delete('impersonationCode');
                this.browserWindow.replaceCurrentUrl(url.pathname + url.search + url.hash);
                const response = await firstValueFrom(
                    this.sdk.client
                        .postAuthImpersonationExchange({
                            version: this.sdk.version,
                            exchangeImpersonationHttpRequest: { code },
                        })
                        .pipe(catchError((error: unknown) => fallbackApiError('impersonation exchange error', error, null))),
                );
                const context = response?.contextId;
                if (context === undefined || context === null || context.length === 0) {
                    return;
                }
                this.browserSession.setContext(context);
            }
        }
        await firstValueFrom(this.refreshSession());
    }

    private clearSession(): void {
        this.sessionVersion++;
        this.sessionEvents.notifySessionEnded();
        this.session.set(null);
        this.browserSession.sessionId.set(null);
        this.browserSession.setContext(null);
        this.emailConfirmedSignal.set(null);
        this.mustChangePasswordSignal.set(false);
        this.authMetadata.clearTabSession();
    }

    public invalidateLocalSession(redirectToAuth = true): void {
        this.sessionEvents.notifyLocalSessionInvalidating();
        this.clearSession();
        this.sessionEvents.notifyLocalSessionInvalidated();
        void this.navigateAfterSessionEndAsync(redirectToAuth);
    }

    private async navigateAfterSessionEndAsync(redirectToAuth: boolean): Promise<void> {
        await this.sessionEvents.runSessionEndNavigationAsync(async () =>
            redirectToAuth ? this.navigationService.navigateToAuthAsync('login') : this.navigationService.navigateToLandingAsync(),
        );
    }

    private roles(): readonly string[] {
        const current = this.session();
        return current?.roles !== undefined && current.roles.length > 0
            ? current.roles
            : this.jwtDecoder.extractRoles(current?.accessToken ?? '');
    }

    private hasRole(role: string): boolean {
        return this.roles().includes(role);
    }

    private getClientOrigin(): string | undefined {
        return this.browserWindow.getOrigin();
    }
}

type AdminSsoStartResponse = { code: string; expiresAtUtc: string };

import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    effect,
    inject,
    input,
    output,
    PLATFORM_ID,
    signal,
    viewChild,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { focusFirstInvalidField } from 'fd-ui-kit/form/fd-ui-form.directive';
import { FdUiTabsComponent } from 'fd-ui-kit/tabs/fd-ui-tabs';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../../../environments/environment';
import { AUTH_PASSWORD_RESET_COOLDOWN_SECONDS } from '../../../../config/runtime-ui.tokens';
import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { getRecordProperty, getStringProperty } from '../../../../shared/lib/unknown-value.utils';
import type { UserId } from '../../../../shared/models/semantics/entity-id';
import { BrowserWindowService } from '../../../../shared/platform/browser-window.service';
import { PwaInstallService } from '../../../../shared/service-worker/pwa-install.service';
import type { GoogleLoginRequest } from '../../models/google-auth.data';
import { TelegramEntryButtonComponent } from '../telegram-entry-button/telegram-entry-button';
import type { LoginFormValues } from './auth-lib/auth.types';
import { buildAdminSignInUrl, buildAdminUnauthorizedUrl, normalizeAdminReturnUrl } from './auth-lib/auth-admin-return-url.utils';
import { startSecondsCountdown } from './auth-lib/auth-countdown.utils';
import { AuthFlowFacade, type AuthLoginResult, type AuthRegisterResult } from './auth-lib/auth-flow.facade';
import { AUTH_TABS } from './auth-lib/auth-form.config';
import { AuthFormManager } from './auth-lib/auth-form.manager';
import { AuthGoogleManager } from './auth-lib/auth-google.manager';
import { getLoginAutofillFieldValues } from './auth-lib/auth-login-autofill.utils';
import { AuthLoginFormComponent } from './auth-login-form/auth-login-form';
import { AuthPasswordResetFormComponent } from './auth-password-reset-form/auth-password-reset-form';
import { AuthRegisterFormComponent } from './auth-register-form/auth-register-form';

@Component({
    selector: 'fd-auth',
    templateUrl: './auth.html',
    styleUrls: ['./auth.scss'],
    providers: [AuthFormManager, AuthGoogleManager],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        TranslatePipe,
        FdUiTabsComponent,
        AuthLoginFormComponent,
        AuthPasswordResetFormComponent,
        AuthRegisterFormComponent,
        TelegramEntryButtonComponent,
    ],
})
export class AuthComponent {
    public readonly useRouting = input(true);
    public readonly inlineSignIn = input(false);
    public readonly signedIn = output();
    public readonly hideTelegram = input(false);
    public readonly confirmationUserId = input<UserId | null>(null);
    public readonly initialMode = input<'login' | 'register'>('login');
    public readonly initialPasswordReset = input(false);
    public readonly initialReturnUrl = input<string | null>(null);
    public readonly initialAdminReturnUrl = input<string | null>(null);
    private readonly loginFormComponent = viewChild(AuthLoginFormComponent);
    private readonly registerFormComponent = viewChild(AuthRegisterFormComponent);

    private readonly route = inject(ActivatedRoute, { optional: true });
    private readonly router = inject(Router, { optional: true });
    private readonly navigationService = inject(NavigationService);
    private readonly authService = inject(AuthService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly isBrowser = isPlatformBrowser(this.platformId);
    private readonly dialogRef = inject(FdUiDialogRef<AuthComponent>, { optional: true });
    private readonly passwordResetCooldownSecondsDefault = inject(AUTH_PASSWORD_RESET_COOLDOWN_SECONDS);
    private readonly formManager = inject(AuthFormManager);
    private readonly googleManager = inject(AuthGoogleManager);
    private readonly authFlowFacade = inject(AuthFlowFacade);
    private readonly browserWindow = inject(BrowserWindowService);
    private readonly pwa = inject(PwaInstallService);
    protected readonly showRememberMe = computed(() => !this.pwa.shouldRememberSession());

    protected authMode: 'login' | 'register' = 'login';

    protected readonly loginForm = this.formManager.loginForm;
    protected readonly registerForm = this.formManager.registerForm;
    protected readonly passwordResetForm = this.formManager.passwordResetForm;
    protected readonly loginModel = this.formManager.loginModel;
    protected readonly registerModel = this.formManager.registerModel;
    protected readonly passwordResetModel = this.formManager.passwordResetModel;
    protected readonly globalError = signal<string | null>(null);
    protected readonly isSubmitting = signal<boolean>(false);
    protected readonly googleReady = this.googleManager.ready;
    protected readonly googleLinkRequired = signal(false);
    protected readonly showRestoreAction = signal<boolean>(false);
    protected readonly isRestoring = signal<boolean>(false);
    protected readonly showPasswordReset = signal<boolean>(false);
    protected readonly isPasswordResetting = signal<boolean>(false);
    protected readonly passwordResetSent = signal<boolean>(false);
    protected readonly passwordResetCooldownSeconds = signal<number>(0);
    protected readonly loginFieldErrors = this.formManager.loginFieldErrors;
    protected readonly registerFieldErrors = this.formManager.registerFieldErrors;
    protected readonly passwordResetFieldErrors = this.formManager.passwordResetFieldErrors;
    protected readonly loginSubmitLabelKey = computed(() => (this.isSubmitting() ? 'COMMON.LOADING' : 'AUTH.LOGIN.LOGIN'));
    private stopPasswordResetCooldown: (() => void) | null = null;
    private passwordResetViewVersion = 0;
    private authTabVersion = 0;
    protected readonly authTabs = AUTH_TABS;

    private returnUrl: string | null = null;
    private adminReturnUrl: string | null = null;
    private authenticatedNavigation: Promise<void> | null = null;
    private pendingGoogleLinkCredential: string | null = null;
    private failedLoginValues: LoginFormValues | null = null;

    public constructor() {
        this.formManager.configureSubmissionActions({
            login: this.submitLoginFormAsync,
            passwordReset: this.submitPasswordResetFormAsync,
            register: this.submitRegisterFormAsync,
        });
        this.subscribeFormChanges();
        this.registerRenderingEffects();
        void this.googleManager.initializeAsync(credential => {
            this.onGoogleCredential(credential);
        });
    }

    private readonly submitLoginFormAsync = async (): Promise<void> => {
        await this.loginAsync();
    };

    private readonly submitRegisterFormAsync = async (): Promise<void> => {
        await this.registerAsync();
    };

    private readonly submitPasswordResetFormAsync = async (): Promise<void> => {
        await this.requestPasswordResetAsync();
    };

    private subscribeFormChanges(): void {
        effect(() => {
            const values = this.loginModel();
            const failed = this.failedLoginValues;
            if (values.email !== failed?.email || values.password !== failed.password || values.rememberMe !== failed.rememberMe) {
                this.clearGlobalError();
            }
        });
        effect(() => {
            this.registerModel();
            this.clearGlobalError();
        });
        effect(() => {
            this.passwordResetModel();
            this.clearGlobalError();
        });
    }

    private registerRenderingEffects(): void {
        effect(() => {
            this.renderGoogleButton();
        });
        effect(() => {
            const routeMode = this.route?.snapshot.queryParamMap.get('auth') === 'register' ? 'register' : 'login';
            this.authMode = this.useRouting() ? routeMode : this.initialMode();
            this.showPasswordReset.set(this.initialPasswordReset());
            this.returnUrl = this.useRouting() ? (this.route?.snapshot.queryParamMap.get('returnUrl') ?? null) : this.initialReturnUrl();
            this.adminReturnUrl = this.useRouting()
                ? (this.route?.snapshot.queryParamMap.get('adminReturnUrl') ?? null)
                : this.initialAdminReturnUrl();
        });
        effect(() => {
            if ((this.adminReturnUrl ?? '').length === 0) {
                return;
            }

            if (!this.authService.isAuthenticated()) {
                return;
            }

            void this.completeAuthenticatedNavigationAsync();
        });
    }

    protected changeAuthMode(value: string): void {
        const mode: 'login' | 'register' = value === 'register' ? 'register' : 'login';
        void this.onTabChangeAsync(mode);
    }

    protected openTelegram(): void {
        this.dialogRef?.close();
        void this.router?.navigate(['/auth/telegram'], { preserveFragment: true });
    }

    protected async onTabChangeAsync(mode: 'login' | 'register'): Promise<void> {
        this.authMode = mode;
        this.authTabVersion++;
        this.formManager.resetAll();
        this.pendingGoogleLinkCredential = null;
        this.googleLinkRequired.set(false);
        this.onPasswordResetClose();
        if (this.useRouting() && this.router !== null) {
            await this.router.navigate(['/'], { queryParams: { auth: mode } });
        }
    }

    private async loginAsync(): Promise<void> {
        this.syncLoginNativeValues();

        if (this.loginForm().invalid() || this.isLoginSubmitDisabled()) {
            this.loginForm().markAsTouched();
            this.formManager.updateFieldErrors();
            return;
        }

        this.isSubmitting.set(true);

        const loginValues = this.loginModel();
        const tabVersion = this.authTabVersion;
        const result = await firstValueFrom(
            this.authFlowFacade.login({ ...loginValues, expectedUserId: this.confirmationUserId() ?? undefined }),
        );
        if (this.destroyRef.destroyed) {
            return;
        }
        this.isSubmitting.set(false);
        if (result === 'success') {
            if (this.pendingGoogleLinkCredential !== null) {
                await this.completeGoogleLinkAsync(this.pendingGoogleLinkCredential);
                return;
            }
            await this.completeAuthenticatedNavigationAsync();
            this.closeDialogIfAny();
            return;
        }

        if (tabVersion === this.authTabVersion) {
            this.failedLoginValues = loginValues;
            this.handleLoginResult(result);
        }
    }

    protected isLoginSubmitDisabled(): boolean {
        return this.isSubmitting() || this.isRestoring();
    }

    protected onLoginNativeInput(): void {
        this.syncLoginNativeValues();
    }

    protected onRestoreSubmit(): void {
        const previousLogin = this.loginModel();
        this.syncLoginNativeValues();
        if (this.loginModel() !== previousLogin) {
            this.clearGlobalError();
            return;
        }

        this.loginForm().markAsTouched();
        this.formManager.updateFieldErrors();
        if (this.loginForm().invalid() || this.isLoginSubmitDisabled()) {
            if (this.loginForm().invalid()) {
                focusFirstInvalidField(this.loginForm);
            }
            return;
        }

        const rememberMe = this.loginModel().rememberMe;
        const tabVersion = this.authTabVersion;
        this.isRestoring.set(true);

        this.authFlowFacade.restoreAccount(this.loginModel(), rememberMe).subscribe(success => {
            if (this.destroyRef.destroyed) {
                return;
            }
            if (success) {
                void this.completeAuthenticatedNavigationAndCloseAsync().finally(() => {
                    if (!this.destroyRef.destroyed) {
                        this.isRestoring.set(false);
                    }
                });
                return;
            }

            this.isRestoring.set(false);
            if (tabVersion === this.authTabVersion) {
                this.setGlobalError('AUTH.LOGIN.RESTORE_ERROR');
            }
        });
    }

    private async registerAsync(): Promise<void> {
        if (this.registerForm().invalid() || this.isSubmitting()) {
            this.registerForm().markAsTouched();
            this.formManager.updateFieldErrors();
            return;
        }

        this.isSubmitting.set(true);

        const tabVersion = this.authTabVersion;
        const result = await firstValueFrom(this.authFlowFacade.register(this.registerModel()));
        if (this.destroyRef.destroyed) {
            return;
        }
        this.isSubmitting.set(false);
        if (result === 'success') {
            await this.navigationService.navigateToEmailVerificationPendingAsync();
            this.closeDialogIfAny();
            return;
        }

        if (tabVersion === this.authTabVersion) {
            this.handleRegisterResult(result);
        }
    }

    private renderGoogleButton(): void {
        this.googleManager.renderButton(
            this.authMode,
            this.loginFormComponent()?.googleButton()?.nativeElement,
            this.registerFormComponent()?.googleButton()?.nativeElement,
        );
    }

    private onGoogleCredential(credential: string): void {
        if (this.isSubmitting()) {
            return;
        }
        this.isSubmitting.set(true);
        const tabVersion = this.authTabVersion;
        const request: GoogleLoginRequest = { credential, rememberMe: true, expectedUserId: this.confirmationUserId() ?? undefined };
        this.authFlowFacade.loginWithGoogle(request).subscribe(result => {
            if (this.destroyRef.destroyed) {
                return;
            }
            this.isSubmitting.set(false);
            if (result === 'success') {
                void this.completeAuthenticatedNavigationAndCloseAsync();
                return;
            }

            if (tabVersion !== this.authTabVersion) {
                return;
            }

            if (result === 'accountMismatch' || (result === 'accountLinkRequired' && this.confirmationUserId() !== null)) {
                this.setGlobalError('AUTH.CONFIRM.ACCOUNT_MISMATCH');
                return;
            }

            if (result === 'accountLinkRequired') {
                void this.prepareGoogleLinkLoginAsync(credential);
                return;
            }

            this.setGlobalError('FORM_ERRORS.UNKNOWN');
        });
    }

    private async prepareGoogleLinkLoginAsync(credential: string): Promise<void> {
        if (this.authMode !== 'login') {
            await this.onTabChangeAsync('login');
        }

        this.pendingGoogleLinkCredential = credential;
        this.googleLinkRequired.set(true);
        this.setGlobalError('AUTH.GOOGLE.LINK_REQUIRED_ACTION');
    }

    private async completeGoogleLinkAsync(credential: string): Promise<void> {
        const linked = await firstValueFrom(this.authFlowFacade.linkGoogle(credential));
        if (this.destroyRef.destroyed) {
            return;
        }
        this.pendingGoogleLinkCredential = null;
        this.googleLinkRequired.set(false);
        await this.navigationService.navigateToProfileAsync({ googleLink: linked ? 'success' : 'failed' });
        this.closeDialogIfAny();
    }

    protected onPasswordResetOpen(): void {
        if (this.showPasswordReset()) {
            return;
        }
        this.clearGlobalError();
        this.passwordResetForm().reset({
            email: this.loginModel().email,
        });
        this.passwordResetSent.set(false);
        this.showPasswordReset.set(true);
    }

    protected onPasswordResetClose(): void {
        this.passwordResetViewVersion++;
        this.showPasswordReset.set(false);
        this.passwordResetSent.set(false);
        this.clearGlobalError();
    }

    private async requestPasswordResetAsync(): Promise<void> {
        if (this.passwordResetCooldownSeconds() > 0) {
            return;
        }

        this.clearGlobalError();
        this.isPasswordResetting.set(true);

        const viewVersion = this.passwordResetViewVersion;
        const result = await firstValueFrom(this.authFlowFacade.requestPasswordReset(this.passwordResetModel()));
        if (this.destroyRef.destroyed) {
            return;
        }
        this.isPasswordResetting.set(false);
        const currentView = viewVersion === this.passwordResetViewVersion;
        if (result === 'success') {
            if (currentView) {
                this.passwordResetSent.set(true);
            }
            this.startPasswordResetCooldown();
            return;
        }

        if (currentView) {
            this.setGlobalError(result === 'rateLimited' ? 'FORM_ERRORS.RATE_LIMITED' : 'FORM_ERRORS.UNKNOWN');
        }
    }

    private startPasswordResetCooldown(seconds = this.passwordResetCooldownSecondsDefault): void {
        this.stopPasswordResetCooldown?.();
        this.stopPasswordResetCooldown = startSecondsCountdown(
            this.passwordResetCooldownSeconds,
            seconds,
            this.destroyRef,
            this.browserWindow,
        );
    }

    private closeDialogIfAny(): void {
        this.dialogRef?.close();
    }

    private async completeAuthenticatedNavigationAndCloseAsync(): Promise<void> {
        try {
            await this.completeAuthenticatedNavigationAsync();
            this.closeDialogIfAny();
        } catch {
            this.setGlobalError('FORM_ERRORS.UNKNOWN');
        }
    }

    private async completeAuthenticatedNavigationAsync(): Promise<void> {
        this.authenticatedNavigation ??= this.navigateAfterAuthenticationAsync().finally(() => {
            this.authenticatedNavigation = null;
        });
        return this.authenticatedNavigation;
    }

    private async navigateAfterAuthenticationAsync(): Promise<void> {
        if (this.authService.mustChangePassword()) {
            await this.navigationService.navigateToRequiredPasswordChangeAsync();
            this.closeDialogIfAny();
            return;
        }

        if (this.authService.requiresEmailVerification()) {
            await this.navigationService.navigateToEmailVerificationPendingAsync({ autoResend: true });
            return;
        }

        if (this.inlineSignIn()) {
            this.signedIn.emit();
            return;
        }

        const adminRedirectUrl = await this.tryBuildAdminRedirectUrlAsync();
        if (this.destroyRef.destroyed) {
            return;
        }
        if (adminRedirectUrl !== null) {
            if (this.isBrowser) {
                this.document.location.assign(adminRedirectUrl);
            }
            return;
        }

        await this.navigationService.navigateToReturnUrlAsync(this.returnUrl);
    }

    private async tryBuildAdminRedirectUrlAsync(): Promise<string | null> {
        const adminReturnUrl = this.adminReturnUrl;
        const adminAppUrl = environment.adminAppUrl ?? '';
        if (adminReturnUrl === null || !this.isBrowser || adminReturnUrl.length === 0 || adminAppUrl.length === 0) {
            return null;
        }

        const adminPath = normalizeAdminReturnUrl(adminReturnUrl, adminAppUrl, this.document.location.origin);
        if (adminPath === null) {
            return null;
        }

        if (!this.authService.isAdmin()) {
            return buildAdminUnauthorizedUrl(adminPath, 'forbidden', adminAppUrl, this.document.location.origin);
        }

        try {
            const adminUrl = new URL(adminPath, adminAppUrl);
            const response = await firstValueFrom(this.authService.startAdminSso());
            adminUrl.searchParams.delete('code');
            const fragment = new URLSearchParams(adminUrl.hash.slice(1));
            fragment.set('code', response.code);
            adminUrl.hash = fragment.toString();
            return adminUrl.toString();
        } catch (error: unknown) {
            if (getStringProperty(getRecordProperty(error, 'error'), 'error') === 'Authentication.ReauthenticationRequired') {
                return buildAdminSignInUrl(
                    new URL(adminAppUrl, this.document.location.origin),
                    adminPath,
                    this.authService.getUserId(),
                ).toString();
            }
            return buildAdminUnauthorizedUrl(adminPath, 'forbidden', adminAppUrl, this.document.location.origin);
        }
    }

    private handleLoginResult(result: AuthLoginResult): void {
        if (result === 'accountMismatch') {
            this.setGlobalError('AUTH.CONFIRM.ACCOUNT_MISMATCH');
        } else if (result === 'invalidCredentials') {
            this.setGlobalError('FORM_ERRORS.INVALID_CREDENTIALS');
            this.showRestoreAction.set(false);
        } else if (result === 'rateLimited') {
            this.setGlobalError('FORM_ERRORS.RATE_LIMITED');
        } else if (result === 'accountDeleted') {
            this.setGlobalError('AUTH.LOGIN.ACCOUNT_DELETED');
            this.showRestoreAction.set(this.confirmationUserId() === null);
        } else {
            this.setGlobalError('FORM_ERRORS.UNKNOWN');
            this.showRestoreAction.set(false);
        }
    }

    private handleRegisterResult(result: AuthRegisterResult): void {
        if (result === 'emailExists') {
            this.formManager.setRegisterEmailExistsError();
        } else if (result === 'rateLimited') {
            this.setGlobalError('FORM_ERRORS.RATE_LIMITED');
        } else if (result === 'accountDeleted') {
            this.setGlobalError('AUTH.REGISTER.ACCOUNT_DELETED');
        } else {
            this.setGlobalError('FORM_ERRORS.UNKNOWN');
        }
    }

    private setGlobalError(errorKey: string): void {
        this.globalError.set(this.translateService.instant(errorKey));
    }

    private clearGlobalError(): void {
        this.failedLoginValues = null;
        this.globalError.set(null);
        this.showRestoreAction.set(false);
    }

    private syncLoginNativeValues(): void {
        const form = this.loginFormComponent()?.formElement()?.nativeElement;

        if (form === undefined) {
            return;
        }

        const { email, password } = getLoginAutofillFieldValues(form);
        this.loginModel.update(value => (email === value.email && password === value.password ? value : { ...value, email, password }));
    }
}

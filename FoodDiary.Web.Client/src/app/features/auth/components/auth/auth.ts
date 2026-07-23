import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    effect,
    inject,
    input,
    PLATFORM_ID,
    signal,
    viewChild,
} from '@angular/core';
import { submit } from '@angular/forms/signals';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiTabsComponent } from 'fd-ui-kit/tabs/fd-ui-tabs';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../../../environments/environment';
import { AUTH_LOGIN_AUTOFILL_CHECK_DELAYS_MS, AUTH_PASSWORD_RESET_COOLDOWN_SECONDS } from '../../../../config/runtime-ui.tokens';
import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import type { GoogleLoginRequest } from '../../models/google-auth.data';
import { buildAdminUnauthorizedUrl, normalizeAdminReturnUrl } from './auth-lib/auth-admin-return-url.utils';
import { startSecondsCountdown } from './auth-lib/auth-countdown.utils';
import { AuthFlowFacade, type AuthLoginResult, type AuthRegisterResult } from './auth-lib/auth-flow.facade';
import { AUTH_TABS } from './auth-lib/auth-form.config';
import { AuthFormManager } from './auth-lib/auth-form.manager';
import { AuthGoogleManager } from './auth-lib/auth-google.manager';
import { getLoginAutofillFieldValues, hasCompleteLoginAutofill } from './auth-lib/auth-login-autofill.utils';
import { AuthLoginFormComponent } from './auth-login-form/auth-login-form';
import { AuthPasswordResetFormComponent } from './auth-password-reset-form/auth-password-reset-form';
import { AuthRegisterFormComponent } from './auth-register-form/auth-register-form';

@Component({
    selector: 'fd-auth',
    templateUrl: './auth.html',
    styleUrls: ['./auth.scss'],
    providers: [AuthFormManager, AuthGoogleManager],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiTabsComponent, AuthLoginFormComponent, AuthPasswordResetFormComponent, AuthRegisterFormComponent],
})
export class AuthComponent {
    public readonly useRouting = input(true);
    public readonly initialMode = input<'login' | 'register'>('login');
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
    private readonly loginAutofillCheckDelaysMs = inject(AUTH_LOGIN_AUTOFILL_CHECK_DELAYS_MS);
    private readonly formManager = inject(AuthFormManager);
    private readonly googleManager = inject(AuthGoogleManager);
    private readonly authFlowFacade = inject(AuthFlowFacade);

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
    protected readonly showRestoreAction = signal<boolean>(false);
    protected readonly isRestoring = signal<boolean>(false);
    protected readonly showPasswordReset = signal<boolean>(false);
    protected readonly isPasswordResetting = signal<boolean>(false);
    protected readonly passwordResetSent = signal<boolean>(false);
    protected readonly passwordResetCooldownSeconds = signal<number>(0);
    protected readonly loginAutofillDetected = signal<boolean>(false);
    protected readonly loginFieldErrors = this.formManager.loginFieldErrors;
    protected readonly registerFieldErrors = this.formManager.registerFieldErrors;
    protected readonly passwordResetFieldErrors = this.formManager.passwordResetFieldErrors;
    protected readonly loginSubmitLabelKey = computed(() => (this.isSubmitting() ? 'COMMON.LOADING' : 'AUTH.LOGIN.LOGIN'));
    private stopPasswordResetCooldown: (() => void) | null = null;
    private loginAutofillCheckTimerIds: number[] = [];
    private hasLoginNativeInteraction = false;
    protected readonly authTabs = AUTH_TABS;

    private returnUrl: string | null = null;
    private adminReturnUrl: string | null = null;

    public constructor() {
        this.formManager.configureSubmissionActions({
            login: this.submitLoginFormAsync,
            passwordReset: this.submitPasswordResetFormAsync,
            register: this.submitRegisterFormAsync,
        });
        this.subscribeFormChanges();
        this.registerRenderingEffects();
        afterNextRender(() => {
            this.startLoginAutofillDetection();
        });
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
            this.loginModel();
            this.clearGlobalError();
            this.updateLoginAutofillState();
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

    protected async onTabChangeAsync(mode: 'login' | 'register'): Promise<void> {
        if (this.authMode === mode) {
            return;
        }
        this.authMode = mode;
        this.formManager.resetAll();
        this.showPasswordReset.set(false);
        this.passwordResetSent.set(false);
        this.hasLoginNativeInteraction = false;
        this.loginAutofillDetected.set(false);
        if (this.useRouting() && this.router !== null) {
            await this.router.navigate(['/'], { queryParams: { auth: mode } });
        }
    }

    protected onLoginSubmit(): void {
        void submit(this.loginForm);
    }

    private async loginAsync(): Promise<void> {
        this.syncLoginNativeValues();

        if (this.loginForm().invalid() || this.isSubmitting()) {
            this.loginForm().markAsTouched();
            this.formManager.updateFieldErrors();
            return;
        }

        this.isSubmitting.set(true);

        const result = await firstValueFrom(this.authFlowFacade.login(this.loginModel()));
        this.isSubmitting.set(false);
        if (result === 'success') {
            await this.completeAuthenticatedNavigationAsync();
            this.closeDialogIfAny();
            return;
        }

        this.handleLoginResult(result);
    }

    protected isLoginSubmitDisabled(): boolean {
        return this.isSubmitting();
    }

    protected onLoginNativeInput(): void {
        this.hasLoginNativeInteraction = true;
        this.syncLoginNativeValues();
        this.updateLoginAutofillState();
    }

    protected onRestoreSubmit(): void {
        if (this.loginForm().invalid() || this.isRestoring()) {
            return;
        }

        const rememberMe = this.loginModel().rememberMe;
        this.isRestoring.set(true);

        this.authFlowFacade.restoreAccount(this.loginModel(), rememberMe).subscribe(success => {
            this.isRestoring.set(false);
            if (success) {
                this.completeAuthenticatedNavigationAndClose();
                return;
            }

            this.setGlobalError('FORM_ERRORS.UNKNOWN');
        });
    }

    protected onRegisterSubmit(): void {
        void submit(this.registerForm);
    }

    private async registerAsync(): Promise<void> {
        if (this.registerForm().invalid() || this.isSubmitting()) {
            this.registerForm().markAsTouched();
            this.formManager.updateFieldErrors();
            return;
        }

        this.isSubmitting.set(true);

        const result = await firstValueFrom(this.authFlowFacade.register(this.registerModel()));
        this.isSubmitting.set(false);
        if (result === 'success') {
            await this.navigationService.navigateToEmailVerificationPendingAsync();
            this.closeDialogIfAny();
            return;
        }

        this.handleRegisterResult(result);
    }

    private renderGoogleButton(): void {
        this.googleManager.renderButton(
            this.authMode,
            this.loginFormComponent()?.googleButton()?.nativeElement,
            this.registerFormComponent()?.googleButton()?.nativeElement,
        );
    }

    private onGoogleCredential(credential: string): void {
        this.isSubmitting.set(true);
        const rememberMe = this.authMode === 'login' ? this.loginModel().rememberMe : false;
        const request: GoogleLoginRequest = { credential, rememberMe: Boolean(rememberMe) };
        this.authFlowFacade.loginWithGoogle(request).subscribe(success => {
            this.isSubmitting.set(false);
            if (success) {
                this.completeAuthenticatedNavigationAndClose();
                return;
            }

            this.setGlobalError('FORM_ERRORS.UNKNOWN');
        });
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
        this.showPasswordReset.set(false);
        this.passwordResetSent.set(false);
        this.clearGlobalError();
    }

    protected onPasswordResetSubmit(): void {
        void submit(this.passwordResetForm);
    }

    private async requestPasswordResetAsync(): Promise<void> {
        if (this.passwordResetForm().invalid() || this.isPasswordResetting()) {
            this.passwordResetForm().markAsTouched();
            this.formManager.updateFieldErrors();
            return;
        }
        if (this.passwordResetCooldownSeconds() > 0) {
            return;
        }

        this.isPasswordResetting.set(true);

        const success = await firstValueFrom(this.authFlowFacade.requestPasswordReset(this.passwordResetModel()));
        this.isPasswordResetting.set(false);
        if (success) {
            this.passwordResetSent.set(true);
            this.startPasswordResetCooldown();
            return;
        }

        this.setGlobalError('FORM_ERRORS.UNKNOWN');
    }

    private startPasswordResetCooldown(seconds = this.passwordResetCooldownSecondsDefault): void {
        this.stopPasswordResetCooldown?.();
        this.stopPasswordResetCooldown = startSecondsCountdown(this.passwordResetCooldownSeconds, seconds, this.destroyRef);
    }

    private closeDialogIfAny(): void {
        this.dialogRef?.close();
    }

    private completeAuthenticatedNavigationAndClose(): void {
        void this.completeAuthenticatedNavigationAsync()
            .then(() => {
                this.closeDialogIfAny();
            })
            .catch(() => {
                this.setGlobalError('FORM_ERRORS.UNKNOWN');
            });
    }

    private async completeAuthenticatedNavigationAsync(): Promise<void> {
        if (this.authService.mustChangePassword()) {
            await this.navigationService.navigateToRequiredPasswordChangeAsync();
            this.closeDialogIfAny();
            return;
        }

        if (!this.authService.isEmailConfirmed()) {
            await this.navigationService.navigateToEmailVerificationPendingAsync({ autoResend: true });
            return;
        }

        const adminRedirectUrl = await this.tryBuildAdminRedirectUrlAsync();
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
        if (adminPath === null || adminPath.length === 0) {
            return null;
        }

        if (!this.authService.isAdmin()) {
            return buildAdminUnauthorizedUrl(adminPath, 'forbidden', adminAppUrl, this.document.location.origin);
        }

        try {
            const response = await firstValueFrom(this.authService.startAdminSso());
            const adminUrl = new URL(adminPath, adminAppUrl);
            adminUrl.searchParams.set('code', response.code);
            return adminUrl.toString();
        } catch {
            return buildAdminUnauthorizedUrl(adminPath, 'forbidden', adminAppUrl, this.document.location.origin);
        }
    }

    private handleLoginResult(result: AuthLoginResult): void {
        if (result === 'invalidCredentials') {
            this.setGlobalError('FORM_ERRORS.INVALID_CREDENTIALS');
            this.showRestoreAction.set(false);
        } else if (result === 'accountDeleted') {
            this.setGlobalError('AUTH.LOGIN.ACCOUNT_DELETED');
            this.showRestoreAction.set(true);
        } else {
            this.setGlobalError('FORM_ERRORS.UNKNOWN');
            this.showRestoreAction.set(false);
        }
    }

    private handleRegisterResult(result: AuthRegisterResult): void {
        if (result === 'emailExists') {
            this.formManager.setRegisterEmailExistsError();
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
        this.globalError.set(null);
        this.showRestoreAction.set(false);
    }

    private syncLoginNativeValues(): void {
        const form = this.loginFormComponent()?.formElement()?.nativeElement;

        if (form === undefined) {
            return;
        }

        const fields = getLoginAutofillFieldValues(form);

        this.loginModel.update(value => ({
            ...value,
            email: fields.email.length > 0 ? fields.email : value.email,
            password: fields.password.length > 0 ? fields.password : value.password,
        }));
    }

    private startLoginAutofillDetection(): void {
        if (!this.isBrowser) {
            return;
        }

        this.updateLoginAutofillState();
        this.loginAutofillCheckTimerIds = this.loginAutofillCheckDelaysMs.map(delay =>
            window.setTimeout(() => {
                this.updateLoginAutofillState();
            }, delay),
        );

        this.destroyRef.onDestroy(() => {
            this.loginAutofillCheckTimerIds.forEach(timerId => {
                window.clearTimeout(timerId);
            });
            this.loginAutofillCheckTimerIds = [];
        });
    }

    private updateLoginAutofillState(): void {
        const form = this.loginFormComponent()?.formElement()?.nativeElement;
        const hasAutofill = hasCompleteLoginAutofill(form, this.hasLoginNativeInteraction);

        if (this.loginAutofillDetected() === hasAutofill) {
            return;
        }

        this.loginAutofillDetected.set(hasAutofill);
    }
}

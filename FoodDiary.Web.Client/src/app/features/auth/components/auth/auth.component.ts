import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    effect,
    inject,
    input,
    signal,
    viewChild,
} from '@angular/core';
import { ChangeDetectorRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiTabsComponent } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
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
import { AUTH_VALIDATION_ERRORS_PROVIDER } from './auth-lib/auth-validation-errors.provider';
import { AuthLoginFormComponent } from './auth-login-form/auth-login-form.component';
import { AuthPasswordResetFormComponent } from './auth-password-reset-form/auth-password-reset-form.component';
import { AuthRegisterFormComponent } from './auth-register-form/auth-register-form.component';

@Component({
    selector: 'fd-auth',
    templateUrl: './auth.component.html',
    styleUrls: ['./auth.component.scss'],
    providers: [AUTH_VALIDATION_ERRORS_PROVIDER, AuthFormManager, AuthGoogleManager],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslateModule, FdUiTabsComponent, AuthLoginFormComponent, AuthPasswordResetFormComponent, AuthRegisterFormComponent],
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
    private readonly cdr = inject(ChangeDetectorRef);
    private readonly destroyRef = inject(DestroyRef);
    private readonly dialogRef = inject(FdUiDialogRef<AuthComponent>, { optional: true });
    private readonly passwordResetCooldownSecondsDefault = inject(AUTH_PASSWORD_RESET_COOLDOWN_SECONDS);
    private readonly loginAutofillCheckDelaysMs = inject(AUTH_LOGIN_AUTOFILL_CHECK_DELAYS_MS);
    private readonly formManager = inject(AuthFormManager);
    private readonly googleManager = inject(AuthGoogleManager);
    private readonly authFlowFacade = inject(AuthFlowFacade);

    public authMode: 'login' | 'register' = 'login';

    public readonly loginForm = this.formManager.loginForm;
    public readonly registerForm = this.formManager.registerForm;
    public readonly passwordResetForm = this.formManager.passwordResetForm;
    public readonly globalError = signal<string | null>(null);
    public readonly isSubmitting = signal<boolean>(false);
    public readonly googleReady = this.googleManager.ready;
    public readonly showRestoreAction = signal<boolean>(false);
    public readonly isRestoring = signal<boolean>(false);
    public readonly showPasswordReset = signal<boolean>(false);
    public readonly isPasswordResetting = signal<boolean>(false);
    public readonly passwordResetSent = signal<boolean>(false);
    public readonly passwordResetCooldownSeconds = signal<number>(0);
    public readonly loginAutofillDetected = signal<boolean>(false);
    public readonly loginFieldErrors = this.formManager.loginFieldErrors;
    public readonly registerFieldErrors = this.formManager.registerFieldErrors;
    public readonly passwordResetFieldErrors = this.formManager.passwordResetFieldErrors;
    public readonly loginSubmitLabelKey = computed(() => (this.isSubmitting() ? 'COMMON.LOADING' : 'AUTH.LOGIN.LOGIN'));
    private stopPasswordResetCooldown: (() => void) | null = null;
    private loginAutofillCheckTimerIds: number[] = [];
    private hasLoginNativeInteraction = false;
    public readonly authTabs = AUTH_TABS;

    private returnUrl: string | null = null;
    private adminReturnUrl: string | null = null;

    public constructor() {
        this.subscribeFormChanges();
        this.registerRenderingEffects();
        afterNextRender(() => {
            this.startLoginAutofillDetection();
        });
        void this.googleManager.initializeAsync(credential => {
            this.onGoogleCredential(credential);
        });
    }

    private subscribeFormChanges(): void {
        this.loginForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.clearGlobalError();
            this.formManager.markDirtyControlsTouched(this.loginForm);
            this.updateLoginAutofillState();
            this.cdr.markForCheck();
        });
        this.registerForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.clearGlobalError();
            this.formManager.markDirtyControlsTouched(this.registerForm);
            this.cdr.markForCheck();
        });
        this.passwordResetForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.clearGlobalError();
            this.formManager.markDirtyControlsTouched(this.passwordResetForm);
            this.cdr.markForCheck();
        });
    }

    private registerRenderingEffects(): void {
        effect(() => {
            this.renderGoogleButton();
        });
        effect(() => {
            const routeMode = this.route?.snapshot.paramMap.get('mode') === 'register' ? 'register' : 'login';
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

    public handleTabChange(value: string): void {
        const mode: 'login' | 'register' = value === 'register' ? 'register' : 'login';
        void this.onTabChangeAsync(mode);
    }

    public async onTabChangeAsync(mode: 'login' | 'register'): Promise<void> {
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
            await this.router.navigate(['/auth', mode]);
        }
    }

    public onLoginSubmit(): void {
        this.syncLoginNativeValues();

        if (!this.loginForm.valid || this.isSubmitting()) {
            this.loginForm.markAllAsTouched();
            this.cdr.markForCheck();
            return;
        }

        this.isSubmitting.set(true);

        this.authFlowFacade.login(this.loginForm.value).subscribe(result => {
            this.isSubmitting.set(false);
            if (result === 'success') {
                this.completeAuthenticatedNavigationAndClose();
                return;
            }

            this.handleLoginResult(result);
        });
    }

    public isLoginSubmitDisabled(): boolean {
        return this.isSubmitting() || (this.loginForm.invalid && !this.loginAutofillDetected());
    }

    public onLoginNativeInput(): void {
        this.hasLoginNativeInteraction = true;
        this.syncLoginNativeValues();
        this.updateLoginAutofillState();
    }

    public onRestoreSubmit(): void {
        if (!this.loginForm.valid || this.isRestoring()) {
            return;
        }

        const rememberMe = this.loginForm.controls.rememberMe.value;
        this.isRestoring.set(true);

        this.authFlowFacade.restoreAccount(this.loginForm.value, rememberMe).subscribe(success => {
            this.isRestoring.set(false);
            if (success) {
                this.completeAuthenticatedNavigationAndClose();
                return;
            }

            this.setGlobalError('FORM_ERRORS.UNKNOWN');
        });
    }

    public onRegisterSubmit(): void {
        if (!this.registerForm.valid || this.isSubmitting()) {
            return;
        }

        this.isSubmitting.set(true);

        this.authFlowFacade.register(this.registerForm.value).subscribe(result => {
            this.isSubmitting.set(false);
            if (result === 'success') {
                void this.navigationService.navigateToEmailVerificationPendingAsync();
                this.closeDialogIfAny();
                return;
            }

            this.handleRegisterResult(result);
        });
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
        const rememberMe = this.authMode === 'login' ? this.loginForm.controls.rememberMe.value : false;
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

    public onPasswordResetOpen(): void {
        if (this.showPasswordReset()) {
            return;
        }
        this.clearGlobalError();
        this.passwordResetForm.reset({
            email: this.loginForm.controls.email.value,
        });
        this.passwordResetSent.set(false);
        this.showPasswordReset.set(true);
    }

    public onPasswordResetClose(): void {
        this.showPasswordReset.set(false);
        this.passwordResetSent.set(false);
        this.clearGlobalError();
    }

    public onPasswordResetSubmit(): void {
        if (!this.passwordResetForm.valid || this.isPasswordResetting()) {
            return;
        }
        if (this.passwordResetCooldownSeconds() > 0) {
            return;
        }

        this.isPasswordResetting.set(true);

        this.authFlowFacade.requestPasswordReset(this.passwordResetForm.value).subscribe(success => {
            this.isPasswordResetting.set(false);
            if (success) {
                this.passwordResetSent.set(true);
                this.startPasswordResetCooldown();
                return;
            }

            this.setGlobalError('FORM_ERRORS.UNKNOWN');
        });
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
        if (!this.authService.isEmailConfirmed()) {
            await this.navigationService.navigateToEmailVerificationPendingAsync({ autoResend: true });
            return;
        }

        const adminRedirectUrl = await this.tryBuildAdminRedirectUrlAsync();
        if (adminRedirectUrl !== null) {
            window.location.assign(adminRedirectUrl);
            return;
        }

        await this.navigationService.navigateToReturnUrlAsync(this.returnUrl);
    }

    private async tryBuildAdminRedirectUrlAsync(): Promise<string | null> {
        const adminReturnUrl = this.adminReturnUrl;
        const adminAppUrl = environment.adminAppUrl ?? '';
        if (adminReturnUrl === null || adminReturnUrl.length === 0 || adminAppUrl.length === 0) {
            return null;
        }

        const adminPath = normalizeAdminReturnUrl(adminReturnUrl, adminAppUrl, window.location.origin);
        if (adminPath === null || adminPath.length === 0) {
            return null;
        }

        if (!this.authService.isAdmin()) {
            return buildAdminUnauthorizedUrl(adminPath, 'forbidden', adminAppUrl, window.location.origin);
        }

        try {
            const response = await firstValueFrom(this.authService.startAdminSso());
            const adminUrl = new URL(adminPath, adminAppUrl);
            adminUrl.searchParams.set('code', response.code);
            return adminUrl.toString();
        } catch {
            return buildAdminUnauthorizedUrl(adminPath, 'forbidden', adminAppUrl, window.location.origin);
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
            const emailField = this.registerForm.controls.email;
            emailField.updateValueAndValidity();
            emailField.setErrors({ userExists: true });
            this.formManager.updateFieldErrors();
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

        this.loginForm.patchValue(
            {
                email: fields.email.length > 0 ? fields.email : this.loginForm.controls.email.value,
                password: fields.password.length > 0 ? fields.password : this.loginForm.controls.password.value,
            },
            { emitEvent: true },
        );
    }

    private startLoginAutofillDetection(): void {
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
        this.cdr.markForCheck();
    }
}

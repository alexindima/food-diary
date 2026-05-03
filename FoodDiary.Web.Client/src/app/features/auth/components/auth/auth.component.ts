import { type HttpErrorResponse } from '@angular/common/http';
import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    effect,
    type ElementRef,
    type FactoryProvider,
    inject,
    input,
    signal,
    viewChild,
} from '@angular/core';
import { ChangeDetectorRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type AbstractControl, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox.component';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FD_VALIDATION_ERRORS, FdUiFormErrorComponent, type FdValidationErrors } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { type FdUiTab, FdUiTabsComponent } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../../../environments/environment';
import { AuthService } from '../../../../services/auth.service';
import { LocalizationService } from '../../../../services/localization.service';
import { NavigationService } from '../../../../services/navigation.service';
import { type FormGroupControls } from '../../../../shared/lib/common.data';
import { matchFieldValidator } from '../../../../validators/match-field.validator';
import { GoogleIdentityService } from '../../lib/google-identity.service';
import { LoginRequest, PasswordResetRequest, RegisterRequest, RestoreAccountRequest } from '../../models/auth.data';
import { type GoogleLoginRequest } from '../../models/google-auth.data';

export const VALIDATION_ERRORS_PROVIDER: FactoryProvider = {
    provide: FD_VALIDATION_ERRORS,
    useFactory: (): FdValidationErrors => ({
        required: () => 'FORM_ERRORS.REQUIRED',
        requiredTrue: () => 'FORM_ERRORS.REQUIRED',
        email: () => 'FORM_ERRORS.EMAIL',
        matchField: () => 'FORM_ERRORS.PASSWORD.MATCH',
        minlength: (error?: unknown) => ({
            key: 'FORM_ERRORS.PASSWORD.MIN_LENGTH',
            params: { requiredLength: (error as { requiredLength?: number } | undefined)?.requiredLength },
        }),
        userExists: () => 'FORM_ERRORS.USER_EXISTS',
    }),
};

@Component({
    selector: 'fd-auth',
    templateUrl: './auth.component.html',
    styleUrls: ['./auth.component.scss'],
    providers: [VALIDATION_ERRORS_PROVIDER],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        TranslateModule,
        ReactiveFormsModule,
        FdUiInputComponent,
        FdUiButtonComponent,
        FdUiCheckboxComponent,
        FdUiFormErrorComponent,
        FdUiTabsComponent,
    ],
})
export class AuthComponent {
    public readonly useRouting = input(true);
    public readonly initialMode = input<'login' | 'register'>('login');
    public readonly initialReturnUrl = input<string | null>(null);
    public readonly initialAdminReturnUrl = input<string | null>(null);
    private readonly loginFormElement = viewChild<ElementRef<HTMLFormElement>>('loginFormElement');
    private readonly googleLoginButton = viewChild<ElementRef<HTMLElement>>('googleLoginButton');
    private readonly googleRegisterButton = viewChild<ElementRef<HTMLElement>>('googleRegisterButton');

    private readonly route = inject(ActivatedRoute, { optional: true });
    private readonly router = inject(Router, { optional: true });
    private readonly navigationService = inject(NavigationService);
    private readonly authService = inject(AuthService);
    private readonly translateService = inject(TranslateService);
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });
    private readonly cdr = inject(ChangeDetectorRef);
    private readonly googleIdentityService = inject(GoogleIdentityService);
    private readonly localizationService = inject(LocalizationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly dialogRef = inject(FdUiDialogRef<AuthComponent>, { optional: true });

    public authMode: 'login' | 'register' = 'login';

    public loginForm: FormGroup<LoginFormGroup>;
    public registerForm: FormGroup<RegisterFormGroup>;
    public passwordResetForm: FormGroup<PasswordResetFormGroup>;
    public readonly globalError = signal<string | null>(null);
    public readonly isSubmitting = signal<boolean>(false);
    public readonly googleReady = signal<boolean>(false);
    public readonly showRestoreAction = signal<boolean>(false);
    public readonly isRestoring = signal<boolean>(false);
    public readonly showPasswordReset = signal<boolean>(false);
    public readonly isPasswordResetting = signal<boolean>(false);
    public readonly passwordResetSent = signal<boolean>(false);
    public readonly passwordResetCooldownSeconds = signal<number>(0);
    public readonly loginAutofillDetected = signal<boolean>(false);
    private passwordResetCooldownTimerId: number | null = null;
    private loginAutofillCheckTimerIds: number[] = [];
    private hasLoginNativeInteraction = false;
    public authBenefits: string[] = ['AUTH.INFO.HIGHLIGHTS.SYNC', 'AUTH.INFO.HIGHLIGHTS.INSIGHTS', 'AUTH.INFO.HIGHLIGHTS.LIBRARY'];
    public readonly authTabs: FdUiTab[] = [
        { value: 'login', labelKey: 'AUTH.LOGIN.TITLE' },
        { value: 'register', labelKey: 'AUTH.REGISTER.TITLE' },
    ];

    private returnUrl: string | null = null;
    private adminReturnUrl: string | null = null;

    public constructor() {
        this.loginForm = new FormGroup<LoginFormGroup>({
            email: new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
            password: new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.minLength(6)] }),
            rememberMe: new FormControl<boolean>(false, { nonNullable: true }),
        });

        this.registerForm = new FormGroup<RegisterFormGroup>({
            email: new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
            password: new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.minLength(6)] }),
            confirmPassword: new FormControl<string>('', {
                nonNullable: true,
                validators: [Validators.required, matchFieldValidator('password')],
            }),
            agreeTerms: new FormControl<boolean>(false, { nonNullable: true, validators: Validators.requiredTrue }),
        });

        this.passwordResetForm = new FormGroup<PasswordResetFormGroup>({
            email: new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
        });

        this.loginForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.clearGlobalError();
            this.markDirtyControlsTouched(this.loginForm);
            this.updateLoginAutofillState();
            this.cdr.markForCheck();
        });
        this.registerForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.clearGlobalError();
            this.markDirtyControlsTouched(this.registerForm);
            this.cdr.markForCheck();
        });
        this.passwordResetForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.clearGlobalError();
            this.markDirtyControlsTouched(this.passwordResetForm);
            this.cdr.markForCheck();
        });

        this.registerForm.controls.password.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.registerForm.controls.confirmPassword.updateValueAndValidity();
        });

        effect(() => {
            this.renderGoogleButton();
        });
        effect(() => {
            const routeMode = this.route?.snapshot.params['mode'] === 'register' ? 'register' : 'login';
            this.authMode = this.useRouting() ? routeMode : this.initialMode();
            this.returnUrl = this.useRouting() ? this.route?.snapshot.queryParams['returnUrl'] || null : this.initialReturnUrl();
            this.adminReturnUrl = this.useRouting()
                ? this.route?.snapshot.queryParams['adminReturnUrl'] || null
                : this.initialAdminReturnUrl();
        });
        effect(() => {
            if (!this.adminReturnUrl) {
                return;
            }

            if (!this.authService.isAuthenticated()) {
                return;
            }

            void this.completeAuthenticatedNavigation();
        });
        afterNextRender(() => {
            this.startLoginAutofillDetection();
        });
        void this.initializeGoogle();
    }

    public handleTabChange(value: string): void {
        const mode: 'login' | 'register' = value === 'register' ? 'register' : 'login';
        void this.onTabChange(mode);
    }

    public async onTabChange(mode: 'login' | 'register'): Promise<void> {
        if (this.authMode === mode) {
            return;
        }
        this.authMode = mode;
        this.loginForm.reset({
            email: '',
            password: '',
            rememberMe: false,
        });
        this.registerForm.reset({
            email: '',
            password: '',
            confirmPassword: '',
            agreeTerms: false,
        });
        this.passwordResetForm.reset({
            email: '',
        });
        this.showPasswordReset.set(false);
        this.passwordResetSent.set(false);
        this.hasLoginNativeInteraction = false;
        this.loginAutofillDetected.set(false);
        if (this.useRouting() && this.router) {
            await this.router.navigate(['/auth', mode]);
        }
    }

    public async onLoginSubmit(): Promise<void> {
        this.syncLoginNativeValues();

        if (!this.loginForm.valid || this.isSubmitting()) {
            this.loginForm.markAllAsTouched();
            this.cdr.markForCheck();
            return;
        }

        const loginRequest = new LoginRequest(this.loginForm.value);
        this.isSubmitting.set(true);

        this.authService.login(loginRequest).subscribe({
            next: () => {
                this.isSubmitting.set(false);
                this.completeAuthenticatedNavigationAndClose();
            },
            error: (error: HttpErrorResponse) => {
                this.isSubmitting.set(false);
                this.handleLoginError(error.error?.error);
            },
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

        const restoreRequest = new RestoreAccountRequest(this.loginForm.value);
        const rememberMe = this.loginForm.controls.rememberMe.value;
        this.isRestoring.set(true);

        this.authService.restoreAccount(restoreRequest, rememberMe).subscribe({
            next: () => {
                this.isRestoring.set(false);
                this.completeAuthenticatedNavigationAndClose();
            },
            error: () => {
                this.isRestoring.set(false);
                this.setGlobalError('FORM_ERRORS.UNKNOWN');
            },
        });
    }

    public async onRegisterSubmit(): Promise<void> {
        if (!this.registerForm.valid || this.isSubmitting()) {
            return;
        }

        const registerRequest = new RegisterRequest({
            ...this.registerForm.value,
            language: this.localizationService.getCurrentLanguage(),
        });

        this.isSubmitting.set(true);

        this.authService.register(registerRequest).subscribe({
            next: () => {
                this.isSubmitting.set(false);
                void this.navigationService.navigateToEmailVerificationPending();
                this.closeDialogIfAny();
            },
            error: (error: HttpErrorResponse) => {
                this.isSubmitting.set(false);
                this.handleRegisterError(error.error?.error);
            },
        });
    }

    private async initializeGoogle(): Promise<void> {
        const clientId = environment.googleClientId;
        if (!clientId) {
            return;
        }
        try {
            await this.googleIdentityService.initialize({
                clientId,
                callback: credential => {
                    this.onGoogleCredential(credential);
                },
            });
            this.googleReady.set(true);
            void this.googleIdentityService.prompt();
        } catch {
            this.googleReady.set(false);
        }
    }

    private renderGoogleButton(): void {
        if (!this.googleReady()) {
            return;
        }
        const target = this.authMode === 'login' ? this.googleLoginButton()?.nativeElement : this.googleRegisterButton()?.nativeElement;
        [this.googleLoginButton(), this.googleRegisterButton()].forEach(ref => {
            if (ref?.nativeElement) {
                ref.nativeElement.innerHTML = '';
            }
        });
        if (target) {
            this.googleIdentityService.renderButton(target, 'filled_blue');
        }
    }

    private onGoogleCredential(credential: string): void {
        this.isSubmitting.set(true);
        const rememberMe = this.authMode === 'login' ? this.loginForm.controls.rememberMe.value : false;
        const request: GoogleLoginRequest = { credential, rememberMe: !!rememberMe };
        this.authService.loginWithGoogle(request).subscribe({
            next: () => {
                this.isSubmitting.set(false);
                this.completeAuthenticatedNavigationAndClose();
            },
            error: () => {
                this.isSubmitting.set(false);
                this.setGlobalError('FORM_ERRORS.UNKNOWN');
            },
        });
    }

    public onPasswordResetOpen(): void {
        if (this.showPasswordReset()) {
            return;
        }
        this.clearGlobalError();
        this.passwordResetForm.reset({
            email: this.loginForm.controls.email.value || '',
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

        const request = new PasswordResetRequest(this.passwordResetForm.value);
        this.isPasswordResetting.set(true);

        this.authService.requestPasswordReset(request).subscribe({
            next: () => {
                this.isPasswordResetting.set(false);
                this.passwordResetSent.set(true);
                this.startPasswordResetCooldown();
            },
            error: () => {
                this.isPasswordResetting.set(false);
                this.setGlobalError('FORM_ERRORS.UNKNOWN');
            },
        });
    }

    private startPasswordResetCooldown(seconds = 60): void {
        this.passwordResetCooldownSeconds.set(seconds);
        if (this.passwordResetCooldownTimerId) {
            window.clearInterval(this.passwordResetCooldownTimerId);
        }
        this.passwordResetCooldownTimerId = window.setInterval(() => {
            const remaining = this.passwordResetCooldownSeconds();
            if (remaining <= 1) {
                this.passwordResetCooldownSeconds.set(0);
                if (this.passwordResetCooldownTimerId) {
                    window.clearInterval(this.passwordResetCooldownTimerId);
                    this.passwordResetCooldownTimerId = null;
                }
                return;
            }
            this.passwordResetCooldownSeconds.set(remaining - 1);
        }, 1000);

        this.destroyRef.onDestroy(() => {
            if (this.passwordResetCooldownTimerId) {
                window.clearInterval(this.passwordResetCooldownTimerId);
                this.passwordResetCooldownTimerId = null;
            }
        });
    }

    private closeDialogIfAny(): void {
        this.dialogRef?.close();
    }

    private completeAuthenticatedNavigationAndClose(): void {
        void this.completeAuthenticatedNavigation()
            .then(() => {
                this.closeDialogIfAny();
            })
            .catch(() => {
                this.setGlobalError('FORM_ERRORS.UNKNOWN');
            });
    }

    private async completeAuthenticatedNavigation(): Promise<void> {
        if (!this.authService.isEmailConfirmed()) {
            await this.navigationService.navigateToEmailVerificationPending({ autoResend: true });
            return;
        }

        const adminRedirectUrl = await this.tryBuildAdminRedirectUrl();
        if (adminRedirectUrl) {
            window.location.assign(adminRedirectUrl);
            return;
        }

        await this.navigationService.navigateToReturnUrl(this.returnUrl);
    }

    private async tryBuildAdminRedirectUrl(): Promise<string | null> {
        if (!this.adminReturnUrl || !environment.adminAppUrl) {
            return null;
        }

        const adminPath = this.normalizeAdminReturnUrl(this.adminReturnUrl);
        if (!adminPath) {
            return null;
        }

        if (!this.authService.isAdmin()) {
            return this.buildAdminUnauthorizedUrl(adminPath, 'forbidden');
        }

        try {
            const response = await firstValueFrom(this.authService.startAdminSso());
            const adminUrl = new URL(adminPath, environment.adminAppUrl);
            adminUrl.searchParams.set('code', response.code);
            return adminUrl.toString();
        } catch {
            return this.buildAdminUnauthorizedUrl(adminPath, 'forbidden');
        }
    }

    private buildAdminUnauthorizedUrl(returnUrl: string, reason: 'forbidden' | 'unauthenticated'): string {
        const unauthorizedUrl = new URL('/unauthorized', environment.adminAppUrl);
        unauthorizedUrl.searchParams.set('reason', reason);
        unauthorizedUrl.searchParams.set('returnUrl', returnUrl);
        return unauthorizedUrl.toString();
    }

    private normalizeAdminReturnUrl(value: string): string | null {
        if (!value) {
            return '/';
        }

        const decoded = this.safeDecode(value);
        if (decoded.includes('returnUrl=')) {
            return '/';
        }

        try {
            const parsed = new URL(decoded, environment.adminAppUrl || window.location.origin);
            if (environment.adminAppUrl) {
                const adminOrigin = new URL(environment.adminAppUrl).origin;
                if (parsed.origin !== adminOrigin) {
                    return '/';
                }
            }

            const search = parsed.searchParams.toString();
            return search ? `${parsed.pathname}?${search}` : parsed.pathname;
        } catch {
            return decoded.startsWith('/') ? decoded : '/';
        }
    }

    private safeDecode(value: string): string {
        try {
            return decodeURIComponent(value);
        } catch {
            return value;
        }
    }

    private handleLoginError(errorCode?: string): void {
        if (errorCode === 'User.InvalidCredentials' || errorCode === 'Authentication.InvalidCredentials') {
            this.setGlobalError('FORM_ERRORS.INVALID_CREDENTIALS');
            this.showRestoreAction.set(false);
        } else if (errorCode === 'Authentication.AccountDeleted') {
            this.setGlobalError('AUTH.LOGIN.ACCOUNT_DELETED');
            this.showRestoreAction.set(true);
        } else {
            this.setGlobalError('FORM_ERRORS.UNKNOWN');
            this.showRestoreAction.set(false);
        }
    }

    private handleRegisterError(errorCode?: string): void {
        if (errorCode === 'User.EmailAlreadyExists' || errorCode === 'Validation.Conflict') {
            const emailField = this.registerForm.controls.email;
            emailField?.updateValueAndValidity();
            emailField?.setErrors({ userExists: true });
        } else if (errorCode === 'Authentication.AccountDeleted') {
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
        const form = this.loginFormElement()?.nativeElement;

        if (!form) {
            return;
        }

        const emailInput = form.querySelector<HTMLInputElement>('input[autocomplete="username"]');
        const passwordInput = form.querySelector<HTMLInputElement>('input[autocomplete="current-password"]');

        this.loginForm.patchValue(
            {
                email: emailInput?.value ?? this.loginForm.controls.email.value,
                password: passwordInput?.value ?? this.loginForm.controls.password.value,
            },
            { emitEvent: true },
        );
    }

    private startLoginAutofillDetection(): void {
        this.updateLoginAutofillState();
        this.loginAutofillCheckTimerIds = [100, 300, 700, 1500, 3000, 5000].map(delay =>
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
        const form = this.loginFormElement()?.nativeElement;
        const hasAutofill = this.hasCompleteLoginAutofill(form);

        if (this.loginAutofillDetected() === hasAutofill) {
            return;
        }

        this.loginAutofillDetected.set(hasAutofill);
        this.cdr.markForCheck();
    }

    private hasCompleteLoginAutofill(form: HTMLFormElement | undefined): boolean {
        if (!form) {
            return false;
        }

        const email = form.querySelector<HTMLInputElement>('input[autocomplete="username"]')?.value ?? '';
        const password = form.querySelector<HTMLInputElement>('input[autocomplete="current-password"]')?.value ?? '';

        if (email && password) {
            return true;
        }

        if (this.hasLoginNativeInteraction) {
            return false;
        }

        if (email || password) {
            return false;
        }

        try {
            return form.querySelectorAll('input:-webkit-autofill').length >= 2;
        } catch {
            return false;
        }
    }

    public getControlError(control: AbstractControl | null): string | null {
        if (!control || !control.invalid) {
            return null;
        }

        const shouldShow = control.touched || control.dirty;
        if (!shouldShow) {
            return null;
        }

        const errors = control.errors;
        if (!errors) {
            return null;
        }

        for (const key of Object.keys(errors)) {
            const resolver = this.validationErrors?.[key];
            if (!resolver) {
                continue;
            }

            const controlParams = typeof errors[key] === 'object' ? errors[key] : {};
            const result = resolver(errors[key]);

            if (typeof result === 'string') {
                return this.translateService.instant(result, controlParams);
            }

            return this.translateService.instant(result.key, {
                ...controlParams,
                ...(result.params ?? {}),
            });
        }

        return this.translateService.instant('FORM_ERRORS.UNKNOWN');
    }

    private markDirtyControlsTouched(form: FormGroup): void {
        Object.values(form.controls).forEach(control => {
            if (control.dirty && !control.touched) {
                control.markAsTouched();
            }
        });
    }
}

interface LoginFormValues {
    email: string;
    password: string;
    rememberMe: boolean;
}

interface RegisterFormValues {
    email: string;
    password: string;
    confirmPassword: string;
    agreeTerms: boolean;
}

type LoginFormGroup = FormGroupControls<LoginFormValues>;
type RegisterFormGroup = FormGroupControls<RegisterFormValues>;

interface PasswordResetFormValues {
    email: string;
}

type PasswordResetFormGroup = FormGroupControls<PasswordResetFormValues>;

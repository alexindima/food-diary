import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    computed,
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
import {
    FD_VALIDATION_ERRORS,
    FdUiFormErrorComponent,
    type FdValidationErrorConfig,
    type FdValidationErrors,
    getNumberProperty,
} from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { type FdUiTab, FdUiTabsComponent } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { EMPTY, firstValueFrom, merge, type Observable } from 'rxjs';

import { environment } from '../../../../../environments/environment';
import { AuthService } from '../../../../services/auth.service';
import { LocalizationService } from '../../../../services/localization.service';
import { NavigationService } from '../../../../services/navigation.service';
import type { FormGroupControls } from '../../../../shared/lib/common.data';
import { matchFieldValidator } from '../../../../validators/match-field.validator';
import { GoogleIdentityService } from '../../lib/google-identity.service';
import { LoginRequest, PasswordResetRequest, RegisterRequest, RestoreAccountRequest } from '../../models/auth.data';
import type { GoogleLoginRequest } from '../../models/google-auth.data';
import type { PasswordResetFieldErrors, PasswordResetFormGroup, RegisterFieldErrors, RegisterFormGroup } from './auth.types';
import { AuthGoogleSectionComponent } from './auth-google-section.component';
import { AuthPasswordResetFormComponent } from './auth-password-reset-form.component';
import { AuthRegisterFieldsComponent } from './auth-register-fields.component';

export const VALIDATION_ERRORS_PROVIDER: FactoryProvider = {
    provide: FD_VALIDATION_ERRORS,
    useFactory: (): FdValidationErrors => ({
        required: () => 'FORM_ERRORS.REQUIRED',
        requiredTrue: () => 'FORM_ERRORS.REQUIRED',
        email: () => 'FORM_ERRORS.EMAIL',
        matchField: () => 'FORM_ERRORS.PASSWORD.MATCH',
        minlength: (error?: unknown) => ({
            key: 'FORM_ERRORS.PASSWORD.MIN_LENGTH',
            params: { requiredLength: getNumberProperty(error, 'requiredLength') },
        }),
        userExists: () => 'FORM_ERRORS.USER_EXISTS',
    }),
};

const LOGIN_ERROR_FIELDS = ['email', 'password'] as const;
const REGISTER_ERROR_FIELDS = ['email', 'password', 'confirmPassword'] as const;
const PASSWORD_RESET_ERROR_FIELDS = ['email'] as const;
const PASSWORD_MIN_LENGTH = 6;
const PASSWORD_RESET_COOLDOWN_SECONDS = 60;
const MS_PER_SECOND = 1_000;
const LOGIN_AUTOFILL_CHECK_DELAY_SHORT_MS = 100;
const LOGIN_AUTOFILL_CHECK_DELAY_MEDIUM_MS = 300;
const LOGIN_AUTOFILL_CHECK_DELAY_LONG_MS = 700;
const LOGIN_AUTOFILL_CHECK_DELAY_EXTENDED_MS = 1_500;
const LOGIN_AUTOFILL_CHECK_DELAY_SLOW_MS = 3_000;
const LOGIN_AUTOFILL_CHECK_DELAY_FINAL_MS = 5_000;
const LOGIN_AUTOFILL_CHECK_DELAYS_MS = [
    LOGIN_AUTOFILL_CHECK_DELAY_SHORT_MS,
    LOGIN_AUTOFILL_CHECK_DELAY_MEDIUM_MS,
    LOGIN_AUTOFILL_CHECK_DELAY_LONG_MS,
    LOGIN_AUTOFILL_CHECK_DELAY_EXTENDED_MS,
    LOGIN_AUTOFILL_CHECK_DELAY_SLOW_MS,
    LOGIN_AUTOFILL_CHECK_DELAY_FINAL_MS,
] as const;
const LOGIN_AUTOFILL_FIELD_COUNT = 2;

type LoginErrorField = (typeof LOGIN_ERROR_FIELDS)[number];
type LoginFieldErrors = Record<LoginErrorField, string | null>;

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
        AuthPasswordResetFormComponent,
        AuthRegisterFieldsComponent,
        AuthGoogleSectionComponent,
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

    public loginForm!: FormGroup<LoginFormGroup>;
    public registerForm!: FormGroup<RegisterFormGroup>;
    public passwordResetForm!: FormGroup<PasswordResetFormGroup>;
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
    public readonly loginFieldErrors = signal<LoginFieldErrors>(this.createEmptyLoginFieldErrors());
    public readonly registerFieldErrors = signal<RegisterFieldErrors>(this.createEmptyRegisterFieldErrors());
    public readonly passwordResetFieldErrors = signal<PasswordResetFieldErrors>(this.createEmptyPasswordResetFieldErrors());
    public readonly loginSubmitLabelKey = computed(() => (this.isSubmitting() ? 'COMMON.LOADING' : 'AUTH.LOGIN.LOGIN'));
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
        this.initializeForms();
        this.subscribeFormChanges();
        this.subscribeValidationUpdates();
        this.registerRenderingEffects();
        this.updateFieldErrors();
        afterNextRender(() => {
            this.startLoginAutofillDetection();
        });
        void this.initializeGoogleAsync();
    }

    private initializeForms(): void {
        this.loginForm = new FormGroup<LoginFormGroup>({
            email: new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
            password: new FormControl<string>('', {
                nonNullable: true,
                validators: [Validators.required, Validators.minLength(PASSWORD_MIN_LENGTH)],
            }),
            rememberMe: new FormControl<boolean>(false, { nonNullable: true }),
        });

        this.registerForm = new FormGroup<RegisterFormGroup>({
            email: new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
            password: new FormControl<string>('', {
                nonNullable: true,
                validators: [Validators.required, Validators.minLength(PASSWORD_MIN_LENGTH)],
            }),
            confirmPassword: new FormControl<string>('', {
                nonNullable: true,
                validators: [Validators.required, matchFieldValidator('password')],
            }),
            agreeTerms: new FormControl<boolean>(false, { nonNullable: true, validators: Validators.requiredTrue }),
        });

        this.passwordResetForm = new FormGroup<PasswordResetFormGroup>({
            email: new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
        });
    }

    private subscribeFormChanges(): void {
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
    }

    private subscribeValidationUpdates(): void {
        const loginFormEvents = (this.loginForm as { events?: Observable<unknown> }).events ?? EMPTY;
        const registerFormEvents = (this.registerForm as { events?: Observable<unknown> }).events ?? EMPTY;
        const passwordResetFormEvents = (this.passwordResetForm as { events?: Observable<unknown> }).events ?? EMPTY;
        merge(
            loginFormEvents,
            this.loginForm.statusChanges,
            this.loginForm.valueChanges,
            registerFormEvents,
            this.registerForm.statusChanges,
            this.registerForm.valueChanges,
            passwordResetFormEvents,
            this.passwordResetForm.statusChanges,
            this.passwordResetForm.valueChanges,
            this.translateService.onLangChange,
        )
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.updateFieldErrors();
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

        const loginRequest = new LoginRequest(this.loginForm.value);
        this.isSubmitting.set(true);

        this.authService.login(loginRequest).subscribe({
            next: () => {
                this.isSubmitting.set(false);
                this.completeAuthenticatedNavigationAndClose();
            },
            error: (error: unknown) => {
                this.isSubmitting.set(false);
                this.handleLoginError(this.getApiErrorCode(error));
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

    public onRegisterSubmit(): void {
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
                void this.navigationService.navigateToEmailVerificationPendingAsync();
                this.closeDialogIfAny();
            },
            error: (error: unknown) => {
                this.isSubmitting.set(false);
                this.handleRegisterError(this.getApiErrorCode(error));
            },
        });
    }

    private async initializeGoogleAsync(): Promise<void> {
        const clientId = environment.googleClientId ?? '';
        if (clientId.length === 0) {
            return;
        }
        try {
            await this.googleIdentityService.initializeAsync({
                clientId,
                callback: credential => {
                    this.onGoogleCredential(credential);
                },
            });
            this.googleReady.set(true);
            this.googleIdentityService.prompt();
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
            const element = ref?.nativeElement;
            if (element !== undefined) {
                element.innerHTML = '';
            }
        });
        if (target !== undefined) {
            this.googleIdentityService.renderButton(target, 'filled_blue');
        }
    }

    private onGoogleCredential(credential: string): void {
        this.isSubmitting.set(true);
        const rememberMe = this.authMode === 'login' ? this.loginForm.controls.rememberMe.value : false;
        const request: GoogleLoginRequest = { credential, rememberMe: Boolean(rememberMe) };
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

    private startPasswordResetCooldown(seconds = PASSWORD_RESET_COOLDOWN_SECONDS): void {
        this.passwordResetCooldownSeconds.set(seconds);
        if (this.passwordResetCooldownTimerId !== null) {
            window.clearInterval(this.passwordResetCooldownTimerId);
        }
        this.passwordResetCooldownTimerId = window.setInterval(() => {
            const remaining = this.passwordResetCooldownSeconds();
            if (remaining <= 1) {
                this.passwordResetCooldownSeconds.set(0);
                if (this.passwordResetCooldownTimerId !== null) {
                    window.clearInterval(this.passwordResetCooldownTimerId);
                    this.passwordResetCooldownTimerId = null;
                }
                return;
            }
            this.passwordResetCooldownSeconds.set(remaining - 1);
        }, MS_PER_SECOND);

        this.destroyRef.onDestroy(() => {
            if (this.passwordResetCooldownTimerId !== null) {
                window.clearInterval(this.passwordResetCooldownTimerId);
                this.passwordResetCooldownTimerId = null;
            }
        });
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

        const adminPath = this.normalizeAdminReturnUrl(adminReturnUrl);
        if (adminPath === null || adminPath.length === 0) {
            return null;
        }

        if (!this.authService.isAdmin()) {
            return this.buildAdminUnauthorizedUrl(adminPath, 'forbidden');
        }

        try {
            const response = await firstValueFrom(this.authService.startAdminSso());
            const adminUrl = new URL(adminPath, adminAppUrl);
            adminUrl.searchParams.set('code', response.code);
            return adminUrl.toString();
        } catch {
            return this.buildAdminUnauthorizedUrl(adminPath, 'forbidden');
        }
    }

    private buildAdminUnauthorizedUrl(returnUrl: string, reason: 'forbidden' | 'unauthenticated'): string {
        const unauthorizedUrl = new URL('/unauthorized', environment.adminAppUrl ?? window.location.origin);
        unauthorizedUrl.searchParams.set('reason', reason);
        unauthorizedUrl.searchParams.set('returnUrl', returnUrl);
        return unauthorizedUrl.toString();
    }

    private normalizeAdminReturnUrl(value: string): string | null {
        if (value.length === 0) {
            return '/';
        }

        const decoded = this.safeDecode(value);
        if (decoded.includes('returnUrl=')) {
            return '/';
        }

        try {
            const adminAppUrl = environment.adminAppUrl ?? '';
            const parsed = new URL(decoded, adminAppUrl.length > 0 ? adminAppUrl : window.location.origin);
            if (adminAppUrl.length > 0) {
                const adminOrigin = new URL(adminAppUrl).origin;
                if (parsed.origin !== adminOrigin) {
                    return '/';
                }
            }

            const search = parsed.searchParams.toString();
            return search.length > 0 ? `${parsed.pathname}?${search}` : parsed.pathname;
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
            emailField.updateValueAndValidity();
            emailField.setErrors({ userExists: true });
            this.updateFieldErrors();
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

        if (form === undefined) {
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
        this.loginAutofillCheckTimerIds = LOGIN_AUTOFILL_CHECK_DELAYS_MS.map(delay =>
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
        if (form === undefined) {
            return false;
        }

        const fields = this.getLoginAutofillFieldValues(form);
        if (this.hasFilledLoginAutofillFields(fields)) {
            return true;
        }

        if (this.hasLoginNativeInteraction || this.hasPartialLoginAutofillFields(fields)) {
            return false;
        }

        return this.hasDetectedLoginWebkitAutofill(form);
    }

    private getLoginAutofillFieldValues(form: HTMLFormElement): { email: string; password: string } {
        return {
            email: form.querySelector<HTMLInputElement>('input[autocomplete="username"]')?.value ?? '',
            password: form.querySelector<HTMLInputElement>('input[autocomplete="current-password"]')?.value ?? '',
        };
    }

    private hasFilledLoginAutofillFields(fields: { email: string; password: string }): boolean {
        return fields.email.length > 0 && fields.password.length > 0;
    }

    private hasPartialLoginAutofillFields(fields: { email: string; password: string }): boolean {
        return fields.email.length > 0 || fields.password.length > 0;
    }

    private hasDetectedLoginWebkitAutofill(form: HTMLFormElement): boolean {
        try {
            return form.querySelectorAll('input:-webkit-autofill').length >= LOGIN_AUTOFILL_FIELD_COUNT;
        } catch {
            return false;
        }
    }

    private updateFieldErrors(): void {
        this.loginFieldErrors.set(
            LOGIN_ERROR_FIELDS.reduce<LoginFieldErrors>((errors, field) => {
                errors[field] = this.resolveControlError(this.loginForm.controls[field]);
                return errors;
            }, this.createEmptyLoginFieldErrors()),
        );
        this.registerFieldErrors.set(
            REGISTER_ERROR_FIELDS.reduce<RegisterFieldErrors>((errors, field) => {
                errors[field] = this.resolveControlError(this.registerForm.controls[field]);
                return errors;
            }, this.createEmptyRegisterFieldErrors()),
        );
        this.passwordResetFieldErrors.set(
            PASSWORD_RESET_ERROR_FIELDS.reduce<PasswordResetFieldErrors>((errors, field) => {
                errors[field] = this.resolveControlError(this.passwordResetForm.controls[field]);
                return errors;
            }, this.createEmptyPasswordResetFieldErrors()),
        );
    }

    private createEmptyLoginFieldErrors(): LoginFieldErrors {
        return {
            email: null,
            password: null,
        };
    }

    private createEmptyRegisterFieldErrors(): RegisterFieldErrors {
        return {
            email: null,
            password: null,
            confirmPassword: null,
        };
    }

    private createEmptyPasswordResetFieldErrors(): PasswordResetFieldErrors {
        return {
            email: null,
        };
    }

    private resolveControlError(control: AbstractControl | null): string | null {
        if (control?.invalid !== true) {
            return null;
        }

        const shouldShow = control.touched || control.dirty;
        if (!shouldShow) {
            return null;
        }

        const errors = control.errors;
        if (errors === null) {
            return null;
        }

        for (const key of Object.keys(errors)) {
            const resolver = this.validationErrors?.[key];
            if (resolver === undefined) {
                continue;
            }

            const controlError: unknown = errors[key];
            const controlParams = this.getValidationParams(controlError);
            const result = resolver(controlError);

            return this.translateValidationResult(result, controlParams);
        }

        return this.translateService.instant('FORM_ERRORS.UNKNOWN');
    }

    private translateValidationResult(result: FdValidationErrorConfig | string, controlParams: Record<string, unknown>): string {
        if (typeof result === 'string') {
            return this.translateService.instant(result, controlParams);
        }

        return this.translateService.instant(result.key, {
            ...controlParams,
            ...(result.params ?? {}),
        });
    }

    private markDirtyControlsTouched(form: FormGroup): void {
        Object.values(form.controls).forEach(control => {
            if (control.dirty && !control.touched) {
                control.markAsTouched();
            }
        });
    }

    private getApiErrorCode(error: unknown): string | undefined {
        if (!this.isRecord(error)) {
            return undefined;
        }

        const responseBody = error['error'];
        return this.isRecord(responseBody) && typeof responseBody['error'] === 'string' ? responseBody['error'] : undefined;
    }

    private getValidationParams(error: unknown): Record<string, unknown> {
        return this.isRecord(error) ? error : {};
    }

    private isRecord(value: unknown): value is Record<string, unknown> {
        return typeof value === 'object' && value !== null && !Array.isArray(value);
    }
}

interface LoginFormValues {
    email: string;
    password: string;
    rememberMe: boolean;
}

type LoginFormGroup = FormGroupControls<LoginFormValues>;

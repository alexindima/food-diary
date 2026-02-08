import {
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    FactoryProvider,
    Input,
    AfterViewInit,
    ElementRef,
    ViewChild,
    OnInit,
    inject,
    signal,
} from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { matchFieldValidator } from '../../validators/match-field.validator';
import { NavigationService } from '../../services/navigation.service';
import { FormGroupControls } from '../../types/common.data';
import {
    LoginRequest,
    RegisterRequest,
    RestoreAccountRequest,
    TelegramLoginWidgetRequest,
    PasswordResetRequest,
} from '../../types/auth.data';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox.component';
import { FdUiFormErrorComponent, FD_VALIDATION_ERRORS, FdValidationErrors } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { GoogleIdentityService } from '../../services/google-identity.service';
import { LocalizationService } from '../../services/localization.service';
import { environment } from '../../../environments/environment';
import { GoogleLoginRequest } from '../../types/google-auth.data';

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
    imports: [TranslateModule, ReactiveFormsModule, FdUiInputComponent, FdUiButtonComponent, FdUiCheckboxComponent, FdUiFormErrorComponent]
})
export class AuthComponent implements OnInit, AfterViewInit {
    @Input() public useRouting = true;
    @Input() public initialMode: 'login' | 'register' = 'login';
    @ViewChild('googleLoginButton') private googleLoginButton?: ElementRef<HTMLElement>;
    @ViewChild('googleRegisterButton') private googleRegisterButton?: ElementRef<HTMLElement>;
    @ViewChild('telegramLoginButton') private telegramLoginButton?: ElementRef<HTMLElement>;

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
    private readonly dialogRef = inject(MatDialogRef<AuthComponent>, { optional: true });

    public authMode: 'login' | 'register' = 'login';

    public loginForm: FormGroup<LoginFormGroup>;
    public registerForm: FormGroup<RegisterFormGroup>;
    public passwordResetForm: FormGroup<PasswordResetFormGroup>;
    public globalError = signal<string | null>(null);
    public googleReady = signal<boolean>(false);
    public telegramLoginEnabled = signal<boolean>(false);
    public showRestoreAction = signal<boolean>(false);
    public isRestoring = signal<boolean>(false);
    public showPasswordReset = signal<boolean>(false);
    public isPasswordResetting = signal<boolean>(false);
    public passwordResetSent = signal<boolean>(false);
    public passwordResetCooldownSeconds = signal<number>(0);
    private passwordResetCooldownTimerId: number | null = null;
    public authBenefits: string[] = [
        'AUTH.INFO.HIGHLIGHTS.SYNC',
        'AUTH.INFO.HIGHLIGHTS.INSIGHTS',
        'AUTH.INFO.HIGHLIGHTS.LIBRARY',
    ];

    private returnUrl: string | null = null;

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

        this.loginForm.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.clearGlobalError();
                this.markDirtyControlsTouched(this.loginForm);
                this.cdr.markForCheck();
            });
        this.registerForm.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.clearGlobalError();
                this.markDirtyControlsTouched(this.registerForm);
                this.cdr.markForCheck();
            });
        this.passwordResetForm.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.clearGlobalError();
                this.markDirtyControlsTouched(this.passwordResetForm);
                this.cdr.markForCheck();
            });

        this.registerForm.controls.password.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.registerForm.controls.confirmPassword.updateValueAndValidity();
        });
    }

    public ngOnInit(): void {
        const routeMode = this.route?.snapshot.params['mode'] === 'register' ? 'register' : 'login';
        this.authMode = this.useRouting ? routeMode : this.initialMode;
        this.returnUrl = this.useRouting ? this.route?.snapshot.queryParams['returnUrl'] || null : null;
    }

    public async ngAfterViewInit(): Promise<void> {
        await this.initializeGoogle();
        this.initializeTelegramLogin();
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
        if (this.useRouting && this.router) {
            await this.router.navigate(['/auth', mode]);
        }
        this.renderGoogleButton();
        this.renderTelegramWidget();
    }

    public async onLoginSubmit(): Promise<void> {
        if (!this.loginForm.valid) {
            return;
        }

        const loginRequest = new LoginRequest(this.loginForm.value);

        this.authService.login(loginRequest).subscribe({
            next: () => {
                this.navigationService.navigateToReturnUrl(this.returnUrl);
                this.closeDialogIfAny();
            },
            error: (error: HttpErrorResponse) => {
                this.handleLoginError(error.error?.error);
            },
        });
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
                this.navigationService.navigateToReturnUrl(this.returnUrl);
                this.closeDialogIfAny();
            },
            error: () => {
                this.isRestoring.set(false);
                this.setGlobalError('FORM_ERRORS.UNKNOWN');
            },
        });
    }

    public async onRegisterSubmit(): Promise<void> {
        if (!this.registerForm.valid) {
            return;
        }

        const registerRequest = new RegisterRequest({
            ...this.registerForm.value,
            language: this.localizationService.getCurrentLanguage(),
        });

        this.authService.register(registerRequest).subscribe({
            next: () => {
                this.navigationService.navigateToEmailVerificationPending();
                this.closeDialogIfAny();
            },
            error: (error: HttpErrorResponse) => {
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
                callback: credential => this.onGoogleCredential(credential),
            });
            this.googleReady.set(true);
            this.renderGoogleButton();
            this.googleIdentityService.prompt();
        } catch (error) {
            console.error('Google init failed', error);
        }
    }

    private initializeTelegramLogin(): void {
        if (!environment.telegramBotUsername) {
            return;
        }
        this.telegramLoginEnabled.set(true);
        (window as { fdTelegramAuth?: (user: TelegramLoginWidgetUser) => void }).fdTelegramAuth =
            user => this.onTelegramAuth(user);
        this.renderTelegramWidget();
    }

    private renderGoogleButton(): void {
        if (!this.googleReady()) {
            return;
        }
        const target = this.authMode === 'login' ? this.googleLoginButton?.nativeElement : this.googleRegisterButton?.nativeElement;
        [this.googleLoginButton, this.googleRegisterButton].forEach(ref => {
            if (ref?.nativeElement) {
                ref.nativeElement.innerHTML = '';
            }
        });
        if (target) {
            this.googleIdentityService.renderButton(target, 'filled_blue');
        }
    }

    private renderTelegramWidget(): void {
        if (!this.telegramLoginEnabled() || this.authMode !== 'login') {
            return;
        }

        const target = this.telegramLoginButton?.nativeElement;
        if (!target || !environment.telegramBotUsername) {
            return;
        }

        target.innerHTML = '';

        const script = document.createElement('script');
        script.async = true;
        script.src = 'https://telegram.org/js/telegram-widget.js?22';
        script.setAttribute('data-telegram-login', environment.telegramBotUsername);
        script.setAttribute('data-size', 'large');
        script.setAttribute('data-userpic', 'false');
        script.setAttribute('data-request-access', 'write');
        script.setAttribute('data-onauth', 'fdTelegramAuth(user)');
        target.appendChild(script);
    }

    private onGoogleCredential(credential: string): void {
        const rememberMe = this.authMode === 'login' ? this.loginForm.controls.rememberMe.value : false;
        const request: GoogleLoginRequest = { credential, rememberMe: !!rememberMe };
        this.authService.loginWithGoogle(request).subscribe({
            next: () => {
                this.navigationService.navigateToReturnUrl(this.returnUrl);
                this.closeDialogIfAny();
            },
            error: () => this.setGlobalError('FORM_ERRORS.UNKNOWN'),
        });
    }

    private onTelegramAuth(user: TelegramLoginWidgetUser): void {
        const rememberMe = this.loginForm.controls.rememberMe.value;
        const request: TelegramLoginWidgetRequest = {
            id: user.id,
            authDate: user.auth_date,
            hash: user.hash,
            username: user.username,
            firstName: user.first_name,
            lastName: user.last_name,
            photoUrl: user.photo_url,
        };

        this.authService.loginWithTelegramWidget(request, rememberMe).subscribe({
            next: () => {
                this.navigationService.navigateToReturnUrl(this.returnUrl);
                this.closeDialogIfAny();
            },
            error: (error: HttpErrorResponse) => {
                const errorCode = error.error?.error;
                if (errorCode === 'Authentication.TelegramNotLinked') {
                    this.setGlobalErrorMessage('Telegram account is not linked. Open the WebApp from the bot once.');
                } else if (errorCode === 'Authentication.TelegramAuthExpired') {
                    this.setGlobalErrorMessage('Telegram auth expired. Please try again.');
                } else {
                    this.setGlobalError('FORM_ERRORS.UNKNOWN');
                }
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

    private setGlobalErrorMessage(message: string): void {
        this.globalError.set(message);
        this.showRestoreAction.set(false);
    }

    private clearGlobalError(): void {
        this.globalError.set(null);
        this.showRestoreAction.set(false);
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

interface TelegramLoginWidgetUser {
    id: number;
    auth_date: number;
    hash: string;
    username?: string;
    first_name?: string;
    last_name?: string;
    photo_url?: string;
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


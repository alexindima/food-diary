import { ChangeDetectionStrategy, Component, DestroyRef, FactoryProvider, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { matchFieldValidator } from '../../validators/match-field.validator';
import { NavigationService } from '../../services/navigation.service';
import { FormGroupControls } from '../../types/common.data';
import { LoginRequest, RegisterRequest } from '../../types/auth.data';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FdUiTabsComponent, FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiFormErrorComponent, FD_VALIDATION_ERRORS, FdValidationErrors } from 'fd-ui-kit/form-error/fd-ui-form-error.component';

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
        FdUiTabsComponent,
        FdUiInputComponent,
        FdUiButtonComponent,
        FdUiCheckboxComponent,
        FdUiCardComponent,
        FdUiFormErrorComponent,
    ]
})
export class AuthComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly navigationService = inject(NavigationService);
    private readonly authService = inject(AuthService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);

    public authTabs: FdUiTab[] = [
        { value: 'login', labelKey: 'AUTH.LOGIN.TITLE' },
        { value: 'register', labelKey: 'AUTH.REGISTER.TITLE' },
    ];
    public authMode: 'login' | 'register' = 'login';

    public loginForm: FormGroup<LoginFormGroup>;
    public registerForm: FormGroup<RegisterFormGroup>;
    public globalError = signal<string | null>(null);
    public authBenefits: string[] = [
        'AUTH.INFO.HIGHLIGHTS.SYNC',
        'AUTH.INFO.HIGHLIGHTS.INSIGHTS',
        'AUTH.INFO.HIGHLIGHTS.LIBRARY',
    ];

    private readonly returnUrl: string | null = null;

    public constructor() {
        this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || null;

        const mode = this.route.snapshot.params['mode'] === 'register' ? 'register' : 'login';
        this.authMode = mode;

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

        this.loginForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.clearGlobalError());
        this.registerForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.clearGlobalError());

        this.registerForm.controls.password.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.registerForm.controls.confirmPassword.updateValueAndValidity();
        });
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
        await this.router.navigate(['/auth', mode]);
    }

    public async onLoginSubmit(): Promise<void> {
        if (!this.loginForm.valid) {
            return;
        }

        const loginRequest = new LoginRequest(this.loginForm.value);

        this.authService.login(loginRequest).subscribe({
            next: () => {
                this.navigationService.navigateToReturnUrl(this.returnUrl);
            },
            error: (error: HttpErrorResponse) => {
                this.handleLoginError(error.error?.error);
            },
        });
    }

    public async onRegisterSubmit(): Promise<void> {
        if (!this.registerForm.valid) {
            return;
        }

        const registerRequest = new RegisterRequest(this.registerForm.value);

        this.authService.register(registerRequest).subscribe({
            next: () => {
                this.navigationService.navigateToReturnUrl(this.returnUrl);
            },
            error: (error: HttpErrorResponse) => {
                this.handleRegisterError(error.error?.error);
            },
        });
    }

    private handleLoginError(errorCode?: string): void {
        if (errorCode === 'User.InvalidCredentials' || errorCode === 'Authentication.InvalidCredentials') {
            this.setGlobalError('FORM_ERRORS.INVALID_CREDENTIALS');
        } else {
            this.setGlobalError('FORM_ERRORS.UNKNOWN');
        }
    }

    private handleRegisterError(errorCode?: string): void {
        if (errorCode === 'User.EmailAlreadyExists') {
            const emailField = this.registerForm.controls.email;
            emailField?.updateValueAndValidity();
            emailField?.setErrors({ userExists: true });
        } else {
            this.setGlobalError('FORM_ERRORS.UNKNOWN');
        }
    }

    private setGlobalError(errorKey: string): void {
        this.globalError.set(this.translateService.instant(errorKey));
    }

    private clearGlobalError(): void {
        this.globalError.set(null);
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

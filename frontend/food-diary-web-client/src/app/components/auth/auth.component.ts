import { ChangeDetectionStrategy, Component, DestroyRef, FactoryProvider, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ErrorCode } from '../../types/api-response.data';
import { TUI_VALIDATION_ERRORS, TuiCheckbox, TuiFieldErrorPipe, TuiTab, TuiTabsHorizontal } from '@taiga-ui/kit';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { TuiButton, TuiError, TuiLabel, TuiTextfieldComponent, TuiTextfieldDirective } from '@taiga-ui/core';
import { AsyncPipe } from '@angular/common';
import { matchFieldValidator } from '../../validators/match-field.validator';
import { NavigationService } from '../../services/navigation.service';
import { FormGroupControls } from '../../types/common.data';
import { LoginRequest, RegisterRequest } from '../../types/auth.data';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

export const VALIDATION_ERRORS_PROVIDER: FactoryProvider = {
    provide: TUI_VALIDATION_ERRORS,
    useFactory: (translate: TranslateService): ValidationErrors => ({
        required: () => translate.instant('FORM_ERRORS.REQUIRED'),
        userExists: () => translate.instant('FORM_ERRORS.USER_EXISTS'),
        email: () => translate.instant('FORM_ERRORS.EMAIL'),
        matchField: () => translate.instant('FORM_ERRORS.PASSWORD.MATCH'),
        minlength: ({ requiredLength }) =>
            translate.instant('FORM_ERRORS.PASSWORD.MIN_LENGTH', {
                requiredLength,
            }),
    }),
    deps: [TranslateService],
};

@Component({
    selector: 'fd-auth',
    templateUrl: './auth.component.html',
    styleUrls: ['./auth.component.less'],
    providers: [VALIDATION_ERRORS_PROVIDER],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        TuiTabsHorizontal,
        TranslateModule,
        ReactiveFormsModule,
        TuiTextfieldComponent,
        TuiTab,
        TuiLabel,
        TuiTextfieldDirective,
        TuiCheckbox,
        TuiButton,
        TuiError,
        TuiFieldErrorPipe,
        AsyncPipe,
    ]
})
export class AuthComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly navigationService = inject(NavigationService);
    private readonly authService = inject(AuthService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);

    public activeItemIndex = 0;

    public loginForm: FormGroup<LoginFormGroup>;
    public registerForm: FormGroup<RegisterFormGroup>;
    public globalError = signal<string | null>(null);

    private readonly returnUrl: string | null = null;

    public constructor() {
        this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || null;

        const mode = this.route.snapshot.params['mode'];
        this.activeItemIndex = mode === 'register' ? 1 : 0;

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

    public async onTabChange(index: number): Promise<void> {
        this.loginForm.reset();
        this.registerForm.reset();
        const mode = index === 0 ? 'login' : 'register';
        await this.router.navigate(['/auth', mode]);
    }

    public async onLoginSubmit(): Promise<void> {
        if (!this.loginForm.valid) {
            return;
        }

        const loginRequest = new LoginRequest(this.loginForm.value);

        this.authService.login(loginRequest).subscribe({
            next: response => {
                if (response.status === 'success') {
                    this.navigationService.navigateToReturnUrl(this.returnUrl);
                } else if (response.status === 'error') {
                    this.handleLoginError(response.error);
                }
            },
        });
    }

    public async onRegisterSubmit(): Promise<void> {
        if (!this.registerForm.valid) {
            return;
        }

        const registerRequest = new RegisterRequest(this.registerForm.value);

        this.authService.register(registerRequest).subscribe({
            next: response => {
                if (response.status === 'success' && response.data) {
                    this.navigationService.navigateToReturnUrl(this.returnUrl);
                } else if (response.status === 'error') {
                    this.handleRegisterError(response.error);
                }
            },
        });
    }

    private handleLoginError(error?: ErrorCode): void {
        if (error === ErrorCode.INVALID_CREDENTIALS) {
            this.setGlobalError('FORM_ERRORS.INVALID_CREDENTIALS');
        } else {
            this.setGlobalError('FORM_ERRORS.UNKNOWN');
        }
    }

    private handleRegisterError(error?: ErrorCode): void {
        if (error === ErrorCode.USER_EXISTS) {
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

interface ValidationErrors {
    required: () => string;
    userExists: () => string;
    email: () => string;
    matchField: () => string;
    minlength: (_params: { requiredLength: string }) => string;
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

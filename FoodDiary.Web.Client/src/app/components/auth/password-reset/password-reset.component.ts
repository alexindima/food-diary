import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FD_VALIDATION_ERRORS, FdUiFormErrorComponent, FdValidationErrors } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { matchFieldValidator } from '../../../validators/match-field.validator';
import { AuthService } from '../../../services/auth.service';
import { NavigationService } from '../../../services/navigation.service';
import { ConfirmPasswordResetRequest } from '../../../types/auth.data';
import { FormGroupControls } from '../../../types/common.data';

type ResetState = 'ready' | 'invalid' | 'error';

@Component({
    selector: 'fd-password-reset',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        TranslateModule,
        FdUiCardComponent,
        FdUiInputComponent,
        FdUiButtonComponent,
        FdUiFormErrorComponent,
    ],
    templateUrl: './password-reset.component.html',
    styleUrl: './password-reset.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PasswordResetComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly authService = inject(AuthService);
    private readonly navigationService = inject(NavigationService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });

    public readonly state = signal<ResetState>('ready');
    public readonly isSubmitting = signal(false);
    public readonly errorMessage = signal<string | null>(null);
    public readonly token = signal<{ userId: string | null; token: string | null }>({ userId: null, token: null });

    public readonly form = new FormGroup<PasswordResetFormGroup>({
        password: new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.minLength(6)] }),
        confirmPassword: new FormControl<string>('', {
            nonNullable: true,
            validators: [Validators.required, matchFieldValidator('password')],
        }),
    });

    public constructor() {
        this.resolveToken();
        this.form.controls.password.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => this.form.controls.confirmPassword.updateValueAndValidity());
    }

    public onSubmit(): void {
        if (this.state() !== 'ready' || this.form.invalid || this.isSubmitting()) {
            return;
        }

        const { userId, token } = this.token();
        if (!userId || !token) {
            this.state.set('invalid');
            this.errorMessage.set(this.translateService.instant('AUTH.RESET.INVALID'));
            return;
        }

        this.isSubmitting.set(true);
        this.errorMessage.set(null);

        const request = new ConfirmPasswordResetRequest({
            userId,
            token,
            newPassword: this.form.controls.password.value,
        });

        this.authService
            .confirmPasswordReset(request)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.isSubmitting.set(false);
                    void this.navigationService.navigateToHome();
                },
                error: () => {
                    this.isSubmitting.set(false);
                    this.state.set('error');
                    this.errorMessage.set(this.translateService.instant('AUTH.RESET.ERROR_GENERIC'));
                },
            });
    }

    public onBackToLogin(): void {
        void this.navigationService.navigateToAuth('login');
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

    private resolveToken(): void {
        const params = this.route.snapshot.queryParamMap;
        const userId = params.get('userId') ?? params.get('user') ?? params.get('id');
        const token = params.get('token');
        this.token.set({ userId, token });

        if (!userId || !token) {
            this.state.set('invalid');
            this.errorMessage.set(this.translateService.instant('AUTH.RESET.INVALID'));
        }
    }
}

interface PasswordResetFormValues {
    password: string;
    confirmPassword: string;
}

type PasswordResetFormGroup = FormGroupControls<PasswordResetFormValues>;

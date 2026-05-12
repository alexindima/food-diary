import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type AbstractControl, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FD_VALIDATION_ERRORS, FdUiFormErrorComponent, type FdValidationErrors } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { EMPTY, merge, type Observable } from 'rxjs';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import type { FormGroupControls } from '../../../../shared/lib/common.data';
import { matchFieldValidator } from '../../../../validators/match-field.validator';
import { AUTH_PASSWORD_MIN_LENGTH } from '../../lib/auth.constants';
import { ConfirmPasswordResetRequest } from '../../models/auth.data';

type ResetState = 'ready' | 'invalid' | 'error';
const ERROR_FIELDS = ['password', 'confirmPassword'] as const;
type ErrorField = (typeof ERROR_FIELDS)[number];
type FieldErrors = Record<ErrorField, string | null>;

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
    public readonly fieldErrors = signal<FieldErrors>(this.createEmptyFieldErrors());

    public readonly form = new FormGroup<PasswordResetFormGroup>({
        password: new FormControl<string>('', {
            nonNullable: true,
            validators: [Validators.required, Validators.minLength(AUTH_PASSWORD_MIN_LENGTH)],
        }),
        confirmPassword: new FormControl<string>('', {
            nonNullable: true,
            validators: [Validators.required, matchFieldValidator('password')],
        }),
    });

    public constructor() {
        this.resolveToken();
        this.form.controls.password.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.form.controls.confirmPassword.updateValueAndValidity();
        });
        const formEvents = (this.form as { events?: Observable<unknown> }).events ?? EMPTY;
        merge(formEvents, this.form.statusChanges, this.form.valueChanges, this.translateService.onLangChange)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.updateFieldErrors();
            });
        this.updateFieldErrors();
    }

    public onSubmit(): void {
        if (this.state() !== 'ready' || this.form.invalid || this.isSubmitting()) {
            return;
        }

        const { userId, token } = this.token();
        if (userId === null || userId.length === 0 || token === null || token.length === 0) {
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
                    void this.navigationService.navigateToHomeAsync();
                },
                error: () => {
                    this.isSubmitting.set(false);
                    this.state.set('error');
                    this.errorMessage.set(this.translateService.instant('AUTH.RESET.ERROR_GENERIC'));
                },
            });
    }

    public onBackToLogin(): void {
        void this.navigationService.navigateToAuthAsync('login');
    }

    private updateFieldErrors(): void {
        this.fieldErrors.set(
            ERROR_FIELDS.reduce<FieldErrors>((errors, field) => {
                errors[field] = this.resolveControlError(this.form.controls[field]);
                return errors;
            }, this.createEmptyFieldErrors()),
        );
    }

    private createEmptyFieldErrors(): FieldErrors {
        return {
            password: null,
            confirmPassword: null,
        };
    }

    private resolveControlError(control: AbstractControl | null): string | null {
        if (control?.invalid !== true) {
            return null;
        }

        const shouldShow = control.touched === true || control.dirty === true;
        if (!shouldShow) {
            return null;
        }
        const errors = control.errors;
        if (errors === null) {
            return null;
        }

        for (const key of Object.keys(errors)) {
            const message = this.resolveValidationErrorMessage(key, errors[key]);
            if (message !== null) {
                return message;
            }
        }

        return this.translateService.instant('FORM_ERRORS.UNKNOWN');
    }

    private resolveValidationErrorMessage(key: string, controlError: unknown): string | null {
        const resolver = this.validationErrors?.[key];
        if (resolver === undefined) {
            return null;
        }

        const controlParams = this.getValidationParams(controlError);
        const result = resolver(controlError);

        if (typeof result === 'string') {
            return this.translateService.instant(result, controlParams);
        }

        return this.translateService.instant(result.key, {
            ...controlParams,
            ...(result.params ?? {}),
        });
    }

    private resolveToken(): void {
        const params = this.route.snapshot.queryParamMap;
        const userId = params.get('userId') ?? params.get('user') ?? params.get('id');
        const token = params.get('token');
        this.token.set({ userId, token });

        if (userId === null || userId.length === 0 || token === null || token.length === 0) {
            this.state.set('invalid');
            this.errorMessage.set(this.translateService.instant('AUTH.RESET.INVALID'));
        }
    }

    private getValidationParams(error: unknown): Record<string, unknown> {
        return this.isRecord(error) ? error : {};
    }

    private isRecord(value: unknown): value is Record<string, unknown> {
        return typeof value === 'object' && value !== null && !Array.isArray(value);
    }
}

type PasswordResetFormValues = {
    password: string;
    confirmPassword: string;
};

type PasswordResetFormGroup = FormGroupControls<PasswordResetFormValues>;

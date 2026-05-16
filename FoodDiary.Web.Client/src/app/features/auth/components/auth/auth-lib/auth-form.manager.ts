import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { FormGroup } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { FD_VALIDATION_ERRORS, type FdValidationErrors } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { EMPTY, merge, type Observable } from 'rxjs';

import { resolveTranslatedControlError } from '../../../../../shared/lib/validation-error.utils';
import type { LoginFieldErrors, PasswordResetFieldErrors, RegisterFieldErrors } from './auth.types';
import { LOGIN_ERROR_FIELDS, PASSWORD_RESET_ERROR_FIELDS, REGISTER_ERROR_FIELDS } from './auth-form.config';
import {
    createEmptyLoginFieldErrors,
    createEmptyPasswordResetFieldErrors,
    createEmptyRegisterFieldErrors,
    createLoginForm,
    createPasswordResetForm,
    createRegisterForm,
} from './auth-form.factory';

@Injectable({ providedIn: 'root' })
export class AuthFormManager {
    private readonly translateService = inject(TranslateService);
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });
    private readonly destroyRef = inject(DestroyRef);

    public readonly loginForm = createLoginForm();
    public readonly registerForm = createRegisterForm();
    public readonly passwordResetForm = createPasswordResetForm();
    public readonly loginFieldErrors = signal<LoginFieldErrors>(createEmptyLoginFieldErrors());
    public readonly registerFieldErrors = signal<RegisterFieldErrors>(createEmptyRegisterFieldErrors());
    public readonly passwordResetFieldErrors = signal<PasswordResetFieldErrors>(createEmptyPasswordResetFieldErrors());

    public constructor() {
        this.registerForm.controls.password.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.registerForm.controls.confirmPassword.updateValueAndValidity();
        });
        this.subscribeValidationUpdates();
        this.updateFieldErrors();
    }

    public resetAll(): void {
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
    }

    public updateFieldErrors(): void {
        this.loginFieldErrors.set(
            LOGIN_ERROR_FIELDS.reduce<LoginFieldErrors>((errors, field) => {
                errors[field] = resolveTranslatedControlError(this.loginForm.controls[field], this.validationErrors, this.translateService);
                return errors;
            }, createEmptyLoginFieldErrors()),
        );
        this.registerFieldErrors.set(
            REGISTER_ERROR_FIELDS.reduce<RegisterFieldErrors>((errors, field) => {
                errors[field] = resolveTranslatedControlError(
                    this.registerForm.controls[field],
                    this.validationErrors,
                    this.translateService,
                );
                return errors;
            }, createEmptyRegisterFieldErrors()),
        );
        this.passwordResetFieldErrors.set(
            PASSWORD_RESET_ERROR_FIELDS.reduce<PasswordResetFieldErrors>((errors, field) => {
                errors[field] = resolveTranslatedControlError(
                    this.passwordResetForm.controls[field],
                    this.validationErrors,
                    this.translateService,
                );
                return errors;
            }, createEmptyPasswordResetFieldErrors()),
        );
    }

    public markDirtyControlsTouched(form: FormGroup): void {
        Object.values(form.controls).forEach(control => {
            if (control.dirty && !control.touched) {
                control.markAsTouched();
            }
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
}

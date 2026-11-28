import { computed, inject, linkedSignal, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { email, type FieldTree, form, maxLength, minLength, required, validate } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import {
    FD_VALIDATION_ERRORS,
    type FdSignalFormField,
    type FdValidationErrors,
    resolveSignalFormFieldError,
} from 'fd-ui-kit/form-error/fd-ui-form-error';

import { AUTH_PASSWORD_MAX_LENGTH, AUTH_PASSWORD_MIN_LENGTH } from '../../../../../shared/auth/auth.constants';
import type {
    LoginFieldErrors,
    LoginFormValues,
    PasswordResetFieldErrors,
    PasswordResetFormValues,
    RegisterFieldErrors,
    RegisterFormValues,
} from './auth.types';
import { LOGIN_ERROR_FIELDS, PASSWORD_RESET_ERROR_FIELDS, REGISTER_ERROR_FIELDS } from './auth-form.config';
import {
    createEmptyLoginFieldErrors,
    createEmptyPasswordResetFieldErrors,
    createEmptyRegisterFieldErrors,
    createLoginFormModel,
    createPasswordResetFormModel,
    createRegisterFormModel,
} from './auth-form.factory';

type AuthFormSubmissionActions = {
    loginAsync: () => Promise<void>;
    requestPasswordResetAsync: () => Promise<void>;
    registerAsync: () => Promise<void>;
};

export class AuthFormManager {
    private readonly translateService = inject(TranslateService);
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });
    private readonly fieldErrorsVersion = signal(0);
    private readonly languageChange = toSignal(this.translateService.onLangChange);

    public readonly loginModel = signal(createLoginFormModel());
    public readonly registerModel = signal(createRegisterFormModel());
    public readonly passwordResetModel = signal(createPasswordResetFormModel());
    private readonly registerEmail = computed(() => this.registerModel().email);
    private readonly registerEmailExists = linkedSignal({ source: this.registerEmail, computation: (): boolean => false });
    public readonly loginForm: FieldTree<LoginFormValues>;
    public readonly registerForm: FieldTree<RegisterFormValues>;
    public readonly passwordResetForm: FieldTree<PasswordResetFormValues>;
    public readonly loginFieldErrors = computed<LoginFieldErrors>(() => {
        this.fieldErrorsVersion();
        this.languageChange();
        this.loginModel();

        return LOGIN_ERROR_FIELDS.reduce<LoginFieldErrors>((errors, field) => {
            errors[field] = this.resolveFieldError(this.loginForm[field], { showOnDirty: false });
            return errors;
        }, createEmptyLoginFieldErrors());
    });
    public readonly registerFieldErrors = computed<RegisterFieldErrors>(() => {
        this.fieldErrorsVersion();
        this.languageChange();
        this.registerModel();

        return REGISTER_ERROR_FIELDS.reduce<RegisterFieldErrors>((errors, field) => {
            errors[field] = this.resolveFieldError(this.registerForm[field], { showOnDirty: false });
            return errors;
        }, createEmptyRegisterFieldErrors());
    });
    public readonly passwordResetFieldErrors = computed<PasswordResetFieldErrors>(() => {
        this.fieldErrorsVersion();
        this.languageChange();
        this.passwordResetModel();

        return PASSWORD_RESET_ERROR_FIELDS.reduce<PasswordResetFieldErrors>((errors, field) => {
            errors[field] = this.resolveFieldError(this.passwordResetForm[field], { showOnDirty: false });
            return errors;
        }, createEmptyPasswordResetFieldErrors());
    });

    public constructor(actions: AuthFormSubmissionActions) {
        this.loginForm = form(
            this.loginModel,
            path => {
                required(path.email);
                email(path.email);
                required(path.password);
                minLength(path.password, AUTH_PASSWORD_MIN_LENGTH);
                maxLength(path.password, AUTH_PASSWORD_MAX_LENGTH);
            },
            {
                submission: {
                    action: actions.loginAsync,
                    ignoreValidators: 'all',
                },
            },
        );
        this.registerForm = form(
            this.registerModel,
            path => {
                required(path.email);
                email(path.email);
                validate(path.email, () => (this.registerEmailExists() ? { kind: 'userExists' } : undefined));
                required(path.password);
                minLength(path.password, AUTH_PASSWORD_MIN_LENGTH);
                maxLength(path.password, AUTH_PASSWORD_MAX_LENGTH);
                validate(path.agreeTerms, ({ value }) => (value() ? undefined : { kind: 'required' }));
            },
            {
                submission: {
                    action: actions.registerAsync,
                    onInvalid: () => {
                        this.registerForm().markAsTouched();
                        this.updateFieldErrors();
                    },
                },
            },
        );
        this.passwordResetForm = form(
            this.passwordResetModel,
            path => {
                required(path.email);
                email(path.email);
            },
            {
                submission: {
                    action: actions.requestPasswordResetAsync,
                    onInvalid: () => {
                        this.updateFieldErrors();
                    },
                },
            },
        );
    }

    public resetAll(): void {
        this.loginForm().reset(createLoginFormModel());
        this.registerForm().reset(createRegisterFormModel());
        this.passwordResetForm().reset(createPasswordResetFormModel());
        this.registerEmailExists.set(false);
    }

    public setRegisterEmailExistsError(submittedEmail: string): void {
        if (submittedEmail !== this.registerEmail()) {
            return;
        }

        this.registerEmailExists.set(true);
        this.registerForm.email().markAsTouched();
        this.updateFieldErrors();
    }

    public updateFieldErrors(): void {
        this.fieldErrorsVersion.update(version => version + 1);
    }

    private resolveFieldError(field: FdSignalFormField, options: { showOnDirty?: boolean } = {}): string | null {
        return resolveSignalFormFieldError(field, this.validationErrors, this.translateService, options);
    }
}

import { computed, DestroyRef, effect, inject, Service, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { email, form, minLength, required, validate } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import {
    FD_VALIDATION_ERRORS,
    type FdSignalFormField,
    type FdValidationErrors,
    resolveSignalFormFieldError,
} from 'fd-ui-kit/form-error/fd-ui-form-error';

import { AUTH_PASSWORD_MIN_LENGTH } from '../../../lib/auth.constants';
import type { LoginFieldErrors, PasswordResetFieldErrors, RegisterFieldErrors } from './auth.types';
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
    login: () => Promise<void>;
    passwordReset: () => Promise<void>;
    register: () => Promise<void>;
};

@Service()
export class AuthFormManager {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });
    private readonly registerEmailExists = signal(false);
    private readonly fieldErrorsVersion = signal(0);
    private readonly languageVersion = signal(0);
    private loginSubmissionActionAsync: () => Promise<void> = async () => {};
    private passwordResetSubmissionActionAsync: () => Promise<void> = async () => {};
    private registerSubmissionActionAsync: () => Promise<void> = async () => {};
    private readonly submitLoginFormAsync = async (): Promise<void> => {
        await this.loginSubmissionActionAsync();
    };
    private readonly submitPasswordResetFormAsync = async (): Promise<void> => {
        await this.passwordResetSubmissionActionAsync();
    };
    private readonly submitRegisterFormAsync = async (): Promise<void> => {
        await this.registerSubmissionActionAsync();
    };

    public readonly loginModel = signal(createLoginFormModel());
    public readonly registerModel = signal(createRegisterFormModel());
    public readonly passwordResetModel = signal(createPasswordResetFormModel());
    public readonly loginForm = form(
        this.loginModel,
        path => {
            required(path.email);
            email(path.email);
            required(path.password);
            minLength(path.password, AUTH_PASSWORD_MIN_LENGTH);
        },
        {
            submission: {
                action: this.submitLoginFormAsync,
                ignoreValidators: 'all',
            },
        },
    );
    public readonly registerForm = form(
        this.registerModel,
        path => {
            required(path.email);
            email(path.email);
            validate(path.email, () => (this.registerEmailExists() ? { kind: 'userExists' } : undefined));
            required(path.password);
            minLength(path.password, AUTH_PASSWORD_MIN_LENGTH);
            required(path.confirmPassword);
            validate(path.confirmPassword, ({ value }) => (value() === this.registerModel().password ? undefined : { kind: 'matchField' }));
            validate(path.agreeTerms, ({ value }) => (value() ? undefined : { kind: 'required' }));
        },
        {
            submission: {
                action: this.submitRegisterFormAsync,
                onInvalid: () => {
                    this.registerForm().markAsTouched();
                    this.updateFieldErrors();
                },
            },
        },
    );
    public readonly passwordResetForm = form(
        this.passwordResetModel,
        path => {
            required(path.email);
            email(path.email);
        },
        {
            submission: {
                action: this.submitPasswordResetFormAsync,
                onInvalid: () => {
                    this.updateFieldErrors();
                },
            },
        },
    );
    public readonly loginFieldErrors = computed<LoginFieldErrors>(() => {
        this.fieldErrorsVersion();
        this.languageVersion();
        this.loginModel();

        return LOGIN_ERROR_FIELDS.reduce<LoginFieldErrors>((errors, field) => {
            errors[field] = this.resolveFieldError(this.loginForm[field], { showOnDirty: false });
            return errors;
        }, createEmptyLoginFieldErrors());
    });
    public readonly registerFieldErrors = computed<RegisterFieldErrors>(() => {
        this.fieldErrorsVersion();
        this.languageVersion();
        this.registerModel();

        return REGISTER_ERROR_FIELDS.reduce<RegisterFieldErrors>((errors, field) => {
            errors[field] = this.resolveFieldError(this.registerForm[field], { showOnDirty: false });
            return errors;
        }, createEmptyRegisterFieldErrors());
    });
    public readonly passwordResetFieldErrors = computed<PasswordResetFieldErrors>(() => {
        this.fieldErrorsVersion();
        this.languageVersion();
        this.passwordResetModel();

        return PASSWORD_RESET_ERROR_FIELDS.reduce<PasswordResetFieldErrors>((errors, field) => {
            errors[field] = this.resolveFieldError(this.passwordResetForm[field], { showOnDirty: false });
            return errors;
        }, createEmptyPasswordResetFieldErrors());
    });

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });

        effect(() => {
            this.registerModel().email;
            this.registerEmailExists.set(false);
        });
    }

    public resetAll(): void {
        this.loginForm().reset(createLoginFormModel());
        this.registerForm().reset(createRegisterFormModel());
        this.passwordResetForm().reset(createPasswordResetFormModel());
        this.registerEmailExists.set(false);
    }

    public configureSubmissionActions(actions: AuthFormSubmissionActions): void {
        this.loginSubmissionActionAsync = actions.login;
        this.passwordResetSubmissionActionAsync = actions.passwordReset;
        this.registerSubmissionActionAsync = actions.register;
    }

    public setRegisterEmailExistsError(): void {
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

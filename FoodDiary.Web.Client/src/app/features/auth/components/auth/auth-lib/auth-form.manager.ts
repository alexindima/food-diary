import { effect, inject, Service, signal } from '@angular/core';
import { email, type FieldTree, form, minLength, required, validate, type ValidationError } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import {
    FD_VALIDATION_ERRORS,
    type FdValidationErrorConfig,
    type FdValidationErrors,
    getNumberProperty,
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

@Service()
export class AuthFormManager {
    private readonly translateService = inject(TranslateService);
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });
    private readonly registerEmailExists = signal(false);

    public readonly loginModel = signal(createLoginFormModel());
    public readonly registerModel = signal(createRegisterFormModel());
    public readonly passwordResetModel = signal(createPasswordResetFormModel());
    public readonly loginForm = form(this.loginModel, path => {
        required(path.email);
        email(path.email);
        required(path.password);
        minLength(path.password, AUTH_PASSWORD_MIN_LENGTH);
    });
    public readonly registerForm = form(this.registerModel, path => {
        required(path.email);
        email(path.email);
        validate(path.email, () => (this.registerEmailExists() ? { kind: 'userExists' } : undefined));
        required(path.password);
        minLength(path.password, AUTH_PASSWORD_MIN_LENGTH);
        required(path.confirmPassword);
        validate(path.confirmPassword, ({ value }) => (value() === this.registerModel().password ? undefined : { kind: 'matchField' }));
        validate(path.agreeTerms, ({ value }) => (value() ? undefined : { kind: 'required' }));
    });
    public readonly passwordResetForm = form(this.passwordResetModel, path => {
        required(path.email);
        email(path.email);
    });
    public readonly loginFieldErrors = signal<LoginFieldErrors>(createEmptyLoginFieldErrors());
    public readonly registerFieldErrors = signal<RegisterFieldErrors>(createEmptyRegisterFieldErrors());
    public readonly passwordResetFieldErrors = signal<PasswordResetFieldErrors>(createEmptyPasswordResetFieldErrors());

    public constructor() {
        effect(() => {
            this.registerModel().email;
            this.registerEmailExists.set(false);
        });
        effect(() => {
            this.loginModel();
            this.registerModel();
            this.passwordResetModel();
            this.translateService.onLangChange;
            this.updateFieldErrors();
        });
    }

    public resetAll(): void {
        this.loginForm().reset(createLoginFormModel());
        this.registerForm().reset(createRegisterFormModel());
        this.passwordResetForm().reset(createPasswordResetFormModel());
        this.registerEmailExists.set(false);
    }

    public setRegisterEmailExistsError(): void {
        this.registerEmailExists.set(true);
        this.registerForm.email().markAsTouched();
        this.updateFieldErrors();
    }

    public updateFieldErrors(): void {
        this.loginFieldErrors.set(
            LOGIN_ERROR_FIELDS.reduce<LoginFieldErrors>((errors, field) => {
                errors[field] = this.resolveTranslatedFieldError(this.loginForm[field], { showOnDirty: false });
                return errors;
            }, createEmptyLoginFieldErrors()),
        );
        this.registerFieldErrors.set(
            REGISTER_ERROR_FIELDS.reduce<RegisterFieldErrors>((errors, field) => {
                errors[field] = this.resolveTranslatedFieldError(this.registerForm[field], { showOnDirty: false });
                return errors;
            }, createEmptyRegisterFieldErrors()),
        );
        this.passwordResetFieldErrors.set(
            PASSWORD_RESET_ERROR_FIELDS.reduce<PasswordResetFieldErrors>((errors, field) => {
                errors[field] = this.resolveTranslatedFieldError(this.passwordResetForm[field], { showOnDirty: false });
                return errors;
            }, createEmptyPasswordResetFieldErrors()),
        );
    }

    private resolveTranslatedFieldError(field: FieldTree<unknown>, options: { showOnDirty?: boolean } = {}): string | null {
        const state = field();
        if (!state.invalid()) {
            return null;
        }

        if (!state.touched() && !((options.showOnDirty ?? true) && state.dirty())) {
            return null;
        }

        const error = state.errors()[0];
        const key = this.mapValidationErrorKey(error);
        const resolver = this.validationErrors?.[key];
        if (resolver === undefined) {
            return this.translateService.instant('FORM_ERRORS.UNKNOWN');
        }

        const params = this.getValidationParams(error);
        const result = resolver(params);
        return this.translateValidationResult(result, params);
    }

    private mapValidationErrorKey(error: ValidationError): string {
        return error.kind === 'minLength' ? 'minlength' : error.kind;
    }

    private getValidationParams(error: ValidationError): Record<string, unknown> {
        if (error.kind === 'minLength') {
            return { requiredLength: getNumberProperty(error, 'minLength') };
        }

        return {};
    }

    private translateValidationResult(result: FdValidationErrorConfig | string, params: Record<string, unknown>): string {
        if (typeof result === 'string') {
            return this.translateService.instant(result, params);
        }

        return this.translateService.instant(result.key, {
            ...params,
            ...result.params,
        });
    }
}

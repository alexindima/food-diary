import type { FormGroup } from '@angular/forms';

import type { FormGroupControls } from '../../../../../shared/lib/common.data';
import type { LOGIN_ERROR_FIELDS, PASSWORD_RESET_ERROR_FIELDS, REGISTER_ERROR_FIELDS } from './auth-form.config';

export type LoginFieldErrors = Record<(typeof LOGIN_ERROR_FIELDS)[number], string | null>;
export type RegisterFieldErrors = Record<(typeof REGISTER_ERROR_FIELDS)[number], string | null>;
export type PasswordResetFieldErrors = Record<(typeof PASSWORD_RESET_ERROR_FIELDS)[number], string | null>;

type LoginFormValues = {
    email: string;
    password: string;
    rememberMe: boolean;
};

type RegisterFormValues = {
    email: string;
    password: string;
    confirmPassword: string;
    agreeTerms: boolean;
};

type PasswordResetFormValues = {
    email: string;
};

export type LoginFormGroup = FormGroupControls<LoginFormValues>;
export type RegisterFormGroup = FormGroupControls<RegisterFormValues>;
export type LoginForm = FormGroup<LoginFormGroup>;
export type RegisterForm = FormGroup<RegisterFormGroup>;
export type PasswordResetFormGroup = FormGroupControls<PasswordResetFormValues>;
export type PasswordResetForm = FormGroup<PasswordResetFormGroup>;

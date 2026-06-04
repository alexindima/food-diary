import type { LOGIN_ERROR_FIELDS, PASSWORD_RESET_ERROR_FIELDS, REGISTER_ERROR_FIELDS } from './auth-form.config';

export type LoginFieldErrors = Record<(typeof LOGIN_ERROR_FIELDS)[number], string | null>;
export type RegisterFieldErrors = Record<(typeof REGISTER_ERROR_FIELDS)[number], string | null>;
export type PasswordResetFieldErrors = Record<(typeof PASSWORD_RESET_ERROR_FIELDS)[number], string | null>;

export type LoginFormValues = {
    email: string;
    password: string;
    rememberMe: boolean;
};

export type RegisterFormValues = {
    email: string;
    password: string;
    confirmPassword: string;
    agreeTerms: boolean;
};

export type PasswordResetFormValues = {
    email: string;
};

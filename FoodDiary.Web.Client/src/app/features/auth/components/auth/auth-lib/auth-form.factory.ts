import type {
    LoginFieldErrors,
    LoginFormValues,
    PasswordResetFieldErrors,
    PasswordResetFormValues,
    RegisterFieldErrors,
    RegisterFormValues,
} from './auth.types';

export function createLoginFormModel(): LoginFormValues {
    return {
        email: '',
        password: '',
        rememberMe: false,
    };
}

export function createRegisterFormModel(): RegisterFormValues {
    return {
        email: '',
        password: '',
        confirmPassword: '',
        agreeTerms: false,
    };
}

export function createPasswordResetFormModel(): PasswordResetFormValues {
    return {
        email: '',
    };
}

export function createEmptyLoginFieldErrors(): LoginFieldErrors {
    return {
        email: null,
        password: null,
    };
}

export function createEmptyRegisterFieldErrors(): RegisterFieldErrors {
    return {
        email: null,
        password: null,
        confirmPassword: null,
    };
}

export function createEmptyPasswordResetFieldErrors(): PasswordResetFieldErrors {
    return {
        email: null,
    };
}

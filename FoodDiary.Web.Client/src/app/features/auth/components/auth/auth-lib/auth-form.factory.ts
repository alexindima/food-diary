import { FormControl, FormGroup, Validators } from '@angular/forms';

import { matchFieldValidator } from '../../../../../validators/match-field.validator';
import { AUTH_PASSWORD_MIN_LENGTH } from '../../../lib/auth.constants';
import type {
    LoginFieldErrors,
    LoginFormGroup,
    PasswordResetFieldErrors,
    PasswordResetFormGroup,
    RegisterFieldErrors,
    RegisterFormGroup,
} from './auth.types';

export function createLoginForm(): FormGroup<LoginFormGroup> {
    return new FormGroup<LoginFormGroup>({
        email: new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
        password: new FormControl<string>('', {
            nonNullable: true,
            validators: [Validators.required, Validators.minLength(AUTH_PASSWORD_MIN_LENGTH)],
        }),
        rememberMe: new FormControl<boolean>(false, { nonNullable: true }),
    });
}

export function createRegisterForm(): FormGroup<RegisterFormGroup> {
    return new FormGroup<RegisterFormGroup>({
        email: new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
        password: new FormControl<string>('', {
            nonNullable: true,
            validators: [Validators.required, Validators.minLength(AUTH_PASSWORD_MIN_LENGTH)],
        }),
        confirmPassword: new FormControl<string>('', {
            nonNullable: true,
            validators: [Validators.required, matchFieldValidator('password')],
        }),
        agreeTerms: new FormControl<boolean>(false, { nonNullable: true, validators: Validators.requiredTrue }),
    });
}

export function createPasswordResetForm(): FormGroup<PasswordResetFormGroup> {
    return new FormGroup<PasswordResetFormGroup>({
        email: new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    });
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

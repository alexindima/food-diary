import type { FormGroup } from '@angular/forms';

import type { FormGroupControls } from '../../../../shared/lib/common.data';

export type RegisterFieldErrors = Record<'email' | 'password' | 'confirmPassword', string | null>;
export type PasswordResetFieldErrors = Record<'email', string | null>;

type RegisterFormValues = {
    email: string;
    password: string;
    confirmPassword: string;
    agreeTerms: boolean;
};

type PasswordResetFormValues = {
    email: string;
};

export type RegisterFormGroup = FormGroupControls<RegisterFormValues>;
export type RegisterForm = FormGroup<RegisterFormGroup>;
export type PasswordResetFormGroup = FormGroupControls<PasswordResetFormValues>;
export type PasswordResetForm = FormGroup<PasswordResetFormGroup>;

import { describe, expect, it } from 'vitest';

import {
    createEmptyLoginFieldErrors,
    createEmptyPasswordResetFieldErrors,
    createEmptyRegisterFieldErrors,
    createLoginForm,
    createPasswordResetForm,
    createRegisterForm,
} from './auth-form.factory';

describe('auth form factory', () => {
    it('should create login form with required email and password validators', () => {
        const form = createLoginForm();

        form.controls.email.setValue('');
        form.controls.password.setValue('');

        expect(form.controls.email.hasError('required')).toBe(true);
        expect(form.controls.password.hasError('required')).toBe(true);
        expect(form.controls.rememberMe.value).toBe(false);
    });

    it('should create register form with password confirmation validator', () => {
        const form = createRegisterForm();

        form.controls.password.setValue('password1');
        form.controls.confirmPassword.setValue('password2');

        expect(form.controls.confirmPassword.hasError('matchField')).toBe(true);
    });

    it('should create password reset form with email validator', () => {
        const form = createPasswordResetForm();

        form.controls.email.setValue('invalid');

        expect(form.controls.email.hasError('email')).toBe(true);
    });

    it('should create empty field error maps', () => {
        expect(createEmptyLoginFieldErrors()).toEqual({ email: null, password: null });
        expect(createEmptyRegisterFieldErrors()).toEqual({ email: null, password: null, confirmPassword: null });
        expect(createEmptyPasswordResetFieldErrors()).toEqual({ email: null });
    });
});

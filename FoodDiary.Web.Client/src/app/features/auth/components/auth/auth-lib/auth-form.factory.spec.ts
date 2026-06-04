import { describe, expect, it } from 'vitest';

import {
    createEmptyLoginFieldErrors,
    createEmptyPasswordResetFieldErrors,
    createEmptyRegisterFieldErrors,
    createLoginFormModel,
    createPasswordResetFormModel,
    createRegisterFormModel,
} from './auth-form.factory';

describe('auth form factory', () => {
    it('should create login form model defaults', () => {
        expect(createLoginFormModel()).toEqual({
            email: '',
            password: '',
            rememberMe: false,
        });
    });

    it('should create register form model defaults', () => {
        expect(createRegisterFormModel()).toEqual({
            email: '',
            password: '',
            confirmPassword: '',
            agreeTerms: false,
        });
    });

    it('should create password reset form model defaults', () => {
        expect(createPasswordResetFormModel()).toEqual({ email: '' });
    });

    it('should create empty field error maps', () => {
        expect(createEmptyLoginFieldErrors()).toEqual({ email: null, password: null });
        expect(createEmptyRegisterFieldErrors()).toEqual({ email: null, password: null, confirmPassword: null });
        expect(createEmptyPasswordResetFieldErrors()).toEqual({ email: null });
    });
});

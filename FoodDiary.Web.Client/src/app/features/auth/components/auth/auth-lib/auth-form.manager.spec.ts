import { TestBed } from '@angular/core/testing';
import { submit } from '@angular/forms/signals';
import type { LangChangeEvent } from '@ngx-translate/core';
import { TranslateService } from '@ngx-translate/core';
import { Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AUTH_PASSWORD_MIN_LENGTH } from '../../../../../shared/auth/auth.constants';
import { AuthFormManager } from './auth-form.manager';

const PASSWORD_CONTRACT_MAX_LENGTH = 256;

describe('AuthFormManager', () => {
    let languageChanges: Subject<LangChangeEvent>;
    let language: string;

    beforeEach(() => {
        languageChanges = new Subject<LangChangeEvent>();
        language = 'en';

        TestBed.configureTestingModule({
            providers: [
                AuthFormManager,
                {
                    provide: TranslateService,
                    useValue: {
                        instant: (key: string): string => `${language}:${key}`,
                        onLangChange: languageChanges.asObservable(),
                    } satisfies Pick<TranslateService, 'instant' | 'onLangChange'>,
                },
            ],
        });
    });

    it('recomputes translated field errors when the language changes', () => {
        const manager = TestBed.inject(AuthFormManager);

        manager.registerModel.update(value => ({
            ...value,
            password: 'a'.repeat(AUTH_PASSWORD_MIN_LENGTH - 1),
        }));
        manager.registerForm.password().markAsTouched();

        expect(manager.registerFieldErrors().password).toBe('en:FORM_ERRORS.PASSWORD.MIN_LENGTH');

        language = 'ru';
        languageChanges.next({ lang: 'ru', translations: {} });

        expect(manager.registerFieldErrors().password).toBe('ru:FORM_ERRORS.PASSWORD.MIN_LENGTH');
    });

    it('shows terms acceptance error after invalid register submit', async () => {
        const manager = TestBed.inject(AuthFormManager);

        manager.registerModel.set({
            email: 'user@example.com',
            password: 'a'.repeat(AUTH_PASSWORD_MIN_LENGTH),
            confirmPassword: 'a'.repeat(AUTH_PASSWORD_MIN_LENGTH),
            agreeTerms: false,
        });

        await submit(manager.registerForm);

        expect(manager.registerFieldErrors().agreeTerms).toBe('en:FORM_ERRORS.REQUIRED');
    });

    it('accepts the 256-character login boundary literally and rejects longer passwords', () => {
        const manager = TestBed.inject(AuthFormManager);
        const password = ` ${'p'.repeat(PASSWORD_CONTRACT_MAX_LENGTH - 2)} `;
        manager.loginModel.set({ email: 'user@example.test', password, rememberMe: false });

        expect(manager.loginForm().valid()).toBe(true);
        expect(manager.loginModel().password).toBe(password);
        manager.loginModel.update(value => ({ ...value, password: 'p'.repeat(PASSWORD_CONTRACT_MAX_LENGTH + 1) }));
        expect(manager.loginForm.password().getError('maxLength')).toBeDefined();
    });

    it('blocks oversized registration before invoking the submission action', async () => {
        const manager = TestBed.inject(AuthFormManager);
        const register = vi.fn().mockResolvedValue(undefined);
        manager.configureSubmissionActions({ login: register, passwordReset: register, register });
        manager.registerModel.set({
            email: 'user@example.test',
            password: 'p'.repeat(PASSWORD_CONTRACT_MAX_LENGTH),
            confirmPassword: 'p'.repeat(PASSWORD_CONTRACT_MAX_LENGTH),
            agreeTerms: true,
        });
        expect(manager.registerForm().valid()).toBe(true);

        manager.registerModel.update(value => ({
            ...value,
            password: 'p'.repeat(PASSWORD_CONTRACT_MAX_LENGTH + 1),
            confirmPassword: 'p'.repeat(PASSWORD_CONTRACT_MAX_LENGTH + 1),
        }));
        await submit(manager.registerForm);

        expect(manager.registerForm.password().getError('maxLength')).toBeDefined();
        expect(manager.registerForm.confirmPassword().getError('maxLength')).toBeDefined();
        expect(register).not.toHaveBeenCalled();
    });
});

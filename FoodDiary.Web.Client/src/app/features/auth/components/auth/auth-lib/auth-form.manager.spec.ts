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
    let manager: AuthFormManager;
    let register: ReturnType<typeof vi.fn<() => Promise<void>>>;

    beforeEach(() => {
        languageChanges = new Subject<LangChangeEvent>();
        language = 'en';

        TestBed.configureTestingModule({
            providers: [
                {
                    provide: TranslateService,
                    useValue: {
                        instant: (key: string): string => `${language}:${key}`,
                        onLangChange: languageChanges.asObservable(),
                    } satisfies Pick<TranslateService, 'instant' | 'onLangChange'>,
                },
            ],
        });
        register = vi.fn<() => Promise<void>>().mockResolvedValue(undefined);
        manager = TestBed.runInInjectionContext(
            () =>
                new AuthFormManager({
                    loginAsync: vi.fn<() => Promise<void>>().mockResolvedValue(undefined),
                    requestPasswordResetAsync: vi.fn<() => Promise<void>>().mockResolvedValue(undefined),
                    registerAsync: register,
                }),
        );
    });

    it('accepts email, one password and terms without requiring a second password entry', () => {
        manager.registerModel.update(value => ({ ...value, email: 'user@example.test', password: 'password123', agreeTerms: true }));
        expect(manager.registerForm().valid()).toBe(true);
    });

    it('recomputes translated field errors when the language changes', () => {
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
        manager.registerModel.set({
            email: 'user@example.com',
            password: 'a'.repeat(AUTH_PASSWORD_MIN_LENGTH),
            agreeTerms: false,
        });

        await submit(manager.registerForm);

        expect(manager.registerFieldErrors().agreeTerms).toBe('en:FORM_ERRORS.REQUIRED');
    });

    it('accepts the 256-character login boundary literally and rejects longer passwords', () => {
        const password = ` ${'p'.repeat(PASSWORD_CONTRACT_MAX_LENGTH - 2)} `;
        manager.loginModel.set({ email: 'user@example.test', password, rememberMe: false });

        expect(manager.loginForm().valid()).toBe(true);
        expect(manager.loginModel().password).toBe(password);
        manager.loginModel.update(value => ({ ...value, password: 'p'.repeat(PASSWORD_CONTRACT_MAX_LENGTH + 1) }));
        expect(manager.loginForm.password().getError('maxLength')).toBeDefined();
    });

    it('does not submit until an oversized password is corrected', async () => {
        manager.registerModel.set({
            email: 'user@example.test',
            password: 'p'.repeat(PASSWORD_CONTRACT_MAX_LENGTH),
            agreeTerms: true,
        });
        expect(manager.registerForm().valid()).toBe(true);

        manager.registerModel.update(value => ({
            ...value,
            password: 'p'.repeat(PASSWORD_CONTRACT_MAX_LENGTH + 1),
        }));
        await submit(manager.registerForm);

        expect(manager.registerForm.password().getError('maxLength')).toBeDefined();
        expect(register).not.toHaveBeenCalled();

        manager.registerForm.password().value.set('p'.repeat(PASSWORD_CONTRACT_MAX_LENGTH));
        await submit(manager.registerForm);

        expect(register).toHaveBeenCalledOnce();
    });
});

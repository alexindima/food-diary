import { TestBed } from '@angular/core/testing';
import { submit } from '@angular/forms/signals';
import type { LangChangeEvent } from '@ngx-translate/core';
import { TranslateService } from '@ngx-translate/core';
import { Subject } from 'rxjs';
import { beforeEach, describe, expect, it } from 'vitest';

import { AUTH_PASSWORD_MIN_LENGTH } from '../../../lib/auth.constants';
import { AuthFormManager } from './auth-form.manager';

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
});

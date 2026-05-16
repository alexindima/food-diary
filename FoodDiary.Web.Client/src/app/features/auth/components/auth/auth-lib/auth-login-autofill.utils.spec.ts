import { describe, expect, it } from 'vitest';

import { getLoginAutofillFieldValues, hasCompleteLoginAutofill } from './auth-login-autofill.utils';

describe('auth login autofill utils', () => {
    it('should read username and current password input values', () => {
        const form = createLoginForm('user@example.com', 'secret');

        expect(getLoginAutofillFieldValues(form)).toEqual({
            email: 'user@example.com',
            password: 'secret',
        });
    });

    it('should detect complete autofill from filled fields', () => {
        expect(hasCompleteLoginAutofill(createLoginForm('user@example.com', 'secret'), false)).toBe(true);
    });

    it('should not detect partial autofill', () => {
        expect(hasCompleteLoginAutofill(createLoginForm('user@example.com', ''), false)).toBe(false);
    });

    it('should not use webkit detection after native interaction', () => {
        expect(hasCompleteLoginAutofill(createLoginForm('', ''), true)).toBe(false);
    });
});

function createLoginForm(email: string, password: string): HTMLFormElement {
    const form = document.createElement('form');
    form.innerHTML = `
        <input autocomplete="username" value="${email}" />
        <input autocomplete="current-password" value="${password}" />
    `;
    return form;
}

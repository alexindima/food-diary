import { describe, expect, it } from 'vitest';

import { getLoginAutofillFieldValues } from './auth-login-autofill.utils';

describe('auth login autofill utils', () => {
    it('should read username and current password input values', () => {
        const form = createLoginForm('user@example.com', 'secret');

        expect(getLoginAutofillFieldValues(form)).toEqual({
            email: 'user@example.com',
            password: 'secret',
        });
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

import { AUTH_LOGIN_AUTOFILL_FIELD_COUNT } from '../../../lib/auth.constants';

type LoginAutofillFields = {
    email: string;
    password: string;
};

export function getLoginAutofillFieldValues(form: HTMLFormElement): LoginAutofillFields {
    return {
        email: form.querySelector<HTMLInputElement>('input[autocomplete="username"]')?.value ?? '',
        password: form.querySelector<HTMLInputElement>('input[autocomplete="current-password"]')?.value ?? '',
    };
}

export function hasCompleteLoginAutofill(form: HTMLFormElement | undefined, hasNativeInteraction: boolean): boolean {
    if (form === undefined) {
        return false;
    }

    const fields = getLoginAutofillFieldValues(form);
    if (hasFilledLoginAutofillFields(fields)) {
        return true;
    }

    if (hasNativeInteraction || hasPartialLoginAutofillFields(fields)) {
        return false;
    }

    return hasDetectedLoginWebkitAutofill(form);
}

function hasFilledLoginAutofillFields(fields: LoginAutofillFields): boolean {
    return fields.email.length > 0 && fields.password.length > 0;
}

function hasPartialLoginAutofillFields(fields: LoginAutofillFields): boolean {
    return fields.email.length > 0 || fields.password.length > 0;
}

function hasDetectedLoginWebkitAutofill(form: HTMLFormElement): boolean {
    try {
        return form.querySelectorAll('input:-webkit-autofill').length >= AUTH_LOGIN_AUTOFILL_FIELD_COUNT;
    } catch {
        return false;
    }
}

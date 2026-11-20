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

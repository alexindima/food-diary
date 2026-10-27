const EXPLICIT_AUTH_PATH =
    /^\/(?:auth(?:\/|$)|reset-password(?:\/|$)|verify-email(?:\/|$)|verify-pending(?:\/|$)|change-password-required(?:\/|$)|mobile(?:\/|$))/u;

export function telegramMiniAppReturnUrl(value: string | null): string | null {
    if (value === null || !value.startsWith('/') || value.startsWith('//') || /[\\\u0000-\u0020]/u.test(value)) {
        return null;
    }
    const url = new URL(value, 'https://fooddiary.invalid');
    return EXPLICIT_AUTH_PATH.test(url.pathname) || ['/', '/ru'].includes(url.pathname) ? null : url.pathname + url.search + url.hash;
}

export function telegramMiniAppEntryUrl(href: string): string | null {
    const url = new URL(href);
    if (EXPLICIT_AUTH_PATH.test(url.pathname)) {
        return null;
    }
    const returnUrl = telegramMiniAppReturnUrl(url.pathname + url.search);
    const query = returnUrl === null ? '' : `?${new URLSearchParams({ returnUrl })}`;
    return `/auth/telegram${query}${url.hash}`;
}

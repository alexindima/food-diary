import { type AdminId, optionalAdminId } from '../../../shared/models/semantics/admin-meaning';

export function adminSignInReturnUrl(value: string | null, origin: string): string {
    if (value === null || !value.startsWith('/') || value.startsWith('//') || !URL.canParse(value, origin)) {
        return '/';
    }
    const url = new URL(value, origin);
    const [route] = url.pathname.slice(1).split(/[/;]/u, 1);
    if (url.origin !== origin || ['sign-in', 'unauthorized'].includes(route) || url.searchParams.has('returnUrl')) {
        return '/';
    }
    removeHandoffParameters(url);
    return url.pathname + url.search + url.hash;
}

function removeHandoffParameters(url: URL): void {
    url.searchParams.delete('code');
    const fragments = new URLSearchParams(url.hash.slice(1));
    if (fragments.has('code') || fragments.has('account')) {
        fragments.delete('code');
        fragments.delete('account');
        url.hash = fragments.toString();
    }
}

export function adminSignInAccount(fragment: string | null): AdminId<'user'> | null {
    const account = new URLSearchParams(fragment ?? '').get('account');
    return optionalAdminId<'user'>(account === '' ? null : account);
}

export function adminSignInAccountFragment(account: AdminId<'user'> | null): string | undefined {
    return account === null ? undefined : new URLSearchParams({ account }).toString();
}

export type AdminUnauthorizedReason = 'forbidden' | 'unauthenticated';

export function buildAdminUnauthorizedUrl(
    returnUrl: string,
    reason: AdminUnauthorizedReason,
    adminAppUrl: string,
    fallbackOrigin: string,
): string {
    const unauthorizedUrl = new URL('/unauthorized', adminAppUrl.length > 0 ? adminAppUrl : fallbackOrigin);
    unauthorizedUrl.searchParams.set('reason', reason);
    unauthorizedUrl.searchParams.set('returnUrl', returnUrl);
    return unauthorizedUrl.toString();
}

export function normalizeAdminReturnUrl(value: string, adminAppUrl: string, fallbackOrigin: string): string | null {
    if (value.length === 0) {
        return '/';
    }

    const decoded = safeDecode(value);
    if (decoded.includes('returnUrl=')) {
        return '/';
    }

    try {
        const parsed = new URL(decoded, adminAppUrl.length > 0 ? adminAppUrl : fallbackOrigin);
        if (adminAppUrl.length > 0) {
            const adminOrigin = new URL(adminAppUrl).origin;
            if (parsed.origin !== adminOrigin) {
                return '/';
            }
        }

        const search = parsed.searchParams.toString();
        return search.length > 0 ? `${parsed.pathname}?${search}` : parsed.pathname;
    } catch {
        return decoded.startsWith('/') ? decoded : '/';
    }
}

function safeDecode(value: string): string {
    try {
        return decodeURIComponent(value);
    } catch {
        return value;
    }
}

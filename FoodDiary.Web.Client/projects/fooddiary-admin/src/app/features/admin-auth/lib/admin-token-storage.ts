/** Migrate old installations once; a different tab never replaces this tab's identity. */
export function readAdminAccessToken(local: Storage | null, session: Storage | null): string | null {
    const tabToken = session?.getItem('authToken') ?? null;
    if (tabToken !== null) {
        return tabToken;
    }
    const legacyToken = local?.getItem('authToken') ?? null;
    if (legacyToken !== null) {
        session?.setItem('authToken', legacyToken);
        local?.removeItem('authToken');
    }
    return legacyToken;
}

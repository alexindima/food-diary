/** Synthetic browser-test credentials; no real issuer or provider is contacted. */
export function syntheticAuthToken(userId: string, role = 'User'): string {
    const encode = (value: Record<string, unknown>): string => Buffer.from(JSON.stringify(value)).toString('base64url');
    return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({ sub: userId, role, exp: 4102444800 })}.synthetic-signature`;
}

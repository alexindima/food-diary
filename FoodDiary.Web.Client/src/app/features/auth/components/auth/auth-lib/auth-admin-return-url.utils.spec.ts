import { describe, expect, it } from 'vitest';

import { buildAdminUnauthorizedUrl, normalizeAdminReturnUrl } from './auth-admin-return-url.utils';

const ADMIN_APP_URL = 'https://admin.fooddiary.club';
const APP_ORIGIN = 'https://fooddiary.club';

describe('normalizeAdminReturnUrl', () => {
    it('should keep a same-origin admin path and query', () => {
        expect(normalizeAdminReturnUrl('/users?tab=active', ADMIN_APP_URL, APP_ORIGIN)).toBe('/users?tab=active');
    });

    it('should normalize encoded admin paths', () => {
        expect(normalizeAdminReturnUrl('%2Fdashboard%3Fview%3Dtoday', ADMIN_APP_URL, APP_ORIGIN)).toBe('/dashboard?view=today');
    });

    it('should reject nested return URL parameters', () => {
        expect(normalizeAdminReturnUrl('/login?returnUrl=/users', ADMIN_APP_URL, APP_ORIGIN)).toBe('/');
    });

    it('should reject cross-origin absolute URLs', () => {
        expect(normalizeAdminReturnUrl('https://evil.example/users', ADMIN_APP_URL, APP_ORIGIN)).toBe('/');
    });

    it('should fall back to root for malformed absolute values', () => {
        expect(normalizeAdminReturnUrl('http://[::1', ADMIN_APP_URL, APP_ORIGIN)).toBe('/');
    });

    it.each([
        `${ADMIN_APP_URL}//attacker.example/capture`,
        `${ADMIN_APP_URL}///attacker.example/capture?tab=users`,
        `${ADMIN_APP_URL}/.//attacker.example/capture`,
        `${ADMIN_APP_URL}/%2e%2e//attacker.example/capture`,
        `${ADMIN_APP_URL}/\\attacker.example/capture`,
        `${ADMIN_APP_URL}/%2f%2fattacker.example/capture`,
        encodeURIComponent(`${ADMIN_APP_URL}//attacker.example/capture`),
        '//admin.fooddiary.club//attacker.example/capture',
        '//[invalid',
    ])('should reject a path that becomes a network reference: %s', value => {
        const path = normalizeAdminReturnUrl(value, ADMIN_APP_URL, APP_ORIGIN);

        expect(path).toBe('/');
        expect(new URL(path ?? '/', ADMIN_APP_URL).origin).toBe(ADMIN_APP_URL);
    });

    it.each([
        ['/users?tab=active#ignored', '/users?tab=active'],
        [`${ADMIN_APP_URL}/users?tab=active`, '/users?tab=active'],
        ['/users/a//b?query=%2F%2Fattacker.example', '/users/a//b?query=%2F%2Fattacker.example'],
    ])('should preserve an unambiguous local path: %s', (value, expected) => {
        expect(normalizeAdminReturnUrl(value, ADMIN_APP_URL, APP_ORIGIN)).toBe(expected);
    });
});

describe('buildAdminUnauthorizedUrl', () => {
    it('should build an admin unauthorized URL with reason and return URL', () => {
        expect(buildAdminUnauthorizedUrl('/users', 'forbidden', ADMIN_APP_URL, APP_ORIGIN)).toBe(
            'https://admin.fooddiary.club/unauthorized?reason=forbidden&returnUrl=%2Fusers',
        );
    });
});

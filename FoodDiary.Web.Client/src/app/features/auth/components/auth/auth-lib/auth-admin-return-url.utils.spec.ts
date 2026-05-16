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
});

describe('buildAdminUnauthorizedUrl', () => {
    it('should build an admin unauthorized URL with reason and return URL', () => {
        expect(buildAdminUnauthorizedUrl('/users', 'forbidden', ADMIN_APP_URL, APP_ORIGIN)).toBe(
            'https://admin.fooddiary.club/unauthorized?reason=forbidden&returnUrl=%2Fusers',
        );
    });
});

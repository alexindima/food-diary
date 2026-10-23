import { describe, expect, it } from 'vitest';

import { resolveAdminAvatarUrl } from './admin-avatar-url';

const ADMIN_BASE_URL = 'https://admin.fooddiary.test/';

describe('Admin avatar image sources', () => {
    it.each([
        ['/assets/avatar.png', 'https://admin.fooddiary.test/assets/avatar.png'],
        ['https://images.example.test/avatar.png', 'https://images.example.test/avatar.png'],
        [' https://images.example.test/avatar.png ', 'https://images.example.test/avatar.png'],
        ['data:image/png;base64,avatar', 'data:image/png;base64,avatar'],
        ['blob:https://admin.fooddiary.test/avatar', 'blob:https://admin.fooddiary.test/avatar'],
    ])('allows the existing supported image source %s', (source, expected) => {
        expect(resolveAdminAvatarUrl(source, ADMIN_BASE_URL)).toBe(expected);
    });

    it.each([
        undefined,
        null,
        '',
        '   ',
        'http://images.example.test/avatar.png',
        'javascript:alert(1)',
        'data:text/html,avatar',
        'blob:https://images.example.test/avatar',
        'https://[invalid-host',
    ])('uses initials for an unavailable or unsupported source %s', source => {
        expect(resolveAdminAvatarUrl(source, ADMIN_BASE_URL)).toBeNull();
    });

    it('keeps same-origin HTTP images available in local development', () => {
        expect(resolveAdminAvatarUrl('/assets/avatar.png', 'http://localhost:4301/')).toBe('http://localhost:4301/assets/avatar.png');
    });

    it.each([
        ['http://127.0.0.1:9000/fooddiary-public/avatar.png', 'http://127.0.0.1:55961/'],
        ['http://127.0.0.1:9000/fooddiary-public/avatar.png', 'http://localhost:4301/'],
        ['http://localhost:9000/fooddiary-public/avatar.png', 'http://127.0.0.1:55961/'],
        ['http://[::1]:9000/fooddiary-public/avatar.png', 'http://[::1]:4301/'],
    ])('keeps the local storage image %s available from %s', (source, baseUrl) => {
        expect(resolveAdminAvatarUrl(source, baseUrl)).toBe(source);
    });

    it.each([
        ['http://127.0.0.1:9000/avatar.png', ADMIN_BASE_URL],
        ['http://127.0.0.1:9000/avatar.png', 'https://localhost:4301/'],
        ['http://images.example.test/avatar.png', 'http://localhost:4301/'],
        ['http://127.0.0.1.evil.test/avatar.png', 'http://localhost:4301/'],
        ['http://127.0.0.1:9000/avatar.png', 'http://localhost.evil.test:4301/'],
    ])('rejects the non-local or mixed-content image %s from %s', (source, baseUrl) => {
        expect(resolveAdminAvatarUrl(source, baseUrl)).toBeNull();
    });
});

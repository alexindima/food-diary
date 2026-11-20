import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { JwtDecoderService } from './jwt-decoder.service';

const NON_STRING_ROLE_VALUE = 42;

let service: JwtDecoderService;

beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(JwtDecoderService);
});

describe('JwtDecoderService decodePayload', () => {
    it('requires an explicit email-optional claim rather than inferring it from missing email', () => {
        expect(service.isEmailOptional(encodePayload({ sub: 'telegram-user', fd_email_optional: 'true' }))).toBe(true);
        expect(service.isEmailOptional(encodePayload({ sub: 'existing-user' }))).toBe(false);
        expect(service.isEmailOptional(null)).toBe(false);
        expect(service.isEmailOptional('invalid')).toBe(false);
    });

    it('should decode a valid JWT payload', () => {
        const token = encodePayload({ sub: '123', name: 'test' });
        const result = service.decodePayload(token);
        expect(result).toEqual({ sub: '123', name: 'test' });
    });

    it('should return null for token without payload segment', () => {
        expect(service.decodePayload('headeronly')).toBeNull();
    });

    it('rejects an empty payload and encoded JSON values that cannot contain claims', () => {
        expect(service.decodePayload('header..signature')).toBeNull();
        expect(service.decodePayload(encodePayload(null))).toBeNull();
        expect(service.decodePayload(encodePayload(['Admin']))).toBeNull();
        expect(service.decodePayload(encodePayload('User'))).toBeNull();
    });

    it('should return null for invalid base64', () => {
        expect(service.decodePayload('header.!!!invalid!!!.sig')).toBeNull();
    });

    it('should handle URL-safe base64 characters', () => {
        const payload = { data: 'test+value/end' };
        const json = JSON.stringify(payload);
        const bytes = new TextEncoder().encode(json);
        const binary = Array.from(bytes, byte => String.fromCodePoint(byte)).join('');
        const urlSafe = btoa(binary).replaceAll('+', '-').replaceAll('/', '_').replace(/=+$/, '');
        const token = `header.${urlSafe}.sig`;
        expect(service.decodePayload(token)).toEqual(payload);
    });

    it('should decode UTF-8 claim values', () => {
        const payload = { fd_impersonation_reason: 'Проверка ошибки пользователя' };
        const token = encodePayload(payload);
        expect(service.decodePayload(token)).toEqual(payload);
    });
});

describe('JwtDecoderService extractUserId', () => {
    it('should extract userId from nameid claim', () => {
        const token = encodePayload({ nameid: 'user-123' });
        expect(service.extractUserId(token)).toBe('user-123');
    });

    it('should extract userId from sub claim', () => {
        const token = encodePayload({ sub: 'user-456' });
        expect(service.extractUserId(token)).toBe('user-456');
    });

    it('should extract userId from Microsoft nameidentifier claim', () => {
        const token = encodePayload({
            'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': 'user-789',
        });
        expect(service.extractUserId(token)).toBe('user-789');
    });

    it('should return null for null token', () => {
        expect(service.extractUserId(null)).toBeNull();
    });

    it('should return null when no known userId claim exists', () => {
        const token = encodePayload({ email: 'test@test.com' });
        expect(service.extractUserId(token)).toBeNull();
    });

    it('rejects an unreadable payload instead of assigning an account identity', () => {
        expect(service.extractUserId('header.!!!invalid!!!.signature')).toBeNull();
    });

    it('uses the subject when legacy identity claims are empty or not strings', () => {
        const token = encodePayload({
            nameid: '',
            'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': NON_STRING_ROLE_VALUE,
            sub: 'subject-user',
        });

        expect(service.extractUserId(token)).toBe('subject-user');
    });
});

describe('JwtDecoderService extractRoles', () => {
    it('should extract roles from role claim as array', () => {
        const token = encodePayload({ role: ['Admin', 'Premium'] });
        expect(service.extractRoles(token)).toEqual(['Admin', 'Premium']);
    });

    it('should extract single role as array', () => {
        const token = encodePayload({ role: 'Admin' });
        expect(service.extractRoles(token)).toEqual(['Admin']);
    });

    it('should extract roles from Microsoft role claim', () => {
        const token = encodePayload({
            'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'Premium',
        });
        expect(service.extractRoles(token)).toEqual(['Premium']);
    });

    it('should return empty array when no role claim', () => {
        const token = encodePayload({ sub: '123' });
        expect(service.extractRoles(token)).toEqual([]);
    });

    it('should filter non-string values from role array', () => {
        const token = encodePayload({ role: ['Admin', NON_STRING_ROLE_VALUE, null, 'User'] });
        expect(service.extractRoles(token)).toEqual(['Admin', 'User']);
    });

    it('should return empty array for invalid token', () => {
        expect(service.extractRoles('invalid')).toEqual([]);
    });

    it('supports the legacy XML role claim and prioritizes a present role claim', () => {
        const legacyRole = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role';
        expect(service.extractRoles(encodePayload({ [legacyRole]: 'User' }))).toEqual(['User']);
        expect(service.extractRoles(encodePayload({ [legacyRole]: 'User', role: 'Premium' }))).toEqual(['Premium']);
    });
});

describe('JwtDecoderService impersonation context', () => {
    it.each([null, '', 'unreadable'])('does not show impersonation context for an unreadable token (%s)', token => {
        expect(service.isImpersonation(token)).toBe(false);
        expect(service.extractImpersonationReason(token)).toBeNull();
    });

    it('preserves the explicit impersonation flag and Unicode reason', () => {
        const reason = 'Проверка дневника';
        const token = encodePayload({ fd_impersonation: 'true', fd_impersonation_reason: reason });

        expect(service.isImpersonation(token)).toBe(true);
        expect(service.extractImpersonationReason(token)).toBe(reason);
    });

    it('does not infer impersonation or a displayable reason from other claim types', () => {
        const token = encodePayload({ fd_impersonation: true, fd_impersonation_reason: { reason: 'Unsupported' } });

        expect(service.isImpersonation(token)).toBe(false);
        expect(service.extractImpersonationReason(token)).toBeNull();
    });
});

describe('JwtDecoderService expiration', () => {
    it.each([null, '', 'unreadable', encodePayload({ exp: '1800000000' })])(
        'does not invent an expiration timestamp for a missing or invalid numeric expiry (%s)',
        token => {
            expect(service.extractExpirationTimeMs(token)).toBeNull();
            expect(service.isExpired(token)).toBe(false);
        },
    );

    it('tells consumers when a numeric expiry crosses the refresh window or its exact time', () => {
        const expirationSeconds = 1_800_000_000;
        const expirationMilliseconds = 1_800_000_000_000;
        const refreshWindowSeconds = 5;
        const refreshWindowMilliseconds = 5000;
        const token = encodePayload({ exp: expirationSeconds });
        const now = vi.spyOn(Date, 'now');
        try {
            now.mockReturnValue(expirationMilliseconds - refreshWindowMilliseconds);
            expect(service.extractExpirationTimeMs(token)).toBe(expirationMilliseconds);
            expect(service.isExpired(token)).toBe(false);
            expect(service.isExpired(token, refreshWindowSeconds)).toBe(true);
            now.mockReturnValue(expirationMilliseconds);
            expect(service.isExpired(token)).toBe(true);
        } finally {
            now.mockRestore();
        }
    });
});

function encodePayload(payload: Record<string, unknown> | readonly unknown[] | string | null): string {
    const json = JSON.stringify(payload);
    const bytes = new TextEncoder().encode(json);
    const binary = Array.from(bytes, byte => String.fromCodePoint(byte)).join('');
    const base64 = btoa(binary);
    return `header.${base64}.signature`;
}

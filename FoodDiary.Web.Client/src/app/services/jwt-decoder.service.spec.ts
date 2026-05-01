import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { JwtDecoderService } from './jwt-decoder.service';

function encodePayload(payload: Record<string, unknown>): string {
    const json = JSON.stringify(payload);
    const bytes = new TextEncoder().encode(json);
    const binary = Array.from(bytes, byte => String.fromCharCode(byte)).join('');
    const base64 = btoa(binary);
    return `header.${base64}.signature`;
}

describe('JwtDecoderService', () => {
    let service: JwtDecoderService;

    beforeEach(() => {
        TestBed.configureTestingModule({});
        service = TestBed.inject(JwtDecoderService);
    });

    describe('decodePayload', () => {
        it('should decode a valid JWT payload', () => {
            const token = encodePayload({ sub: '123', name: 'test' });
            const result = service.decodePayload(token);
            expect(result).toEqual({ sub: '123', name: 'test' });
        });

        it('should return null for token without payload segment', () => {
            expect(service.decodePayload('headeronly')).toBeNull();
        });

        it('should return null for invalid base64', () => {
            expect(service.decodePayload('header.!!!invalid!!!.sig')).toBeNull();
        });

        it('should handle URL-safe base64 characters', () => {
            const payload = { data: 'test+value/end' };
            const json = JSON.stringify(payload);
            const bytes = new TextEncoder().encode(json);
            const binary = Array.from(bytes, byte => String.fromCharCode(byte)).join('');
            const urlSafe = btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
            const token = `header.${urlSafe}.sig`;
            expect(service.decodePayload(token)).toEqual(payload);
        });

        it('should decode UTF-8 claim values', () => {
            const payload = { fd_impersonation_reason: 'Проверка ошибки пользователя' };
            const token = encodePayload(payload);
            expect(service.decodePayload(token)).toEqual(payload);
        });
    });

    describe('extractUserId', () => {
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
    });

    describe('extractRoles', () => {
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
            const token = encodePayload({ role: ['Admin', 42, null, 'User'] });
            expect(service.extractRoles(token)).toEqual(['Admin', 'User']);
        });

        it('should return empty array for invalid token', () => {
            expect(service.extractRoles('invalid')).toEqual([]);
        });
    });
});

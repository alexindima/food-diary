import { Injectable } from '@angular/core';

import { isRecord } from '../shared/lib/unknown-value.utils';

const BASE64_BLOCK_SIZE = 4;
const EXPIRATION_SECONDS_TO_MS = 1000;

@Injectable({ providedIn: 'root' })
export class JwtDecoderService {
    public decodePayload(token: string): Record<string, unknown> | null {
        const payloadSegment = token.split('.').at(1);
        if (payloadSegment === undefined || payloadSegment.length === 0) {
            return null;
        }

        try {
            const normalized = payloadSegment.replace(/-/g, '+').replace(/_/g, '/');
            const remainder = normalized.length % BASE64_BLOCK_SIZE;
            const padLength = (BASE64_BLOCK_SIZE - (remainder === 0 ? BASE64_BLOCK_SIZE : remainder)) % BASE64_BLOCK_SIZE;
            const padded = normalized.padEnd(normalized.length + padLength, '=');
            const decoded = atob(padded);
            const bytes = Uint8Array.from(decoded, character => character.charCodeAt(0));
            const payload = new TextDecoder().decode(bytes);
            const parsed: unknown = JSON.parse(payload);
            return isRecord(parsed) ? parsed : null;
        } catch {
            return null;
        }
    }

    public extractUserId(token: string | null): string | null {
        if (token === null || token.length === 0) {
            return null;
        }

        const payload = this.decodePayload(token);
        if (payload === null) {
            return null;
        }

        return (
            this.stringClaim(payload, 'nameid') ??
            this.stringClaim(payload, 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier') ??
            this.stringClaim(payload, 'sub')
        );
    }

    public extractRoles(token: string): string[] {
        const payload = this.decodePayload(token);
        if (payload === null) {
            return [];
        }

        const roleClaim =
            payload['role'] ??
            payload['roles'] ??
            payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
            payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role'];

        if (Array.isArray(roleClaim)) {
            return roleClaim.filter((value): value is string => typeof value === 'string');
        }

        if (typeof roleClaim === 'string') {
            return [roleClaim];
        }

        return [];
    }

    public isImpersonation(token: string | null): boolean {
        if (token === null || token.length === 0) {
            return false;
        }

        const payload = this.decodePayload(token);
        return payload?.['fd_impersonation'] === 'true';
    }

    public extractImpersonationReason(token: string | null): string | null {
        if (token === null || token.length === 0) {
            return null;
        }

        const payload = this.decodePayload(token);
        const value = payload?.['fd_impersonation_reason'];
        return typeof value === 'string' ? value : null;
    }

    public extractExpirationTimeMs(token: string | null): number | null {
        if (token === null || token.length === 0) {
            return null;
        }

        const payload = this.decodePayload(token);
        const exp = payload?.['exp'];
        if (typeof exp !== 'number') {
            return null;
        }

        return exp * EXPIRATION_SECONDS_TO_MS;
    }

    public isExpired(token: string | null, leewaySeconds = 0): boolean {
        const expirationTimeMs = this.extractExpirationTimeMs(token);
        if (expirationTimeMs === null) {
            return false;
        }

        return expirationTimeMs <= Date.now() + leewaySeconds * EXPIRATION_SECONDS_TO_MS;
    }

    private stringClaim(payload: Record<string, unknown>, claim: string): string | null {
        const value = payload[claim];
        return typeof value === 'string' && value.length > 0 ? value : null;
    }
}

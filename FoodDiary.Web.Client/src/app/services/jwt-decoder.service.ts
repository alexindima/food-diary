import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root',
})
export class JwtDecoderService {
    public decodePayload(token: string): Record<string, unknown> | null {
        const [, payloadSegment] = token.split('.');
        if (!payloadSegment) {
            return null;
        }

        try {
            const normalized = payloadSegment.replace(/-/g, '+').replace(/_/g, '/');
            const padLength = (4 - (normalized.length % 4 || 4)) % 4;
            const padded = normalized.padEnd(normalized.length + padLength, '=');
            const decoded = atob(padded);
            return JSON.parse(decoded) as Record<string, unknown>;
        } catch {
            return null;
        }
    }

    public extractUserId(token: string | null): string | null {
        if (!token) {
            return null;
        }

        const payload = this.decodePayload(token);
        if (!payload) {
            return null;
        }

        return (
            (payload['nameid'] as string) ||
            (payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] as string) ||
            (payload['sub'] as string) ||
            null
        );
    }

    public extractRoles(token: string): string[] {
        const payload = this.decodePayload(token);
        if (!payload) {
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
}

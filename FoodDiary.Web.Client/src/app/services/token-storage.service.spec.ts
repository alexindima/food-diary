import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { TokenStorageService } from './token-storage.service';

let service: TokenStorageService;

beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(TokenStorageService);
});

describe('TokenStorageService getToken', () => {
    it('migrates the legacy token into this tab and removes persistent credentials', () => {
        localStorage.setItem('authToken', 'local-token');
        expect(service.getToken()).toBe('local-token');
        expect(localStorage.getItem('authToken'), 'Access tokens must not persist in localStorage').toBeNull();
        expect(sessionStorage.getItem('authToken')).toBe('local-token');
        expect(service.isRemembered()).toBe(true);
    });

    it('should return token from sessionStorage when not in localStorage', () => {
        sessionStorage.setItem('authToken', 'session-token');
        expect(service.getToken()).toBe('session-token');
    });

    it('should prefer the tab token over the shared remembered token', () => {
        localStorage.setItem('authToken', 'local');
        sessionStorage.setItem('authToken', 'session');
        expect(service.getToken()).toBe('session');
    });

    it('should return null when no token stored', () => {
        expect(service.getToken()).toBeNull();
    });
});

describe('TokenStorageService setToken', () => {
    it('keeps remembered access credentials in the current tab only', () => {
        service.setToken('test-token', true);
        expect(localStorage.getItem('authToken')).toBeNull();
        expect(sessionStorage.getItem('authToken')).toBe('test-token');
    });

    it('should store in sessionStorage when rememberMe is false', () => {
        service.setToken('test-token', false);
        expect(sessionStorage.getItem('authToken')).toBe('test-token');
        expect(localStorage.getItem('authToken')).toBeNull();
    });

    it('should update existing storage when rememberMe is undefined', () => {
        localStorage.setItem('authToken', 'old-token');
        service.setToken('new-token');
        expect(localStorage.getItem('authToken')).toBeNull();
        expect(sessionStorage.getItem('authToken')).toBe('new-token');
    });

    it('should default to sessionStorage when no existing token and rememberMe is undefined', () => {
        service.setToken('new-token');
        expect(sessionStorage.getItem('authToken')).toBe('new-token');
    });
});

describe('TokenStorageService clearToken', () => {
    it('should remove token from both storages', () => {
        localStorage.setItem('authToken', 'token');
        sessionStorage.setItem('authToken', 'token');
        service.clearToken();
        expect(localStorage.getItem('authToken')).toBeNull();
        expect(sessionStorage.getItem('authToken')).toBeNull();
    });
});

describe('TokenStorageService refreshToken', () => {
    it('migrates an unbound legacy credential with only a historical user id', () => {
        localStorage.setItem('userId', 'historical-alice');
        localStorage.setItem('refreshToken', 'legacy-alice');

        expect(service.peekLegacyRefreshTokenForBootstrap()).toBe('legacy-alice');
        expect(localStorage.getItem('refreshToken')).toBe('legacy-alice');
        expect(service.getToken()).toBeNull();
        expect(service.loadEmailConfirmed()).toBeNull();
    });

    it.each(['local', 'session'])('preserves a legacy credential when an unbound %s token already has an identity', area => {
        localStorage.setItem('userId', 'historical-alice');
        localStorage.setItem('refreshToken', 'legacy-alice');
        const storage = area === 'local' ? localStorage : sessionStorage;
        storage.setItem('authToken', `header.${btoa(JSON.stringify({ sub: 'bob' }))}.signature`);

        expect(service.peekLegacyRefreshTokenForBootstrap()).toBeNull();
        expect(localStorage.getItem('refreshToken')).toBe('legacy-alice');
    });

    it('does not treat historical account hints as a verified owner during guest migration', () => {
        localStorage.setItem('userId', 'historical-alice');
        localStorage.setItem('accountAccess', JSON.stringify(['historical-alice', 'Premium']));
        localStorage.setItem('emailConfirmed', 'true');
        localStorage.setItem('refreshSession', 'true');
        localStorage.setItem('refreshToken', 'legacy-alice');

        expect(service.peekLegacyRefreshTokenForBootstrap()).toBe('legacy-alice');
        expect(service.getToken()).toBeNull();
        expect(service.loadEmailConfirmed()).toBeNull();
    });

    it('clears only the captured legacy credential and preserves its replacement', () => {
        localStorage.setItem('refreshToken', 'legacy-bob');
        service.clearLegacyRefreshTokenIfMatches('legacy-alice');
        expect(localStorage.getItem('refreshToken')).toBe('legacy-bob');
        service.clearLegacyRefreshTokenIfMatches('legacy-bob');
        expect(localStorage.getItem('refreshToken')).toBeNull();
    });

    it('requires both an origin owner and refresh-session hint before preserving guest push', () => {
        localStorage.setItem('refreshSession', 'true');
        sessionStorage.setItem('userId', 'old-tab-owner');
        expect(service.hasPublishedSession()).toBe(false);
        localStorage.setItem('userId', 'current-owner');
        expect(service.hasPublishedSession()).toBe(true);
        localStorage.removeItem('refreshSession');
        expect(service.hasPublishedSession()).toBe(false);
    });
    it('should persist only a non-sensitive refresh-session marker', () => {
        service.markRefreshSession();
        expect(service.hasRefreshSession()).toBe(true);
        expect(localStorage.getItem('refreshSession')).toBe('true');
        expect(localStorage.getItem('refreshToken')).toBeNull();
    });

    it('should consume and remove a legacy refresh token once', () => {
        localStorage.setItem('refreshToken', 'legacy-refresh');
        expect(service.consumeLegacyRefreshToken()).toBe('legacy-refresh');
        expect(localStorage.getItem('refreshToken')).toBeNull();
    });

    it('should reject invalid legacy refresh tokens', () => {
        localStorage.setItem('refreshToken', 'undefined');
        expect(service.consumeLegacyRefreshToken()).toBeNull();
    });

    it('should clear the marker and legacy token', () => {
        service.markRefreshSession();
        localStorage.setItem('refreshToken', 'legacy-refresh');
        service.clearRefreshSession();
        expect(service.hasRefreshSession()).toBe(false);
    });
});

describe('TokenStorageService userId', () => {
    it('should load stored userId', () => {
        localStorage.setItem('userId', 'user-123');
        expect(service.loadUserId()).toBe('user-123');
    });

    it('should return null for undefined userId', () => {
        localStorage.setItem('userId', 'undefined');
        expect(service.loadUserId()).toBeNull();
    });

    it('should set and clear userId', () => {
        service.setUserId('user-456');
        expect(localStorage.getItem('userId')).toBe('user-456');
        service.clearUserId();
        expect(localStorage.getItem('userId')).toBeNull();
    });
});

describe('TokenStorageService emailConfirmed', () => {
    beforeEach(() => {
        service.setUserId('user-123');
    });
    it('should load true', () => {
        localStorage.setItem('emailConfirmed', 'true');
        expect(service.loadEmailConfirmed()).toBe(true);
    });

    it('should load false', () => {
        localStorage.setItem('emailConfirmed', 'false');
        expect(service.loadEmailConfirmed()).toBe(false);
    });

    it('should return null when not set', () => {
        expect(service.loadEmailConfirmed()).toBeNull();
    });

    it('should set and clear', () => {
        service.setEmailConfirmed(true);
        expect(localStorage.getItem('emailConfirmed')).toBe('true');
        service.clearEmailConfirmed();
        expect(localStorage.getItem('emailConfirmed')).toBeNull();
    });
});

describe('TokenStorageService clearAll', () => {
    it('should clear all stored values', () => {
        localStorage.setItem('authToken', 'token');
        localStorage.setItem('refreshToken', 'refresh');
        localStorage.setItem('userId', 'user');
        localStorage.setItem('emailConfirmed', 'true');
        service.bindOwner('user');

        service.clearAll();

        expect(localStorage.getItem('authToken')).toBeNull();
        expect(localStorage.getItem('refreshToken')).toBeNull();
        expect(localStorage.getItem('userId')).toBeNull();
        expect(localStorage.getItem('emailConfirmed')).toBeNull();
    });
});

describe('TokenStorageService owner boundaries', () => {
    function token(owner: string): string {
        return `${btoa('{}')}.${btoa(JSON.stringify({ sub: owner }))}.signature`;
    }

    function rememberBob(): void {
        localStorage.setItem('authToken', token('bob'));
        localStorage.setItem('userId', 'bob');
        localStorage.setItem('emailConfirmed', 'false');
        localStorage.setItem('mustChangePassword', 'true');
        localStorage.setItem('accountAccess', JSON.stringify(['bob', 'User']));
        localStorage.setItem('refreshSession', 'true');
        localStorage.setItem('refreshToken', 'legacy-bob');
    }

    it('preserves another remembered owner during refresh persistence and cleanup', () => {
        service.bindOwner('alice');
        service.setToken(token('alice'), true);
        service.setUserId('alice');
        rememberBob();
        const shared = Object.fromEntries(Object.entries(localStorage));

        service.setToken(token('alice'));
        service.setUserId('alice');
        service.setEmailConfirmed(true);
        service.setMustChangePassword(false);
        service.markRefreshSession();
        service.saveAccountAccess('alice', ['Premium']);
        expect(service.consumeLegacyRefreshToken()).toBeNull();
        expect(service.loadEmailConfirmed()).toBe(true);
        expect(service.loadMustChangePassword()).toBe(false);
        expect(sessionStorage.getItem('authToken')).toBe(token('alice'));
        expect(Object.fromEntries(Object.entries(localStorage))).toEqual(shared);

        service.clearAll();

        expect(sessionStorage.getItem('authToken')).toBeNull();
        expect(Object.fromEntries(Object.entries(localStorage))).toEqual(shared);
    });

    it('refreshes the remembered token for the same owner', () => {
        service.bindOwner('alice');
        service.setToken(token('alice'), true);
        service.setUserId('alice');
        service.setToken(`${token('alice')}updated`);

        expect(service.isRemembered()).toBe(true);
        expect(localStorage.getItem('authToken')).toBeNull();
        expect(sessionStorage.getItem('authToken')).toBe(`${token('alice')}updated`);
    });

    it('does not infer account metadata from a foreign shared user', () => {
        rememberBob();
        service.bindOwner('alice');
        expect(service.loadEmailConfirmed()).toBeNull();
        expect(service.loadMustChangePassword()).toBeNull();
    });

    it('keeps a tokenless guest from erasing an identified cookie session', () => {
        rememberBob();
        localStorage.removeItem('authToken');
        const shared = Object.fromEntries(Object.entries(localStorage));

        service.clearAll();

        expect(Object.fromEntries(Object.entries(localStorage))).toEqual(shared);
        expect(service.hasRefreshSession()).toBe(true);
    });
});

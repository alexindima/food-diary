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
    it('should return token from localStorage', () => {
        localStorage.setItem('authToken', 'local-token');
        expect(service.getToken()).toBe('local-token');
    });

    it('should return token from sessionStorage when not in localStorage', () => {
        sessionStorage.setItem('authToken', 'session-token');
        expect(service.getToken()).toBe('session-token');
    });

    it('should prefer localStorage over sessionStorage', () => {
        localStorage.setItem('authToken', 'local');
        sessionStorage.setItem('authToken', 'session');
        expect(service.getToken()).toBe('local');
    });

    it('should return null when no token stored', () => {
        expect(service.getToken()).toBeNull();
    });
});

describe('TokenStorageService setToken', () => {
    it('should store in localStorage when rememberMe is true', () => {
        service.setToken('test-token', true);
        expect(localStorage.getItem('authToken')).toBe('test-token');
        expect(sessionStorage.getItem('authToken')).toBeNull();
    });

    it('should store in sessionStorage when rememberMe is false', () => {
        service.setToken('test-token', false);
        expect(sessionStorage.getItem('authToken')).toBe('test-token');
        expect(localStorage.getItem('authToken')).toBeNull();
    });

    it('should update existing storage when rememberMe is undefined', () => {
        localStorage.setItem('authToken', 'old-token');
        service.setToken('new-token');
        expect(localStorage.getItem('authToken')).toBe('new-token');
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
    it('should get and set refresh token', () => {
        service.setRefreshToken('refresh-123');
        expect(service.getRefreshToken()).toBe('refresh-123');
    });

    it('should return null for invalid refresh tokens', () => {
        localStorage.setItem('refreshToken', 'undefined');
        expect(service.getRefreshToken()).toBeNull();
    });

    it('should clear refresh token', () => {
        service.setRefreshToken('refresh-123');
        service.clearRefreshToken();
        expect(service.getRefreshToken()).toBeNull();
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

        service.clearAll();

        expect(localStorage.getItem('authToken')).toBeNull();
        expect(localStorage.getItem('refreshToken')).toBeNull();
        expect(localStorage.getItem('userId')).toBeNull();
        expect(localStorage.getItem('emailConfirmed')).toBeNull();
    });
});

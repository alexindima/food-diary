import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { ACCOUNT_ACCESS_STORAGE_KEY, AuthMetadataStorageService } from './auth-metadata-storage.service';

let service: AuthMetadataStorageService;
beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    TestBed.resetTestingModule();
    service = TestBed.inject(AuthMetadataStorageService);
});

function publishBob(): void {
    localStorage.setItem(ACCOUNT_ACCESS_STORAGE_KEY, JSON.stringify(['bob', 'User']));
    localStorage.setItem('userId', 'bob');
}

describe('non-secret session metadata', () => {
    it('discards legacy access and refresh credentials instead of restoring them', () => {
        localStorage.setItem('authToken', 'legacy-access');
        sessionStorage.setItem('refreshToken', 'legacy-refresh');
        TestBed.resetTestingModule();
        TestBed.inject(AuthMetadataStorageService);
        expect(localStorage.getItem('authToken')).toBeNull();
        expect(sessionStorage.getItem('refreshToken')).toBeNull();
    });

    it('stores the remember-me preference without a credential', () => {
        expect(service.isRemembered()).toBe(false);
        service.setRemembered(true);
        expect(service.isRemembered()).toBe(true);
        expect(localStorage.getItem('authToken')).toBeNull();
        expect(sessionStorage.getItem('authToken')).toBeNull();
    });

    it('publishes deduplicated role hints and owner metadata after a new login', () => {
        publishBob();
        service.setUserId('alice');
        service.saveAccountAccess('alice', ['User', 'Premium', 'User'], true);
        service.setEmailConfirmed(true);
        expect(localStorage.getItem(ACCOUNT_ACCESS_STORAGE_KEY)).toBe(JSON.stringify(['alice', 'Premium', 'User']));
        expect(localStorage.getItem('userId')).toBe('alice');
        expect(localStorage.getItem('emailConfirmed')).toBe('true');
        expect(service.hasForeignSession('alice')).toBe(false);
    });

    it('recognizes a published cookie session without the legacy refresh marker', () => {
        publishBob();
        expect(service.hasPublishedSession()).toBe(true);
        expect(service.hasForeignSession('alice')).toBe(true);
        expect(localStorage.getItem('refreshSession')).toBeNull();
    });

    it('does not overwrite or erase a different account from an obsolete tab', () => {
        service.bindOwner('alice');
        service.setUserId('alice');
        publishBob();
        const shared = Object.fromEntries(Object.entries(localStorage));
        service.setEmailConfirmed(true);
        service.setMustChangePassword(false);
        service.saveAccountAccess('alice', ['Premium']);
        expect(sessionStorage.getItem('userId')).toBe('alice');
        expect(Object.fromEntries(Object.entries(localStorage))).toEqual(shared);
        service.clearAll();
        expect(sessionStorage.getItem('userId')).toBeNull();
        expect(Object.fromEntries(Object.entries(localStorage))).toEqual(shared);
    });

    it('does not let an unidentified guest erase another account', () => {
        publishBob();
        service.clearAll();
        expect(localStorage.getItem('userId')).toBe('bob');
    });

    it('clears the current account hints on a scoped logout', () => {
        service.setUserId('alice');
        service.saveAccountAccess('alice', ['User'], true);
        service.setRemembered(true);
        service.clearAll();
        expect(service.hasPublishedSession()).toBe(false);
        expect(service.isRemembered()).toBe(false);
        expect(sessionStorage.getItem('userId')).toBeNull();
    });

    it.each(['undefined', 'null', ''])('ignores invalid legacy owner metadata (%s)', value => {
        localStorage.setItem('userId', value);
        expect(service.hasPublishedSession()).toBe(false);
    });
});

import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { BrowserStorageService } from './browser-storage.service';

const STORAGE_KEY = 'browser-storage-test';

let service: BrowserStorageService;

beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    TestBed.configureTestingModule({ providers: [BrowserStorageService] });
    service = TestBed.inject(BrowserStorageService);
});

afterEach(() => {
    localStorage.clear();
    sessionStorage.clear();
});

describe('BrowserStorageService string values', () => {
    it('stores, reads, and removes values by scope', () => {
        service.setItem('local', STORAGE_KEY, 'local-value');
        service.setItem('session', STORAGE_KEY, 'session-value');

        expect(service.getItem('local', STORAGE_KEY)).toBe('local-value');
        expect(service.getItem('session', STORAGE_KEY)).toBe('session-value');

        service.removeItem('local', STORAGE_KEY);
        service.removeItem('session', STORAGE_KEY);

        expect(service.getItem('local', STORAGE_KEY)).toBeNull();
        expect(service.getItem('session', STORAGE_KEY)).toBeNull();
    });
});

describe('BrowserStorageService JSON values', () => {
    it('stores and reads JSON values', () => {
        service.setJson('local', STORAGE_KEY, { enabled: true });

        expect(service.getJson('local', STORAGE_KEY)).toEqual({ enabled: true });
    });

    it('clears invalid JSON and returns null', () => {
        localStorage.setItem(STORAGE_KEY, '{invalid');

        expect(service.getJson('local', STORAGE_KEY)).toBeNull();
        expect(localStorage.getItem(STORAGE_KEY)).toBeNull();
    });
});

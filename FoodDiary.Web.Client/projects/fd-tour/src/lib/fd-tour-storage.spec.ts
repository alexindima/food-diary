import { DOCUMENT } from '@angular/common';
import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { FD_TOUR_STORAGE, FdTourLocalStorage } from './fd-tour-storage';

const TOUR_ID = 'dashboard-welcome';
const OTHER_TOUR_ID = 'meal-editor';
const CURRENT_VERSION = 3;
const NEXT_VERSION = 4;
const TOUR_KEY = `fd-tour:${TOUR_ID}`;
const OTHER_TOUR_KEY = `fd-tour:${OTHER_TOUR_ID}`;
type StorageWindow = { readonly localStorage?: Storage };
type StorageDocument = { readonly defaultView: StorageWindow | null };
type StorageMock = Pick<Storage, 'length' | 'key'> & {
    clear: ReturnType<typeof vi.fn<Storage['clear']>>;
    getItem: ReturnType<typeof vi.fn<Storage['getItem']>>;
    setItem: ReturnType<typeof vi.fn<Storage['setItem']>>;
    removeItem: ReturnType<typeof vi.fn<Storage['removeItem']>>;
};

describe('FdTourLocalStorage persistence contract', () => {
    it('persists the version under the existing prefix and reads it after the provider is recreated', () => {
        const storage = createStorageMock();
        const document: StorageDocument = { defaultView: { localStorage: storage } };
        const first = createProvider(document);
        expect(TestBed.inject(FD_TOUR_STORAGE)).toBe(first);
        expect(first.isCompleted(TOUR_ID, CURRENT_VERSION)).toBe(false);

        first.markCompleted(TOUR_ID, CURRENT_VERSION);

        expect(storage.setItem).toHaveBeenCalledExactlyOnceWith(TOUR_KEY, String(CURRENT_VERSION));
        TestBed.resetTestingModule();
        const recreated = createProvider(document);
        expect(recreated).not.toBe(first);
        expect(recreated.isCompleted(TOUR_ID, CURRENT_VERSION)).toBe(true);
        expect(storage.getItem).toHaveBeenLastCalledWith(TOUR_KEY);
    });

    it('requires an exact stored version and replaces the completion when a new version finishes', () => {
        const storage = createStorageMock();
        const provider = createProvider({ defaultView: { localStorage: storage } });
        provider.markCompleted(TOUR_ID, CURRENT_VERSION);
        expect(provider.isCompleted(TOUR_ID, NEXT_VERSION)).toBe(false);
        storage.setItem(TOUR_KEY, `0${CURRENT_VERSION}`);
        expect(provider.isCompleted(TOUR_ID, CURRENT_VERSION)).toBe(false);

        provider.markCompleted(TOUR_ID, NEXT_VERSION);

        expect(provider.isCompleted(TOUR_ID, CURRENT_VERSION)).toBe(false);
        expect(provider.isCompleted(TOUR_ID, NEXT_VERSION)).toBe(true);
        expect(storage.getItem(TOUR_KEY)).toBe(String(NEXT_VERSION));
    });

    it('clears only the requested tour and preserves other tours and application storage', () => {
        const storage = createStorageMock();
        const provider = createProvider({ defaultView: { localStorage: storage } });
        provider.markCompleted(TOUR_ID, CURRENT_VERSION);
        provider.markCompleted(OTHER_TOUR_ID, NEXT_VERSION);
        storage.setItem('application-preference', 'retained');

        provider.clearCompleted(TOUR_ID);

        expect(storage.removeItem).toHaveBeenCalledExactlyOnceWith(TOUR_KEY);
        expect(storage.clear).not.toHaveBeenCalled();
        expect(provider.isCompleted(TOUR_ID, CURRENT_VERSION)).toBe(false);
        expect(provider.isCompleted(OTHER_TOUR_ID, NEXT_VERSION)).toBe(true);
        expect(storage.getItem(OTHER_TOUR_KEY)).toBe(String(NEXT_VERSION));
        expect(storage.getItem('application-preference')).toBe('retained');
    });
});

describe('FdTourLocalStorage platform availability', () => {
    it.each([
        { missing: 'defaultView', document: { defaultView: null } },
        { missing: 'localStorage', document: { defaultView: {} } },
    ] satisfies Array<{ missing: string; document: StorageDocument }>)(
        'treats browser $missing absence as optional persistence',
        ({ document }) => {
            const provider = createProvider(document);

            expect(provider.isCompleted(TOUR_ID, CURRENT_VERSION)).toBe(false);
            expect(() => {
                provider.markCompleted(TOUR_ID, CURRENT_VERSION);
            }).not.toThrow();
            expect(() => {
                provider.clearCompleted(TOUR_ID);
            }).not.toThrow();
        },
    );

    it('never reads the document viewport or localStorage getters on the server', () => {
        const getLocalStorage = vi.fn((): Storage => {
            throw new Error('Server rendering must not access localStorage');
        });
        const getDefaultView = vi.fn((): StorageWindow => ({
            get localStorage(): Storage {
                return getLocalStorage();
            },
        }));
        const provider = createProvider(
            {
                get defaultView(): StorageWindow {
                    return getDefaultView();
                },
            },
            'server',
        );

        expect(provider.isCompleted(TOUR_ID, CURRENT_VERSION)).toBe(false);
        provider.markCompleted(TOUR_ID, CURRENT_VERSION);
        provider.clearCompleted(TOUR_ID);
        expect(getDefaultView).not.toHaveBeenCalled();
        expect(getLocalStorage).not.toHaveBeenCalled();
    });
});

describe('FdTourLocalStorage denied browser access', () => {
    it.each(['read', 'write', 'remove'] as const)('keeps %s optional when the localStorage getter is denied', operation => {
        const getLocalStorage = vi.fn((): Storage => {
            throw new DOMException('Storage is unavailable for this document', 'SecurityError');
        });
        const provider = createProvider({
            defaultView: {
                get localStorage(): Storage {
                    return getLocalStorage();
                },
            },
        });

        switch (operation) {
            case 'read': {
                expect(provider.isCompleted(TOUR_ID, CURRENT_VERSION)).toBe(false);
                break;
            }
            case 'write': {
                expect(() => {
                    provider.markCompleted(TOUR_ID, CURRENT_VERSION);
                }).not.toThrow();
                break;
            }
            case 'remove': {
                expect(() => {
                    provider.clearCompleted(TOUR_ID);
                }).not.toThrow();
                break;
            }
        }
        expect(getLocalStorage).toHaveBeenCalledOnce();
    });
});

describe('FdTourLocalStorage storage operation failures', () => {
    it('returns incomplete after a failed read and reads persisted completion when access recovers', () => {
        const storage = createStorageMock();
        const provider = createProvider({ defaultView: { localStorage: storage } });
        provider.markCompleted(TOUR_ID, CURRENT_VERSION);
        storage.getItem.mockImplementationOnce(() => {
            throw new DOMException('Storage read denied', 'SecurityError');
        });

        expect(provider.isCompleted(TOUR_ID, CURRENT_VERSION)).toBe(false);
        expect(storage.getItem).toHaveBeenCalledExactlyOnceWith(TOUR_KEY);
        expect(provider.isCompleted(TOUR_ID, CURRENT_VERSION)).toBe(true);
    });

    it('contains a failed write and allows a later completion to persist', () => {
        const storage = createStorageMock();
        const provider = createProvider({ defaultView: { localStorage: storage } });
        storage.setItem.mockImplementationOnce(() => {
            throw new DOMException('Storage quota exceeded', 'QuotaExceededError');
        });

        expect(() => {
            provider.markCompleted(TOUR_ID, CURRENT_VERSION);
        }).not.toThrow();
        expect(storage.setItem).toHaveBeenCalledExactlyOnceWith(TOUR_KEY, String(CURRENT_VERSION));
        expect(provider.isCompleted(TOUR_ID, CURRENT_VERSION)).toBe(false);
        provider.markCompleted(TOUR_ID, CURRENT_VERSION);
        expect(provider.isCompleted(TOUR_ID, CURRENT_VERSION)).toBe(true);
    });

    it('contains a failed removal without erasing completion and supports retry', () => {
        const storage = createStorageMock();
        const provider = createProvider({ defaultView: { localStorage: storage } });
        provider.markCompleted(TOUR_ID, CURRENT_VERSION);
        storage.removeItem.mockImplementationOnce(() => {
            throw new DOMException('Storage removal denied', 'SecurityError');
        });

        expect(() => {
            provider.clearCompleted(TOUR_ID);
        }).not.toThrow();
        expect(storage.removeItem).toHaveBeenCalledExactlyOnceWith(TOUR_KEY);
        expect(provider.isCompleted(TOUR_ID, CURRENT_VERSION)).toBe(true);
        provider.clearCompleted(TOUR_ID);
        expect(provider.isCompleted(TOUR_ID, CURRENT_VERSION)).toBe(false);
    });
});

function createProvider(document: StorageDocument, platformId = 'browser'): FdTourLocalStorage {
    TestBed.configureTestingModule({
        providers: [
            { provide: DOCUMENT, useValue: document },
            { provide: PLATFORM_ID, useValue: platformId },
        ],
    });
    return TestBed.inject(FdTourLocalStorage);
}

function createStorageMock(): StorageMock {
    const entries = new Map<string, string>();
    return {
        get length(): number {
            return entries.size;
        },
        clear: vi.fn((): void => {
            entries.clear();
        }),
        getItem: vi.fn((key: string): string | null => entries.get(key) ?? null),
        key: vi.fn((index: number): string | null => [...entries.keys()][index] ?? null),
        setItem: vi.fn((key: string, value: string): void => {
            entries.set(key, value);
        }),
        removeItem: vi.fn((key: string): void => {
            entries.delete(key);
        }),
    };
}

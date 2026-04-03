installWebStorageMock('localStorage');
installWebStorageMock('sessionStorage');

function installWebStorageMock(storageName: 'localStorage' | 'sessionStorage'): void {
    const current = globalThis[storageName];
    if (isStorageLike(current)) {
        return;
    }

    const storage = createMockStorage();

    Object.defineProperty(globalThis, storageName, {
        value: storage,
        configurable: true,
    });

    if (typeof window !== 'undefined') {
        Object.defineProperty(window, storageName, {
            value: storage,
            configurable: true,
        });
    }
}

function isStorageLike(value: unknown): value is Storage {
    return Boolean(
        value &&
            typeof value === 'object' &&
            typeof (value as Storage).getItem === 'function' &&
            typeof (value as Storage).setItem === 'function' &&
            typeof (value as Storage).removeItem === 'function' &&
            typeof (value as Storage).clear === 'function',
    );
}

function createMockStorage(): Storage {
    const state = new Map<string, string>();

    return {
        get length(): number {
            return state.size;
        },
        clear(): void {
            state.clear();
        },
        getItem(key: string): string | null {
            return state.get(key) ?? null;
        },
        key(index: number): string | null {
            return Array.from(state.keys())[index] ?? null;
        },
        removeItem(key: string): void {
            state.delete(key);
        },
        setItem(key: string, value: string): void {
            state.set(key, value);
        },
    };
}

installWebStorageMock('localStorage');
installWebStorageMock('sessionStorage');
installCssParseWarningFilter();
installCssParseStderrFilter();

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

function installCssParseWarningFilter(): void {
    const ignoredMessage = 'Could not parse CSS stylesheet';
    const originalConsoleError = console.error.bind(console);
    const originalConsoleWarn = console.warn.bind(console);

    console.error = (...args: unknown[]): void => {
        if (shouldIgnoreCssParseWarning(args, ignoredMessage)) {
            return;
        }

        originalConsoleError(...args);
    };

    console.warn = (...args: unknown[]): void => {
        if (shouldIgnoreCssParseWarning(args, ignoredMessage)) {
            return;
        }

        originalConsoleWarn(...args);
    };
}

function shouldIgnoreCssParseWarning(args: unknown[], ignoredMessage: string): boolean {
    return args.some(arg => {
        if (typeof arg === 'string') {
            return arg.includes(ignoredMessage);
        }

        if (arg instanceof Error) {
            return arg.message.includes(ignoredMessage);
        }

        if (arg && typeof arg === 'object' && 'message' in arg) {
            const message = (arg as { message?: unknown }).message;
            return typeof message === 'string' && message.includes(ignoredMessage);
        }

        return false;
    });
}

function installCssParseStderrFilter(): void {
    const ignoredMessage = 'Could not parse CSS stylesheet';
    const processRef = (
        globalThis as typeof globalThis & {
            process?: {
                stderr?: {
                    write?: (...args: unknown[]) => unknown;
                };
            };
        }
    ).process;

    const stderr = processRef?.stderr;
    if (!stderr?.write) {
        return;
    }

    const originalWrite = stderr.write.bind(stderr) as (...args: unknown[]) => unknown;
    const decoder = new TextDecoder();

    const filteredWrite = (...args: unknown[]): boolean => {
        const firstArg = args[0];
        const text = typeof firstArg === 'string' ? firstArg : firstArg instanceof Uint8Array ? decoder.decode(firstArg) : '';

        if (text.includes(ignoredMessage)) {
            const maybeCallback = args.find(arg => typeof arg === 'function') as ((error?: Error | null) => void) | undefined;
            maybeCallback?.();
            return true;
        }

        return Boolean(originalWrite(...args));
    };

    stderr.write = filteredWrite as typeof stderr.write;
}

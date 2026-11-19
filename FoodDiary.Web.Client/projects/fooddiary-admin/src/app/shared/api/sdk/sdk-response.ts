export type RequiredSdkFields<T, K extends keyof T> = T & { [P in K]-?: NonNullable<T[P]> };

/** Assert the application's required fields without inventing missing values. */
function assertSdkFields<T extends object, K extends keyof T>(value: T, keys: readonly K[]): asserts value is RequiredSdkFields<T, K> {
    for (const key of keys) {
        if (value[key] === undefined || value[key] === null) {
            throw new Error(`API response is missing required field: ${String(key)}.`);
        }
    }
}

export function requireSdkFields<T extends object, K extends keyof T>(value: T, keys: readonly K[]): RequiredSdkFields<T, K> {
    assertSdkFields(value, keys);
    return value;
}

export function sdkEnum<T extends string | number>(value: string | number, values: readonly T[], fallback?: T): T {
    const result = values.find(item => item === value);
    if (result !== undefined) {
        return result;
    }
    if (fallback !== undefined) {
        return fallback;
    }
    throw new Error('API response contains an unsupported enum value.');
}

/** An instant remains an instant; calendar-date strings are never passed here. */
export function sdkInstant(value: string | Date): Date {
    return value instanceof Date ? value : new Date(value);
}

/** Preserve nullable/204 results even when the current schema omits nullability. */
export function sdkOptional<T, U>(value: T | null | undefined, mapper: (value: T) => U): U | null {
    return value === null || value === undefined ? null : mapper(value);
}

/** Preserve the difference between an omitted optional value and explicit null. */
export function sdkMaybe<T, U>(value: T | null | undefined, mapper: (value: T) => U): U | null | undefined {
    if (value === null) {
        return null;
    }
    return value === undefined ? undefined : mapper(value);
}

type NormalizedSdkFields<T, K extends keyof T, Absent> = Omit<T, K> & { [P in K]-?: Exclude<T[P], null | undefined> | Absent };
export type NullableSdkFields<T, K extends keyof T> = NormalizedSdkFields<T, K, null>;
export type DefinedSdkFields<T, K extends keyof T> = NormalizedSdkFields<T, K, undefined>;

function normalizeSdkFields<T extends object, K extends keyof T, Absent extends null | undefined>(
    value: T,
    keys: readonly K[],
    absent: Absent,
): NormalizedSdkFields<T, K, Absent>;
// eslint-disable-next-line no-redeclare -- Every selected key is written as a concrete property with the requested fallback.
function normalizeSdkFields<T extends object, K extends keyof T>(value: T, keys: readonly K[], absent: null | undefined): object {
    const result = { ...value };
    for (const key of keys) {
        Object.defineProperty(result, key, { value: value[key] ?? absent, enumerable: true, configurable: true, writable: true });
    }
    return result;
}

/** Application optional fields use undefined for absence, preserving zero/false. */
export function sdkDefinedFields<T extends object, K extends keyof T>(value: T, keys: readonly K[]): DefinedSdkFields<T, K> {
    return normalizeSdkFields(value, keys, undefined);
}

/** Normalize only fields whose application contract explicitly allows null. */
export function sdkNullableFields<T extends object, K extends keyof T>(value: T, keys: readonly K[]): NullableSdkFields<T, K> {
    return normalizeSdkFields(value, keys, null);
}

export function sdkPage<T, U>(
    response: { data?: T[] | null; page?: number; limit?: number; totalPages?: number; totalItems?: number },
    mapper: (value: T) => U,
): { data: U[]; page: number; limit: number; totalPages: number; totalItems: number } {
    const value = requireSdkFields(response, ['data', 'page', 'limit', 'totalPages', 'totalItems']);
    return { ...value, data: value.data.map(mapper) };
}

export function sdkItemsPage<T, U>(
    response: { data?: T[] | null; page?: number; limit?: number; totalPages?: number; totalItems?: number },
    mapper: (value: T) => U,
): { items: U[]; page: number; limit: number; totalPages: number; totalItems: number } {
    const { data, ...pagination } = sdkPage(response, mapper);
    return { ...pagination, items: data };
}

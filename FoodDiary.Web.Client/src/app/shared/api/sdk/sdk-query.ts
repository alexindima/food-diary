/** Match the adapters' existing omission of empty optional strings. */
export function sdkQueryString(value: string | null | undefined): string | undefined {
    return value === null || value === undefined || value.length === 0 ? undefined : value;
}

export function sdkQueryValue<T>(value: T | null | undefined): T | undefined {
    return value ?? undefined;
}

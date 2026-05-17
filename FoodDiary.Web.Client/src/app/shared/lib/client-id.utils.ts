const RANDOM_ID_RADIX = 36;
const RANDOM_ID_SLICE_START = 2;

export function createClientId(prefix: string): string {
    const cryptoLike = (globalThis as { crypto?: { randomUUID?: () => string } }).crypto;
    return `${prefix}-${cryptoLike?.randomUUID?.() ?? Math.random().toString(RANDOM_ID_RADIX).slice(RANDOM_ID_SLICE_START)}`;
}

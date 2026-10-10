export const HTTP_OK = 200;
export const HTTP_CREATED = 201;
export const HTTP_NO_CONTENT = 204;
export const HTTP_BAD_REQUEST = 400;

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
}

export function wireRecord(value: unknown): Record<string, unknown> {
    if (!isRecord(value)) {
        throw new Error('Expected a wire object');
    }
    return value;
}

export function wireRecords(value: unknown): Array<Record<string, unknown>> {
    if (!Array.isArray(value) || !value.every(isRecord)) {
        throw new Error('Expected wire objects');
    }
    return value;
}

export function wireString(value: unknown): string {
    if (typeof value !== 'string' || value.length === 0) {
        throw new Error('Expected a nonempty wire string');
    }
    return value;
}

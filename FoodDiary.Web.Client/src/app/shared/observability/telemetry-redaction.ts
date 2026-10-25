const REDACTED = '[redacted]';
const MAX_TEXT_LENGTH = 1024;
const MAX_FIELDS = 16;
const MAX_DEPTH = 4;
const MAX_DETAIL_CHARACTERS = 2048;
const MAX_DETAIL_STRING_LENGTH = 512;
const MAX_DETAILS_SERIALIZED_LENGTH = 4096;
const MAX_TOTAL_FIELDS = 40;
const MAX_KEY_LENGTH = 64;
type RedactionBudget = { characters: number; fields: number };
const PRIVATE_FIELD = /password|token|authorization|cookie|email|(?:user|meal|recipe|product)id/iu;
const UUID = /\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b/giu;

/** Keep bounded diagnostic text, never URL credentials, query values or obvious PII. */
export function redactTelemetryText(text: string): string {
    return text
        .replaceAll(/https?:\/\/[^\s"'<>]+/giu, value => {
            try {
                return new URL(value).pathname;
            } catch {
                return REDACTED;
            }
        })
        .replaceAll(/\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b/giu, REDACTED)
        .replaceAll(/\bBearer\s+[^\s,;]+/giu, `Bearer ${REDACTED}`)
        .replaceAll(/\beyJ[\w-]+\.[\w-]+\.[\w-]+\b/gu, REDACTED)
        .replaceAll(UUID, ':id')
        .slice(0, MAX_TEXT_LENGTH);
}

export function redactTelemetryDetails(details: Record<string, unknown>): Record<string, unknown> {
    const bounded = redactRecord(details, 0, { characters: MAX_DETAIL_CHARACTERS, fields: MAX_TOTAL_FIELDS });
    return JSON.stringify(bounded).length <= MAX_DETAILS_SERIALIZED_LENGTH ? bounded : { truncated: true };
}

function redactRecord(record: Record<string, unknown>, depth: number, budget: RedactionBudget): Record<string, unknown> {
    const entries: Array<[string, unknown]> = [];
    for (const [key, value] of Object.entries(record).slice(0, MAX_FIELDS)) {
        if (budget.fields <= 0) {
            break;
        }
        budget.fields--;
        entries.push([key.slice(0, MAX_KEY_LENGTH), PRIVATE_FIELD.test(key) ? REDACTED : redactValue(value, depth + 1, budget)]);
    }
    return Object.fromEntries(entries);
}

function redactValue(value: unknown, depth: number, budget: RedactionBudget): unknown {
    if (depth >= MAX_DEPTH) {
        return '[truncated]';
    }
    if (typeof value === 'string') {
        const text = redactTelemetryText(value).slice(0, Math.min(budget.characters, MAX_DETAIL_STRING_LENGTH));
        budget.characters -= text.length;
        return text;
    }
    if (Array.isArray(value)) {
        const items: unknown[] = [];
        for (const item of value.slice(0, MAX_FIELDS)) {
            if (budget.fields <= 0) {
                break;
            }
            budget.fields--;
            items.push(redactValue(item, depth + 1, budget));
        }
        return items;
    }
    if (isRecord(value)) {
        return redactRecord(value, depth, budget);
    }
    return typeof value === 'boolean' || typeof value === 'number' || value === null ? value : undefined;
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
}

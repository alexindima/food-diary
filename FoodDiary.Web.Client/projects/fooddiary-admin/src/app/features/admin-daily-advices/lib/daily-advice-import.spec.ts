import { describe, expect, it } from 'vitest';

import { DAILY_ADVICE_IMPORT_EXAMPLE, isDailyAdviceImport } from './daily-advice-import';

const OVERLONG_TEXT = 513;
const OVERLONG_TAG = 65;

describe('daily advice JSON import', () => {
    it('accepts the example and optional defaults', () => {
        expect(isDailyAdviceImport(DAILY_ADVICE_IMPORT_EXAMPLE)).toBe(true);
        expect(isDailyAdviceImport({ version: 1, advices: [{ value: 'Advice', locale: 'en-US' }] })).toBe(true);
    });
    it('rejects malformed envelopes and invalid entries', () => {
        for (const invalid of [null, [], {}, { version: 2, advices: DAILY_ADVICE_IMPORT_EXAMPLE.advices }, { version: 1, advices: [] }]) {
            expect(isDailyAdviceImport(invalid)).toBe(false);
        }
        for (const item of [
            null,
            {},
            { value: '', locale: 'ru' },
            { value: 'Advice', locale: 'de' },
            { value: 'Advice', locale: 'en', weight: 0 },
            { value: 'Advice', locale: 'en', weight: 1.5 },
            { value: 'x'.repeat(OVERLONG_TEXT), locale: 'en' },
            { value: 'Advice', locale: 'en', tag: 'x'.repeat(OVERLONG_TAG) },
        ]) {
            expect(isDailyAdviceImport({ version: 1, advices: [item] })).toBe(false);
        }
        expect(isDailyAdviceImport({ version: 1, advices: Array.from({ length: 1001 }, () => ({ value: 'Advice', locale: 'en' })) })).toBe(
            false,
        );
    });
});

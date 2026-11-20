import { describe, expect, it } from 'vitest';

import { devDay, devQuery, devText } from './development-input';

const TEXT_LIMIT = 32;
const MAX_FILTER_LENGTH = 200;

describe('Local development text and calendar inputs', () => {
    it('rejects whitespace for required text and preserves an explicitly empty optional field', () => {
        expect(devText('', TEXT_LIMIT, true)).toBe('');
        expect(() => devText('   ', TEXT_LIMIT)).toThrow('Invalid text value.');
    });

    it('accepts a real leap day without changing the selected calendar date', () => {
        expect(devDay('2024-02-29')).toBe('2024-02-29');
    });

    it.each(['2024/02/29', '2023-02-29'])('rejects an unsupported calendar day %s', value => {
        expect(() => devDay(value)).toThrow('Expected a valid calendar date YYYY-MM-DD.');
    });
});

describe('Local development catalog filters', () => {
    const filterKeys = ['search', 'hasImage', 'caloriesFrom', 'maxTotalTime'] as const;

    it('omits a cleared filter while preserving explicit false, zero, and text', () => {
        expect(devQuery({ search: 'Soup', hasImage: false, caloriesFrom: 0, maxTotalTime: null }, filterKeys)).toEqual({
            search: 'Soup',
            hasImage: 'false',
            caloriesFrom: '0',
        });
    });

    it.each([
        { label: 'an array', raw: ['Soup'] },
        { label: 'an object', raw: { min: 20 } },
        { label: 'a non-finite number', raw: Number.POSITIVE_INFINITY },
    ])('rejects $label as a catalog filter', ({ raw }) => {
        expect(() => devQuery({ maxTotalTime: raw }, filterKeys)).toThrow('Filters must contain finite scalar values or null.');
    });

    it('accepts the maximum filter length and rejects a longer value', () => {
        const search = 'x'.repeat(MAX_FILTER_LENGTH);
        expect(devQuery({ search }, filterKeys)).toEqual({ search });
        expect(() => devQuery({ search: 'x'.repeat(MAX_FILTER_LENGTH + 1) }, filterKeys)).toThrow('Filter value is too long.');
    });
});

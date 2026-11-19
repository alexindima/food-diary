import { describe, expect, it } from 'vitest';

import { fdUiFormatDateInputValue, fdUiParseLocalDate, fdUiParseLocalDateTime } from './fd-ui-date.utils';

describe('UI kit local date parsers', () => {
    it.each([null, undefined, '', 'not a date', new Date(Number.NaN)])('rejects an absent or invalid value %s', value => {
        expect(fdUiParseLocalDate(value)).toBeNull();
        expect(fdUiParseLocalDateTime(value)).toBeNull();
    });

    it('keeps local date fields and drops the time only when selecting a day', () => {
        const local = new Date('2024-02-29T17:45:00');

        expect(fdUiParseLocalDate(local)).toEqual(new Date('2024-02-29T00:00:00'));
        expect(fdUiParseLocalDateTime(local)).toBe(local);
    });

    it('keeps the lexical day and time when a form sends ISO text with an offset', () => {
        const wire = '2024-02-29T23:45:00+05:00';

        expect(fdUiParseLocalDate(wire)).toEqual(new Date('2024-02-29T00:00:00'));
        expect(fdUiParseLocalDateTime(wire)).toEqual(new Date('2024-02-29T23:45:00'));
    });

    it('normalizes the local day for a legacy GMT timestamp while retaining its instant for date time', () => {
        const timestamp = 'Thu, 29 Feb 2024 17:45:00 GMT';
        const instant = new Date('2024-02-29T17:45:00Z');
        const localDay = fdUiParseLocalDate(timestamp);
        if (localDay === null) {
            throw new Error('Expected the legacy timestamp to be accepted');
        }

        expect(fdUiFormatDateInputValue(localDay)).toBe(fdUiFormatDateInputValue(instant));
        expect(localDay.getHours()).toBe(0);
        expect(localDay.getMinutes()).toBe(0);
        expect(fdUiParseLocalDateTime(timestamp)).toEqual(instant);
    });
});

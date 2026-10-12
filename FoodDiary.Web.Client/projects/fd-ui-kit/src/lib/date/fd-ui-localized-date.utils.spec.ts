import { describe, expect, it } from 'vitest';

import { fdUiLocalizedDateHint, fdUiParseLocalizedDate } from './fd-ui-localized-date.utils';

describe('localized calendar input', () => {
    it.each([
        ['ru', '29.02.2024', 'DD.MM.YYYY'],
        ['en', '02/29/2024', 'MM/DD/YYYY'],
    ])('uses the %s application format', (locale, value, hint) => {
        expect(fdUiLocalizedDateHint(locale)).toBe(hint);
        expect(fdUiParseLocalizedDate(value, locale)).toEqual(new Date('2024-02-29T00:00:00'));
    });
    it.each(['29.02.2023', '31.04.1990', '15.13.1990', '15.06.90', 'abc', '15.06', ''])('rejects %s without rolling the day', value => {
        expect(fdUiParseLocalizedDate(value, 'ru')).toBeNull();
    });
});

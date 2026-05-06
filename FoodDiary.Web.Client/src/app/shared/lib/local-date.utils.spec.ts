import { describe, expect, it } from 'vitest';

import { normalizeEndOfLocalDay, normalizeStartOfLocalDay, toLocalDayEndIso, toLocalDayStartIso } from './local-date.utils';

describe('local-date.utils', () => {
    it('normalizes dates to local day boundaries', () => {
        const date = new Date(2026, 4, 5, 14, 30, 25, 123);

        expect(normalizeStartOfLocalDay(date)).toEqual(new Date(2026, 4, 5, 0, 0, 0, 0));
        expect(normalizeEndOfLocalDay(date)).toEqual(new Date(2026, 4, 5, 23, 59, 59, 999));
    });

    it('returns ISO values for local day boundaries', () => {
        const date = new Date(2026, 4, 5, 14, 30, 25, 123);

        expect(toLocalDayStartIso(date)).toBe(new Date(2026, 4, 5, 0, 0, 0, 0).toISOString());
        expect(toLocalDayEndIso(date)).toBe(new Date(2026, 4, 5, 23, 59, 59, 999).toISOString());
    });

    it('keeps empty optional dates undefined', () => {
        expect(toLocalDayStartIso(null)).toBeUndefined();
        expect(toLocalDayEndIso(undefined)).toBeUndefined();
    });
});

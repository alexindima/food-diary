import { describe, expect, it } from 'vitest';

import { formatPremiumMediumDate } from './premium-access-date.utils';

describe('formatPremiumMediumDate', () => {
    it('formats valid dates with provided locale', () => {
        expect(formatPremiumMediumDate('2026-05-15T00:00:00Z', 'en-US')).toBe('May 15, 2026');
    });

    it('returns null for empty or invalid dates', () => {
        expect(formatPremiumMediumDate(null, 'en-US')).toBeNull();
        expect(formatPremiumMediumDate('', 'en-US')).toBeNull();
        expect(formatPremiumMediumDate('not-a-date', 'en-US')).toBeNull();
    });
});

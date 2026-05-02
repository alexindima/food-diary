import { describe, expect, it } from 'vitest';

import { normalizeMealType, resolveMealTypeByTime } from './meal-type.util';

describe('meal type utils', () => {
    it('normalizes API enum casing to select option values', () => {
        expect(normalizeMealType('Breakfast')).toBe('BREAKFAST');
        expect(normalizeMealType('lunch')).toBe('LUNCH');
        expect(normalizeMealType('DINNER')).toBe('DINNER');
        expect(normalizeMealType('unknown')).toBeNull();
    });

    it('resolves meal type from local time buckets', () => {
        expect(resolveMealTypeByTime(new Date('2026-05-02T07:30:00'))).toBe('BREAKFAST');
        expect(resolveMealTypeByTime(new Date('2026-05-02T12:00:00'))).toBe('LUNCH');
        expect(resolveMealTypeByTime(new Date('2026-05-02T19:00:00'))).toBe('DINNER');
        expect(resolveMealTypeByTime(new Date('2026-05-02T23:50:00'))).toBe('SNACK');
    });
});

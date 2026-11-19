import { describe, expect, expectTypeOf, it } from 'vitest';

import { type AdminCalendarDate, type AdminUtcInstant, optionalAdminCalendarDate } from './admin-meaning';

describe('optional Admin calendar dates', () => {
    it('keeps a leap-day date in its wire format and preserves the null and absent values', () => {
        const leapDay = optionalAdminCalendarDate('2024-02-29');

        expect(leapDay).toBe('2024-02-29');
        expectTypeOf(leapDay).toEqualTypeOf<AdminCalendarDate | null>();
        expectTypeOf<NonNullable<typeof leapDay>>().not.toExtend<AdminUtcInstant>();
        expect(optionalAdminCalendarDate(null)).toBeNull();
        expect(optionalAdminCalendarDate(undefined)).toBeUndefined();
    });
});

import { describe, expect, it } from 'vitest';

import { getDateInputValue, getTimeInputValue } from './meal-date-input.utils';

const YEAR = 2026;
const MAY_MONTH_INDEX = 4;
const DAY = 9;
const HOURS = 7;
const MINUTES = 5;

describe('meal date input utils', () => {
    it('should format date for native date input', () => {
        expect(getDateInputValue(new Date(YEAR, MAY_MONTH_INDEX, DAY, HOURS, MINUTES))).toBe('2026-05-09');
    });

    it('should format time for native time input', () => {
        expect(getTimeInputValue(new Date(YEAR, MAY_MONTH_INDEX, DAY, HOURS, MINUTES))).toBe('07:05');
    });
});

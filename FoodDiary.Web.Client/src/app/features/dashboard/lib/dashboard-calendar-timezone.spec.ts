import { describe, expect, it } from 'vitest';

import { formatDateInputValue } from '../../../shared/lib/local-date.utils';
import { getDashboardDateUtc, getHydrationDateUtc, getWeightTrendRange, parseDashboardDate } from './dashboard-date.utils';

const dates = [
    '2024-02-29',
    '2026-01-01',
    '2026-03-08',
    '2026-03-29',
    '2026-04-05',
    '2026-09-20',
    '2026-10-04',
    '2026-10-25',
    '2026-11-01',
    '2026-12-31',
];
const times = ['00:01:00', '01:00:00', '12:00:00', '23:59:00'];
const trendDays = 30;
const noon = 12;

describe('dashboard in the actual host time zone', () => {
    it.each(dates)('preserves the selected day %s in requests and hydration writes', day => {
        const selected = parseDashboardDate(day);
        if (selected === null) {
            throw new Error('Expected a valid date');
        }
        expect(getDashboardDateUtc(selected).toISOString()).toBe(`${day}T00:00:00.000Z`);
        const nextDay = new Date(selected);
        nextDay.setDate(nextDay.getDate() + 1);
        for (const time of times) {
            const now = new Date(`${day}T${time}`);
            const water = getHydrationDateUtc(selected, now);
            expect(water.getTime()).toBe(now.getTime());
            expect(water.getTime()).toBeGreaterThanOrEqual(selected.getTime());
            expect(water.getTime()).toBeLessThan(nextDay.getTime());
            expect(formatDateInputValue(water)).toBe(day);
        }
        const historicalWater = getHydrationDateUtc(selected, new Date('2030-01-01T12:00:00'));
        expect(historicalWater.getHours()).toBe(noon);
        expect(formatDateInputValue(historicalWater)).toBe(day);
        expect(historicalWater.getTime()).toBeGreaterThanOrEqual(selected.getTime());
        expect(historicalWater.getTime()).toBeLessThan(nextDay.getTime());
        expect(getWeightTrendRange(selected, trendDays).end.toISOString()).toBe(`${day}T23:59:59.999Z`);
    });
});

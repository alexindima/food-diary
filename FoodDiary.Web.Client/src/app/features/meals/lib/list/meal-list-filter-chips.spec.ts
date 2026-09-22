import { describe, expect, it } from 'vitest';

import { emptyMealFilters, mealFilterChips, removeMealFilter } from './meal-list-filter-chips';

const TEST_YEAR = 2026;

describe('meal filter chips', () => {
    it('represents each constraint including zero and false independently', () => {
        const filters = {
            ...emptyMealFilters(),
            dateRange: { start: new Date(TEST_YEAR, 0, 1), end: new Date(TEST_YEAR, 0, 2) },
            mealTypes: ['Breakfast', 'Dinner'],
            caloriesFrom: 0,
            caloriesTo: 1000,
            hasImage: false,
            hasAiSession: true,
        };
        const chips = mealFilterChips(filters, key => key, 'ru-RU');
        expect(chips.map(chip => chip.id)).toEqual([
            'start',
            'end',
            'mealType:Breakfast',
            'mealType:Dinner',
            'caloriesFrom',
            'caloriesTo',
            'hasImage',
            'hasAiSession',
        ]);
        expect(chips.find(chip => chip.id === 'caloriesFrom')?.label).toContain('0');
        expect(chips.find(chip => chip.id === 'hasImage')?.label).toBe('MEAL_LIST.FILTER_IMAGE_WITHOUT');
        expect(chips[0].label).toContain('2026-01-01');
        for (const chip of chips) {
            expect(mealFilterChips(removeMealFilter(filters, chip.id), key => key, 'en-US').map(item => item.id)).toEqual(
                chips.filter(item => item.id !== chip.id).map(item => item.id),
            );
        }
        expect(filters.mealTypes).toEqual(['Breakfast', 'Dinner']);
        expect(filters.dateRange.start).toEqual(new Date(TEST_YEAR, 0, 1));
    });
    it('removing a date boundary preserves the opposite boundary', () => {
        const end = new Date(TEST_YEAR, 0, 2);
        const filters = { ...emptyMealFilters(), dateRange: { start: null, end } };
        expect(removeMealFilter(filters, 'start').dateRange).toEqual({ start: null, end });
        expect(removeMealFilter(emptyMealFilters(), 'end').dateRange).toEqual({ start: null, end: null });
        expect(removeMealFilter(filters, 'unknown')).toBe(filters);
    });
    it('reset returns an empty independent model', () => {
        expect(mealFilterChips(emptyMealFilters(), key => key, 'ru-RU')).toEqual([]);
        expect(emptyMealFilters()).not.toBe(emptyMealFilters());
    });
});

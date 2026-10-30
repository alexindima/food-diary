import { describe, expect, it } from 'vitest';

import { recipeFilterChips, removeRecipeFilter, resetRecipeFilters } from './recipe-list-filter-chips';
import type { RecipeListQuery } from './recipe-list-query';

const query: RecipeListQuery = {
    page: 2,
    search: 'Rice',
    onlyMine: true,
    category: 'soups',
    maxTotalTime: 30,
    caloriesFrom: 0,
    caloriesTo: 500,
    hasImage: false,
};

describe('recipe filter chips', () => {
    it('shows the actual category, zero lower bound, time and no-photo choice', () => {
        const labels = Object.fromEntries(recipeFilterChips(query, key => key, 'en').map(chip => [chip.id, chip.label]));
        expect(labels).toEqual({
            onlyMine: 'RECIPE_LIST.FILTER_MY_RECIPES',
            category: 'RECIPE_CATEGORIES.soups',
            maxTotalTime: 'RECIPE_LIST.FILTER_MAX_TOTAL_TIME: 30',
            caloriesFrom: 'RECIPE_LIST.FILTER_CALORIES_FROM: 0',
            caloriesTo: 'RECIPE_LIST.FILTER_CALORIES_TO: 500',
            hasImage: 'RECIPE_LIST.FILTER_IMAGE_WITHOUT',
        });
    });

    it('removes one bound, preserving the other filters and search, and returns to page one', () => {
        expect(removeRecipeFilter(query, 'caloriesFrom')).toEqual({ ...query, page: 1, caloriesFrom: null });
        expect(query.caloriesFrom).toBe(0);
        expect(removeRecipeFilter(query, 'onlyMine')).toEqual({ ...query, page: 1, onlyMine: false });
        expect(removeRecipeFilter(query, 'unknown')).toBe(query);
    });

    it('clears all structured filters while preserving search', () => {
        const reset = resetRecipeFilters(query);
        expect(reset).toEqual({
            page: 1,
            search: 'Rice',
            onlyMine: false,
            category: null,
            maxTotalTime: null,
            caloriesFrom: null,
            caloriesTo: null,
            hasImage: null,
        });
        expect(recipeFilterChips(reset, key => key, 'ru')).toEqual([]);
    });
});

import { convertToParamMap } from '@angular/router';
import { describe, expect, it } from 'vitest';

import { readRecipeListQuery, recipeQueryFilters, writeRecipeListQuery } from './recipe-list-query';

describe('Recipe list bookmarks', () => {
    it('preserves all transport filters including false and zero', () => {
        const params = {
            page: '2',
            search: '  Rice  ',
            onlyMine: 'false',
            category: 'soups',
            maxTotalTime: '0',
            caloriesFrom: '0',
            caloriesTo: '1000',
            hasImage: 'false',
        };
        const query = readRecipeListQuery(convertToParamMap(params));
        expect(query.page).toBe(2);
        expect(query.onlyMine).toBe(false);
        expect(recipeQueryFilters(query)).toEqual({
            search: 'Rice',
            category: 'soups',
            maxTotalTime: 0,
            caloriesFrom: 0,
            caloriesTo: 1000,
            hasImage: false,
        });
        expect(writeRecipeListQuery(query)).toEqual({ ...params, search: 'Rice' });
    });

    it('keeps the owned collection default and rejects unsupported page and filter values', () => {
        const query = readRecipeListQuery(
            convertToParamMap({ page: '999999', category: 'invalid', maxTotalTime: '-1', caloriesFrom: 'Infinity', hasImage: 'invalid' }),
        );
        expect(query).toEqual({
            page: 1,
            search: null,
            onlyMine: true,
            category: null,
            maxTotalTime: null,
            caloriesFrom: null,
            caloriesTo: null,
            hasImage: null,
        });
        expect(writeRecipeListQuery(query)['page']).toBeNull();
        expect(writeRecipeListQuery(query)['onlyMine']).toBeNull();
    });
});

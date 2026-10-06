import type { ParamMap } from '@angular/router';

import type { RecipeFilters } from '../../../../shared/models/recipe.data';
import { isRecipeCategory } from '../../../../shared/models/recipe-category';
import { readPaginationPage } from '../../../../shared/navigation/pagination-query.utils';

export type RecipeListQuery = {
    page: number;
    search: string | null;
    onlyMine: boolean;
    category: string | null;
    maxTotalTime: number | null;
    caloriesFrom: number | null;
    caloriesTo: number | null;
    hasImage: boolean | null;
};

export function readRecipeListQuery(params: ParamMap): RecipeListQuery {
    const category = params.get('category');
    const hasImage = params.get('hasImage');
    return {
        page: readPaginationPage(params.get('page')),
        search: normalizeRecipeSearch(params.get('search')),
        onlyMine: params.get('onlyMine') !== 'false',
        category: isRecipeCategory(category) ? category : null,
        maxTotalTime: readNumber(params.get('maxTotalTime'), true),
        caloriesFrom: readNumber(params.get('caloriesFrom')),
        caloriesTo: readNumber(params.get('caloriesTo')),
        hasImage: hasImage === 'true' || hasImage === 'false' ? hasImage === 'true' : null,
    };
}

export function writeRecipeListQuery(query: RecipeListQuery): Record<string, string | null> {
    return {
        page: query.page === 1 ? null : String(query.page),
        search: normalizeRecipeSearch(query.search),
        onlyMine: query.onlyMine ? null : 'false',
        category: query.category,
        maxTotalTime: query.maxTotalTime === null ? null : String(query.maxTotalTime),
        caloriesFrom: query.caloriesFrom === null ? null : String(query.caloriesFrom),
        caloriesTo: query.caloriesTo === null ? null : String(query.caloriesTo),
        hasImage: query.hasImage === null ? null : String(query.hasImage),
    };
}

export function recipeListQueryKey(query: RecipeListQuery): string {
    return JSON.stringify(writeRecipeListQuery(query));
}

export function recipeQueryFilters(query: RecipeListQuery): RecipeFilters {
    return {
        search: query.search,
        category: query.category ?? undefined,
        maxTotalTime: query.maxTotalTime ?? undefined,
        caloriesFrom: query.caloriesFrom ?? undefined,
        caloriesTo: query.caloriesTo ?? undefined,
        hasImage: query.hasImage ?? undefined,
    };
}

export function createRecipeListQuery(page: number, filters: RecipeFilters, onlyMine: boolean): RecipeListQuery {
    return {
        page,
        search: normalizeRecipeSearch(filters.search ?? null),
        onlyMine,
        category: filters.category ?? null,
        maxTotalTime: filters.maxTotalTime ?? null,
        caloriesFrom: filters.caloriesFrom ?? null,
        caloriesTo: filters.caloriesTo ?? null,
        hasImage: filters.hasImage ?? null,
    };
}

function normalizeRecipeSearch(value: string | null): string | null {
    const normalized = value?.trim() ?? '';
    return normalized.length > 0 ? normalized : null;
}

function readNumber(value: string | null, integer = false): number | null {
    if (value === null || value.trim().length === 0) {
        return null;
    }
    const number = Number(value);
    return Number.isFinite(number) && number >= 0 && (!integer || Number.isSafeInteger(number)) ? number : null;
}

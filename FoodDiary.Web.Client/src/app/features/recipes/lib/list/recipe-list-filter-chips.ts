import { recipeCategoryKey } from '../../../../shared/models/recipe-category';
import type { ActiveFilterChip } from '../../../../shared/ui/filters/active-filter-chip';
import type { RecipeListQuery } from './recipe-list-query';

export function recipeFilterChips(model: RecipeListQuery, translate: (key: string) => string, locale: string): ActiveFilterChip[] {
    const chips: ActiveFilterChip[] = [];
    const number = new Intl.NumberFormat(locale);
    if (model.onlyMine) {
        chips.push({ id: 'onlyMine', label: translate('RECIPE_LIST.FILTER_MY_RECIPES') });
    }
    if (model.category !== null) {
        chips.push({ id: 'category', label: translate(recipeCategoryKey(model.category)) });
    }
    for (const [id, key] of [
        ['maxTotalTime', 'RECIPE_LIST.FILTER_MAX_TOTAL_TIME'],
        ['caloriesFrom', 'RECIPE_LIST.FILTER_CALORIES_FROM'],
        ['caloriesTo', 'RECIPE_LIST.FILTER_CALORIES_TO'],
    ] as const) {
        const value = model[id];
        if (value !== null) {
            chips.push({ id, label: `${translate(key)}: ${number.format(value)}` });
        }
    }
    if (model.hasImage !== null) {
        chips.push({
            id: 'hasImage',
            label: translate(model.hasImage ? 'RECIPE_LIST.FILTER_IMAGE_WITH' : 'RECIPE_LIST.FILTER_IMAGE_WITHOUT'),
        });
    }
    return chips;
}

export function removeRecipeFilter(model: RecipeListQuery, id: string): RecipeListQuery {
    if (id === 'onlyMine') {
        return { ...model, page: 1, onlyMine: false };
    }
    if (['category', 'maxTotalTime', 'caloriesFrom', 'caloriesTo', 'hasImage'].includes(id)) {
        return { ...model, page: 1, [id]: null };
    }
    return model;
}

export function resetRecipeFilters(model: RecipeListQuery): RecipeListQuery {
    return { ...model, page: 1, onlyMine: false, category: null, maxTotalTime: null, caloriesFrom: null, caloriesTo: null, hasImage: null };
}

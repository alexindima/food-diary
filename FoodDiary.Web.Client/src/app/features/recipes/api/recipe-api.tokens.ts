import { InjectionToken } from '@angular/core';

const DEFAULT_RECIPE_RECENT_LIMIT = 10;
const DEFAULT_RECIPE_OVERVIEW_RECENT_LIMIT = 10;
const DEFAULT_RECIPE_OVERVIEW_FAVORITE_LIMIT = 10;

export type RecipeApiLimitsConfig = {
    recent: number;
    overviewRecent: number;
    overviewFavorite: number;
};

export const RECIPE_API_LIMITS = new InjectionToken<RecipeApiLimitsConfig>('RECIPE_API_LIMITS', {
    providedIn: 'root',
    factory: (): RecipeApiLimitsConfig => ({
        recent: DEFAULT_RECIPE_RECENT_LIMIT,
        overviewRecent: DEFAULT_RECIPE_OVERVIEW_RECENT_LIMIT,
        overviewFavorite: DEFAULT_RECIPE_OVERVIEW_FAVORITE_LIMIT,
    }),
});

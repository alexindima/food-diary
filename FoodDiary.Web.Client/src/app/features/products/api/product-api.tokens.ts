import { InjectionToken } from '@angular/core';

const DEFAULT_PRODUCT_RECENT_LIMIT = 10;
const DEFAULT_PRODUCT_FAVORITE_LIMIT = 10;
const DEFAULT_PRODUCT_SUGGESTIONS_LIMIT = 5;
const DEFAULT_OPEN_FOOD_FACTS_SEARCH_LIMIT = 10;

export type ProductApiLimitsConfig = {
    recent: number;
    favorite: number;
    suggestions: number;
};

export const PRODUCT_API_LIMITS = new InjectionToken<ProductApiLimitsConfig>('PRODUCT_API_LIMITS', {
    providedIn: 'root',
    factory: (): ProductApiLimitsConfig => ({
        recent: DEFAULT_PRODUCT_RECENT_LIMIT,
        favorite: DEFAULT_PRODUCT_FAVORITE_LIMIT,
        suggestions: DEFAULT_PRODUCT_SUGGESTIONS_LIMIT,
    }),
});

export const OPEN_FOOD_FACTS_SEARCH_LIMIT = new InjectionToken<number>('OPEN_FOOD_FACTS_SEARCH_LIMIT', {
    providedIn: 'root',
    factory: (): number => DEFAULT_OPEN_FOOD_FACTS_SEARCH_LIMIT,
});

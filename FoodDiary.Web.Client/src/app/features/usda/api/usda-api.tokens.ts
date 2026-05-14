import { InjectionToken } from '@angular/core';

const DEFAULT_USDA_SEARCH_LIMIT = 20;

export const USDA_SEARCH_LIMIT = new InjectionToken<number>('USDA_SEARCH_LIMIT', {
    providedIn: 'root',
    factory: (): number => DEFAULT_USDA_SEARCH_LIMIT,
});

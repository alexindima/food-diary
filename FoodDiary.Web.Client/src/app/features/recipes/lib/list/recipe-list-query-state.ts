import { InjectionToken, type Signal } from '@angular/core';
import type { Observable } from 'rxjs';

import type { RecipeListQuery } from './recipe-list-query';

export type RecipeListQueryState = {
    readonly initial: RecipeListQuery;
    readonly current: Signal<RecipeListQuery>;
    readonly changes: Observable<RecipeListQuery>;
    writeAsync: (query: RecipeListQuery, options?: { replaceUrl?: boolean }) => Promise<boolean>;
};

export const RECIPE_LIST_QUERY_STATE = new InjectionToken<RecipeListQueryState>('RecipeListQueryState');

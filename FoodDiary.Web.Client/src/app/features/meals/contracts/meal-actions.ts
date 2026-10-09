import { InjectionToken } from '@angular/core';
import type { Observable } from 'rxjs';

import type { FavoriteMeal, Meal } from '../../../shared/models/meal.data';
import type { UtcInstant } from '../../../shared/models/semantics/date-value';
import type { FavoriteMealId, MealId } from '../../../shared/models/semantics/entity-id';

export type MealActions = {
    repeat: (id: MealId, date: UtcInstant, mealType: string) => Observable<Meal>;
    deleteById: (id: MealId) => Observable<void>;
};

export type FavoriteMealActions = {
    getLookupPage: () => Observable<FavoriteMeal[]>;
    add: (mealId: MealId) => Observable<FavoriteMeal>;
    remove: (id: FavoriteMealId) => Observable<void>;
};

export const MEAL_ACTIONS = new InjectionToken<MealActions>('MealActions');
export const FAVORITE_MEAL_ACTIONS = new InjectionToken<FavoriteMealActions>('FavoriteMealActions');

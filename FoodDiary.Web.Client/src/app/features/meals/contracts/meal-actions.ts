import { InjectionToken } from '@angular/core';
import type { Observable } from 'rxjs';

import type { FavoriteMeal, Meal } from '../../../shared/models/meal.data';

export type MealActions = {
    repeat: (id: string, date: string, mealType: string) => Observable<Meal>;
    deleteById: (id: string) => Observable<void>;
};

export type FavoriteMealActions = {
    getLookupPage: () => Observable<FavoriteMeal[]>;
    add: (mealId: string) => Observable<FavoriteMeal>;
    remove: (id: string) => Observable<void>;
};

export const MEAL_ACTIONS = new InjectionToken<MealActions>('MealActions');
export const FAVORITE_MEAL_ACTIONS = new InjectionToken<FavoriteMealActions>('FavoriteMealActions');
